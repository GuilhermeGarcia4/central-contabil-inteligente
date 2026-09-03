using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Knowledge.Domain;
using CentralContabil.Api.Modules.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CentralContabil.Api.Modules.Knowledge.Application;

public interface IEmbeddingService
{
    string ProviderName { get; }
    string ModelName { get; }
    int Dimensions { get; }
    Task<GeneratedEmbedding> GenerateEmbeddingAsync(string text, EmbeddingPurpose purpose, CancellationToken cancellationToken);
}

public enum EmbeddingPurpose { Document, Query }
public sealed record GeneratedEmbedding(float[] Vector, string Provider, string Model, int Dimensions);

public sealed class LocalEmbeddingService(IConfiguration configuration) : IEmbeddingService
{
    public string ProviderName => "local";
    public string ModelName => "local-hashing-v1";
    public int Dimensions => Math.Clamp(configuration.GetValue("AI:EmbeddingDimensions", 384), 64, 2048);
    public Task<GeneratedEmbedding> GenerateEmbeddingAsync(string text, EmbeddingPurpose purpose, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var vector = new float[Dimensions];
        foreach (var token in TextNormalizer.Terms(text))
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            var index = Math.Abs(BitConverter.ToInt32(hash, 0) % Dimensions);
            vector[index] += (hash[4] & 1) == 0 ? 1f : -1f;
        }
        var norm = MathF.Sqrt(vector.Sum(x => x * x));
        if (norm > 0) for (var i = 0; i < vector.Length; i++) vector[i] /= norm;
        return Task.FromResult(new GeneratedEmbedding(vector, ProviderName, ModelName, Dimensions));
    }
}

public static class TextNormalizer
{
    private static readonly Dictionary<string, string> Synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["descanso"] = "ferias", ["folga"] = "ferias", ["trabalho"] = "trabalhista", ["mei"] = "microempreendedor",
        ["decimo"] = "decimoterceiro", ["13"] = "decimoterceiro", ["investimento"] = "juros", ["rendimento"] = "juros"
    };
    public static string Normalize(string value)
    {
        var decomposed = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var plain = new string(decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        return Regex.Replace(plain, @"[^a-z0-9]+", " ").Trim();
    }
    public static IReadOnlyList<string> Terms(string value) => Normalize(value).Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Select(x => Synonyms.GetValueOrDefault(x, x)).Where(x => x.Length > 1).Distinct().ToArray();
}

public sealed class KnowledgeChunker(IConfiguration configuration)
{
    public IReadOnlyList<string> Split(string content)
    {
        var max = Math.Clamp(configuration.GetValue("AI:ChunkCharacters", 1400), 400, 4000);
        var overlap = Math.Clamp(configuration.GetValue("AI:ChunkOverlapCharacters", 160), 0, max / 3);
        var normalized = Regex.Replace(content.Replace("\r", " ").Replace("\n", " "), @"\s+", " ").Trim();
        if (normalized.Length == 0) return [];
        var result = new List<string>();
        for (var start = 0; start < normalized.Length;)
        {
            var length = Math.Min(max, normalized.Length - start);
            var end = start + length;
            if (end < normalized.Length) { var boundary = normalized.LastIndexOfAny(['.', '!', '?', ';'], end - 1, length); if (boundary > start + max / 2) end = boundary + 1; }
            result.Add(normalized[start..end].Trim());
            if (end >= normalized.Length) break;
            start = Math.Max(start + 1, end - overlap);
        }
        return result;
    }
}

public sealed class KnowledgeIndexingService(AppDbContext db, KnowledgeChunker chunker, IEmbeddingService embeddings, IConfiguration configuration, ILogger<KnowledgeIndexingService> logger)
{
    public static string Hash(string content) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));

    public async Task<int> IndexAllEligibleAsync(CancellationToken ct)
    {
        var articles = await db.Articles.Include(x => x.ArticleSources).ThenInclude(x => x.Source)
            .Where(x => x.Status == ArticleStatus.Published && x.ReviewStatus != ReviewStatus.RequiresChanges).ToListAsync(ct);
        var changed = 0;
        foreach (var article in articles) if (await IndexArticleAsync(article, ct)) changed++;
        var calculators = await db.Calculators.Where(x => x.IsActive).ToListAsync(ct);
        foreach (var calculator in calculators) if (await IndexCalculatorAsync(calculator, ct)) changed++;
        return changed;
    }

    public async Task<bool> IndexArticleByIdAsync(Guid articleId, CancellationToken ct)
    {
        var article = await db.Articles.Include(x => x.ArticleSources).ThenInclude(x => x.Source).FirstOrDefaultAsync(x => x.Id == articleId, ct);
        return article is not null && article.Status == ArticleStatus.Published && article.ReviewStatus != ReviewStatus.RequiresChanges && await IndexArticleAsync(article, ct);
    }

    private Task<bool> IndexArticleAsync(Article article, CancellationToken ct)
    {
        var source = article.ArticleSources.Select(x => x.Source).OrderByDescending(x => x.IsOfficial).FirstOrDefault(x => x.IsActive);
        var content = $"{article.Title}\n{article.Summary}\n{article.SimpleContent}\n{article.TechnicalContent}";
        return UpsertAsync(new KnowledgeDocument { Title = article.Title, DocumentType = KnowledgeDocumentType.Article, ArticleId = article.Id,
            SourceId = source?.Id, Content = content, ContentHash = Hash(content), Jurisdiction = article.Jurisdiction, OriginUrl = source?.Url,
            IsOfficial = source?.IsOfficial ?? false, ValidFrom = article.ValidFrom, ValidUntil = article.ValidUntil, LastVerifiedAt = source?.LastVerifiedAt ?? article.LastReviewedAt,
            NeedsReviewAt = article.NeedsReviewAt, Status = article.Status == ArticleStatus.NeedsReview ? KnowledgeStatus.NeedsReview : KnowledgeStatus.Active }, ct);
    }

    private Task<bool> IndexCalculatorAsync(Calculator calculator, CancellationToken ct)
    {
        var content = $"{calculator.Name}. {calculator.Description}. Calculadora determinística da categoria {calculator.Category}.";
        return UpsertAsync(new KnowledgeDocument { Title = calculator.Name, DocumentType = KnowledgeDocumentType.CalculatorExplanation,
            Content = content, ContentHash = Hash(content), Status = KnowledgeStatus.Active }, ct);
    }

    private async Task<bool> UpsertAsync(KnowledgeDocument candidate, CancellationToken ct)
    {
        var current = candidate.ArticleId is not null
            ? await db.KnowledgeDocuments.Include(x => x.Chunks).FirstOrDefaultAsync(x => x.ArticleId == candidate.ArticleId, ct)
            : await db.KnowledgeDocuments.Include(x => x.Chunks).FirstOrDefaultAsync(x => x.DocumentType == candidate.DocumentType && x.Title == candidate.Title, ct);
        var embeddingMatches = current is not null && current.Chunks.Count > 0 && current.Chunks.All(x =>
            x.EmbeddingStatus == EmbeddingStatus.Ready &&
            x.EmbeddingProvider == embeddings.ProviderName &&
            x.EmbeddingModel == embeddings.ModelName &&
            x.EmbeddingDimensions == embeddings.Dimensions);
        if (current is not null && current.ContentHash == candidate.ContentHash && embeddingMatches) return false;
        List<KnowledgeChunk> chunks;
        if (current is null)
        {
            current = candidate;
            db.KnowledgeDocuments.Add(current);
            chunks = CreateChunks(current);
            current.Chunks = chunks;
        }
        else if (current.ContentHash == candidate.ContentHash)
        {
            chunks = current.Chunks.ToList();
        }
        else
        {
            db.KnowledgeChunks.RemoveRange(current.Chunks);
            current.Chunks.Clear();
            current.Title = candidate.Title; current.Content = candidate.Content; current.ContentHash = candidate.ContentHash;
            current.SourceId = candidate.SourceId; current.OriginUrl = candidate.OriginUrl; current.IsOfficial = candidate.IsOfficial; current.ValidFrom = candidate.ValidFrom;
            current.ValidUntil = candidate.ValidUntil; current.LastVerifiedAt = candidate.LastVerifiedAt; current.NeedsReviewAt = candidate.NeedsReviewAt; current.Status = candidate.Status; current.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            chunks = CreateChunks(current);
            current.Chunks = chunks;
        }
        current.LastIndexedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        foreach (var chunk in chunks)
        {
            try
            {
                var embedding = await embeddings.GenerateEmbeddingAsync(chunk.Content, EmbeddingPurpose.Document, ct);
                await SaveEmbeddingAsync(chunk.Id, embedding, ct);
                chunk.EmbeddingStatus = EmbeddingStatus.Ready;
                chunk.EmbeddingProvider = embedding.Provider;
                chunk.EmbeddingModel = embedding.Model;
                chunk.EmbeddingDimensions = embedding.Dimensions;
            }
            catch (Exception ex) { chunk.EmbeddingStatus = EmbeddingStatus.Failed; logger.LogWarning(ex, "Falha ao gerar embedding do chunk {ChunkId}", chunk.Id); }
        }
        await db.SaveChangesAsync(ct); return true;
    }

    private List<KnowledgeChunk> CreateChunks(KnowledgeDocument document) => chunker.Split(document.Content)
        .Select((content, index) => new KnowledgeChunk { KnowledgeDocument = document, ChunkIndex = index, Content = content, TokenEstimate = Math.Max(1, content.Length / 4) })
        .ToList();

    private async Task SaveEmbeddingAsync(Guid chunkId, GeneratedEmbedding embedding, CancellationToken ct)
    {
        var dimensions = Math.Clamp(configuration.GetValue("AI:EmbeddingDimensions", 384), 64, 2048);
        if (embedding.Vector.Length != dimensions || embedding.Dimensions != dimensions) throw new InvalidOperationException("Dimensão do embedding diferente de AI:EmbeddingDimensions.");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE central.\"KnowledgeChunks\" SET \"Embedding\" = CAST({VectorLiteral(embedding.Vector)} AS vector), \"EmbeddingStatus\" = 1 WHERE \"Id\" = {chunkId}", ct);
    }
    internal static string VectorLiteral(IEnumerable<float> vector) => "[" + string.Join(',', vector.Select(x => x.ToString("R", CultureInfo.InvariantCulture))) + "]";
}

public sealed class HybridKnowledgeSearch(AppDbContext db, IEmbeddingService embeddings, IConfiguration configuration, ILogger<HybridKnowledgeSearch> logger)
{
    public async Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(string query, DateOnly referenceDate, bool critical, CancellationToken ct)
    {
        var terms = TextNormalizer.Terms(query); if (terms.Count == 0) return [];
        var now = DateTimeOffset.UtcNow; var pattern = $"%{TextNormalizer.Normalize(query).Replace(' ', '%')}%";
        var documents = await db.KnowledgeDocuments.AsNoTracking().Include(x => x.Chunks)
            .Where(x => x.Status == KnowledgeStatus.Active && (x.ValidFrom == null || x.ValidFrom <= referenceDate) && (x.ValidUntil == null || x.ValidUntil >= referenceDate))
            .Where(x => !critical || x.NeedsReviewAt == null || x.NeedsReviewAt > now)
            .Where(x => EF.Functions.ILike(x.Title, pattern) || EF.Functions.ILike(x.Content, pattern))
            .Take(40).ToListAsync(ct);
        var vectorScores = await VectorScoresAsync(query, referenceDate, critical, ct);
        var knownIds = documents.Select(x => x.Id).ToHashSet();
        var semanticIds = vectorScores.Keys.Where(x => !knownIds.Contains(x)).ToArray();
        if (semanticIds.Length > 0)
            documents.AddRange(await db.KnowledgeDocuments.AsNoTracking().Include(x => x.Chunks).Where(x => semanticIds.Contains(x.Id)).ToListAsync(ct));
        var articleIds = documents.Where(x => x.ArticleId != null).Select(x => x.ArticleId!.Value).ToArray();
        var slugs = await db.Articles.AsNoTracking().Where(x => articleIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Slug, ct);
        return documents.Select(document => {
            var normalized = TextNormalizer.Normalize(document.Content); var lexical = terms.Count(term => normalized.Contains(term, StringComparison.Ordinal)) / (double)terms.Count;
            var vector = vectorScores.GetValueOrDefault(document.Id);
            var freshnessPenalty = document.NeedsReviewAt <= now ? .2 : 0; var officialBoost = document.IsOfficial ? .12 : 0;
            var score = Math.Clamp(lexical * .55 + vector * .33 + officialBoost - freshnessPenalty, 0, 1);
            return new KnowledgeSearchResult(document.Id, document.Title, document.Content[..Math.Min(220, document.Content.Length)], document.DocumentType.ToString(),
                document.ArticleId is Guid id ? slugs.GetValueOrDefault(id) : null, score, document.IsOfficial ? "Fonte oficial" : null, document.OriginUrl,
                document.IsOfficial, document.ValidFrom, document.ValidUntil, document.LastVerifiedAt, document.NeedsReviewAt <= now, document.Chunks.FirstOrDefault()?.Content ?? document.Content);
        }).OrderByDescending(x => x.Score).Take(Math.Clamp(configuration.GetValue("AI:MaxRetrievedDocuments", 6), 1, 12)).ToArray();
    }

    private async Task<Dictionary<Guid, double>> VectorScoresAsync(string query, DateOnly referenceDate, bool critical, CancellationToken ct)
    {
        var result = new Dictionary<Guid, double>();
        try {
            var generated = await embeddings.GenerateEmbeddingAsync(query, EmbeddingPurpose.Query, ct);
            var vector = KnowledgeIndexingService.VectorLiteral(generated.Vector);
            var connection = db.Database.GetDbConnection(); if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT d.\"Id\", MAX(GREATEST(1 - (c.\"Embedding\" <=> CAST(@v AS vector)), 0) * 0.75 + ts_rank_cd(to_tsvector('portuguese', coalesce(d.\"Title\", '') || ' ' || coalesce(d.\"Content\", '')), plainto_tsquery('portuguese', @q)) * 0.25) AS score FROM central.\"KnowledgeChunks\" c JOIN central.\"KnowledgeDocuments\" d ON d.\"Id\"=c.\"KnowledgeDocumentId\" WHERE c.\"Embedding\" IS NOT NULL AND c.\"EmbeddingProvider\"=@provider AND c.\"EmbeddingModel\"=@model AND c.\"EmbeddingDimensions\"=@dimensions AND d.\"Status\"=1 AND (d.\"ValidFrom\" IS NULL OR d.\"ValidFrom\"<=@date) AND (d.\"ValidUntil\" IS NULL OR d.\"ValidUntil\">=@date) AND (@critical=false OR d.\"NeedsReviewAt\" IS NULL OR d.\"NeedsReviewAt\">now()) GROUP BY d.\"Id\" ORDER BY score DESC LIMIT 30";
            var p = command.CreateParameter(); p.ParameterName = "v"; p.Value = vector; command.Parameters.Add(p);
            p = command.CreateParameter(); p.ParameterName = "date"; p.Value = referenceDate; command.Parameters.Add(p);
            p = command.CreateParameter(); p.ParameterName = "critical"; p.Value = critical; command.Parameters.Add(p);
            p = command.CreateParameter(); p.ParameterName = "q"; p.Value = query; command.Parameters.Add(p);
            p = command.CreateParameter(); p.ParameterName = "provider"; p.Value = generated.Provider; command.Parameters.Add(p);
            p = command.CreateParameter(); p.ParameterName = "model"; p.Value = generated.Model; command.Parameters.Add(p);
            p = command.CreateParameter(); p.ParameterName = "dimensions"; p.Value = generated.Dimensions; command.Parameters.Add(p);
            await using var reader = await command.ExecuteReaderAsync(ct); while (await reader.ReadAsync(ct)) result[reader.GetGuid(0)] = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
        } catch (Exception ex) { logger.LogWarning(ex, "Busca vetorial indisponível; mantendo fallback lexical"); }
        return result;
    }
}

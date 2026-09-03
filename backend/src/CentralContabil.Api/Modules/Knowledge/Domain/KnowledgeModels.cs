using System.ComponentModel.DataAnnotations.Schema;

namespace CentralContabil.Api.Modules.Knowledge.Domain;

public enum KnowledgeDocumentType { Article, OfficialSource, LegalReference, InternalKnowledge, CalculatorExplanation }
public enum KnowledgeStatus { Draft, Active, NeedsReview, Archived }
public enum EmbeddingStatus { Pending, Ready, Failed }

public sealed class KnowledgeDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Title { get; set; }
    public KnowledgeDocumentType DocumentType { get; set; }
    public Guid? ArticleId { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? LegalReferenceId { get; set; }
    public required string Content { get; set; }
    public required string ContentHash { get; set; }
    public KnowledgeStatus Status { get; set; } = KnowledgeStatus.Active;
    public string Jurisdiction { get; set; } = "BR";
    public string? OriginUrl { get; set; }
    public bool IsOfficial { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public DateTimeOffset? LastVerifiedAt { get; set; }
    public DateTimeOffset? NeedsReviewAt { get; set; }
    public DateTimeOffset? LastIndexedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<KnowledgeChunk> Chunks { get; set; } = [];
}

public sealed class KnowledgeChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid KnowledgeDocumentId { get; set; }
    public KnowledgeDocument KnowledgeDocument { get; set; } = null!;
    public int ChunkIndex { get; set; }
    public required string Content { get; set; }
    public int TokenEstimate { get; set; }
    public EmbeddingStatus EmbeddingStatus { get; set; } = EmbeddingStatus.Pending;
    public string? EmbeddingProvider { get; set; }
    public string? EmbeddingModel { get; set; }
    public int? EmbeddingDimensions { get; set; }
    [NotMapped] public float[]? Embedding { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed record KnowledgeSearchResult(Guid DocumentId, string Title, string Summary, string Type, string? Slug,
    double Score, string? Source, string? Url, bool IsOfficial, DateOnly? ValidFrom, DateOnly? ValidUntil,
    DateTimeOffset? LastVerifiedAt, bool NeedsReview, string Context);

using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.AI.Application;
using CentralContabil.Api.Modules.AI.Domain;
using CentralContabil.Api.Modules.Knowledge.Application;
using CentralContabil.Api.Modules.Knowledge.Domain;
using CentralContabil.Api.Modules.Knowledge.Infrastructure;
using CentralContabil.Api.Modules.AI.Infrastructure;
using CentralContabil.Api.Modules.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.Api.Endpoints;

public static class V3Endpoints
{
    public static IEndpointRouteBuilder MapV3Api(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/v1");
        api.MapGet("/search/intelligent", async (string q, HybridKnowledgeSearch search, CancellationToken ct) =>
        {
            if (q.Trim().Length is < 2 or > 200) throw new FormatException("A busca deve conter entre 2 e 200 caracteres.");
            return Results.Ok(await search.SearchAsync(q.Trim(), DateOnly.FromDateTime(DateTime.UtcNow), false, ct));
        }).AllowAnonymous().RequireRateLimiting("public").WithTags("V3 Search").WithName("HybridSearch");

        api.MapGet("/search/suggestions", async (string q, AppDbContext db, CancellationToken ct) =>
        {
            q = q.Trim(); if (q.Length < 2) return Results.Ok(Array.Empty<object>()); var pattern = $"%{q}%";
            var articles = await db.Articles.AsNoTracking().Where(x => x.Status == ArticleStatus.Published && EF.Functions.ILike(x.Title, pattern)).Take(5).Select(x => new { title = x.Title, x.Slug, type = "article" }).ToListAsync(ct);
            var calculators = await db.Calculators.AsNoTracking().Where(x => x.IsActive && EF.Functions.ILike(x.Name, pattern)).Take(4).Select(x => new { title = x.Name, x.Slug, type = "calculator" }).ToListAsync(ct);
            var categories = await db.Categories.AsNoTracking().Where(x => x.IsActive && EF.Functions.ILike(x.Name, pattern)).Take(4).Select(x => new { title = x.Name, x.Slug, type = "category" }).ToListAsync(ct);
            return Results.Ok(articles.Cast<object>().Concat(calculators).Concat(categories).Take(8));
        }).AllowAnonymous().RequireRateLimiting("public").WithTags("V3 Search");

        api.MapPost("/assistant/ask", async (AssistantAskRequest request, HttpContext http, AssistantService assistant, CancellationToken ct) =>
            Results.Ok(await assistant.AskAsync(request.Question, http.User, ct, request.ConversationId)))
            .AllowAnonymous().RequireRateLimiting("ai").WithTags("V3 Assistant").WithName("AskAssistant").Produces<AssistantAnswer>().ProducesProblem(400).ProducesProblem(429);
        api.MapGet("/assistant/status", (IConfiguration configuration) =>
        {
            var settings = AiProviderSettings.From(configuration);
            return Results.Ok(new
            {
                mode = settings.ChatConfigured ? "external" : "local-grounded",
                provider = settings.Provider.ToString().ToLowerInvariant(),
                model = settings.ChatConfigured ? settings.SelectedChatModel : "grounded-template-v1",
                externalChatConfigured = settings.ChatConfigured,
                externalEmbeddingConfigured = settings.EmbeddingConfigured,
                message = settings.ChatConfigured
                    ? "Modelo externo configurado. As respostas continuam limitadas às fontes e ferramentas da plataforma."
                    : "Modo local ativo. Configure provider, chave e modelo no backend para habilitar o modelo externo."
            });
        }).AllowAnonymous().WithTags("V3 Assistant");

        var admin = api.MapGroup("/admin").RequireAuthorization("AdminOnly");
        admin.MapGet("/knowledge", async (AppDbContext db, CancellationToken ct) => Results.Ok(new {
            documents = await db.KnowledgeDocuments.AsNoTracking().OrderByDescending(x => x.LastIndexedAt).Select(x => new { x.Id, x.Title, x.DocumentType, x.Status, x.IsOfficial, x.ValidFrom, x.ValidUntil, x.LastVerifiedAt, x.NeedsReviewAt, x.LastIndexedAt, chunks = x.Chunks.Count, readyChunks = x.Chunks.Count(c => c.EmbeddingStatus == EmbeddingStatus.Ready), x.OriginUrl }).ToListAsync(ct),
            totals = new { documents = await db.KnowledgeDocuments.CountAsync(ct), chunks = await db.KnowledgeChunks.CountAsync(ct), ready = await db.KnowledgeChunks.CountAsync(x => x.EmbeddingStatus == EmbeddingStatus.Ready, ct) }
        })).WithTags("V3 Admin Knowledge");
        admin.MapPost("/knowledge/reindex", (IKnowledgeIndexQueue queue) => queue.Enqueue() ? Results.Accepted() : Results.StatusCode(503)).WithTags("V3 Admin Knowledge");
        admin.MapPost("/knowledge/{id:guid}/reindex", async (Guid id, AppDbContext db, IKnowledgeIndexQueue queue, CancellationToken ct) => {
            var articleId = await db.KnowledgeDocuments.Where(x => x.Id == id).Select(x => x.ArticleId).FirstOrDefaultAsync(ct);
            return articleId is Guid value && queue.Enqueue(value) ? Results.Accepted() : Results.NotFound();
        }).WithTags("V3 Admin Knowledge");
        admin.MapPatch("/knowledge/{id:guid}/deactivate", async (Guid id, AppDbContext db, CancellationToken ct) => {
            var document = await db.KnowledgeDocuments.FindAsync([id], ct); if (document is null) return Results.NotFound(); document.Status = KnowledgeStatus.Archived; document.UpdatedAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return Results.NoContent();
        }).WithTags("V3 Admin Knowledge");
        admin.MapGet("/ai", async (AppDbContext db, CancellationToken ct) => {
            var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero); var requests = db.AIRequests.AsNoTracking().Where(x => x.CreatedAt >= today);
            return Results.Ok(new { questionsToday = await requests.CountAsync(ct), usersToday = await requests.Where(x => x.UserId != null).Select(x => x.UserId).Distinct().CountAsync(ct), averageDurationMs = await requests.Select(x => (double?)x.DurationMs).AverageAsync(ct) ?? 0, errors = await requests.CountAsync(x => x.Status == AIRequestStatus.Failed, ct), unanswered = await db.UnansweredQuestions.CountAsync(x => x.Status == UnansweredQuestionStatus.Open, ct), topIntents = await requests.GroupBy(x => x.Intent).Select(x => new { intent = x.Key, count = x.Count() }).OrderByDescending(x => x.count).Take(8).ToListAsync(ct) });
        }).WithTags("V3 Admin AI");
        admin.MapGet("/ai/unanswered", async (AppDbContext db, CancellationToken ct) => Results.Ok(await db.UnansweredQuestions.AsNoTracking().OrderByDescending(x => x.Count).Take(100).ToListAsync(ct))).WithTags("V3 Admin AI");
        return app;
    }
}

public sealed record AssistantAskRequest(string Question, Guid? ConversationId = null);

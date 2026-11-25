using CentralContabil.Api.Modules.Journal.Application;

namespace CentralContabil.Api.Endpoints;

public static class JournalEndpoints
{
    public static IEndpointRouteBuilder MapJournalApi(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/v1/journal")
            .RequireRateLimiting("public")
            .WithTags("Jornal Contábil")
            .MapGet("", async (string? q, string? category, int? page, int? pageSize, string? order, JournalNewsService service, CancellationToken ct) =>
                Results.Ok(await service.SearchAsync(q, category, page ?? 1, pageSize ?? 12, order, ct)))
            .WithName("SearchJournalNews");
        return app;
    }
}

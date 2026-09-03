using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Integrations.Application;
using CentralContabil.Api.Modules.Integrations.Domain;
using CentralContabil.Api.Modules.Search.Application;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.Api.Endpoints;

public static class V2Endpoints
{
    public static IEndpointRouteBuilder MapV2Api(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/v2").RequireRateLimiting("public");

        api.MapGet("/companies/{cnpj}", async (string cnpj, CompanyLookupService service, CancellationToken ct) =>
            Results.Ok(await service.GetAsync(cnpj, ct)))
            .WithTags("V2 Companies").WithName("GetCompanyByCnpj").Produces<CompanyData>().ProducesProblem(400).ProducesProblem(404).ProducesProblem(502).ProducesProblem(503);

        api.MapGet("/ncm/{code}", async (string code, NcmLookupService service, CancellationToken ct) =>
            Results.Ok(await service.GetAsync(code, ct)))
            .WithTags("V2 NCM").WithName("GetNcmByCode").Produces<NcmData>().ProducesProblem(400).ProducesProblem(404).ProducesProblem(503);

        api.MapGet("/ncm", async (string query, NcmLookupService service, CancellationToken ct) =>
            Results.Ok(await service.SearchAsync(query, ct)))
            .WithTags("V2 NCM").WithName("SearchNcm");

        api.MapGet("/search", async (string q, UnifiedSearchService service, CancellationToken ct) =>
            Results.Ok(await service.SearchAsync(q, ct)))
            .WithTags("V2 Search").WithName("UnifiedSearch").Produces<UnifiedSearchResponse>().ProducesProblem(400);

        api.MapGet("/legislation/search", async (string q, IEnumerable<ILegislationProvider> providers, CancellationToken ct) =>
        {
            if (q.Trim().Length < 2) throw new FormatException("Informe ao menos dois caracteres.");
            var results = new List<LegalSearchItem>();
            foreach (var provider in providers.Where(x => x.Enabled)) results.AddRange(await provider.SearchAsync(q.Trim(), ct));
            return Results.Ok(results);
        }).WithTags("V2 Legislation").WithName("SearchLegislation");

        api.MapGet("/updates", async (int? days, AppDbContext db, CancellationToken ct) =>
        {
            var since = DateTimeOffset.UtcNow.AddDays(-Math.Clamp(days ?? 30, 1, 365));
            var articles = await db.Articles.AsNoTracking().Where(x => x.UpdatedAt >= since)
                .OrderByDescending(x => x.UpdatedAt).Take(50)
                .Select(x => new { x.Title, x.Slug, x.Summary, x.Status, x.UpdatedAt, kind = "content" }).ToListAsync(ct);
            var snapshots = await db.ExternalEntitySnapshots.AsNoTracking().Where(x => x.RetrievedAt >= since)
                .GroupBy(x => new { x.Provider, x.EntityType }).Select(x => new { x.Key.Provider, x.Key.EntityType, total = x.Count(), lastRetrievedAt = x.Max(y => y.RetrievedAt), kind = "integration" })
                .ToListAsync(ct);
            return Results.Ok(new { since, articles, integrations = snapshots });
        }).WithTags("V2 Updates").WithName("GetUpdates");

        api.MapGet("/admin/integrations", async (IEnumerable<ICompanyDataProvider> companyProviders, IEnumerable<INcmDataProvider> ncmProviders,
            IEnumerable<ILegislationProvider> legalProviders, IIntegrationHealthStore health, AppDbContext db, CancellationToken ct) =>
        {
            var providers = companyProviders.Select(x => (x.Name, x.Enabled))
                .Concat(ncmProviders.Select(x => (x.Name, x.Enabled))).Concat(legalProviders.Select(x => (x.Name, x.Enabled)));
            var now = DateTimeOffset.UtcNow;
            var cache = await db.ExternalEntitySnapshots.AsNoTracking().GroupBy(x => new { x.Provider, x.EntityType })
                .Select(x => new { x.Key.Provider, x.Key.EntityType, entries = x.Count(), validEntries = x.Count(y => y.ExpiresAt > now), lastRetrievedAt = x.Max(y => y.RetrievedAt) }).ToListAsync(ct);
            return Results.Ok(new { providers = health.Snapshot(providers), cache });
        }).RequireAuthorization("AdminOnly").WithTags("V2 Admin").WithName("GetIntegrationHealth");

        return app;
    }
}

using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Integrations.Application;
using CentralContabil.Api.Modules.Integrations.Domain;
using CentralContabil.Api.Modules.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.Api.Modules.Search.Application;

public sealed class UnifiedSearchService(AppDbContext db, CompanyLookupService companies, NcmLookupService ncms,
    IEnumerable<ILegislationProvider> legislationProviders)
{
    public async Task<UnifiedSearchResponse> SearchAsync(string rawQuery, CancellationToken cancellationToken)
    {
        var query = rawQuery.Trim();
        if (query.Length < 2 || query.Length > 120) throw new FormatException("A busca deve conter entre 2 e 120 caracteres.");
        var intent = SearchIntentDetector.Detect(query);
        var pattern = $"%{query}%";
        var internalResults = new
        {
            articles = await db.Articles.AsNoTracking().Where(x => x.Status == ArticleStatus.Published &&
                (EF.Functions.ILike(x.Title, pattern) || EF.Functions.ILike(x.Summary, pattern)))
                .OrderBy(x => x.Title).Take(20).Select(x => new { x.Title, x.Slug, x.Summary, kind = "article" }).ToListAsync(cancellationToken),
            calculators = await db.Calculators.AsNoTracking().Where(x => x.IsActive &&
                (EF.Functions.ILike(x.Name, pattern) || EF.Functions.ILike(x.Description, pattern)))
                .OrderBy(x => x.Name).Take(10).Select(x => new { title = x.Name, x.Slug, summary = x.Description, kind = "calculator" }).ToListAsync(cancellationToken),
            categories = await db.Categories.AsNoTracking().Where(x => x.IsActive &&
                (EF.Functions.ILike(x.Name, pattern) || EF.Functions.ILike(x.Description, pattern)))
                .OrderBy(x => x.Name).Take(10).Select(x => new { title = x.Name, x.Slug, summary = x.Description, kind = "category" }).ToListAsync(cancellationToken),
            legalReferences = await db.LegalReferences.AsNoTracking().Where(x =>
                EF.Functions.ILike(x.Title, pattern) || EF.Functions.ILike(x.ReferenceNumber, pattern) || EF.Functions.ILike(x.Description, pattern))
                .OrderBy(x => x.Title).Take(10).Select(x => new { x.Title, key = x.ReferenceNumber, summary = x.Description, x.SourceUrl, kind = "legal-norm" }).ToListAsync(cancellationToken)
        };

        var companyResults = new List<CompanyData>();
        var ncmResults = new List<NcmData>();
        var legalResults = new List<LegalSearchItem>();
        var warnings = new List<string>();

        if (intent == SearchIntent.Cnpj)
            await TryExternal(async () => companyResults.Add(await companies.GetAsync(query, cancellationToken)), "empresas", warnings);
        else if (intent == SearchIntent.Ncm)
            await TryExternal(async () =>
            {
                var normalized = CentralContabil.Api.Modules.Integrations.Infrastructure.BrasilApiNcmProvider.NormalizeNcm(query);
                if (normalized.Length == 8) ncmResults.Add(await ncms.GetAsync(normalized, cancellationToken));
                else ncmResults.AddRange(await ncms.SearchAsync(query.Replace("NCM", "", StringComparison.OrdinalIgnoreCase).Trim(), cancellationToken));
            }, "NCM", warnings);
        else if (intent == SearchIntent.LegalReference)
            foreach (var provider in legislationProviders.Where(x => x.Enabled))
                await TryExternal(async () => legalResults.AddRange(await provider.SearchAsync(query, cancellationToken)), provider.Name, warnings);

        return new UnifiedSearchResponse(query, intent, internalResults, companyResults, ncmResults, legalResults, warnings);
    }

    private static async Task TryExternal(Func<Task> action, string label, List<string> warnings)
    {
        try { await action(); }
        catch (ExternalNotFoundException) { warnings.Add($"Nenhum resultado externo encontrado em {label}."); }
        catch (ExternalTransientException) { warnings.Add($"A integração {label} está temporariamente indisponível."); }
        catch (ExternalProviderException) { warnings.Add($"A integração {label} não aceitou esta consulta."); }
    }
}

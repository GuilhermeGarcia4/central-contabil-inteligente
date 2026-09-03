using CentralContabil.Api.Modules.Integrations.Domain;

namespace CentralContabil.Api.Modules.Integrations.Application;

public sealed class CompanyLookupService(IEnumerable<ICompanyDataProvider> providers, IExternalSnapshotCache cache, IConfiguration configuration)
{
    private readonly ICompanyDataProvider _primary = providers.Single(x => x.Name == "BrasilAPI");
    private readonly ICompanyDataProvider? _fallback = providers.SingleOrDefault(x => x.Name == "CNPJ.ws");
    private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(configuration.GetValue("Integrations:Cache:CompanyHours", 24));

    public async Task<CompanyData> GetAsync(string rawCnpj, CancellationToken cancellationToken)
    {
        if (!CnpjIdentifier.TryParse(rawCnpj, out var cnpj, out var error)) throw new FormatException(error);
        var cached = await cache.GetAsync<CompanyData>(_primary.Name, "company", cnpj.Value, cancellationToken);
        if (cached is not null) return cached with { FromCache = true };

        try
        {
            var company = await _primary.GetByCnpjAsync(cnpj, cancellationToken);
            await cache.SetAsync(_primary.Name, "company", cnpj.Value, company, _cacheDuration, cancellationToken);
            return company;
        }
        catch (ExternalTransientException) when (_fallback?.Enabled == true)
        {
            var fallbackCached = await cache.GetAsync<CompanyData>(_fallback.Name, "company", cnpj.Value, cancellationToken);
            if (fallbackCached is not null) return fallbackCached with { FromCache = true };
            var company = await _fallback.GetByCnpjAsync(cnpj, cancellationToken);
            await cache.SetAsync(_fallback.Name, "company", cnpj.Value, company, _cacheDuration, cancellationToken);
            return company;
        }
    }
}

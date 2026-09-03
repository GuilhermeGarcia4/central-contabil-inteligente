using CentralContabil.Api.Modules.Integrations.Domain;

namespace CentralContabil.Api.Modules.Integrations.Application;

public sealed class NcmLookupService(INcmDataProvider provider, IExternalSnapshotCache cache, IConfiguration configuration)
{
    private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(configuration.GetValue("Integrations:Cache:NcmHours", 168));

    public async Task<NcmData> GetAsync(string rawCode, CancellationToken cancellationToken)
    {
        var code = Infrastructure.BrasilApiNcmProvider.NormalizeNcm(rawCode);
        if (code.Length != 8) throw new FormatException("O código NCM deve conter oito dígitos.");
        var cached = await cache.GetAsync<NcmData>(provider.Name, "ncm", code, cancellationToken);
        if (cached is not null) return cached with { FromCache = true };
        var result = await provider.GetByCodeAsync(code, cancellationToken);
        await cache.SetAsync(provider.Name, "ncm", code, result, _cacheDuration, cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<NcmData>> SearchAsync(string rawQuery, CancellationToken cancellationToken)
    {
        var query = rawQuery.Trim();
        if (query.Length < 2) throw new FormatException("Informe ao menos dois caracteres para consultar NCM.");
        var key = query.ToUpperInvariant();
        var cached = await cache.GetAsync<List<NcmData>>(provider.Name, "ncm-search", key, cancellationToken);
        if (cached is not null) return cached.Select(x => x with { FromCache = true }).ToArray();
        var result = await provider.SearchAsync(query, cancellationToken);
        await cache.SetAsync(provider.Name, "ncm-search", key, result, TimeSpan.FromHours(12), cancellationToken);
        return result;
    }
}

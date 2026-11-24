using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CentralContabil.Api.Modules.Integrations.Infrastructure;

public sealed record CreditRateReference(decimal AnnualRatePercent, DateOnly ReferenceMonth, DateTimeOffset RetrievedAt, int SeriesCode, string SourceUrl, string Description);
public sealed record InstitutionCreditRate(string Institution, string Modality, string ReferenceMonth, decimal MonthlyRatePercent, decimal AnnualRatePercent);
public sealed record CreditRateCatalog(IReadOnlyList<string> Modalities, string ReferenceMonth, DateTimeOffset RetrievedAt, string SourceUrl, IReadOnlyList<InstitutionCreditRate> Rates);
public sealed record EconomicSeries(int Code, string Name, string Unit, string Periodicity, string SourceUrl);
public sealed record EconomicObservation(string Date, decimal Value);

public sealed class BacenCreditRateProvider(HttpClient httpClient)
{
    private static readonly Uri SourceUri = new("https://api.bcb.gov.br/dados/serie/bcdata.sgs.20748/dados/ultimos/1?formato=json");
    private static readonly string ReferenceUrl = "https://dadosabertos.bcb.gov.br/dataset/20748-taxa-media-de-juros-das-operacoes-de-credito-com-recursos-livres---pessoas-fisicas---credito-";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static CreditRateReference? cached;
    private static DateTimeOffset cacheUntil;
    private static CreditRateCatalog? catalogCache;
    private static DateTimeOffset catalogCacheUntil;

    private static readonly Uri MonthlyRatesUri = new("https://olinda.bcb.gov.br/olinda/servico/taxaJuros/versao/v2/odata/TaxasJurosMensalPorMes");
    private const string OlindaSource = "https://dadosabertos.bcb.gov.br/dataset/taxas-de-juros-de-operacoes-de-credito";
    private static readonly EconomicSeries[] Series = [
        new(433, "IPCA", "% ao mês", "Mensal", "https://api.bcb.gov.br/dados/serie/bcdata.sgs.433/dados"),
        new(188, "INPC", "% ao mês", "Mensal", "https://api.bcb.gov.br/dados/serie/bcdata.sgs.188/dados"),
        new(189, "IGP-M", "% ao mês", "Mensal", "https://api.bcb.gov.br/dados/serie/bcdata.sgs.189/dados"),
        new(7811, "TR (período mensal com início no primeiro dia)", "% ao mês", "Mensal", "https://api.bcb.gov.br/dados/serie/bcdata.sgs.7811/dados"),
        new(196, "Poupança (depósitos desde 04/05/2012, período iniciado no dia 1º)", "% ao mês", "Mensal", "https://api.bcb.gov.br/dados/serie/bcdata.sgs.196/dados"),
        new(4390, "Selic acumulada no mês", "% ao mês", "Mensal", "https://api.bcb.gov.br/dados/serie/bcdata.sgs.4390/dados")
    ];

    public IReadOnlyList<EconomicSeries> GetSeries() => Series;

    public async Task<IReadOnlyList<EconomicObservation>> GetSeriesAsync(int code, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var series = Series.SingleOrDefault(x => x.Code == code) ?? throw new KeyNotFoundException("Série econômica não disponível.");
        if (from > to || to > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) || from < to.AddYears(-30)) throw new ArgumentOutOfRangeException(nameof(from), "Informe um intervalo válido de até 30 anos.");
        var url = $"{series.SourceUrl}?formato=json&dataInicial={Uri.EscapeDataString(from.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))}&dataFinal={Uri.EscapeDataString(to.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))}";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var rows = await response.Content.ReadFromJsonAsync<List<SgsObservation>>(cancellationToken: cancellationToken) ?? [];
        return rows.Select(x => DateOnly.TryParseExact(x.Date, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) && decimal.TryParse(x.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? new EconomicObservation(date.ToString("yyyy-MM-dd"), value) : null).Where(x => x is not null).Select(x => x!).ToArray();
    }

    public async Task<CreditRateCatalog> GetInstitutionRatesAsync(string? modality, CancellationToken cancellationToken)
    {
        if (catalogCache is not null && catalogCacheUntil > DateTimeOffset.UtcNow)
        {
            var cachedRates = modality is null ? catalogCache.Rates : catalogCache.Rates.Where(x => x.Modality == modality).ToArray();
            if (modality is null || cachedRates.Count > 0) return catalogCache with { Rates = cachedRates };
        }
        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (catalogCache is null || catalogCacheUntil <= DateTimeOffset.UtcNow)
            {
                using var latestResponse = await httpClient.GetAsync($"{MonthlyRatesUri}?$top=1&$format=json", cancellationToken);
                latestResponse.EnsureSuccessStatusCode();
                using var latestDoc = JsonDocument.Parse(await latestResponse.Content.ReadAsStringAsync(cancellationToken));
                var latest = latestDoc.RootElement.GetProperty("value").EnumerateArray().FirstOrDefault();
                var month = latest.ValueKind == JsonValueKind.Undefined ? null : latest.GetProperty("anoMes").GetString();
                if (string.IsNullOrWhiteSpace(month)) throw new InvalidDataException("O Banco Central não informou o mês de referência.");
                var filter = Uri.EscapeDataString($"anoMes eq '{month}'");
                using var response = await httpClient.GetAsync($"{MonthlyRatesUri}?$top=10000&$format=json&$filter={filter}", cancellationToken);
                response.EnsureSuccessStatusCode();
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                var rates = doc.RootElement.GetProperty("value").EnumerateArray().Select(x => new InstitutionCreditRate(
                    x.GetProperty("InstituicaoFinanceira").GetString() ?? "",
                    x.GetProperty("Modalidade").GetString() ?? "",
                    x.GetProperty("anoMes").GetString() ?? month,
                    x.GetProperty("TaxaJurosAoMes").GetDecimal(),
                    x.GetProperty("TaxaJurosAoAno").GetDecimal())).Where(x => x.Institution.Length > 0 && x.Modality.Length > 0).ToArray();
                catalogCache = new CreditRateCatalog(rates.Select(x => x.Modality).Distinct().Order(StringComparer.CurrentCulture).ToArray(), month, DateTimeOffset.UtcNow, OlindaSource, rates);
                catalogCacheUntil = DateTimeOffset.UtcNow.AddHours(6);
            }
            var selected = modality is null ? catalogCache.Rates : catalogCache.Rates.Where(x => x.Modality == modality).ToArray();
            return catalogCache with { Rates = selected };
        }
        finally { Gate.Release(); }
    }

    public async Task<CreditRateReference> GetPersonalCreditReferenceAsync(CancellationToken cancellationToken)
    {
        if (cached is not null && cacheUntil > DateTimeOffset.UtcNow) return cached;

        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (cached is not null && cacheUntil > DateTimeOffset.UtcNow) return cached;
            using var response = await httpClient.GetAsync(SourceUri, cancellationToken);
            response.EnsureSuccessStatusCode();
            var rows = await response.Content.ReadFromJsonAsync<List<SgsObservation>>(cancellationToken: cancellationToken);
            var row = rows?.LastOrDefault() ?? throw new InvalidDataException("O Banco Central não retornou observações para a série solicitada.");
            if (!decimal.TryParse(row.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var annualRate) || annualRate < 0)
                throw new InvalidDataException("O Banco Central retornou uma taxa inválida.");
            if (!DateOnly.TryParseExact(row.Date, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var referenceDate))
                throw new InvalidDataException("O Banco Central retornou uma data de referência inválida.");

            cached = new CreditRateReference(annualRate, new DateOnly(referenceDate.Year, referenceDate.Month, 1), DateTimeOffset.UtcNow,
                20748, ReferenceUrl, "Taxa média anual das novas operações de crédito pessoal livre para pessoas físicas.");
            cacheUntil = DateTimeOffset.UtcNow.Add(CacheDuration);
            return cached;
        }
        finally { Gate.Release(); }
    }

    private sealed record SgsObservation([property: JsonPropertyName("data")] string Date, [property: JsonPropertyName("valor")] string Value);
}

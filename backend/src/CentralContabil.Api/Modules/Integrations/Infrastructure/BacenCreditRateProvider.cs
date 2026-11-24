using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CentralContabil.Api.Modules.Integrations.Infrastructure;

public sealed record CreditRateReference(decimal AnnualRatePercent, DateOnly ReferenceMonth, DateTimeOffset RetrievedAt, int SeriesCode, string SourceUrl, string Description);

public sealed class BacenCreditRateProvider(HttpClient httpClient)
{
    private static readonly Uri SourceUri = new("https://api.bcb.gov.br/dados/serie/bcdata.sgs.20748/dados/ultimos/1?formato=json");
    private static readonly string ReferenceUrl = "https://dadosabertos.bcb.gov.br/dataset/20748-taxa-media-de-juros-das-operacoes-de-credito-com-recursos-livres---pessoas-fisicas---credito-";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static CreditRateReference? cached;
    private static DateTimeOffset cacheUntil;

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

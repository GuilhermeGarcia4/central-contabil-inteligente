using CentralContabil.Api.Modules.Integrations.Application;
using CentralContabil.Api.Modules.Integrations.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CentralContabil.Api.Modules.Integrations.Infrastructure;

public sealed class ChamberLegislationProvider(HttpClient client, ResilientHttpExecutor executor, IConfiguration configuration) : ILegislationProvider
{
    public string Name => "Câmara dos Deputados";
    public bool Enabled => configuration.GetValue("Integrations:Chamber:Enabled", true);

    public async Task<IReadOnlyList<LegalSearchItem>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (!Enabled) return [];
        var result = await executor.GetAsync<ChamberEnvelope>(Name, client,
            $"proposicoes?keywords={Uri.EscapeDataString(query)}&itens=10&ordem=DESC&ordenarPor=id", cancellationToken);
        return result.Data.Select(item => new LegalSearchItem(item.Id.ToString(),
            $"{item.Type} {item.Number}/{item.Year}", item.Summary ?? string.Empty, LegalDocumentKind.LegislativeProposal,
            "Câmara dos Deputados", "Em tramitação/consultar fonte", null,
            $"https://www.camara.leg.br/propostas-legislativas/{item.Id}", Name)).ToArray();
    }
}

public sealed record ChamberEnvelope([property: JsonPropertyName("dados")] List<ChamberProposal> Data);
public sealed record ChamberProposal(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("siglaTipo")] string Type,
    [property: JsonPropertyName("numero")] int Number,
    [property: JsonPropertyName("ano")] int Year,
    [property: JsonPropertyName("ementa")] string? Summary);

public sealed class SenateLegislationProvider(HttpClient client, ResilientHttpExecutor executor, IConfiguration configuration,
    ILogger<SenateLegislationProvider> logger) : ILegislationProvider
{
    public string Name => "Senado Federal";
    public bool Enabled => configuration.GetValue("Integrations:Senate:Enabled", true);

    public async Task<IReadOnlyList<LegalSearchItem>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (!Enabled) return [];
        // O endpoint com palavra-chave ainda é o contrato oficial disponível, mas está marcado como deprecated.
        // O parser tolerante isola essa instabilidade até o sucessor oferecer busca textual equivalente.
        var document = await executor.GetAsync<JsonElement>(Name, client,
            $"dadosabertos/materia/pesquisa/lista?palavraChave={Uri.EscapeDataString(query)}&tramitando=S&v=7", cancellationToken);
        var items = new List<LegalSearchItem>();
        CollectMatterObjects(document, items);
        if (items.Count == 0) logger.LogInformation("Senate search returned no recognizable matter items for query {Query}", query);
        return items.Take(10).ToArray();
    }

    private static void CollectMatterObjects(JsonElement element, List<LegalSearchItem> output)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var code = FirstText(element, "CodigoMateria", "codigoMateria", "Codigo");
            var summary = FirstText(element, "EmentaMateria", "ementaMateria", "Ementa");
            var type = FirstText(element, "SiglaSubtipoMateria", "sigla", "SiglaMateria");
            var number = FirstText(element, "NumeroMateria", "numero");
            var year = FirstText(element, "AnoMateria", "ano");
            if (!string.IsNullOrWhiteSpace(code) && (!string.IsNullOrWhiteSpace(summary) || !string.IsNullOrWhiteSpace(type)))
                output.Add(new LegalSearchItem(code, $"{type} {number}/{year}".Trim(), summary,
                    LegalDocumentKind.LegislativeProposal, "Senado Federal", "Em tramitação/consultar fonte", null,
                    $"https://www25.senado.leg.br/web/atividade/materias/-/materia/{code}", "Senado Federal"));
            foreach (var property in element.EnumerateObject()) CollectMatterObjects(property.Value, output);
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) CollectMatterObjects(item, output);
    }

    private static string FirstText(JsonElement element, params string[] names)
    {
        foreach (var name in names)
            if (element.TryGetProperty(name, out var property))
                return property.ValueKind == JsonValueKind.String ? property.GetString() ?? string.Empty : property.GetRawText().Trim('"');
        return string.Empty;
    }
}

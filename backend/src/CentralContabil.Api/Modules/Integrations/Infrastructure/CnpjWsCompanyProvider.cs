using CentralContabil.Api.Modules.Integrations.Application;
using CentralContabil.Api.Modules.Integrations.Domain;
using System.Text.Json;

namespace CentralContabil.Api.Modules.Integrations.Infrastructure;

public sealed class CnpjWsCompanyProvider(HttpClient client, ResilientHttpExecutor executor, IConfiguration configuration) : ICompanyDataProvider
{
    public string Name => "CNPJ.ws";
    public bool Enabled => configuration.GetValue("Integrations:CnpjWs:Enabled", false);

    public async Task<CompanyData> GetByCnpjAsync(CnpjIdentifier cnpj, CancellationToken cancellationToken)
    {
        if (!Enabled) throw new ExternalProviderException("O fallback CNPJ.ws está desabilitado.");
        var json = await executor.GetAsync<JsonElement>(Name, client, $"cnpj/{Uri.EscapeDataString(cnpj.Value)}", cancellationToken);
        var establishment = Property(json, "estabelecimento");
        var primary = Property(establishment, "atividade_principal");
        var secondaries = Property(establishment, "atividades_secundarias");
        var activities = secondaries.ValueKind == JsonValueKind.Array
            ? secondaries.EnumerateArray().Select(x => new EconomicActivity(Text(x, "id"), Text(x, "descricao"), false)).ToArray()
            : [];

        return new CompanyData(cnpj.Value, cnpj.Formatted, Text(json, "razao_social"), Text(establishment, "nome_fantasia"),
            Text(Property(establishment, "situacao_cadastral"), "descricao"), Date(establishment, "data_inicio_atividade"),
            Text(Property(json, "porte"), "descricao"), Text(Property(json, "natureza_juridica"), "descricao"),
            new AddressData(Text(establishment, "logradouro"), Text(establishment, "numero"), Text(establishment, "complemento"),
                Text(establishment, "bairro"), Text(Property(establishment, "cidade"), "nome"), Text(Property(establishment, "estado"), "sigla"),
                Text(establishment, "cep")),
            primary.ValueKind == JsonValueKind.Object ? new EconomicActivity(Text(primary, "id"), Text(primary, "descricao"), true) : null,
            activities, Boolean(Property(json, "simples"), "simples"), Boolean(Property(json, "simples"), "mei"),
            Name, DateTimeOffset.UtcNow, false);
    }

    private static JsonElement Property(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? value : default;
    private static string Text(JsonElement element, string name) => Property(element, name).ValueKind switch
    {
        JsonValueKind.String => Property(element, name).GetString() ?? string.Empty,
        JsonValueKind.Number => Property(element, name).GetRawText(),
        _ => string.Empty
    };
    private static DateOnly? Date(JsonElement element, string name) => DateOnly.TryParse(Text(element, name), out var value) ? value : null;
    private static bool? Boolean(JsonElement element, string name) => Property(element, name).ValueKind switch
    {
        JsonValueKind.True => true, JsonValueKind.False => false, _ => null
    };
}

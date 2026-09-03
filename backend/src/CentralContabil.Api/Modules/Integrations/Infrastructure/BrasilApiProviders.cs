using CentralContabil.Api.Modules.Integrations.Application;
using CentralContabil.Api.Modules.Integrations.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CentralContabil.Api.Modules.Integrations.Infrastructure;

public sealed class BrasilApiCompanyProvider(HttpClient client, ResilientHttpExecutor executor) : ICompanyDataProvider
{
    public string Name => "BrasilAPI";
    public bool Enabled => true;

    public async Task<CompanyData> GetByCnpjAsync(CnpjIdentifier cnpj, CancellationToken cancellationToken)
    {
        var dto = await executor.GetAsync<BrasilApiCompanyDto>(Name, client, $"cnpj/v1/{Uri.EscapeDataString(cnpj.Value)}", cancellationToken);
        return BrasilApiMapper.ToCompany(dto, cnpj, Name);
    }
}

public sealed class BrasilApiNcmProvider(HttpClient client, ResilientHttpExecutor executor) : INcmDataProvider
{
    public string Name => "BrasilAPI";
    public bool Enabled => true;

    public async Task<IReadOnlyList<NcmData>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var result = await executor.GetAsync<List<BrasilApiNcmDto>>(Name, client, $"ncm/v1?search={Uri.EscapeDataString(query)}", cancellationToken);
        return result.Take(30).Select(x => BrasilApiMapper.ToNcm(x, Name)).ToArray();
    }

    public async Task<NcmData> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var result = await executor.GetAsync<BrasilApiNcmDto>(Name, client, $"ncm/v1/{Uri.EscapeDataString(NormalizeNcm(code))}", cancellationToken);
        return BrasilApiMapper.ToNcm(result, Name);
    }

    public static string NormalizeNcm(string value) => new(value.Where(char.IsAsciiDigit).ToArray());
}

public sealed record BrasilApiCompanyDto
{
    [JsonPropertyName("cnpj")] public string Cnpj { get; init; } = string.Empty;
    [JsonPropertyName("razao_social")] public string LegalName { get; init; } = string.Empty;
    [JsonPropertyName("nome_fantasia")] public string? TradeName { get; init; }
    [JsonPropertyName("descricao_situacao_cadastral")] public string? Status { get; init; }
    [JsonPropertyName("data_inicio_atividade")] public string? OpenedOn { get; init; }
    [JsonPropertyName("porte")] public string? CompanySize { get; init; }
    [JsonPropertyName("natureza_juridica")] public string? LegalNature { get; init; }
    [JsonPropertyName("codigo_natureza_juridica")] public JsonElement LegalNatureCode { get; init; }
    [JsonPropertyName("descricao_tipo_de_logradouro")] public string? StreetType { get; init; }
    [JsonPropertyName("logradouro")] public string? Street { get; init; }
    [JsonPropertyName("numero")] public string? Number { get; init; }
    [JsonPropertyName("complemento")] public string? Complement { get; init; }
    [JsonPropertyName("bairro")] public string? District { get; init; }
    [JsonPropertyName("municipio")] public string? City { get; init; }
    [JsonPropertyName("uf")] public string? State { get; init; }
    [JsonPropertyName("cep")] public string? PostalCode { get; init; }
    [JsonPropertyName("cnae_fiscal")] public JsonElement PrimaryCode { get; init; }
    [JsonPropertyName("cnae_fiscal_descricao")] public string? PrimaryDescription { get; init; }
    [JsonPropertyName("cnaes_secundarios")] public List<BrasilApiActivityDto>? SecondaryActivities { get; init; }
    [JsonPropertyName("opcao_pelo_simples")] public bool? IsSimpleNational { get; init; }
    [JsonPropertyName("opcao_pelo_mei")] public bool? IsMei { get; init; }
}

public sealed record BrasilApiActivityDto
{
    [JsonPropertyName("codigo")] public JsonElement Code { get; init; }
    [JsonPropertyName("descricao")] public string? Description { get; init; }
}

public sealed record BrasilApiNcmDto
{
    [JsonPropertyName("codigo")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("descricao")] public string Description { get; init; } = string.Empty;
    [JsonPropertyName("data_inicio")] public string? EffectiveFrom { get; init; }
    [JsonPropertyName("data_fim")] public string? EffectiveUntil { get; init; }
    [JsonPropertyName("tipo_ato")] public string? ActType { get; init; }
    [JsonPropertyName("numero_ato")] public JsonElement ActNumber { get; init; }
    [JsonPropertyName("ano_ato")] public JsonElement ActYear { get; init; }
}

public static class BrasilApiMapper
{
    public static CompanyData ToCompany(BrasilApiCompanyDto dto, CnpjIdentifier requestedCnpj, string provider)
    {
        var street = string.Join(' ', new[] { dto.StreetType, dto.Street }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var primaryCode = ElementText(dto.PrimaryCode);
        return new CompanyData(requestedCnpj.Value, requestedCnpj.Formatted, dto.LegalName, dto.TradeName ?? string.Empty,
            dto.Status ?? string.Empty, ParseDate(dto.OpenedOn), dto.CompanySize ?? string.Empty,
            dto.LegalNature ?? ElementText(dto.LegalNatureCode),
            new AddressData(street, dto.Number ?? string.Empty, dto.Complement ?? string.Empty, dto.District ?? string.Empty,
                dto.City ?? string.Empty, dto.State ?? string.Empty, dto.PostalCode ?? string.Empty),
            string.IsNullOrWhiteSpace(primaryCode) ? null : new EconomicActivity(primaryCode, dto.PrimaryDescription ?? string.Empty, true),
            dto.SecondaryActivities?.Select(x => new EconomicActivity(ElementText(x.Code), x.Description ?? string.Empty, false)).ToArray() ?? [],
            dto.IsSimpleNational, dto.IsMei, provider, DateTimeOffset.UtcNow, false);
    }

    public static NcmData ToNcm(BrasilApiNcmDto dto, string provider) => new(dto.Code, dto.Description.TrimStart('-', ' '),
        ParseDate(dto.EffectiveFrom), ParseDate(dto.EffectiveUntil), dto.ActType ?? string.Empty, ElementText(dto.ActNumber),
        ElementText(dto.ActYear), provider, DateTimeOffset.UtcNow, false);

    private static string ElementText(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Number => value.GetRawText(),
        _ => string.Empty
    };

    private static DateOnly? ParseDate(string? value) => DateOnly.TryParse(value, out var date) ? date : null;
}

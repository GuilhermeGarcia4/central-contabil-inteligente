using CentralContabil.Api.Modules.Integrations.Application;
using CentralContabil.Api.Modules.Integrations.Domain;
using CentralContabil.Api.Modules.Integrations.Infrastructure;
using CentralContabil.Api.Modules.Search.Application;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace CentralContabil.UnitTests;

public sealed class CnpjIdentifierTests
{
    [Theory]
    [InlineData("00.000.000/0001-91", "00000000000191")]
    [InlineData("19.131.243/0001-97", "19131243000197")]
    [InlineData("00.000.000/E08G-12", "00000000E08G12")]
    [InlineData("00.000.000/e08g-12", "00000000E08G12")]
    public void Accepts_numeric_and_alphanumeric_cnpj_without_losing_leading_zeroes(string input, string expected)
    {
        Assert.True(CnpjIdentifier.TryParse(input, out var cnpj, out var error), error);
        Assert.Equal(expected, cnpj.Value);
    }

    [Theory]
    [InlineData("00000000000000")]
    [InlineData("00.000.000/E08G-AA")]
    [InlineData("00.000.000/E08G-13")]
    [InlineData("123")]
    public void Rejects_invalid_format_or_check_digits(string input) =>
        Assert.False(CnpjIdentifier.TryParse(input, out _, out _));

    [Theory]
    [InlineData("00.000.000/E08G-12", SearchIntent.Cnpj)]
    [InlineData("3305.10.00", SearchIntent.Ncm)]
    [InlineData("Lei 14.133", SearchIntent.LegalReference)]
    [InlineData("como calcular férias", SearchIntent.General)]
    public void Detects_search_intent_without_calling_every_provider(string query, SearchIntent expected) =>
        Assert.Equal(expected, SearchIntentDetector.Detect(query));
}

public sealed class ProviderMappingTests
{
    [Fact]
    public void Brasil_api_company_mapper_accepts_numeric_schema_fields()
    {
        using var document = JsonDocument.Parse("{\"nature\":2062,\"cnae\":6201501}");
        var dto = new BrasilApiCompanyDto
        {
            LegalName = "Empresa",
            LegalNatureCode = document.RootElement.GetProperty("nature").Clone(),
            PrimaryCode = document.RootElement.GetProperty("cnae").Clone(),
            PrimaryDescription = "Desenvolvimento"
        };

        var result = BrasilApiMapper.ToCompany(dto, CnpjIdentifier.Parse("19.131.243/0001-97"), "BrasilAPI");

        Assert.Equal("2062", result.LegalNature);
        Assert.Equal("6201501", result.PrimaryActivity?.Code);
    }

    [Fact]
    public void Brasil_api_ncm_mapper_tolerates_numeric_act_fields()
    {
        using var document = JsonDocument.Parse("{\"number\":10,\"year\":2026}");
        var dto = new BrasilApiNcmDto { Code = "3305.10.00", Description = "- Xampus",
            ActNumber = document.RootElement.GetProperty("number").Clone(), ActYear = document.RootElement.GetProperty("year").Clone() };

        var result = BrasilApiMapper.ToNcm(dto, "BrasilAPI");

        Assert.Equal("Xampus", result.Description);
        Assert.Equal("10", result.ActNumber);
        Assert.Equal("2026", result.ActYear);
    }
}

public sealed class CompanyFallbackTests
{
    [Fact]
    public async Task Uses_fallback_only_for_transient_primary_failure()
    {
        var primary = new FakeProvider("BrasilAPI", _ => throw new ExternalTransientException("temporary"));
        var fallback = new FakeProvider("CNPJ.ws", cnpj => Company(cnpj, "CNPJ.ws"));
        var service = Service(primary, fallback, new MemoryCache());

        var result = await service.GetAsync("19.131.243/0001-97", default);

        Assert.Equal("CNPJ.ws", result.Provider);
        Assert.Equal(1, fallback.Calls);
    }

    [Fact]
    public async Task Does_not_use_fallback_for_permanent_provider_rejection()
    {
        var primary = new FakeProvider("BrasilAPI", _ => throw new ExternalProviderException("bad request"));
        var fallback = new FakeProvider("CNPJ.ws", cnpj => Company(cnpj, "CNPJ.ws"));
        var service = Service(primary, fallback, new MemoryCache());

        await Assert.ThrowsAsync<ExternalProviderException>(() => service.GetAsync("19.131.243/0001-97", default));
        Assert.Equal(0, fallback.Calls);
    }

    [Fact]
    public async Task Returns_cached_snapshot_without_external_request()
    {
        var cache = new MemoryCache();
        var cnpj = CnpjIdentifier.Parse("19.131.243/0001-97");
        await cache.SetAsync("BrasilAPI", "company", cnpj.Value, Company(cnpj, "BrasilAPI"), TimeSpan.FromHours(1), default);
        var primary = new FakeProvider("BrasilAPI", value => Company(value, "BrasilAPI"));
        var fallback = new FakeProvider("CNPJ.ws", value => Company(value, "CNPJ.ws"));

        var result = await Service(primary, fallback, cache).GetAsync(cnpj.Value, default);

        Assert.True(result.FromCache);
        Assert.Equal(0, primary.Calls);
    }

    private static CompanyLookupService Service(ICompanyDataProvider primary, ICompanyDataProvider fallback, IExternalSnapshotCache cache)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Integrations:CnpjWs:Enabled"] = "true" }).Build();
        return new CompanyLookupService([primary, fallback], cache, configuration);
    }

    internal static CompanyData Company(CnpjIdentifier cnpj, string provider) => new(cnpj.Value, cnpj.Formatted, "Empresa teste", "Teste", "ATIVA",
        new DateOnly(2020, 1, 1), "ME", "Sociedade", new AddressData("Rua Teste", "1", "", "Centro", "São Paulo", "SP", "01000000"),
        new EconomicActivity("6201501", "Desenvolvimento", true), [], true, false, provider, DateTimeOffset.UtcNow, false);

    internal sealed class FakeProvider(string name, Func<CnpjIdentifier, CompanyData> response) : ICompanyDataProvider
    {
        public string Name => name;
        public bool Enabled => true;
        public int Calls { get; private set; }
        public Task<CompanyData> GetByCnpjAsync(CnpjIdentifier cnpj, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(response(cnpj));
        }
    }

    internal sealed class MemoryCache : IExternalSnapshotCache
    {
        private readonly Dictionary<string, object> _items = [];
        public Task<T?> GetAsync<T>(string provider, string entityType, string externalKey, CancellationToken cancellationToken) =>
            Task.FromResult(_items.TryGetValue($"{provider}:{entityType}:{externalKey}", out var value) ? (T?)value : default);
        public Task SetAsync<T>(string provider, string entityType, string externalKey, T value, TimeSpan timeToLive, CancellationToken cancellationToken)
        {
            _items[$"{provider}:{entityType}:{externalKey}"] = value!;
            return Task.CompletedTask;
        }
    }
}

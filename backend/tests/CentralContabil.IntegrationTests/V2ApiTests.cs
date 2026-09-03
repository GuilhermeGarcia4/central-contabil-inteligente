using CentralContabil.Api.Modules.Integrations.Application;
using CentralContabil.Api.Modules.Integrations.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;

namespace CentralContabil.IntegrationTests;

public sealed class V2ApiTests
{
    private static WebApplicationFactory<Program>? Factory()
    {
        var connection = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connection)) return null;
        Environment.SetEnvironmentVariable("CONNECTION_STRING", connection);
        Environment.SetEnvironmentVariable("JWT_KEY", "integration-test-key-with-more-than-32-characters");
        Environment.SetEnvironmentVariable("ADMIN_RESET_PASSWORD_ON_START", "false");
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICompanyDataProvider>();
            services.RemoveAll<IExternalSnapshotCache>();
            services.AddScoped<ICompanyDataProvider>(_ => new FakeCompanyProvider("BrasilAPI"));
            services.AddScoped<ICompanyDataProvider>(_ => new FakeCompanyProvider("CNPJ.ws"));
            services.AddSingleton<IExternalSnapshotCache, MemorySnapshotCache>();
        }));
    }

    [PostgreSqlFact]
    public async Task Company_endpoint_uses_mocked_provider_and_accepts_alphanumeric_cnpj()
    {
        using var factory = Factory()!;
        var response = await factory.CreateClient().GetAsync("/api/v2/companies/00000000E08G12");
        response.EnsureSuccessStatusCode();
        var company = await response.Content.ReadFromJsonAsync<CompanyData>();
        Assert.Equal("00000000E08G12", company?.Cnpj);
        Assert.Equal("Mock BrasilAPI", company?.LegalName);
    }

    [PostgreSqlFact]
    public async Task Company_endpoint_returns_problem_details_for_invalid_cnpj()
    {
        using var factory = Factory()!;
        var response = await factory.CreateClient().GetAsync("/api/v2/companies/not-a-cnpj");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private sealed class FakeCompanyProvider(string name) : ICompanyDataProvider
    {
        public string Name => name;
        public bool Enabled => true;
        public Task<CompanyData> GetByCnpjAsync(CnpjIdentifier cnpj, CancellationToken cancellationToken) => Task.FromResult(new CompanyData(
            cnpj.Value, cnpj.Formatted, $"Mock {name}", "Mock", "ATIVA", new DateOnly(2026, 7, 31), "ME", "Sociedade",
            new AddressData("Rua A", "1", "", "Centro", "Brasília", "DF", "70000000"), null, [], true, false, name, DateTimeOffset.UtcNow, false));
    }

    private sealed class MemorySnapshotCache : IExternalSnapshotCache
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

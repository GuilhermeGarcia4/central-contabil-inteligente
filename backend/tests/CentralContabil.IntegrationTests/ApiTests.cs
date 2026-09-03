using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using CentralContabil.Api.Modules.Shared.Domain;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Knowledge.Application;
using CentralContabil.Api.Modules.Knowledge.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.IntegrationTests;

public sealed class ApiTests
{
    private static WebApplicationFactory<Program>? Factory()
    {
        var connection = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connection)) return null;
        Environment.SetEnvironmentVariable("CONNECTION_STRING", connection);
        Environment.SetEnvironmentVariable("JWT_KEY", "integration-test-key-with-more-than-32-characters");
        Environment.SetEnvironmentVariable("ADMIN_RESET_PASSWORD_ON_START", "false");
        return new WebApplicationFactory<Program>();
    }

    [PostgreSqlFact] public async Task Public_articles_endpoint_responds()
    { using var factory = Factory()!; var response = await factory.CreateClient().GetAsync("/api/v1/articles"); Assert.Equal(HttpStatusCode.OK, response.StatusCode); }

    [PostgreSqlFact] public async Task User_can_register()
    { using var factory = Factory()!; var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/register", new { displayName = "Integration", email = $"integration-{Guid.NewGuid():N}@example.test", password = "Strong!Pass123" }); Assert.Equal(HttpStatusCode.Created, response.StatusCode); }

    [PostgreSqlFact] public async Task Admin_can_login_and_access_admin_endpoint()
    {
        using var factory = Factory()!;
        var email = $"admin-{Guid.NewGuid():N}@example.test";
        const string password = "Strong!Pass123";
        using (var scope = factory.Services.CreateScope()) {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = email, Email = email, DisplayName = "Admin Integration" };
            Assert.True((await users.CreateAsync(user, password)).Succeeded);
            Assert.True((await users.AddToRoleAsync(user, "Admin")).Succeeded);
        }
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();
        var json = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/dashboard")).StatusCode);
    }

    [PostgreSqlFact] public async Task Google_status_is_available_without_exposing_secrets()
    {
        using var factory = Factory()!;
        var response = await factory.CreateClient().GetAsync("/api/v1/auth/google/status");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("clientSecret", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clientId", body, StringComparison.OrdinalIgnoreCase);
    }

    [PostgreSqlFact] public async Task Knowledge_indexing_creates_chunks_and_hybrid_search_finds_semantic_match()
    {
        using var factory = Factory()!; using var scope = factory.Services.CreateScope();
        var indexing = scope.ServiceProvider.GetRequiredService<KnowledgeIndexingService>();
        await indexing.IndexAllEligibleAsync(default);
        var search = scope.ServiceProvider.GetRequiredService<HybridKnowledgeSearch>();
        var results = await search.SearchAsync("quanto recebo quando tiro descanso do trabalho", DateOnly.FromDateTime(DateTime.Today), false, default);
        Assert.Contains(results, x => x.Title.Contains("Férias", StringComparison.OrdinalIgnoreCase));
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.KnowledgeChunks.AnyAsync(x => x.EmbeddingStatus == EmbeddingStatus.Ready));
    }

    [PostgreSqlFact] public async Task Assistant_uses_deterministic_calculator_and_does_not_invent_law()
    {
        using var factory = Factory()!; var client = factory.CreateClient();
        var calculation = await client.PostAsJsonAsync("/api/v1/assistant/ask", new { question = "Quanto recebo de 13º com salário de 3000 e 6 meses?" });
        calculation.EnsureSuccessStatusCode(); var calculationBody = await calculation.Content.ReadAsStringAsync(); Assert.Contains("1500", calculationBody); Assert.Contains("decimo-terceiro", calculationBody);
        var missing = await client.PostAsJsonAsync("/api/v1/assistant/ask", new { question = "Qual é a nova lei XYZ que nunca existiu?" });
        missing.EnsureSuccessStatusCode(); var missingBody = await missing.Content.ReadAsStringAsync(); Assert.Contains("Não encontrei informação suficiente", missingBody); Assert.Contains("\"sources\":[]", missingBody);
    }

    [PostgreSqlFact] public async Task Expired_knowledge_is_excluded_from_search()
    {
        using var factory = Factory()!; using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var marker = $"expirado-{Guid.NewGuid():N}"; var document = new KnowledgeDocument { Title = marker, Content = marker, ContentHash = KnowledgeIndexingService.Hash(marker), Status = KnowledgeStatus.Active, ValidUntil = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)) };
        db.KnowledgeDocuments.Add(document); await db.SaveChangesAsync();
        var results = await scope.ServiceProvider.GetRequiredService<HybridKnowledgeSearch>().SearchAsync(marker, DateOnly.FromDateTime(DateTime.Today), true, default);
        Assert.DoesNotContain(results, x => x.DocumentId == document.Id);
        db.KnowledgeDocuments.Remove(document); await db.SaveChangesAsync();
    }

    [PostgreSqlFact] public async Task Compound_interest_endpoint_returns_auditable_result()
    { using var factory = Factory()!; var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/calculations/compound-interest", new { initialCapital = 1000m, monthlyRatePercent = 1m, months = 12, monthlyContribution = 0m }); response.EnsureSuccessStatusCode(); var json = await response.Content.ReadAsStringAsync(); Assert.Contains("formula", json); Assert.Contains("rule", json); }
}

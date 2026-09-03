using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Shared.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CentralContabil.IntegrationTests;

public sealed class FinanceV6ApiTests
{
    private static async Task<(WebApplicationFactory<Program> Factory, HttpClient Client, Guid UserId)> CreateUserAsync(string tag)
    {
        var connection = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("TEST_CONNECTION_STRING não definida.");
        Environment.SetEnvironmentVariable("CONNECTION_STRING", connection);
        Environment.SetEnvironmentVariable("JWT_KEY", "integration-test-key-with-more-than-32-characters");
        Environment.SetEnvironmentVariable("ADMIN_RESET_PASSWORD_ON_START", "false");
        var factory = new WebApplicationFactory<Program>();
        var email = $"finance-v6-{tag}-{Guid.NewGuid():N}@example.test"; const string password = "Strong!Pass123"; Guid userId;
        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = email, Email = email, DisplayName = "Finance V6" };
            Assert.True((await users.CreateAsync(user, password)).Succeeded); userId = user.Id;
        }
        var client = factory.CreateClient(); var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password }); login.EnsureSuccessStatusCode();
        var loginJson = await login.Content.ReadFromJsonAsync<JsonElement>(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginJson.GetProperty("accessToken").GetString());
        return (factory, client, userId);
    }

    [PostgreSqlFact]
    public async Task Card_account_scheduled_debt_reserve_networth_flow_works()
    {
        var (factory, client, userId) = await CreateUserAsync("flow");
        try
        {
            var card = await client.PostAsJsonAsync("/api/v1/finance/cards", new { name = "Nubank", bank = "Nubank", limit = 5000m, closingDay = 10, dueDay = 15, last4 = "1234", color = "#660240" });
            Assert.Equal(HttpStatusCode.Created, card.StatusCode);
            var cardJson = await card.Content.ReadFromJsonAsync<JsonElement>(); var cardId = cardJson.GetProperty("id").GetGuid();

            var account = await client.PostAsJsonAsync("/api/v1/finance/accounts", new { name = "Conta Corrente", type = "Checking", balance = 2000m, color = "#168261" });
            Assert.Equal(HttpStatusCode.Created, account.StatusCode);
            var accountJson = await account.Content.ReadFromJsonAsync<JsonElement>(); var accountId = accountJson.GetProperty("id").GetGuid();

            var transfer = await client.PostAsJsonAsync("/api/v1/finance/accounts/transfer", new { fromAccountId = accountId, toAccountId = accountId, amount = 100m, date = "2026-09-01", notes = "x" });
            Assert.Equal(HttpStatusCode.BadRequest, transfer.StatusCode);

            var categories = await client.GetFromJsonAsync<JsonElement>("/api/v1/finance/categories");
            var expenseCategory = categories.EnumerateArray().First(x => x.GetProperty("type").GetString() == "Expense").GetProperty("id").GetGuid();
            var scheduled = await client.PostAsJsonAsync("/api/v1/finance/scheduled", new { type = "Expense", categoryId = expenseCategory, description = "Internet", amount = 100m, dueDate = "2026-09-10", accountId = accountId, notes = (string?)null });
            Assert.Equal(HttpStatusCode.Created, scheduled.StatusCode);
            var scheduledJson = await scheduled.Content.ReadFromJsonAsync<JsonElement>(); var scheduledId = scheduledJson.GetProperty("id").GetGuid();
            var status = await client.PostAsJsonAsync($"/api/v1/finance/scheduled/{scheduledId}/status", new { status = "Paid" });
            status.EnsureSuccessStatusCode();

            var debt = await client.PostAsJsonAsync("/api/v1/finance/debts", new { name = "Cartão antigo", originalAmount = 1000m, currentAmount = 500m, installmentCount = 5, interestRate = 3.5m, dueDate = "2026-12-01", institution = "Banco", notes = (string?)null });
            Assert.Equal(HttpStatusCode.Created, debt.StatusCode);

            var reserve = await client.PutAsJsonAsync("/api/v1/finance/reserve", new { targetAmount = 6000m, currentAmount = 1500m, targetMonths = 6, notes = (string?)null });
            reserve.EnsureSuccessStatusCode();
            var reserveJson = await reserve.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(25m, reserveJson.GetProperty("progressPercentage").GetDecimal());

            var netWorth = await client.GetFromJsonAsync<JsonElement>("/api/v1/finance/net-worth");
            Assert.Equal(2000m + 1500m, netWorth.GetProperty("assets").GetDecimal());
            Assert.Equal(500m, netWorth.GetProperty("passives").GetDecimal());
            Assert.Equal(3000m, netWorth.GetProperty("netWorth").GetDecimal());

            var invoices = await client.GetFromJsonAsync<JsonElement>("/api/v1/finance/cards/invoices?year=2026&month=9");
            Assert.Contains(invoices.EnumerateArray(), x => x.GetProperty("cardId").GetGuid() == cardId);

            var calendar = await client.GetFromJsonAsync<JsonElement>("/api/v1/finance/calendar?year=2026&month=9");
            Assert.True(calendar.EnumerateArray().Any());

            var annual = await client.GetFromJsonAsync<JsonElement>("/api/v1/finance/annual-report?year=2026");
            Assert.Equal(12, annual.GetProperty("months").EnumerateArray().Count());
        }
        finally { await factory.DisposeAsync(); }
    }

    [PostgreSqlFact]
    public async Task User_cannot_access_another_users_data()
    {
        var (factoryA, clientA, _) = await CreateUserAsync("iso-a");
        var (factoryB, clientB, _) = await CreateUserAsync("iso-b");
        try
        {
            var cardA = await clientA.PostAsJsonAsync("/api/v1/finance/cards", new { name = "Cartão A", bank = "A", limit = 1000m, closingDay = 5, dueDay = 10, last4 = "0001", color = (string?)null });
            var cardAJson = await cardA.Content.ReadFromJsonAsync<JsonElement>(); var cardAId = cardAJson.GetProperty("id").GetGuid();

            var cardsB = await clientB.GetFromJsonAsync<JsonElement>("/api/v1/finance/cards");
            Assert.DoesNotContain(cardsB.EnumerateArray(), x => x.GetProperty("id").GetGuid() == cardAId);

            var update = await clientB.PutAsJsonAsync($"/api/v1/finance/cards/{cardAId}", new { name = "Hack", bank = "X", limit = 1m, closingDay = 1, dueDay = 2, last4 = "9999", color = (string?)null });
            Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);

            var delete = await clientB.DeleteAsync($"/api/v1/finance/cards/{cardAId}");
            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);

            var accountA = await clientA.PostAsJsonAsync("/api/v1/finance/accounts", new { name = "Conta A", type = "Checking", balance = 100m, color = (string?)null });
            var accountAJson = await accountA.Content.ReadFromJsonAsync<JsonElement>(); var accountAId = accountAJson.GetProperty("id").GetGuid();
            var transferB = await clientB.PostAsJsonAsync("/api/v1/finance/accounts/transfer", new { fromAccountId = accountAId, toAccountId = accountAId, amount = 10m, date = "2026-09-01", notes = (string?)null });
            Assert.Equal(HttpStatusCode.BadRequest, transferB.StatusCode);
        }
        finally { await factoryA.DisposeAsync(); await factoryB.DisposeAsync(); }
    }
}

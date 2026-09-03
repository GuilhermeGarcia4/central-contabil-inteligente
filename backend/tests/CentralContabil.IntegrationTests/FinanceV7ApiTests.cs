using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Shared.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CentralContabil.IntegrationTests;

public sealed class FinanceV7ApiTests
{
    private static async Task<(WebApplicationFactory<Program> Factory, HttpClient Client, Guid UserId)> CreateUserAsync(string tag)
    {
        var connection = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("TEST_CONNECTION_STRING não definida.");
        Environment.SetEnvironmentVariable("CONNECTION_STRING", connection);
        Environment.SetEnvironmentVariable("JWT_KEY", "integration-test-key-with-more-than-32-characters");
        Environment.SetEnvironmentVariable("ADMIN_RESET_PASSWORD_ON_START", "false");
        var factory = new WebApplicationFactory<Program>();
        var email = $"finance-v7-{tag}-{Guid.NewGuid():N}@example.test"; const string password = "Strong!Pass123"; Guid userId;
        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = email, Email = email, DisplayName = "Finance V7" };
            Assert.True((await users.CreateAsync(user, password)).Succeeded); userId = user.Id;
        }
        var client = factory.CreateClient(); var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password }); login.EnsureSuccessStatusCode();
        var loginJson = await login.Content.ReadFromJsonAsync<JsonElement>(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginJson.GetProperty("accessToken").GetString());
        return (factory, client, userId);
    }

    [PostgreSqlFact]
    public async Task Insights_alerts_and_preferences_work()
    {
        var (factory, client, _) = await CreateUserAsync("insights");
        try
        {
            var categories = await client.GetFromJsonAsync<JsonElement>("/api/v1/finance/categories");
            var expenseCategory = categories.EnumerateArray().First(x => x.GetProperty("type").GetString() == "Expense").GetProperty("id").GetGuid();
            var incomeCategory = categories.EnumerateArray().First(x => x.GetProperty("type").GetString() == "Income").GetProperty("id").GetGuid();
            await client.PostAsJsonAsync("/api/v1/finance/transactions", new { type = "Income", categoryId = incomeCategory, description = "Salário", amount = 5000m, transactionDate = "2026-09-05", paymentMethod = "Pix", isRecurring = false, notes = (string?)null });
            await client.PostAsJsonAsync("/api/v1/finance/transactions", new { type = "Expense", categoryId = expenseCategory, description = "Mercado", amount = 1200m, transactionDate = "2026-09-10", paymentMethod = "Pix", isRecurring = false, notes = (string?)null });
            var today = DateTime.Today;
            await client.PutAsJsonAsync("/api/v1/finance/budgets", new { categoryId = expenseCategory, year = today.Year, month = today.Month, plannedAmount = 100m });
            await client.PostAsJsonAsync("/api/v1/finance/transactions", new { type = "Expense", categoryId = expenseCategory, description = "Mercado do mês", amount = 500m, transactionDate = $"{today.Year}-{today.Month:D2}-10", paymentMethod = "Pix", isRecurring = false, notes = (string?)null });

            var understandResp = await client.GetAsync("/api/v1/finance/understand-month?year=2026&month=9");
            if (!understandResp.IsSuccessStatusCode) { var body = await understandResp.Content.ReadAsStringAsync(); throw new Exception($"understand-month {understandResp.StatusCode}: {body}"); }
            var understand = await understandResp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(5000m, understand.GetProperty("totalIncome").GetDecimal());
            Assert.Equal(1200m, understand.GetProperty("totalExpenses").GetDecimal());
            Assert.True(understand.GetProperty("insights").EnumerateArray().Any());

            var weeklyResp = await client.GetAsync("/api/v1/finance/summary-weekly");
            if (!weeklyResp.IsSuccessStatusCode) { var body = await weeklyResp.Content.ReadAsStringAsync(); throw new Exception($"summary-weekly {weeklyResp.StatusCode}: {body}"); }
            var weekly = await weeklyResp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(weekly.GetProperty("insights").EnumerateArray().Any());

            var alerts = await client.GetFromJsonAsync<JsonElement>("/api/v1/finance/alerts");
            Assert.True(alerts.EnumerateArray().Any());

            var prefs = await client.PutAsJsonAsync("/api/v1/finance/preferences", new { explanationProfile = "Detailed", alertBills = false, alertInvoices = true, alertBudget = true, alertGoals = true, alertInstallments = true, alertWeeklySummary = true, alertMonthlySummary = true });
            prefs.EnsureSuccessStatusCode();
            var prefsJson = await prefs.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Detailed", prefsJson.GetProperty("explanationProfile").GetString());
            Assert.False(prefsJson.GetProperty("alertBills").GetBoolean());
        }
        finally { await factory.DisposeAsync(); }
    }

    [PostgreSqlFact]
    public async Task Csv_import_preview_and_confirm_work()
    {
        var (factory, client, _) = await CreateUserAsync("import");
        try
        {
            var csv = "data;descricao;valor;tipo\n10/09/2026;Mercado;150,50;Saida\n11/09/2026;Salario;3000,00;Entrada\n";
            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "file", "extrato.csv");
            var preview = await client.PostAsync("/api/v1/finance/imports/preview", content);
            if (!preview.IsSuccessStatusCode) { var body = await preview.Content.ReadAsStringAsync(); throw new Exception($"preview {preview.StatusCode}: {body}"); }
            Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
            var previewJson = await preview.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(2, previewJson.GetProperty("total").GetInt32());
            var rows = previewJson.GetProperty("rows").EnumerateArray().ToList();
            Assert.Equal(2, rows.Count);
            Assert.Equal("Mercado", rows[0].GetProperty("description").GetString());
            Assert.Equal("Expense", rows[0].GetProperty("type").GetString());
            Assert.Equal("Income", rows[1].GetProperty("type").GetString());

            var confirm = await client.PostAsJsonAsync("/api/v1/finance/imports/confirm", new { rows = new[] { new { index = 1, date = "2026-09-10", description = "Mercado", amount = 150.50m, type = "Expense", categoryId = rows[0].GetProperty("categoryId").GetGuid() } } });
            confirm.EnsureSuccessStatusCode();
            var confirmJson = await confirm.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, confirmJson.GetProperty("imported").GetInt32());

            var summary = await client.GetFromJsonAsync<JsonElement>("/api/v1/finance/summary?year=2026&month=9");
            Assert.Equal(150.50m, summary.GetProperty("totalExpenses").GetDecimal());
        }
        finally { await factory.DisposeAsync(); }
    }

    [PostgreSqlFact]
    public async Task Assistant_conversations_are_isolated_per_user()
    {
        var (factoryA, clientA, _) = await CreateUserAsync("conv-a");
        var (factoryB, clientB, _) = await CreateUserAsync("conv-b");
        try
        {
            var created = await clientA.PostAsJsonAsync("/api/v1/assistant/conversations", new { title = "Minha conversa" });
            created.EnsureSuccessStatusCode();
            var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

            var listA = await clientA.GetFromJsonAsync<JsonElement>("/api/v1/assistant/conversations");
            Assert.Contains(listA.EnumerateArray(), x => x.GetProperty("id").GetGuid() == id);

            var listB = await clientB.GetFromJsonAsync<JsonElement>("/api/v1/assistant/conversations");
            Assert.DoesNotContain(listB.EnumerateArray(), x => x.GetProperty("id").GetGuid() == id);

            var getB = await clientB.GetAsync($"/api/v1/assistant/conversations/{id}");
            Assert.Equal(HttpStatusCode.OK, getB.StatusCode);
            var messagesB = await getB.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Empty(messagesB.EnumerateArray());

            var deleteB = await clientB.DeleteAsync($"/api/v1/assistant/conversations/{id}");
            Assert.Equal(HttpStatusCode.NotFound, deleteB.StatusCode);

            var listA2 = await clientA.GetFromJsonAsync<JsonElement>("/api/v1/assistant/conversations");
            Assert.Contains(listA2.EnumerateArray(), x => x.GetProperty("id").GetGuid() == id);
        }
        finally { await factoryA.DisposeAsync(); await factoryB.DisposeAsync(); }
    }

    [PostgreSqlFact]
    public async Task Financial_question_returns_grounded_financial_answer()
    {
        var (factory, client, _) = await CreateUserAsync("finq");
        try
        {
            var categories = await client.GetFromJsonAsync<JsonElement>("/api/v1/finance/categories");
            var incomeCategory = categories.EnumerateArray().First(x => x.GetProperty("type").GetString() == "Income").GetProperty("id").GetGuid();
            var today = DateTime.Today;
            var currentMonthDate = $"{today.Year}-{today.Month:D2}-05";
            await client.PostAsJsonAsync("/api/v1/finance/transactions", new { type = "Income", categoryId = incomeCategory, description = "Salário", amount = 4000m, transactionDate = currentMonthDate, paymentMethod = "Pix", isRecurring = false, notes = (string?)null });

            var answer = await client.PostAsJsonAsync("/api/v1/assistant/ask", new { question = "Quanto entrou no meu mês?" });
            answer.EnsureSuccessStatusCode();
            var json = await answer.Content.ReadFromJsonAsync<JsonElement>();
            if (json.GetProperty("confidence").GetString() != "high") throw new Exception($"confidence={json.GetProperty("confidence").GetString()} answer={json.GetProperty("answer").GetString()}");
            Assert.Equal("high", json.GetProperty("confidence").GetString());
            var answerText = json.GetProperty("answer").GetString()!;
            Assert.Contains("4000", answerText.Replace(".", "").Replace(",", ""));
        }
        finally { await factory.DisposeAsync(); }
    }
}

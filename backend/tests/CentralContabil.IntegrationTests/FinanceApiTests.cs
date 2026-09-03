using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CentralContabil.IntegrationTests;

public sealed class FinanceApiTests
{
    private static WebApplicationFactory<Program>? Factory()
    {
        var connection = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING"); if (string.IsNullOrWhiteSpace(connection)) return null;
        Environment.SetEnvironmentVariable("CONNECTION_STRING", connection); Environment.SetEnvironmentVariable("JWT_KEY", "integration-test-key-with-more-than-32-characters"); Environment.SetEnvironmentVariable("ADMIN_RESET_PASSWORD_ON_START", "false");
        return new WebApplicationFactory<Program>();
    }

    [PostgreSqlFact] public async Task User_can_create_edit_and_delete_income_and_expense()
    {
        using var factory = Factory()!; var client = await AuthenticatedClient(factory);
        var categories = await client.GetFromJsonAsync<JsonElement>("/api/v1/finance/categories");
        var salary = Category(categories, "Salário"); var food = Category(categories, "Alimentação");
        var income = await client.PostAsJsonAsync("/api/v1/finance/transactions", Payload("Income", salary, "Salário teste", 4500)); Assert.True(income.IsSuccessStatusCode, await income.Content.ReadAsStringAsync());
        var expense = await client.PostAsJsonAsync("/api/v1/finance/transactions", Payload("Expense", food, "Mercado teste", 100)); expense.EnsureSuccessStatusCode();
        var expenseId = (await expense.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var update = await client.PutAsJsonAsync($"/api/v1/finance/transactions/{expenseId}", Payload("Expense", food, "Mercado editado", 120)); update.EnsureSuccessStatusCode();
        var summary = await client.GetFromJsonAsync<JsonElement>("/api/v1/finance/summary?year=2026&month=8");
        Assert.True(summary.GetProperty("totalIncome").GetDecimal() >= 4500); Assert.True(summary.GetProperty("totalExpenses").GetDecimal() >= 120);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/finance/transactions/{expenseId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/finance/transactions/{expenseId}")).StatusCode);
    }

    [PostgreSqlFact] public async Task Transactions_and_custom_categories_are_isolated_between_users()
    {
        using var factory = Factory()!; var userA = await AuthenticatedClient(factory); var userB = await AuthenticatedClient(factory);
        var categories = await userA.GetFromJsonAsync<JsonElement>("/api/v1/finance/categories"); var food = Category(categories, "Alimentação");
        var created = await userA.PostAsJsonAsync("/api/v1/finance/transactions", Payload("Expense", food, "Dado privado", 99)); Assert.True(created.IsSuccessStatusCode, await created.Content.ReadAsStringAsync());
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await userB.GetAsync($"/api/v1/finance/transactions/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await userB.PutAsJsonAsync($"/api/v1/finance/transactions/{id}", Payload("Expense", food, "Ataque", 1))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await userB.DeleteAsync($"/api/v1/finance/transactions/{id}")).StatusCode);
        var marker = $"Privada-{Guid.NewGuid():N}"; (await userA.PostAsJsonAsync("/api/v1/finance/categories", new { name=marker,type="Expense",icon=(string?)null })).EnsureSuccessStatusCode();
        var categoriesB = await userB.GetStringAsync("/api/v1/finance/categories"); Assert.DoesNotContain(marker, categoriesB);
    }

    private static async Task<HttpClient> AuthenticatedClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient(); var email = $"finance-{Guid.NewGuid():N}@example.test"; const string password="Strong!Pass123";
        (await client.PostAsJsonAsync("/api/v1/auth/register", new { displayName="Finance Test",email,password })).EnsureSuccessStatusCode();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email,password }); login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString(); client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token); return client;
    }
    private static Guid Category(JsonElement categories, string name) => categories.EnumerateArray().Single(x=>x.GetProperty("name").GetString()==name).GetProperty("id").GetGuid();
    private static object Payload(string type, Guid categoryId, string description, decimal amount) => new { type,categoryId,description,amount,transactionDate="2026-08-15",paymentMethod="Pix",isRecurring=false,recurrenceEndDate=(string?)null,notes=(string?)null };
}

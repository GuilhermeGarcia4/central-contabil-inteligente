using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Finance.Domain;
using CentralContabil.Api.Modules.Shared.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CentralContabil.IntegrationTests;

public sealed class FinanceV5ApiTests
{
    [PostgreSqlFact]
    public async Task Installment_purchase_creates_exact_monthly_transactions_and_real_xlsx()
    {
        var connection = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connection)) return;
        Environment.SetEnvironmentVariable("CONNECTION_STRING", connection);
        Environment.SetEnvironmentVariable("JWT_KEY", "integration-test-key-with-more-than-32-characters");
        Environment.SetEnvironmentVariable("ADMIN_RESET_PASSWORD_ON_START", "false");
        using var factory = new WebApplicationFactory<Program>();
        var email = $"finance-v5-{Guid.NewGuid():N}@example.test"; const string password = "Strong!Pass123"; Guid userId;
        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = email, Email = email, DisplayName = "Finance V5" };
            Assert.True((await users.CreateAsync(user, password)).Succeeded); userId = user.Id;
        }
        var client = factory.CreateClient(); var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password }); login.EnsureSuccessStatusCode();
        var loginJson = await login.Content.ReadFromJsonAsync<JsonElement>(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginJson.GetProperty("accessToken").GetString());
        var categories = await client.GetFromJsonAsync<JsonElement>("/api/v1/finance/categories");
        var expenseCategory = categories.EnumerateArray().First(x => x.GetProperty("type").GetString() == "Expense").GetProperty("id").GetGuid();
        var created = await client.PostAsJsonAsync("/api/v1/finance/transactions", new { type="Expense", categoryId=expenseCategory, description="Notebook V5", amount=3600m, transactionDate="2026-09-10", paymentMethod="CreditCard", isRecurring=false, recurrenceEndDate=(string?)null, notes="teste", isInstallment=true, installmentCount=12, firstInstallmentDate="2026-09-10" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var rows = await scope.ServiceProvider.GetRequiredService<AppDbContext>().FinancialTransactions.AsNoTracking().Where(x => x.UserId == userId && x.Description == "Notebook V5").OrderBy(x => x.InstallmentNumber).ToListAsync();
            Assert.Equal(12, rows.Count); Assert.Equal(3600m, rows.Sum(x => x.Amount)); Assert.All(rows, x => Assert.Equal(300m, x.Amount)); Assert.Equal(new DateOnly(2027, 8, 10), rows[^1].TransactionDate);
        }
        var export = await client.GetAsync("/api/v1/finance/export/excel?startDate=2026-09-01&endDate=2027-08-31"); export.EnsureSuccessStatusCode();
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", export.Content.Headers.ContentType?.MediaType);
        Assert.True((await export.Content.ReadAsByteArrayAsync()).Length > 1000);
    }
}

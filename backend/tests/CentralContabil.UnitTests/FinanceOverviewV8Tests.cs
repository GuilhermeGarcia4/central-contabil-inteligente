using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Finance.Application;
using CentralContabil.Api.Modules.Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.UnitTests;

public sealed class FinanceOverviewV8Tests
{
    [Fact]
    public async Task Overview_aggregates_period_categories_percentages_comparison_and_planning()
    {
        await using var db = Db();
        var userId = Guid.NewGuid();
        var salary = await db.FinancialCategories.SingleAsync(item => item.Name == "Salário");
        var food = await db.FinancialCategories.SingleAsync(item => item.Name == "Alimentação");
        var leisure = await db.FinancialCategories.SingleAsync(item => item.Name == "Lazer");
        db.FinancialTransactions.AddRange(
            Tx(userId, salary, FinancialTransactionType.Income, 4500m, new(2026, 8, 5)),
            Tx(userId, food, FinancialTransactionType.Expense, 800m, new(2026, 8, 10)),
            Tx(userId, leisure, FinancialTransactionType.Expense, 200m, new(2026, 8, 11)),
            Tx(userId, salary, FinancialTransactionType.Income, 4000m, new(2026, 7, 5)),
            Tx(userId, food, FinancialTransactionType.Expense, 700m, new(2026, 7, 10)));
        db.MonthlyBudgets.Add(new MonthlyBudget
            { UserId = userId, Category = food, CategoryId = food.Id, Year = 2026, Month = 8, PlannedAmount = 900m });
        await db.SaveChangesAsync();

        var result = await new FinanceOverviewService(db).GetAsync(userId, new(2026, 8, 1), new(2026, 8, 31), default);

        Assert.Equal(4500m, result.TotalEntries);
        Assert.Equal(1000m, result.TotalExits);
        Assert.Equal(3500m, result.Balance);
        Assert.Equal(80m, result.ExitsByCategory[0].Percentage);
        Assert.Equal(100m, Assert.Single(result.EntriesByCategory).Percentage);
        Assert.Equal(500m, result.Comparison.IncomeDifference);
        Assert.Equal(300m, result.Comparison.ExpenseDifference);
        Assert.Equal(900m, result.Planning.Planned);
        Assert.Equal(800m, result.Planning.Used);
        Assert.Single(result.Evolution);
    }

    [Fact]
    public async Task Overview_isolates_users_and_applies_only_their_color_preference()
    {
        await using var db = Db();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var food = await db.FinancialCategories.SingleAsync(item => item.Name == "Alimentação");
        db.FinancialTransactions.AddRange(
            Tx(userA, food, FinancialTransactionType.Expense, 100m, new(2026, 9, 1)),
            Tx(userB, food, FinancialTransactionType.Expense, 900m, new(2026, 9, 1)));
        db.FinancialCategoryChartPreferences.Add(new FinancialCategoryChartPreference
            { UserId = userA, Category = food, CategoryId = food.Id, ChartColor = "#8A1455" });
        await db.SaveChangesAsync();

        var service = new FinanceOverviewService(db);
        var first = await service.GetAsync(userA, new(2026, 9, 1), new(2026, 9, 30), default);
        var second = await service.GetAsync(userB, new(2026, 9, 1), new(2026, 9, 30), default);

        Assert.Equal(100m, first.TotalExits);
        Assert.Equal("#8A1455", Assert.Single(first.ExitsByCategory).Color);
        Assert.True(first.ExitsByCategory[0].HasCustomColor);
        Assert.Equal(900m, second.TotalExits);
        Assert.False(Assert.Single(second.ExitsByCategory).HasCustomColor);
        Assert.NotEqual("#8A1455", second.ExitsByCategory[0].Color);
    }

    [Fact]
    public async Task Overview_without_transactions_returns_safe_empty_collections()
    {
        await using var db = Db();

        var result = await new FinanceOverviewService(db).GetAsync(Guid.NewGuid(), new(2026, 1, 1), new(2026, 3, 31), default);

        Assert.Equal(0, result.TotalEntries);
        Assert.Equal(0, result.TotalExits);
        Assert.Empty(result.EntriesByCategory);
        Assert.Empty(result.ExitsByCategory);
        Assert.Equal(3, result.Evolution.Count);
    }

    [Theory]
    [InlineData("#8A1455", true)]
    [InlineData("#ffffff", true)]
    [InlineData("red", false)]
    [InlineData("#FFF", false)]
    [InlineData("red; background:url(x)", false)]
    public void Chart_color_accepts_only_safe_hex(string value, bool expected) =>
        Assert.Equal(expected, FinancialChartPalette.IsValid(value));

    private static AppDbContext Db()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Database.EnsureCreated();
        return db;
    }

    private static FinancialTransaction Tx(Guid userId, FinancialCategory category, FinancialTransactionType type,
        decimal amount, DateOnly date) => new()
    {
        UserId = userId,
        Category = category,
        CategoryId = category.Id,
        Type = type,
        Description = category.Name,
        Amount = amount,
        TransactionDate = date,
        PaymentMethod = FinancialPaymentMethod.Pix
    };
}

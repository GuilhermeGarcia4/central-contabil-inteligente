using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Finance.Application;
using CentralContabil.Api.Modules.Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.UnitTests;

public sealed class FinanceTests
{
    [Theory]
    [InlineData("Comprar ração para o cachorro", "Pets")]
    [InlineData("RAÇÃO DO GATO!!!", "Pets")]
    [InlineData("mensalidade da faculdade", "Educação")]
    [InlineData("uber até o trabalho", "Transporte")]
    [InlineData("compra no supermercado", "Alimentação")]
    [InlineData("netflix", "Assinaturas")]
    [InlineData("consulta dentista", "Saúde")]
    [InlineData("conta de energia", "Contas")]
    [InlineData("presente de aniversário", "Presentes")]
    [InlineData("salário agosto", "Salário")]
    public async Task Classifier_recognizes_expected_category(string text, string category)
    {
        await using var db = CreateDb(); var userId = Guid.NewGuid();
        var result = await new RuleBasedCategorySuggestionService(db).SuggestAsync(userId,
            category == "Salário" ? FinancialTransactionType.Income : FinancialTransactionType.Expense, text, null, default);
        Assert.Equal(category, result[0].CategoryName); Assert.True(result[0].Confidence >= .75m);
    }

    [Fact] public async Task Classifier_returns_nothing_for_empty_or_unknown_text()
    {
        await using var db = CreateDb(); var service = new RuleBasedCategorySuggestionService(db); var userId = Guid.NewGuid();
        Assert.Empty(await service.SuggestAsync(userId, FinancialTransactionType.Expense, "", null, default));
        Assert.Empty(await service.SuggestAsync(userId, FinancialTransactionType.Expense, "xyz sem correspondencia", null, default));
    }

    [Fact] public async Task User_preference_overrides_keyword_without_affecting_another_user()
    {
        await using var db = CreateDb(); var first = Guid.NewGuid(); var second = Guid.NewGuid();
        var pets = await db.FinancialCategories.SingleAsync(x => x.Name == "Pets"); var leisure = await db.FinancialCategories.SingleAsync(x => x.Name == "Lazer");
        var service = new RuleBasedCategorySuggestionService(db); await service.RememberAsync(first, FinancialTransactionType.Expense, "Netflix", leisure.Id, default); await db.SaveChangesAsync();
        Assert.Equal("Lazer", (await service.SuggestAsync(first, FinancialTransactionType.Expense, "Netflix", null, default))[0].CategoryName);
        Assert.Equal("Assinaturas", (await service.SuggestAsync(second, FinancialTransactionType.Expense, "Netflix", null, default))[0].CategoryName);
        Assert.NotEqual(pets.Id, leisure.Id);
    }

    [Fact] public async Task Summary_calculates_totals_groups_commitment_and_previous_month()
    {
        await using var db = CreateDb(); var userId = Guid.NewGuid();
        var salary = await db.FinancialCategories.SingleAsync(x => x.Name == "Salário"); var food = await db.FinancialCategories.SingleAsync(x => x.Name == "Alimentação");
        db.FinancialTransactions.AddRange(
            Tx(userId, salary.Id, FinancialTransactionType.Income, 4500, new(2026,8,5), "Salário"),
            Tx(userId, food.Id, FinancialTransactionType.Expense, 650, new(2026,8,10), "Mercado"),
            Tx(userId, food.Id, FinancialTransactionType.Expense, 350, new(2026,8,11), "Padaria"),
            Tx(userId, salary.Id, FinancialTransactionType.Income, 4000, new(2026,7,5), "Salário anterior"),
            Tx(userId, food.Id, FinancialTransactionType.Expense, 800, new(2026,7,10), "Mercado anterior"));
        await db.SaveChangesAsync(); var summary = await new FinanceSummaryService(db).GetAsync(userId, 2026, 8, default);
        Assert.Equal(4500, summary.TotalIncome); Assert.Equal(1000, summary.TotalExpenses); Assert.Equal(3500, summary.Balance);
        Assert.Equal(22.22m, summary.IncomeCommitmentPercentage); Assert.Single(summary.ExpensesByCategory); Assert.Equal(1000, summary.ExpensesByCategory[0].Amount); Assert.Equal(100, summary.ExpensesByCategory[0].Percentage);
        Assert.Equal(12.5m, summary.Comparison.IncomePercentage); Assert.Equal(25m, summary.Comparison.ExpensePercentage);
    }

    [Fact] public async Task Summary_handles_month_without_income_and_empty_month()
    {
        await using var db = CreateDb(); var userId = Guid.NewGuid(); var food = await db.FinancialCategories.SingleAsync(x => x.Name == "Alimentação");
        db.FinancialTransactions.Add(Tx(userId, food.Id, FinancialTransactionType.Expense, 100, new(2026,8,1), "Mercado")); await db.SaveChangesAsync();
        var service = new FinanceSummaryService(db); var august = await service.GetAsync(userId, 2026, 8, default); var september = await service.GetAsync(userId, 2026, 9, default);
        Assert.Null(august.IncomeCommitmentPercentage); Assert.Equal(-100, august.Balance); Assert.Equal(1, august.TransactionCount);
        Assert.Equal(0, september.TotalIncome); Assert.Equal(0, september.TotalExpenses); Assert.Empty(september.ExpensesByCategory); Assert.Null(september.Comparison.IncomePercentage);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options); db.Database.EnsureCreated(); return db;
    }
    private static FinancialTransaction Tx(Guid userId, Guid categoryId, FinancialTransactionType type, decimal amount, DateOnly date, string description) => new() { UserId=userId, CategoryId=categoryId, Type=type, Amount=amount, TransactionDate=date, Description=description, PaymentMethod=FinancialPaymentMethod.Pix };
}

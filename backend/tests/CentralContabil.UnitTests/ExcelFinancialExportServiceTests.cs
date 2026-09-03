using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Finance.Application;
using CentralContabil.Api.Modules.Finance.Domain;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.UnitTests;

public sealed class ExcelFinancialExportServiceTests
{
    [Fact]
    public async Task Creates_real_workbook_with_numeric_values_dates_and_user_isolation()
    {
        await using var db = Db(); var userA = Guid.NewGuid(); var userB = Guid.NewGuid();
        var income = Category("Salário", FinancialTransactionType.Income); var expense = Category("Compras", FinancialTransactionType.Expense); db.AddRange(income, expense);
        db.FinancialTransactions.AddRange(
            Transaction(userA, income, "Entrada A", 4000m, new(2026, 9, 5), FinancialTransactionType.Income),
            Transaction(userA, expense, "Notebook A", 300m, new(2026, 9, 10), FinancialTransactionType.Expense),
            Transaction(userB, expense, "SEGREDO-USUARIO-B", 999m, new(2026, 9, 10), FinancialTransactionType.Expense));
        await db.SaveChangesAsync();
        var bytes = await new ExcelFinancialExportService(db, new FinanceSummaryService(db)).ExportAsync(userA,
            new(new(2026, 9, 1), new(2026, 9, 30), null, null, null, null, null, null), default);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        Assert.Equal(["Resumo", "Entradas", "Saídas", "Todos os lançamentos"], workbook.Worksheets.Select(x => x.Name).ToArray());
        Assert.Equal(XLDataType.DateTime, workbook.Worksheet("Entradas").Cell("A2").DataType);
        Assert.Equal(XLDataType.Number, workbook.Worksheet("Entradas").Cell("E2").DataType);
        Assert.Equal(4000d, workbook.Worksheet("Entradas").Cell("E2").GetDouble());
        Assert.DoesNotContain("SEGREDO-USUARIO-B", string.Join(' ', workbook.Worksheets.SelectMany(x => x.CellsUsed()).Select(x => x.GetString())));
    }

    [Fact]
    public async Task Exports_empty_month_with_all_expected_sheets()
    {
        await using var db = Db(); var bytes = await new ExcelFinancialExportService(db, new FinanceSummaryService(db)).ExportAsync(Guid.NewGuid(), new(new(2026, 10, 1), new(2026, 10, 31), null, null, null, null, null, null), default);
        using var workbook = new XLWorkbook(new MemoryStream(bytes)); Assert.Equal(4, workbook.Worksheets.Count); Assert.True(bytes.Length > 1000);
    }

    private static FinancialCategory Category(string name, FinancialTransactionType type) => new() { Name=name, Type=type, IsActive=true };
    private static FinancialTransaction Transaction(Guid user, FinancialCategory category, string description, decimal amount, DateOnly date, FinancialTransactionType type) => new() { UserId=user, Category=category, CategoryId=category.Id, Description=description, Amount=amount, TransactionDate=date, Type=type, PaymentMethod=FinancialPaymentMethod.BankTransfer };
    private static AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}

using System.Globalization;
using ClosedXML.Excel;
using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.Api.Modules.Finance.Application;

public sealed class FinancialInstallmentService(AppDbContext db)
{
    public static IReadOnlyList<decimal> Split(decimal total, int count)
    {
        if (total <= 0 || count < 2) throw new ArgumentOutOfRangeException(nameof(count));
        var cents = decimal.ToInt64(decimal.Round(total, 2, MidpointRounding.AwayFromZero) * 100);
        var baseCents = cents / count; var remainder = cents % count;
        return Enumerable.Range(0, count).Select(index => (baseCents + (index < remainder ? 1 : 0)) / 100m).ToArray();
    }

    public async Task<FinancialInstallmentPlan> CreateAsync(Guid userId, Guid categoryId, string description, decimal total,
        int count, DateOnly firstDueDate, FinancialPaymentMethod paymentMethod, string? notes, CancellationToken ct, Guid? creditCardId = null, Guid? accountId = null)
    {
        if (paymentMethod != FinancialPaymentMethod.CreditCard) throw new ArgumentException("Parcelamento está disponível apenas para saídas no cartão de crédito.");
        var plan = new FinancialInstallmentPlan { UserId = userId, CategoryId = categoryId, Description = description.Trim(), TotalAmount = decimal.Round(total, 2), InstallmentCount = count, FirstDueDate = firstDueDate, PaymentMethod = paymentMethod };
        db.FinancialInstallmentPlans.Add(plan);
        var values = Split(total, count);
        for (var index = 0; index < count; index++) db.FinancialTransactions.Add(new FinancialTransaction
        {
            UserId = userId, Type = FinancialTransactionType.Expense, CategoryId = categoryId, Description = description.Trim(),
            Amount = values[index], TransactionDate = firstDueDate.AddMonths(index), PaymentMethod = paymentMethod,
            InstallmentPlanId = plan.Id, InstallmentNumber = index + 1, InstallmentCount = count, Notes = notes, CreditCardId = creditCardId, AccountId = accountId
        });
        await db.SaveChangesAsync(ct); return plan;
    }
}

public sealed record ForecastSummary(int Year, int Month, decimal ExpectedIncome, decimal ExpectedExpenses, decimal ExpectedBalance, string Disclaimer);
public sealed class FinanceForecastService(AppDbContext db)
{
    public async Task<ForecastSummary> GetAsync(Guid userId, int year, int month, CancellationToken ct)
    {
        var start = new DateOnly(year, month, 1); var end = start.AddMonths(1);
        var known = await db.FinancialTransactions.AsNoTracking().Where(x => x.UserId == userId && x.TransactionDate >= start && x.TransactionDate < end)
            .Select(x => new { x.Type, x.Amount, x.RecurrenceId }).ToListAsync(ct);
        var income = known.Where(x => x.Type == FinancialTransactionType.Income).Sum(x => x.Amount);
        var expenses = known.Where(x => x.Type == FinancialTransactionType.Expense).Sum(x => x.Amount);
        var materializedRecurrences = known.Where(x => x.RecurrenceId != null).Select(x => x.RecurrenceId!.Value).ToHashSet();
        var recurrences = await db.FinancialRecurrences.AsNoTracking().Where(x => x.UserId == userId && x.IsActive && x.StartDate < end && (x.EndDate == null || x.EndDate >= start)).ToListAsync(ct);
        foreach (var item in recurrences.Where(x => !materializedRecurrences.Contains(x.Id))) if (item.Type == FinancialTransactionType.Income) income += item.Amount; else expenses += item.Amount;
        return new(year, month, income, expenses, income - expenses, "Estimativa baseada em recorrências, parcelas e lançamentos já cadastrados.");
    }
}

public sealed record FinancialExportFilter(DateOnly StartDate, DateOnly EndDate, FinancialTransactionType? Type, Guid? CategoryId,
    FinancialPaymentMethod? PaymentMethod, string? Search, decimal? MinAmount, decimal? MaxAmount);
public interface IFinancialExportService { Task<byte[]> ExportAsync(Guid userId, FinancialExportFilter filter, CancellationToken ct); }

public sealed class ExcelFinancialExportService(AppDbContext db, FinanceSummaryService summaries) : IFinancialExportService
{
    public async Task<byte[]> ExportAsync(Guid userId, FinancialExportFilter filter, CancellationToken ct)
    {
        var endExclusive = filter.EndDate.AddDays(1);
        var query = db.FinancialTransactions.AsNoTracking().Where(x => x.UserId == userId && x.TransactionDate >= filter.StartDate && x.TransactionDate < endExclusive);
        if (filter.Type is not null) query = query.Where(x => x.Type == filter.Type);
        if (filter.CategoryId is not null) query = query.Where(x => x.CategoryId == filter.CategoryId);
        if (filter.PaymentMethod is not null) query = query.Where(x => x.PaymentMethod == filter.PaymentMethod);
        if (filter.MinAmount is not null) query = query.Where(x => x.Amount >= filter.MinAmount);
        if (filter.MaxAmount is not null) query = query.Where(x => x.Amount <= filter.MaxAmount);
        if (!string.IsNullOrWhiteSpace(filter.Search)) query = query.Where(x => EF.Functions.ILike(x.Description, $"%{filter.Search.Trim()}%") || x.Notes != null && EF.Functions.ILike(x.Notes, $"%{filter.Search.Trim()}%"));
        var items = await query.OrderBy(x => x.TransactionDate).Select(x => new ExportRow(x.TransactionDate, x.Description, x.Category.Name, x.PaymentMethod, x.Amount, x.Notes, x.Type, x.InstallmentNumber, x.InstallmentCount)).ToListAsync(ct);

        using var workbook = new XLWorkbook(); var summarySheet = workbook.AddWorksheet("Resumo");
        var sameMonth = filter.StartDate.Year == filter.EndDate.Year && filter.StartDate.Month == filter.EndDate.Month;
        var summary = sameMonth ? await summaries.GetAsync(userId, filter.StartDate.Year, filter.StartDate.Month, ct) : null;
        summarySheet.Cell("A1").Value = "Controle Financeiro"; summarySheet.Cell("A1").Style.Font.Bold = true; summarySheet.Cell("A1").Style.Font.FontSize = 18;
        summarySheet.Cell("A2").Value = $"{filter.StartDate:dd/MM/yyyy} a {filter.EndDate:dd/MM/yyyy}";
        var totalIncome = items.Where(x => x.Type == FinancialTransactionType.Income).Sum(x => x.Amount); var totalExpense = items.Where(x => x.Type == FinancialTransactionType.Expense).Sum(x => x.Amount);
        AddSummaryValue(summarySheet, 4, "Total de entradas", totalIncome); AddSummaryValue(summarySheet, 5, "Total de saídas", totalExpense); AddSummaryValue(summarySheet, 6, "Quanto sobrou", totalIncome - totalExpense);
        summarySheet.Cell(7, 1).Value = "Percentual utilizado"; if (totalIncome > 0) { summarySheet.Cell(7, 2).Value = totalExpense / totalIncome; summarySheet.Cell(7, 2).Style.NumberFormat.Format = "0.0%"; }
        var row = 10; summarySheet.Cell(row++, 1).Value = "Saídas por categoria"; summarySheet.Cell(row - 1, 1).Style.Font.Bold = true;
        foreach (var group in items.Where(x => x.Type == FinancialTransactionType.Expense).GroupBy(x => x.Category).OrderByDescending(x => x.Sum(y => y.Amount))) { summarySheet.Cell(row, 1).Value = group.Key; summarySheet.Cell(row, 2).Value = group.Sum(x => x.Amount); summarySheet.Cell(row++, 2).Style.NumberFormat.Format = "[$R$-pt-BR] #,##0.00"; }
        summarySheet.Columns().AdjustToContents();
        AddTransactionsSheet(workbook, "Entradas", items.Where(x => x.Type == FinancialTransactionType.Income));
        AddTransactionsSheet(workbook, "Saídas", items.Where(x => x.Type == FinancialTransactionType.Expense));
        AddTransactionsSheet(workbook, "Todos os lançamentos", items);
        using var stream = new MemoryStream(); workbook.SaveAs(stream); return stream.ToArray();
    }

    private static void AddSummaryValue(IXLWorksheet sheet, int row, string label, decimal value) { sheet.Cell(row, 1).Value = label; sheet.Cell(row, 2).Value = value; sheet.Cell(row, 2).Style.NumberFormat.Format = "[$R$-pt-BR] #,##0.00"; }
    private static void AddTransactionsSheet(XLWorkbook workbook, string name, IEnumerable<ExportRow> source)
    {
        var sheet = workbook.AddWorksheet(name); string[] headers = ["Data", "Descrição", "Categoria", "Forma", "Valor", "Observações", "Parcela"];
        for (var column = 1; column <= headers.Length; column++) { sheet.Cell(1, column).Value = headers[column - 1]; sheet.Cell(1, column).Style.Font.Bold = true; sheet.Cell(1, column).Style.Fill.BackgroundColor = XLColor.FromHtml("#660240"); sheet.Cell(1, column).Style.Font.FontColor = XLColor.FromHtml("#FFFD74"); }
        var row = 2; foreach (var item in source) { sheet.Cell(row, 1).Value = item.Date.ToDateTime(TimeOnly.MinValue); sheet.Cell(row, 1).Style.DateFormat.Format = "dd/mm/yyyy"; sheet.Cell(row, 2).Value = item.Description; sheet.Cell(row, 3).Value = item.Category; sheet.Cell(row, 4).Value = PaymentLabel(item.PaymentMethod); sheet.Cell(row, 5).Value = item.Amount; sheet.Cell(row, 5).Style.NumberFormat.Format = "[$R$-pt-BR] #,##0.00"; sheet.Cell(row, 6).Value = item.Notes ?? ""; sheet.Cell(row, 7).Value = item.InstallmentNumber is null ? "" : $"{item.InstallmentNumber}/{item.InstallmentCount}"; row++; }
        var range = sheet.Range(1, 1, Math.Max(1, row - 1), headers.Length); range.SetAutoFilter(); sheet.SheetView.FreezeRows(1); sheet.Columns().AdjustToContents();
    }
    private static string PaymentLabel(FinancialPaymentMethod value) => value switch { FinancialPaymentMethod.Cash => "Dinheiro", FinancialPaymentMethod.Pix => "Pix", FinancialPaymentMethod.DebitCard => "Cartão de débito", FinancialPaymentMethod.CreditCard => "Cartão de crédito", FinancialPaymentMethod.BankTransfer => "Transferência bancária", FinancialPaymentMethod.Boleto => "Boleto", _ => "Outro" };
    private sealed record ExportRow(DateOnly Date, string Description, string Category, FinancialPaymentMethod PaymentMethod, decimal Amount, string? Notes, FinancialTransactionType Type, int? InstallmentNumber, int? InstallmentCount);
}

public sealed record BudgetProgress(Guid Id, Guid CategoryId, string CategoryName, decimal PlannedAmount, decimal UsedAmount, decimal RemainingAmount, decimal Percentage, bool IsExceeded);
public sealed record MonthlyPlanningSummary(int Year, int Month, decimal TotalPlanned, decimal TotalUsed, decimal Available, IReadOnlyList<BudgetProgress> Categories);
public sealed class FinancePlanningService(AppDbContext db)
{
    public async Task<MonthlyPlanningSummary> BudgetsAsync(Guid userId, int year, int month, CancellationToken ct)
    {
        var start = new DateOnly(year, month, 1); var end = start.AddMonths(1);
        var budgets = await db.MonthlyBudgets.AsNoTracking().Where(x => x.UserId == userId && x.Year == year && x.Month == month).Select(x => new { x.Id, x.CategoryId, CategoryName=x.Category.Name, x.PlannedAmount }).ToListAsync(ct);
        var used = await db.FinancialTransactions.AsNoTracking().Where(x => x.UserId == userId && x.Type == FinancialTransactionType.Expense && x.TransactionDate >= start && x.TransactionDate < end).GroupBy(x => x.CategoryId).Select(x => new { CategoryId=x.Key, Amount=x.Sum(y=>y.Amount) }).ToDictionaryAsync(x=>x.CategoryId,x=>x.Amount,ct);
        var rows = budgets.Select(x => { var amount = used.GetValueOrDefault(x.CategoryId); return new BudgetProgress(x.Id,x.CategoryId,x.CategoryName,x.PlannedAmount,amount,x.PlannedAmount-amount,x.PlannedAmount>0?decimal.Round(amount/x.PlannedAmount*100,2):0,amount>x.PlannedAmount); }).ToArray();
        return new(year,month,rows.Sum(x=>x.PlannedAmount),rows.Sum(x=>x.UsedAmount),rows.Sum(x=>x.RemainingAmount),rows);
    }
}

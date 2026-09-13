using System.Text.RegularExpressions;
using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.Api.Modules.Finance.Application;

public static partial class FinancialChartPalette
{
    public static readonly IReadOnlyList<string> Colors =
    [
        "#660240", "#B51F72", "#D9578B", "#8A1455", "#C96B98",
        "#6B5B95", "#397367", "#C17C24", "#355C7D", "#A23B72",
        "#4F6D7A", "#8C6A43", "#7A4EAB", "#2E7D6E"
    ];

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex HexColorPattern();

    public static bool IsValid(string? value) => value is not null && HexColorPattern().IsMatch(value);

    public static string Normalize(string value) => value.ToUpperInvariant();

    public static string Fallback(Guid categoryId)
    {
        Span<byte> bytes = stackalloc byte[16];
        categoryId.TryWriteBytes(bytes);
        var hash = 17;
        foreach (var value in bytes) hash = unchecked(hash * 31 + value);
        return Colors[(hash & int.MaxValue) % Colors.Count];
    }
}

public sealed record FinanceOverviewCategory(Guid CategoryId, string CategoryName, decimal Amount, decimal Percentage,
    string Color, bool HasCustomColor);
public sealed record FinanceOverviewComparison(decimal IncomeDifference, decimal ExpenseDifference, decimal BalanceDifference,
    decimal? IncomePercentage, decimal? ExpensePercentage, decimal? BalancePercentage,
    string? LargestIncreaseCategory, decimal? LargestIncreaseAmount, string? LargestReductionCategory, decimal? LargestReductionAmount);
public sealed record FinanceOverviewMonth(int Year, int Month, decimal Income, decimal Expenses, decimal Balance);
public sealed record FinanceOverviewPlanning(decimal Planned, decimal Used, decimal Remaining, IReadOnlyList<FinanceOverviewBudget> NearLimits);
public sealed record FinanceOverviewBudget(Guid CategoryId, string CategoryName, decimal Planned, decimal Used, decimal Percentage);
public sealed record FinanceOverviewCommitment(Guid Id, string Description, decimal Amount, DateOnly DueDate, string Kind);
public sealed record FinanceOverviewGoal(Guid Id, string Name, decimal CurrentAmount, decimal TargetAmount, decimal Percentage);
public sealed record FinanceOverview(DateOnly StartDate, DateOnly EndDate, decimal TotalEntries, decimal TotalExits, decimal Balance,
    IReadOnlyList<FinanceOverviewCategory> EntriesByCategory, IReadOnlyList<FinanceOverviewCategory> ExitsByCategory,
    FinanceOverviewComparison Comparison, IReadOnlyList<FinanceOverviewMonth> Evolution, FinanceOverviewPlanning Planning,
    IReadOnlyList<FinanceOverviewCommitment> UpcomingCommitments, IReadOnlyList<FinanceOverviewGoal> Goals);

public sealed class FinanceOverviewService(AppDbContext db)
{
    public async Task<FinanceOverview> GetAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken ct)
    {
        var current = db.FinancialTransactions.AsNoTracking()
            .Where(item => item.UserId == userId && item.TransactionDate >= startDate && item.TransactionDate <= endDate);
        var dayCount = endDate.DayNumber - startDate.DayNumber + 1;
        var previousEnd = startDate.AddDays(-1);
        var previousStart = previousEnd.AddDays(-(dayCount - 1));
        var previous = db.FinancialTransactions.AsNoTracking()
            .Where(item => item.UserId == userId && item.TransactionDate >= previousStart && item.TransactionDate <= previousEnd);

        var currentTotals = await TotalsAsync(current, ct);
        var previousTotals = await TotalsAsync(previous, ct);
        var currentGroups = await GroupsAsync(current, ct);
        var previousGroups = await GroupsAsync(previous, ct);
        var categoryIds = currentGroups.Select(item => item.CategoryId).Distinct().ToArray();
        var colors = await db.FinancialCategoryChartPreferences.AsNoTracking()
            .Where(item => item.UserId == userId && categoryIds.Contains(item.CategoryId))
            .ToDictionaryAsync(item => item.CategoryId, item => item.ChartColor, ct);

        var entries = Categories(currentGroups, FinancialTransactionType.Income, currentTotals.Income, colors);
        var exits = Categories(currentGroups, FinancialTransactionType.Expense, currentTotals.Expenses, colors);
        var categoryChanges = currentGroups.Concat(previousGroups)
            .GroupBy(item => new { item.CategoryId, item.CategoryName, item.Type })
            .Select(group => new
            {
                group.Key.CategoryName,
                Difference = currentGroups
                    .Where(item => item.CategoryId == group.Key.CategoryId && item.Type == group.Key.Type).Sum(item => item.Amount) -
                    previousGroups.Where(item => item.CategoryId == group.Key.CategoryId && item.Type == group.Key.Type).Sum(item => item.Amount)
            }).ToArray();
        var increase = categoryChanges.Where(item => item.Difference > 0).OrderByDescending(item => item.Difference).FirstOrDefault();
        var reduction = categoryChanges.Where(item => item.Difference < 0).OrderBy(item => item.Difference).FirstOrDefault();

        var monthlyRows = await current.GroupBy(item => new { item.TransactionDate.Year, item.TransactionDate.Month, item.Type })
            .Select(group => new { group.Key.Year, group.Key.Month, group.Key.Type, Amount = group.Sum(item => item.Amount) })
            .ToListAsync(ct);
        var evolution = Months(startDate, endDate).Select(month =>
        {
            var income = monthlyRows.Where(item => item.Year == month.Year && item.Month == month.Month && item.Type == FinancialTransactionType.Income).Sum(item => item.Amount);
            var expense = monthlyRows.Where(item => item.Year == month.Year && item.Month == month.Month && item.Type == FinancialTransactionType.Expense).Sum(item => item.Amount);
            return new FinanceOverviewMonth(month.Year, month.Month, income, expense, income - expense);
        }).ToArray();

        var budgets = await db.MonthlyBudgets.AsNoTracking().Where(item => item.UserId == userId &&
            (item.Year > startDate.Year || item.Year == startDate.Year && item.Month >= startDate.Month) &&
            (item.Year < endDate.Year || item.Year == endDate.Year && item.Month <= endDate.Month))
            .Select(item => new { item.CategoryId, CategoryName = item.Category.Name, item.PlannedAmount }).ToListAsync(ct);
        var expenseByCategory = currentGroups.Where(item => item.Type == FinancialTransactionType.Expense)
            .ToDictionary(item => item.CategoryId, item => item.Amount);
        var budgetRows = budgets.GroupBy(item => new { item.CategoryId, item.CategoryName }).Select(group =>
        {
            var planned = group.Sum(item => item.PlannedAmount);
            var used = expenseByCategory.GetValueOrDefault(group.Key.CategoryId);
            return new FinanceOverviewBudget(group.Key.CategoryId, group.Key.CategoryName, planned, used,
                planned > 0 ? decimal.Round(used / planned * 100, 2) : 0);
        }).ToArray();
        var totalPlanned = budgetRows.Sum(item => item.Planned);
        var totalUsed = budgetRows.Sum(item => item.Used);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var upcoming = await db.ScheduledTransactions.AsNoTracking()
            .Where(item => item.UserId == userId && item.Status == ScheduledTransactionStatus.Pending && item.DueDate >= today)
            .OrderBy(item => item.DueDate).Take(5)
            .Select(item => new FinanceOverviewCommitment(item.Id, item.Description, item.Amount, item.DueDate, "scheduled"))
            .ToListAsync(ct);
        var goals = await db.FinancialGoals.AsNoTracking().Where(item => item.UserId == userId && !item.IsCompleted)
            .OrderBy(item => item.TargetDate).Take(3)
            .Select(item => new FinanceOverviewGoal(item.Id, item.Name, item.CurrentAmount, item.TargetAmount,
                item.TargetAmount > 0 ? decimal.Round(item.CurrentAmount / item.TargetAmount * 100, 2) : 0))
            .ToListAsync(ct);

        return new FinanceOverview(startDate, endDate, currentTotals.Income, currentTotals.Expenses,
            currentTotals.Income - currentTotals.Expenses, entries, exits,
            new FinanceOverviewComparison(
                currentTotals.Income - previousTotals.Income,
                currentTotals.Expenses - previousTotals.Expenses,
                currentTotals.Balance - previousTotals.Balance,
                Change(currentTotals.Income, previousTotals.Income),
                Change(currentTotals.Expenses, previousTotals.Expenses),
                Change(currentTotals.Balance, previousTotals.Balance),
                increase?.CategoryName, increase?.Difference, reduction?.CategoryName, reduction?.Difference),
            evolution, new FinanceOverviewPlanning(totalPlanned, totalUsed, totalPlanned - totalUsed,
                budgetRows.Where(item => item.Percentage >= 80).OrderByDescending(item => item.Percentage).Take(3).ToArray()),
            upcoming, goals);
    }

    private static async Task<(decimal Income, decimal Expenses, decimal Balance)> TotalsAsync(IQueryable<FinancialTransaction> query, CancellationToken ct)
    {
        var rows = await query.GroupBy(item => item.Type)
            .Select(group => new { Type = group.Key, Amount = group.Sum(item => item.Amount) }).ToListAsync(ct);
        var income = rows.Where(item => item.Type == FinancialTransactionType.Income).Sum(item => item.Amount);
        var expenses = rows.Where(item => item.Type == FinancialTransactionType.Expense).Sum(item => item.Amount);
        return (income, expenses, income - expenses);
    }

    private static async Task<List<CategoryRow>> GroupsAsync(IQueryable<FinancialTransaction> query, CancellationToken ct)
    {
        var rows = await query.GroupBy(item => new { item.CategoryId, item.Category.Name, item.Type })
            .Select(group => new
            {
                group.Key.CategoryId,
                group.Key.Name,
                group.Key.Type,
                Amount = group.Sum(item => item.Amount)
            })
            .OrderByDescending(item => item.Amount)
            .ToListAsync(ct);
        return rows.Select(item => new CategoryRow(item.CategoryId, item.Name, item.Type, item.Amount)).ToList();
    }

    private static IReadOnlyList<FinanceOverviewCategory> Categories(IEnumerable<CategoryRow> groups, FinancialTransactionType type,
        decimal total, IReadOnlyDictionary<Guid, string> colors) => groups.Where(item => item.Type == type)
        .Select(item => new FinanceOverviewCategory(item.CategoryId, item.CategoryName, item.Amount,
            total > 0 ? decimal.Round(item.Amount / total * 100, 2) : 0,
            colors.GetValueOrDefault(item.CategoryId) ?? FinancialChartPalette.Fallback(item.CategoryId),
            colors.ContainsKey(item.CategoryId))).ToArray();

    private static decimal? Change(decimal current, decimal previous) => previous == 0
        ? null : decimal.Round((current - previous) / Math.Abs(previous) * 100, 2);

    private static IEnumerable<(int Year, int Month)> Months(DateOnly start, DateOnly end)
    {
        for (var month = new DateOnly(start.Year, start.Month, 1); month <= end; month = month.AddMonths(1))
            yield return (month.Year, month.Month);
    }

    private sealed record CategoryRow(Guid CategoryId, string CategoryName, FinancialTransactionType Type, decimal Amount);
}

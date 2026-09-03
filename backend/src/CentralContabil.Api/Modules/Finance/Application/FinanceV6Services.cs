using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.Api.Modules.Finance.Application;

public sealed record CreditCardRow(Guid Id, string Name, string? Bank, decimal Limit, int ClosingDay, int DueDay, string? Last4, string? Color, bool IsActive);
public sealed record InvoiceRow(Guid CardId, string CardName, string? CardColor, int Year, int Month, decimal Value, decimal Limit, decimal LimitUsed, decimal LimitAvailable, DateOnly ClosingDate, DateOnly DueDate, int TransactionCount);
public sealed class CreditCardService(AppDbContext db)
{
    public async Task<IReadOnlyList<CreditCardRow>> ListAsync(Guid userId, CancellationToken ct)
        => await db.CreditCards.AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.Name)
            .Select(x => new CreditCardRow(x.Id, x.Name, x.Bank, x.Limit, x.ClosingDay, x.DueDay, x.Last4, x.Color, x.IsActive)).ToListAsync(ct);

    public async Task<CreditCard> CreateAsync(Guid userId, string name, string? bank, decimal limit, int closingDay, int dueDay, string? last4, string? color, CancellationToken ct)
    {
        var card = new CreditCard { UserId = userId, Name = name.Trim(), Bank = Clean(bank), Limit = decimal.Round(limit, 2), ClosingDay = closingDay, DueDay = dueDay, Last4 = Clean(last4), Color = Clean(color) };
        db.CreditCards.Add(card); await db.SaveChangesAsync(ct); return card;
    }

    public async Task<CreditCard?> UpdateAsync(Guid id, Guid userId, string name, string? bank, decimal limit, int closingDay, int dueDay, string? last4, string? color, CancellationToken ct)
    {
        var card = await db.CreditCards.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct); if (card is null) return null;
        card.Name = name.Trim(); card.Bank = Clean(bank); card.Limit = decimal.Round(limit, 2); card.ClosingDay = closingDay; card.DueDay = dueDay; card.Last4 = Clean(last4); card.Color = Clean(color); card.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct); return card;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct)
    {
        var card = await db.CreditCards.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct); if (card is null) return false;
        db.CreditCards.Remove(card); await db.SaveChangesAsync(ct); return true;
    }

    public async Task<IReadOnlyList<InvoiceRow>> InvoicesAsync(Guid userId, int year, int month, CancellationToken ct)
    {
        var cards = await db.CreditCards.AsNoTracking().Where(x => x.UserId == userId && x.IsActive).ToListAsync(ct);
        var rows = new List<InvoiceRow>();
        foreach (var card in cards)
        {
            var (start, end) = InvoicePeriod(card.ClosingDay, year, month);
            var transactions = await db.FinancialTransactions.AsNoTracking().Where(x => x.UserId == userId && x.CreditCardId == card.Id && x.TransactionDate >= start && x.TransactionDate <= end).ToListAsync(ct);
            var value = transactions.Sum(x => x.Amount);
            var closing = new DateOnly(year, month, Math.Min(card.ClosingDay, DateTime.DaysInMonth(year, month)));
            var due = new DateOnly(year, month, Math.Min(card.DueDay, DateTime.DaysInMonth(year, month)));
            rows.Add(new InvoiceRow(card.Id, card.Name, card.Color, year, month, value, card.Limit, value, Math.Max(0, card.Limit - value), closing, due, transactions.Count));
        }
        return rows;
    }

    public static (DateOnly Start, DateOnly End) InvoicePeriod(int closingDay, int year, int month)
    {
        var prev = new DateOnly(year, month, 1).AddMonths(-1);
        var startDay = Math.Min(closingDay + 1, DateTime.DaysInMonth(prev.Year, prev.Month));
        var start = new DateOnly(prev.Year, prev.Month, startDay);
        var endDay = Math.Min(closingDay, DateTime.DaysInMonth(year, month));
        var end = new DateOnly(year, month, endDay);
        return (start, end);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record AccountRow(Guid Id, string Name, FinancialAccountType Type, decimal Balance, string? Color, bool IsActive);
public sealed class FinancialAccountService(AppDbContext db)
{
    public async Task<IReadOnlyList<AccountRow>> ListAsync(Guid userId, CancellationToken ct)
        => await db.FinancialAccounts.AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.Name)
            .Select(x => new AccountRow(x.Id, x.Name, x.Type, x.Balance, x.Color, x.IsActive)).ToListAsync(ct);

    public async Task<FinancialAccount> CreateAsync(Guid userId, string name, FinancialAccountType type, decimal balance, string? color, CancellationToken ct)
    {
        var account = new FinancialAccount { UserId = userId, Name = name.Trim(), Type = type, Balance = decimal.Round(balance, 2), Color = Clean(color) };
        db.FinancialAccounts.Add(account); await db.SaveChangesAsync(ct); return account;
    }

    public async Task<FinancialAccount?> UpdateAsync(Guid id, Guid userId, string name, FinancialAccountType type, decimal balance, string? color, CancellationToken ct)
    {
        var account = await db.FinancialAccounts.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct); if (account is null) return null;
        account.Name = name.Trim(); account.Type = type; account.Balance = decimal.Round(balance, 2); account.Color = Clean(color); account.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct); return account;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct)
    {
        var account = await db.FinancialAccounts.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct); if (account is null) return false;
        db.FinancialAccounts.Remove(account); await db.SaveChangesAsync(ct); return true;
    }

    public async Task<(bool Ok, string? Error)> TransferAsync(Guid userId, Guid fromId, Guid toId, decimal amount, DateOnly date, string? notes, CancellationToken ct)
    {
        if (fromId == toId) return (false, "Escolha duas contas diferentes.");
        if (amount <= 0) return (false, "O valor da transferência deve ser maior que zero.");
        var from = await db.FinancialAccounts.FirstOrDefaultAsync(x => x.Id == fromId && x.UserId == userId, ct);
        var to = await db.FinancialAccounts.FirstOrDefaultAsync(x => x.Id == toId && x.UserId == userId, ct);
        if (from is null || to is null) return (false, "Conta de origem ou destino não encontrada.");
        if (from.Balance < amount) return (false, "Saldo insuficiente na conta de origem.");
        from.Balance -= amount; to.Balance += amount; from.UpdatedAt = DateTimeOffset.UtcNow; to.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct); return (true, null);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record ScheduledRow(Guid Id, FinancialTransactionType Type, Guid CategoryId, string CategoryName, string Description, decimal Amount, DateOnly DueDate, ScheduledTransactionStatus Status, Guid? AccountId, string? AccountName, string? Notes);
public sealed class ScheduledTransactionService(AppDbContext db)
{
    public async Task<IReadOnlyList<ScheduledRow>> ListAsync(Guid userId, int? year, int? month, ScheduledTransactionStatus? status, CancellationToken ct)
    {
        var query = db.ScheduledTransactions.AsNoTracking().Where(x => x.UserId == userId);
        if (year is not null && month is not null) { var start = new DateOnly(year.Value, month.Value, 1); var end = start.AddMonths(1); query = query.Where(x => x.DueDate >= start && x.DueDate < end); }
        if (status is not null) query = query.Where(x => x.Status == status);
        return await query.OrderBy(x => x.DueDate).Select(x => new ScheduledRow(x.Id, x.Type, x.CategoryId, x.Category.Name, x.Description, x.Amount, x.DueDate, x.Status, x.AccountId, x.Account != null ? x.Account.Name : null, x.Notes)).ToListAsync(ct);
    }

    public async Task<ScheduledTransaction> CreateAsync(Guid userId, FinancialTransactionType type, Guid categoryId, string description, decimal amount, DateOnly dueDate, Guid? accountId, string? notes, CancellationToken ct)
    {
        var item = new ScheduledTransaction { UserId = userId, Type = type, CategoryId = categoryId, Description = description.Trim(), Amount = decimal.Round(amount, 2), DueDate = dueDate, Status = type == FinancialTransactionType.Income ? ScheduledTransactionStatus.Pending : ScheduledTransactionStatus.Pending, AccountId = accountId, Notes = Clean(notes) };
        db.ScheduledTransactions.Add(item); await db.SaveChangesAsync(ct); return item;
    }

    public async Task<ScheduledTransaction?> UpdateAsync(Guid id, Guid userId, FinancialTransactionType type, Guid categoryId, string description, decimal amount, DateOnly dueDate, Guid? accountId, string? notes, CancellationToken ct)
    {
        var item = await db.ScheduledTransactions.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct); if (item is null) return null;
        item.Type = type; item.CategoryId = categoryId; item.Description = description.Trim(); item.Amount = decimal.Round(amount, 2); item.DueDate = dueDate; item.AccountId = accountId; item.Notes = Clean(notes); item.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct); return item;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct)
    {
        var item = await db.ScheduledTransactions.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct); if (item is null) return false;
        db.ScheduledTransactions.Remove(item); await db.SaveChangesAsync(ct); return true;
    }

    public async Task<ScheduledTransaction?> SetStatusAsync(Guid id, Guid userId, ScheduledTransactionStatus status, CancellationToken ct)
    {
        var item = await db.ScheduledTransactions.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct); if (item is null) return null;
        item.Status = status; item.UpdatedAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return item;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record DebtRow(Guid Id, string Name, decimal OriginalAmount, decimal CurrentAmount, int? InstallmentCount, decimal? InterestRate, DateOnly? DueDate, string? Institution, string? Notes, bool IsPaid);
public sealed class DebtService(AppDbContext db)
{
    public async Task<IReadOnlyList<DebtRow>> ListAsync(Guid userId, CancellationToken ct)
        => await db.Debts.AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.IsPaid).ThenBy(x => x.DueDate)
            .Select(x => new DebtRow(x.Id, x.Name, x.OriginalAmount, x.CurrentAmount, x.InstallmentCount, x.InterestRate, x.DueDate, x.Institution, x.Notes, x.IsPaid)).ToListAsync(ct);

    public async Task<Debt> CreateAsync(Guid userId, string name, decimal originalAmount, decimal currentAmount, int? installmentCount, decimal? interestRate, DateOnly? dueDate, string? institution, string? notes, CancellationToken ct)
    {
        var debt = new Debt { UserId = userId, Name = name.Trim(), OriginalAmount = decimal.Round(originalAmount, 2), CurrentAmount = decimal.Round(currentAmount, 2), InstallmentCount = installmentCount, InterestRate = interestRate, DueDate = dueDate, Institution = Clean(institution), Notes = Clean(notes), IsPaid = currentAmount <= 0 };
        db.Debts.Add(debt); await db.SaveChangesAsync(ct); return debt;
    }

    public async Task<Debt?> UpdateAsync(Guid id, Guid userId, string name, decimal originalAmount, decimal currentAmount, int? installmentCount, decimal? interestRate, DateOnly? dueDate, string? institution, string? notes, CancellationToken ct)
    {
        var debt = await db.Debts.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct); if (debt is null) return null;
        debt.Name = name.Trim(); debt.OriginalAmount = decimal.Round(originalAmount, 2); debt.CurrentAmount = decimal.Round(currentAmount, 2); debt.InstallmentCount = installmentCount; debt.InterestRate = interestRate; debt.DueDate = dueDate; debt.Institution = Clean(institution); debt.Notes = Clean(notes); debt.IsPaid = currentAmount <= 0; debt.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct); return debt;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct)
    {
        var debt = await db.Debts.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct); if (debt is null) return false;
        db.Debts.Remove(debt); await db.SaveChangesAsync(ct); return true;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record ReserveRow(decimal TargetAmount, decimal CurrentAmount, int? TargetMonths, string? Notes, decimal ProgressPercentage, decimal Remaining);
public sealed class EmergencyReserveService(AppDbContext db)
{
    public async Task<ReserveRow> GetAsync(Guid userId, CancellationToken ct)
    {
        var reserve = await db.EmergencyReserves.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (reserve is null) return new(0, 0, null, null, 0, 0);
        return new(reserve.TargetAmount, reserve.CurrentAmount, reserve.TargetMonths, reserve.Notes, reserve.TargetAmount > 0 ? decimal.Round(reserve.CurrentAmount / reserve.TargetAmount * 100, 2) : 0, Math.Max(0, reserve.TargetAmount - reserve.CurrentAmount));
    }

    public async Task<ReserveRow> UpsertAsync(Guid userId, decimal targetAmount, decimal currentAmount, int? targetMonths, string? notes, CancellationToken ct)
    {
        var reserve = await db.EmergencyReserves.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (reserve is null) { reserve = new EmergencyReserve { UserId = userId, TargetAmount = decimal.Round(targetAmount, 2), CurrentAmount = decimal.Round(currentAmount, 2), TargetMonths = targetMonths, Notes = Clean(notes) }; db.EmergencyReserves.Add(reserve); }
        else { reserve.TargetAmount = decimal.Round(targetAmount, 2); reserve.CurrentAmount = decimal.Round(currentAmount, 2); reserve.TargetMonths = targetMonths; reserve.Notes = Clean(notes); reserve.UpdatedAt = DateTimeOffset.UtcNow; }
        await db.SaveChangesAsync(ct);
        return new(reserve.TargetAmount, reserve.CurrentAmount, reserve.TargetMonths, reserve.Notes, reserve.TargetAmount > 0 ? decimal.Round(reserve.CurrentAmount / reserve.TargetAmount * 100, 2) : 0, Math.Max(0, reserve.TargetAmount - reserve.CurrentAmount));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record NetWorthRow(decimal Assets, decimal Passives, decimal NetWorth, IReadOnlyList<AccountRow> Accounts, decimal Reserve, decimal Debts);
public sealed class NetWorthService(AppDbContext db, FinancialAccountService accounts, EmergencyReserveService reserve)
{
    public async Task<NetWorthRow> GetAsync(Guid userId, CancellationToken ct)
    {
        var accountRows = await accounts.ListAsync(userId, ct);
        var reserveRow = await reserve.GetAsync(userId, ct);
        var debts = await db.Debts.AsNoTracking().Where(x => x.UserId == userId && !x.IsPaid).SumAsync(x => (decimal?)x.CurrentAmount, ct) ?? 0;
        var assets = accountRows.Sum(x => x.Balance) + reserveRow.CurrentAmount;
        return new(assets, debts, assets - debts, accountRows, reserveRow.CurrentAmount, debts);
    }
}

public sealed record CalendarDay(DateOnly Date, IReadOnlyList<CalendarEvent> Events);
public sealed record CalendarEvent(string Kind, string Description, decimal Amount, string? Category, string? Status);
public sealed class FinanceCalendarService(AppDbContext db)
{
    public async Task<IReadOnlyList<CalendarDay>> GetAsync(Guid userId, int year, int month, CancellationToken ct)
    {
        var start = new DateOnly(year, month, 1); var end = start.AddMonths(1);
        var transactions = await db.FinancialTransactions.AsNoTracking().Where(x => x.UserId == userId && x.TransactionDate >= start && x.TransactionDate < end)
            .Select(x => new { x.TransactionDate, x.Description, x.Amount, x.Type, Category = x.Category.Name }).ToListAsync(ct);
        var scheduled = await db.ScheduledTransactions.AsNoTracking().Where(x => x.UserId == userId && x.DueDate >= start && x.DueDate < end)
            .Select(x => new { x.DueDate, x.Description, x.Amount, x.Type, Category = x.Category.Name, x.Status }).ToListAsync(ct);
        var cards = await db.CreditCards.AsNoTracking().Where(x => x.UserId == userId && x.IsActive).ToListAsync(ct);
        var invoices = new List<(DateOnly Date, string Description, decimal Amount)>();
        foreach (var card in cards)
        {
            var due = new DateOnly(year, month, Math.Min(card.DueDay, DateTime.DaysInMonth(year, month)));
            var (pStart, pEnd) = CreditCardService.InvoicePeriod(card.ClosingDay, year, month);
            var value = await db.FinancialTransactions.AsNoTracking().Where(x => x.UserId == userId && x.CreditCardId == card.Id && x.TransactionDate >= pStart && x.TransactionDate <= pEnd).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
            if (value > 0) invoices.Add((due, $"Fatura {card.Name}", value));
        }
        var byDay = new Dictionary<DateOnly, List<CalendarEvent>>();
        foreach (var item in transactions) Add(byDay, item.TransactionDate, new CalendarEvent(item.Type == FinancialTransactionType.Income ? "entrada" : "saida", item.Description, item.Amount, item.Category, null));
        foreach (var item in scheduled) Add(byDay, item.DueDate, new CalendarEvent(item.Type == FinancialTransactionType.Income ? "previsto-entrada" : "previsto-saida", item.Description, item.Amount, item.Category, item.Status.ToString()));
        foreach (var item in invoices) Add(byDay, item.Date, new CalendarEvent("fatura", item.Description, item.Amount, null, null));
        return byDay.OrderBy(x => x.Key).Select(x => new CalendarDay(x.Key, x.Value)).ToArray();
    }

    private static void Add(Dictionary<DateOnly, List<CalendarEvent>> map, DateOnly date, CalendarEvent item)
    { if (!map.TryGetValue(date, out var list)) { list = []; map[date] = list; } list.Add(item); }
}

public sealed record AnnualReportRow(int Year, decimal TotalIncome, decimal TotalExpenses, decimal Balance, IReadOnlyList<MonthlySlice> Months, IReadOnlyList<CategorySlice> TopCategories);
public sealed record MonthlySlice(int Month, decimal Income, decimal Expenses, decimal Balance);
public sealed class AnnualReportService(AppDbContext db)
{
    public async Task<AnnualReportRow> GetAsync(Guid userId, int year, CancellationToken ct)
    {
        var start = new DateOnly(year, 1, 1); var end = start.AddYears(1);
        var items = await db.FinancialTransactions.AsNoTracking().Where(x => x.UserId == userId && x.TransactionDate >= start && x.TransactionDate < end)
            .Select(x => new { x.TransactionDate, x.Amount, x.Type, Category = x.Category.Name }).ToListAsync(ct);
        var months = Enumerable.Range(1, 12).Select(m => new MonthlySlice(m,
            items.Where(x => x.TransactionDate.Month == m && x.Type == FinancialTransactionType.Income).Sum(x => x.Amount),
            items.Where(x => x.TransactionDate.Month == m && x.Type == FinancialTransactionType.Expense).Sum(x => x.Amount), 0)).ToList();
        for (var index = 0; index < months.Count; index++) months[index] = months[index] with { Balance = months[index].Income - months[index].Expenses };
        var totalIncome = items.Where(x => x.Type == FinancialTransactionType.Income).Sum(x => x.Amount);
        var totalExpenses = items.Where(x => x.Type == FinancialTransactionType.Expense).Sum(x => x.Amount);
        var top = items.Where(x => x.Type == FinancialTransactionType.Expense).GroupBy(x => x.Category).Select(x => new CategorySlice(x.Key, x.Sum(y => y.Amount))).OrderByDescending(x => x.Amount).Take(5).ToArray();
        return new(year, totalIncome, totalExpenses, totalIncome - totalExpenses, months, top);
    }
}

public sealed record CategorySlice(string Name, decimal Amount);

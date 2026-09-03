using System.Globalization;
using System.Text;
using System.Text.Json;
using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.AI.Domain;
using CentralContabil.Api.Modules.Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.Api.Modules.Finance.Application;

public sealed record MonthUnderstanding(int Year, int Month, decimal TotalIncome, decimal TotalExpenses, decimal Balance,
    decimal? SavingsRate, string? LargestExpenseCategory, decimal? LargestExpenseAmount, FinanceMonthComparison Comparison,
    IReadOnlyList<string> Insights);

public sealed record UpcomingBill(DateOnly DueDate, string Description, decimal Amount, string Status);
public sealed record WeeklySummary(DateOnly StartDate, DateOnly EndDate, decimal Income, decimal Expenses, decimal Balance,
    IReadOnlyList<FinanceCategorySlice> TopCategories, IReadOnlyList<UpcomingBill> UpcomingBills, IReadOnlyList<string> Insights);

public sealed record GoalProgress(Guid Id, string Name, decimal TargetAmount, decimal CurrentAmount, decimal Percentage);
public sealed record MonthlySummary(int Year, int Month, decimal Income, decimal Expenses, decimal Balance,
    string? LargestExpenseCategory, decimal? LargestExpenseAmount, decimal? SavingsRate, FinanceMonthComparison Comparison,
    IReadOnlyList<GoalProgress> Goals, IReadOnlyList<string> Insights);

public sealed class FinanceInsightService(AppDbContext db, FinanceSummaryService summaries, FinancePlanningService planning)
{
    public async Task<MonthUnderstanding> UnderstandMonthAsync(Guid userId, int year, int month, CancellationToken ct)
    {
        var summary = await summaries.GetAsync(userId, year, month, ct);
        var largest = summary.ExpensesByCategory.FirstOrDefault();
        decimal? savingsRate = summary.TotalIncome > 0 ? decimal.Round(summary.Balance / summary.TotalIncome * 100, 1) : null;
        var insights = new List<string>();
        if (largest is not null) insights.Add($"{largest.CategoryName} foi sua maior saída neste mês ({largest.Amount:C}).");
        if (summary.Comparison.ExpensePercentage is { } expenseChange && expenseChange < 0) insights.Add($"Você gastou {Math.Abs(expenseChange):0}% menos em saídas que no mês anterior.");
        if (summary.Comparison.ExpensePercentage is { } expenseUp && expenseUp > 0) insights.Add($"Suas saídas subiram {expenseUp:0}% em relação ao mês anterior.");
        if (savingsRate is { } rate) insights.Add($"Você guardou {rate:0.0}% das entradas deste mês.");
        if (summary.Balance < 0) insights.Add("Suas saídas superaram as entradas neste mês. Revise o planejamento.");
        if (insights.Count == 0) insights.Add("Seu mês está equilibrado. Continue acompanhando.");
        return new(year, month, summary.TotalIncome, summary.TotalExpenses, summary.Balance, savingsRate,
            largest?.CategoryName, largest?.Amount, summary.Comparison, insights);
    }

    public async Task<WeeklySummary> WeeklySummaryAsync(Guid userId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var start = today.AddDays(-(int)today.DayOfWeek);
        var end = start.AddDays(6);
        var endExclusive = end.AddDays(1);
        var items = await db.FinancialTransactions.AsNoTracking().Where(x => x.UserId == userId && x.TransactionDate >= start && x.TransactionDate < endExclusive)
            .Select(x => new { x.Type, x.Amount, Category = x.Category.Name, x.CategoryId }).ToListAsync(ct);
        var income = items.Where(x => x.Type == FinancialTransactionType.Income).Sum(x => x.Amount);
        var expenses = items.Where(x => x.Type == FinancialTransactionType.Expense).Sum(x => x.Amount);
        var top = items.Where(x => x.Type == FinancialTransactionType.Expense).GroupBy(x => x.Category).Select(x => new FinanceCategorySlice(x.First().CategoryId, x.Key, x.Sum(y => y.Amount), 0)).OrderByDescending(x => x.Amount).Take(3).ToArray();
        var bills = (await db.ScheduledTransactions.AsNoTracking().Where(x => x.UserId == userId && x.DueDate >= start && x.DueDate <= end && x.Status == ScheduledTransactionStatus.Pending)
            .Select(x => new { x.DueDate, x.Description, x.Amount, x.Status }).OrderBy(x => x.DueDate).ToListAsync(ct))
            .Select(x => new UpcomingBill(x.DueDate, x.Description, x.Amount, x.Status.ToString())).ToArray();
        var insights = new List<string>();
        if (bills.Length > 0) insights.Add($"Você possui {bills.Length} conta(s) prevista(s) nesta semana.");
        if (expenses > 0) insights.Add($"Suas saídas da semana somam {expenses:C}.");
        if (insights.Count == 0) insights.Add("Nenhum movimento registrado nesta semana.");
        return new(start, end, income, expenses, income - expenses, top, bills, insights);
    }

    public async Task<MonthlySummary> MonthlySummaryAsync(Guid userId, int year, int month, CancellationToken ct)
    {
        var understanding = await UnderstandMonthAsync(userId, year, month, ct);
        var goals = await db.FinancialGoals.AsNoTracking().Where(x => x.UserId == userId && !x.IsCompleted)
            .Select(x => new GoalProgress(x.Id, x.Name, x.TargetAmount, x.CurrentAmount, x.TargetAmount > 0 ? decimal.Round(x.CurrentAmount / x.TargetAmount * 100, 1) : 0)).ToListAsync(ct);
        return new(year, month, understanding.TotalIncome, understanding.TotalExpenses, understanding.Balance,
            understanding.LargestExpenseCategory, understanding.LargestExpenseAmount, understanding.SavingsRate,
            understanding.Comparison, goals, understanding.Insights);
    }

    public async Task<IReadOnlyList<string>> GenerateInsightsAsync(Guid userId, int year, int month, CancellationToken ct)
    {
        var understanding = await UnderstandMonthAsync(userId, year, month, ct);
        var budget = await planning.BudgetsAsync(userId, year, month, ct);
        var insights = new List<string>(understanding.Insights);
        foreach (var category in budget.Categories.Where(x => x.Percentage >= 90))
            insights.Add(category.IsExceeded
                ? $"Você ultrapassou o planejamento de {category.CategoryName} em {Math.Abs(category.RemainingAmount):C}."
                : $"Você utilizou {category.Percentage:0}% do planejamento de {category.CategoryName}.");
        var nextMonth = new DateOnly(year, month, 1).AddMonths(1);
        var nextMonthStart = nextMonth; var nextMonthEnd = nextMonth.AddMonths(1);
        var futureInstallments = await db.FinancialTransactions.AsNoTracking().Where(x => x.UserId == userId && x.InstallmentPlanId != null && x.TransactionDate >= nextMonthStart && x.TransactionDate < nextMonthEnd).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        if (futureInstallments > 0) insights.Add($"Existem {futureInstallments:C} em parcelas previstas para {nextMonth:MMMM}.");
        return insights.Distinct().Take(8).ToArray();
    }
}

public sealed record FinancialAlert(string Type, string Severity, string Message, string? Link);
public sealed class FinancialAlertService(AppDbContext db, FinancePlanningService planning)
{
    public async Task<IReadOnlyList<FinancialAlert>> GenerateAsync(Guid userId, CancellationToken ct)
    {
        var preferences = await db.UserPreferences.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var alerts = new List<FinancialAlert>();
        if (preferences is null || preferences.AlertBudget)
        {
            var budgets = await planning.BudgetsAsync(userId, today.Year, today.Month, ct);
            foreach (var category in budgets.Categories.Where(x => x.Percentage >= 90))
                alerts.Add(new FinancialAlert("budget", category.IsExceeded ? "danger" : "warning",
                    category.IsExceeded ? $"Você ultrapassou o planejamento de {category.CategoryName} em {Math.Abs(category.RemainingAmount):C}." : $"Você utilizou {category.Percentage:0}% do planejamento de {category.CategoryName}.", "/minha-conta/financas/planejamento"));
        }
        if (preferences is null || preferences.AlertGoals)
        {
            var goals = await db.FinancialGoals.AsNoTracking().Where(x => x.UserId == userId && !x.IsCompleted).ToListAsync(ct);
            foreach (var goal in goals.Where(x => x.TargetAmount > 0 && x.CurrentAmount / x.TargetAmount >= 0.75m))
                alerts.Add(new FinancialAlert("goal", "info", $"Você atingiu {decimal.Round(goal.CurrentAmount / goal.TargetAmount * 100, 0):0}% da meta {goal.Name}.", "/minha-conta/financas/metas"));
        }
        if (preferences is null || preferences.AlertInstallments)
        {
            var nextMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(1);
            var nextMonthEnd = nextMonth.AddMonths(1);
            var installments = await db.FinancialTransactions.AsNoTracking().Where(x => x.UserId == userId && x.InstallmentPlanId != null && x.TransactionDate >= nextMonth && x.TransactionDate < nextMonthEnd).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
            if (installments > 0) alerts.Add(new FinancialAlert("installments", "info", $"Existem {installments:C} em parcelas previstas para {nextMonth:MMMM}.", "/minha-conta/financas/compromissos"));
        }
        if (preferences is null || preferences.AlertBills)
        {
            var weekEnd = today.AddDays(7);
            var bills = await db.ScheduledTransactions.AsNoTracking().Where(x => x.UserId == userId && x.DueDate >= today && x.DueDate <= weekEnd && x.Status == ScheduledTransactionStatus.Pending).CountAsync(ct);
            if (bills > 0) alerts.Add(new FinancialAlert("bills", "info", $"Você possui {bills} conta(s) prevista(s) nos próximos 7 dias.", "/minha-conta/financas/contas-a-pagar"));
        }
        if (preferences is null || preferences.AlertInvoices)
        {
            var cards = await db.CreditCards.AsNoTracking().Where(x => x.UserId == userId && x.IsActive).ToListAsync(ct);
            foreach (var card in cards)
            {
                var due = new DateOnly(today.Year, today.Month, Math.Min(card.DueDay, DateTime.DaysInMonth(today.Year, today.Month)));
                var days = due.DayNumber - today.DayNumber;
                if (days is >= 0 and <= 3) alerts.Add(new FinancialAlert("invoice", "warning", $"A fatura do cartão {card.Name} vence em {days} dia(s).", "/minha-conta/financas/cartoes"));
            }
        }
        return alerts.OrderBy(x => x.Severity == "danger" ? 0 : x.Severity == "warning" ? 1 : 2).Take(12).ToArray();
    }
}

public sealed record ImportPreviewRow(int Index, DateOnly Date, string Description, decimal Amount, FinancialTransactionType Type,
    Guid? CategoryId, string? CategoryName, decimal? CategoryConfidence, bool IsDuplicate, string? DuplicateReason, string Status);
public sealed record ImportPreview(string FileName, int Total, int Duplicates, int Uncategorized, IReadOnlyList<ImportPreviewRow> Rows);
public sealed record ImportRowInput(int Index, DateOnly Date, string Description, decimal Amount, FinancialTransactionType Type, Guid? CategoryId);
public sealed record ImportResult(int Imported, int SkippedDuplicates, int SkippedInvalid);

public sealed class FinancialDuplicateDetectionService(AppDbContext db)
{
    public async Task<bool> IsDuplicateAsync(Guid userId, DateOnly date, decimal amount, string description, FinancialTransactionType type, CancellationToken ct)
    {
        var normalized = RuleBasedCategorySuggestionService.Normalize(description);
        var candidates = await db.FinancialTransactions.AsNoTracking().Where(x => x.UserId == userId && x.TransactionDate == date && x.Amount == amount && x.Type == type)
            .Select(x => x.Description).ToListAsync(ct);
        return candidates.Any(x => RuleBasedCategorySuggestionService.Normalize(x) == normalized);
    }
}

public sealed class FinancialImportService(AppDbContext db, ICategorySuggestionService suggestions, FinancialDuplicateDetectionService duplicates)
{
    public async Task<ImportPreview> ParseCsvAsync(Guid userId, string fileName, Stream content, CancellationToken ct)
    {
        if (content.Length > 5 * 1024 * 1024) throw new InvalidOperationException("O arquivo excede o limite de 5 MB.");
        var lines = await ReadLinesAsync(content, ct);
        if (lines.Count < 2) throw new InvalidOperationException("O arquivo não contém linhas suficientes.");
        var delimiter = lines[0].Contains(';') ? ';' : ',';
        var header = ParseCsvLine(lines[0], delimiter);
        var dateIndex = IndexOf(header, "data", "date", "lançamento", "lancamento");
        var descIndex = IndexOf(header, "descricao", "descrição", "historico", "histórico", "nome", "titulo", "título");
        var valueIndex = IndexOf(header, "valor", "value", "amount", "montante");
        if (dateIndex < 0 || descIndex < 0 || valueIndex < 0) throw new InvalidOperationException("O CSV precisa de colunas de data, descrição e valor.");
        var typeIndex = IndexOf(header, "tipo", "entrada", "saida", "saída", "tipo_lancamento");
        var rows = new List<ImportPreviewRow>();
        for (var lineIndex = 1; lineIndex < lines.Count; lineIndex++)
        {
            var cells = ParseCsvLine(lines[lineIndex], delimiter); if (cells.Count <= Math.Max(dateIndex, Math.Max(descIndex, valueIndex))) continue;
            if (!DateOnly.TryParse(cells[dateIndex], CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out var date)) continue;
            if (!TryParseAmount(cells[valueIndex], out var amount) || amount <= 0) continue;
            var description = SanitizeCell(cells[descIndex]); if (string.IsNullOrWhiteSpace(description)) continue;
            var type = ResolveType(cells, typeIndex, amount);
            var suggestion = (await suggestions.SuggestAsync(userId, type, description, null, ct)).FirstOrDefault();
            var isDuplicate = await duplicates.IsDuplicateAsync(userId, date, amount, description, type, ct);
            rows.Add(new ImportPreviewRow(lineIndex, date, description, amount, type, suggestion?.CategoryId, suggestion?.CategoryName, suggestion?.Confidence, isDuplicate, isDuplicate ? "Lançamento com mesma data, valor e descrição já cadastrado." : null, isDuplicate ? "duplicate" : suggestion is null ? "uncategorized" : "ready"));
        }
        return new(fileName, rows.Count, rows.Count(x => x.IsDuplicate), rows.Count(x => x.CategoryId is null), rows);
    }

    public async Task<ImportResult> ConfirmAsync(Guid userId, IReadOnlyList<ImportRowInput> rows, CancellationToken ct)
    {
        var imported = 0; var skippedDuplicates = 0; var skippedInvalid = 0;
        foreach (var row in rows)
        {
            if (row.Amount <= 0 || row.Date == default || string.IsNullOrWhiteSpace(row.Description)) { skippedInvalid++; continue; }
            if (await duplicates.IsDuplicateAsync(userId, row.Date, row.Amount, row.Description, row.Type, ct)) { skippedDuplicates++; continue; }
            var categoryId = row.CategoryId ?? (await suggestions.SuggestAsync(userId, row.Type, row.Description, null, ct)).FirstOrDefault()?.CategoryId;
            if (categoryId is null) { skippedInvalid++; continue; }
            db.FinancialTransactions.Add(new FinancialTransaction { UserId = userId, Type = row.Type, CategoryId = categoryId.Value, Description = row.Description.Trim(), Amount = decimal.Round(row.Amount, 2), TransactionDate = row.Date, PaymentMethod = FinancialPaymentMethod.Other });
            await suggestions.RememberAsync(userId, row.Type, row.Description, categoryId.Value, ct);
            imported++;
        }
        await db.SaveChangesAsync(ct);
        return new(imported, skippedDuplicates, skippedInvalid);
    }

    private static async Task<List<string>> ReadLinesAsync(Stream content, CancellationToken ct)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, true, 1024, leaveOpen: true);
        var lines = new List<string>(); string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null) { if (!string.IsNullOrWhiteSpace(line)) lines.Add(line); if (lines.Count > 20000) throw new InvalidOperationException("O arquivo tem linhas demais."); }
        return lines;
    }

    private static List<string> ParseCsvLine(string line, char delimiter)
    {
        var cells = new List<string>(); var current = new StringBuilder(); var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];
            if (character == '"') { if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; } else inQuotes = !inQuotes; }
            else if (character == delimiter && !inQuotes) { cells.Add(current.ToString().Trim()); current.Clear(); }
            else current.Append(character);
        }
        cells.Add(current.ToString().Trim()); return cells;
    }

    private static int IndexOf(IReadOnlyList<string> header, params string[] names)
    {
        for (var i = 0; i < header.Count; i++) { var normalized = RuleBasedCategorySuggestionService.Normalize(header[i]); if (names.Any(name => normalized == name)) return i; }
        return -1;
    }

    private static bool TryParseAmount(string value, out decimal amount)
    {
        var cleaned = value.Trim().Replace("R$", "").Replace(" ", "").Replace(".", "").Replace(',', '.');
        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }

    private static FinancialTransactionType ResolveType(IReadOnlyList<string> cells, int typeIndex, decimal amount)
    {
        if (typeIndex >= 0 && typeIndex < cells.Count)
        {
            var normalized = RuleBasedCategorySuggestionService.Normalize(cells[typeIndex]);
            if (normalized.Contains("entrada") || normalized.Contains("receita") || normalized.Contains("credito") || normalized.Contains("crédito")) return FinancialTransactionType.Income;
            if (normalized.Contains("saida") || normalized.Contains("saída") || normalized.Contains("despesa") || normalized.Contains("debito") || normalized.Contains("débito")) return FinancialTransactionType.Expense;
        }
        return amount < 0 ? FinancialTransactionType.Expense : FinancialTransactionType.Income;
    }

    private static string SanitizeCell(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length > 0 && (trimmed[0] is '=' or '+' or '-' or '@')) return "'" + trimmed;
        return trimmed;
    }
}

public sealed record UserPreferenceView(ExplanationProfile ExplanationProfile, bool AlertBills, bool AlertInvoices, bool AlertBudget, bool AlertGoals, bool AlertInstallments, bool AlertWeeklySummary, bool AlertMonthlySummary);
public sealed class UserPreferenceService(AppDbContext db)
{
    public async Task<UserPreferenceView> GetAsync(Guid userId, CancellationToken ct)
    {
        var preference = await db.UserPreferences.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
        return preference is null ? new(ExplanationProfile.Simple, true, true, true, true, true, true, true) : ToView(preference);
    }

    public async Task<UserPreferenceView> UpsertAsync(Guid userId, ExplanationProfile profile, bool bills, bool invoices, bool budget, bool goals, bool installments, bool weekly, bool monthly, CancellationToken ct)
    {
        var preference = await db.UserPreferences.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (preference is null) { preference = new UserPreference { UserId = userId }; db.UserPreferences.Add(preference); }
        preference.ExplanationProfile = profile; preference.AlertBills = bills; preference.AlertInvoices = invoices; preference.AlertBudget = budget;
        preference.AlertGoals = goals; preference.AlertInstallments = installments; preference.AlertWeeklySummary = weekly; preference.AlertMonthlySummary = monthly;
        preference.UpdatedAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return ToView(preference);
    }

    private static UserPreferenceView ToView(UserPreference x) => new(x.ExplanationProfile, x.AlertBills, x.AlertInvoices, x.AlertBudget, x.AlertGoals, x.AlertInstallments, x.AlertWeeklySummary, x.AlertMonthlySummary);
}

public sealed record ConversationRow(Guid Id, string Title, DateTimeOffset CreatedAt, int MessageCount);
public sealed record ConversationMessage(string Role, string Content, string? SourcesJson, DateTimeOffset CreatedAt);
public sealed class AssistantConversationService(AppDbContext db)
{
    public async Task<IReadOnlyList<ConversationRow>> ListAsync(Guid userId, CancellationToken ct)
        => await db.AssistantConversations.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).Take(50)
            .Select(x => new ConversationRow(x.Id, x.Title, x.CreatedAt, x.Messages.Count)).ToListAsync(ct);

    public async Task<IReadOnlyList<ConversationMessage>> GetAsync(Guid conversationId, Guid userId, CancellationToken ct)
    {
        var conversation = await db.AssistantConversations.AsNoTracking().Include(x => x.Messages).FirstOrDefaultAsync(x => x.Id == conversationId && x.UserId == userId, ct);
        return conversation is null ? [] : conversation.Messages.OrderBy(x => x.CreatedAt).Select(x => new ConversationMessage(x.Role, x.Content, x.SourcesJson, x.CreatedAt)).ToArray();
    }

    public async Task<Guid> CreateAsync(Guid userId, string title, CancellationToken ct)
    {
        var conversation = new AssistantConversation { UserId = userId, Title = string.IsNullOrWhiteSpace(title) ? "Nova conversa" : title.Trim()[..Math.Min(200, title.Trim().Length)] };
        db.AssistantConversations.Add(conversation); await db.SaveChangesAsync(ct); return conversation.Id;
    }

    public async Task AddMessageAsync(Guid conversationId, Guid userId, string role, string content, string? sourcesJson, CancellationToken ct)
    {
        var conversation = await db.AssistantConversations.FirstOrDefaultAsync(x => x.Id == conversationId && x.UserId == userId, ct);
        if (conversation is null) return;
        db.AssistantMessages.Add(new AssistantMessage { ConversationId = conversationId, Role = role, Content = content[..Math.Min(20000, content.Length)], SourcesJson = sourcesJson });
        if (conversation.Title == "Nova conversa" && role == "user") conversation.Title = content.Trim()[..Math.Min(60, content.Trim().Length)];
        conversation.UpdatedAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid conversationId, Guid userId, CancellationToken ct)
    {
        var conversation = await db.AssistantConversations.FirstOrDefaultAsync(x => x.Id == conversationId && x.UserId == userId, ct);
        if (conversation is null) return false;
        db.AssistantConversations.Remove(conversation); await db.SaveChangesAsync(ct); return true;
    }

    public async Task<int> ClearAsync(Guid userId, CancellationToken ct)
    {
        var conversations = await db.AssistantConversations.Where(x => x.UserId == userId).ToListAsync(ct);
        db.AssistantConversations.RemoveRange(conversations); await db.SaveChangesAsync(ct); return conversations.Count;
    }
}

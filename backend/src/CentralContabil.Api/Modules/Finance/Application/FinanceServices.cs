using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.Api.Modules.Finance.Application;

public sealed record FinanceCategorySlice(Guid CategoryId, string CategoryName, decimal Amount, decimal Percentage);
public sealed record FinanceMonthComparison(decimal? IncomePercentage, decimal? ExpensePercentage, decimal? BalancePercentage);
public sealed record FinanceTransactionItem(Guid Id, FinancialTransactionType Type, Guid CategoryId, string CategoryName,
    string Description, decimal Amount, DateOnly TransactionDate, FinancialPaymentMethod PaymentMethod,
    bool IsRecurring, Guid? RecurrenceId, string? Notes, DateTimeOffset CreatedAt);
public sealed record FinanceSummary(int Year, int Month, decimal TotalIncome, decimal TotalExpenses, decimal Balance,
    decimal? IncomeCommitmentPercentage, int TransactionCount, IReadOnlyList<FinanceCategorySlice> ExpensesByCategory,
    IReadOnlyList<FinanceCategorySlice> IncomeByCategory, FinanceMonthComparison Comparison,
    IReadOnlyList<FinanceTransactionItem> LatestTransactions);

public sealed class FinanceSummaryService(AppDbContext db)
{
    public async Task<FinanceSummary> GetAsync(Guid userId, int year, int month, CancellationToken ct)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);
        var previousStart = start.AddMonths(-1);
        var current = db.FinancialTransactions.AsNoTracking().Where(x => x.UserId == userId && x.TransactionDate >= start && x.TransactionDate < end);
        var previous = db.FinancialTransactions.AsNoTracking().Where(x => x.UserId == userId && x.TransactionDate >= previousStart && x.TransactionDate < start);

        var income = await current.Where(x => x.Type == FinancialTransactionType.Income).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        var expense = await current.Where(x => x.Type == FinancialTransactionType.Expense).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        var previousIncome = await previous.Where(x => x.Type == FinancialTransactionType.Income).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        var previousExpense = await previous.Where(x => x.Type == FinancialTransactionType.Expense).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;

        var expenseGroups = await CategoryGroups(current, FinancialTransactionType.Expense, ct);
        var incomeGroups = await CategoryGroups(current, FinancialTransactionType.Income, ct);
        var latest = await current.OrderByDescending(x => x.TransactionDate).ThenByDescending(x => x.CreatedAt).Take(8)
            .Select(x => new FinanceTransactionItem(x.Id, x.Type, x.CategoryId, x.Category.Name, x.Description, x.Amount,
                x.TransactionDate, x.PaymentMethod, x.IsRecurring, x.RecurrenceId, x.Notes, x.CreatedAt)).ToListAsync(ct);

        var balance = income - expense;
        var previousBalance = previousIncome - previousExpense;
        return new FinanceSummary(year, month, income, expense, balance,
            income > 0 ? decimal.Round(expense / income * 100, 2) : null,
            await current.CountAsync(ct),
            WithPercentages(expenseGroups, expense), WithPercentages(incomeGroups, income),
            new FinanceMonthComparison(Change(income, previousIncome), Change(expense, previousExpense), Change(balance, previousBalance)), latest);
    }

    private async Task<List<CategoryAggregate>> CategoryGroups(
        IQueryable<FinancialTransaction> query, FinancialTransactionType type, CancellationToken ct)
    {
        var amounts = await query.Where(x => x.Type == type).GroupBy(x => x.CategoryId)
            .Select(x => new CategoryAmount { Id = x.Key, Amount = x.Sum(y => y.Amount) }).ToListAsync(ct);
        var ids = amounts.Select(x => x.Id).ToArray();
        var names = await db.FinancialCategories.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        return amounts.Select(x => new CategoryAggregate(x.Id, names[x.Id], x.Amount)).OrderByDescending(x => x.Amount).ToList();
    }

    private static IReadOnlyList<FinanceCategorySlice> WithPercentages(IEnumerable<CategoryAggregate> groups, decimal total) =>
        groups.Select(x => new FinanceCategorySlice(x.Id, x.Name, x.Amount, total > 0 ? decimal.Round(x.Amount / total * 100, 2) : 0)).ToArray();

    private static decimal? Change(decimal current, decimal previous) => previous == 0 ? null : decimal.Round((current - previous) / Math.Abs(previous) * 100, 2);
    private sealed class CategoryAmount { public Guid Id { get; init; } public decimal Amount { get; init; } }
    private sealed record CategoryAggregate(Guid Id, string Name, decimal Amount);
}

public sealed record CategorySuggestion(Guid CategoryId, string CategoryName, decimal Confidence, string Source);
public interface ICategorySuggestionService
{
    Task<IReadOnlyList<CategorySuggestion>> SuggestAsync(Guid userId, FinancialTransactionType type, string description, string? notes, CancellationToken ct);
    Task RememberAsync(Guid userId, FinancialTransactionType type, string description, Guid categoryId, CancellationToken ct);
}

public sealed class RuleBasedCategorySuggestionService(AppDbContext db) : ICategorySuggestionService
{
    private static readonly IReadOnlyDictionary<string, string[]> Keywords = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["Pets"] = ["racao", "pet", "cachorro", "cao", "gato", "veterinario", "petshop", "pet shop", "banho e tosa"],
        ["Alimentação"] = ["mercado", "supermercado", "restaurante", "lanche", "comida", "ifood", "delivery", "padaria", "acougue"],
        ["Transporte"] = ["uber", "99", "taxi", "onibus", "combustivel", "gasolina", "etanol", "estacionamento", "pedagio"],
        ["Educação"] = ["faculdade", "universidade", "curso", "mensalidade escolar", "livro", "material escolar"],
        ["Saúde"] = ["farmacia", "remedio", "medico", "dentista", "consulta", "exame", "hospital"],
        ["Moradia"] = ["aluguel", "condominio", "reforma", "moveis"],
        ["Contas"] = ["energia", "luz", "agua", "internet", "telefone", "celular", "gas"],
        ["Assinaturas"] = ["netflix", "spotify", "youtube premium", "amazon prime", "disney", "hbo", "assinatura"],
        ["Lazer"] = ["cinema", "show", "parque", "viagem", "jogo"],
        ["Presentes"] = ["presente", "aniversario"], ["Dívidas"] = ["emprestimo", "financiamento", "divida"],
        ["Impostos"] = ["imposto", "ipva", "iptu", "darf", "das"],
        ["Salário"] = ["salario", "pagamento empresa", "ordenado"],
        ["Freelance"] = ["freelance", "freela", "servico extra"], ["Vendas"] = ["venda", "vendi"]
    };

    public async Task<IReadOnlyList<CategorySuggestion>> SuggestAsync(Guid userId, FinancialTransactionType type, string description, string? notes, CancellationToken ct)
    {
        var normalized = Normalize($"{description} {notes}");
        if (normalized.Length < 3) return [];
        var exactDescription = Normalize(description);
        var categories = await db.FinancialCategories.AsNoTracking()
            .Where(x => x.IsActive && x.Type == type && (x.UserId == null || x.UserId == userId)).ToListAsync(ct);
        var preference = await db.FinancialCategoryPreferences.AsNoTracking()
            .Where(x => x.UserId == userId && x.Type == type && x.NormalizedText == exactDescription)
            .OrderByDescending(x => x.UsageCount).FirstOrDefaultAsync(ct);
        var scores = new Dictionary<Guid, CategorySuggestion>();
        if (preference is not null)
        {
            var category = categories.FirstOrDefault(x => x.Id == preference.CategoryId);
            if (category is not null) scores[category.Id] = new(category.Id, category.Name, 0.99m, "preference");
        }

        foreach (var category in categories)
        {
            if (!Keywords.TryGetValue(category.Name, out var words)) continue;
            var hits = words.Count(word => ContainsTerm(normalized, Normalize(word)));
            if (hits == 0) continue;
            var confidence = Math.Min(0.98m, 0.82m + (hits - 1) * 0.06m);
            if (!scores.TryGetValue(category.Id, out var current) || current.Confidence < confidence)
                scores[category.Id] = new(category.Id, category.Name, confidence, "keyword");
        }
        return scores.Values.OrderByDescending(x => x.Confidence).ThenBy(x => x.CategoryName).Take(3).ToArray();
    }

    public async Task RememberAsync(Guid userId, FinancialTransactionType type, string description, Guid categoryId, CancellationToken ct)
    {
        var normalized = Normalize(description);
        if (normalized.Length < 3) return;
        var item = await db.FinancialCategoryPreferences.FirstOrDefaultAsync(x => x.UserId == userId && x.Type == type && x.NormalizedText == normalized, ct);
        if (item is null) db.FinancialCategoryPreferences.Add(new FinancialCategoryPreference { UserId = userId, Type = type, NormalizedText = normalized, CategoryId = categoryId });
        else { item.CategoryId = categoryId; item.UsageCount++; item.UpdatedAt = DateTimeOffset.UtcNow; }
    }

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark) builder.Append(char.ToLowerInvariant(character));
        return Regex.Replace(builder.ToString().Normalize(NormalizationForm.FormC), @"[^a-z0-9]+", " ").Trim();
    }

    private static bool ContainsTerm(string text, string term) => $" {text} ".Contains($" {term} ", StringComparison.Ordinal);
}

public sealed class FinancialRecurrenceService(AppDbContext db)
{
    public async Task MaterializeAsync(Guid userId, DateOnly throughDate, CancellationToken ct)
    {
        var recurrences = await db.FinancialRecurrences.Where(x => x.UserId == userId && x.IsActive && x.NextOccurrence <= throughDate).ToListAsync(ct);
        foreach (var recurrence in recurrences)
        {
            while (recurrence.IsActive && recurrence.NextOccurrence <= throughDate)
            {
                if (recurrence.EndDate is not null && recurrence.NextOccurrence > recurrence.EndDate) { recurrence.IsActive = false; break; }
                var date = recurrence.NextOccurrence;
                var exists = await db.FinancialTransactions.AnyAsync(x => x.RecurrenceId == recurrence.Id && x.TransactionDate == date, ct);
                if (!exists) db.FinancialTransactions.Add(new FinancialTransaction { UserId = userId, Type = recurrence.Type, CategoryId = recurrence.CategoryId, Description = recurrence.Description, Amount = recurrence.Amount, TransactionDate = date, PaymentMethod = recurrence.PaymentMethod, IsRecurring = true, RecurrenceId = recurrence.Id, Notes = recurrence.Notes });
                recurrence.NextOccurrence = recurrence.Frequency switch
                {
                    FinancialRecurrenceFrequency.Monthly => date.AddMonths(1),
                    _ => throw new NotSupportedException("Somente recorrência mensal está disponível nesta versão.")
                };
                recurrence.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
        if (recurrences.Count > 0) await db.SaveChangesAsync(ct);
    }
}

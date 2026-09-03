using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.Api.Modules.Finance.Application;

public sealed record FinancialToolResult(string Kind, string Summary, object? Data);

public sealed class FinancialAssistantTools(AppDbContext db, FinanceSummaryService summaries, FinancePlanningService planning)
{
    public async Task<FinancialToolResult?> TryAnswerAsync(Guid userId, string question, CancellationToken ct)
    {
        var normalized = RuleBasedCategorySuggestionService.Normalize(question);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var year = today.Year; var month = today.Month;

        if (ContainsAny(normalized, "entrou", "entrada", "recebi", "receita", "quanto entrou", "meu mes", "meu mês", "resumo do mes", "resumo do mês"))
        {
            var summary = await summaries.GetAsync(userId, year, month, ct);
            return new("month-summary", $"Entradas: {summary.TotalIncome:C}. Saídas: {summary.TotalExpenses:C}. Quanto sobrou: {summary.Balance:C}.", summary);
        }
        if (ContainsAny(normalized, "gastei", "saida", "saída", "despesa", "maior saida", "maior saída", "para onde"))
        {
            var summary = await summaries.GetAsync(userId, year, month, ct);
            var largest = summary.ExpensesByCategory.FirstOrDefault();
            return new("expenses", $"Suas saídas do mês somam {summary.TotalExpenses:C}.{(largest is null ? "" : $" Maior categoria: {largest.CategoryName} ({largest.Amount:C}).")}", summary.ExpensesByCategory);
        }
        if (ContainsAny(normalized, "parcela", "parcelas", "compromisso", "ainda falta pagar"))
        {
            var nextMonth = new DateOnly(year, month, 1).AddMonths(1); var nextMonthEnd = nextMonth.AddMonths(1);
            var total = await db.FinancialTransactions.AsNoTracking().Where(x => x.UserId == userId && x.InstallmentPlanId != null && x.TransactionDate >= nextMonth && x.TransactionDate < nextMonthEnd).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
            return new("installments", $"Existem {total:C} em parcelas previstas para {nextMonth:MMMM}.", new { total, month = nextMonth.Month, year = nextMonth.Year });
        }
        if (ContainsAny(normalized, "meta", "objetivo", "falta para"))
        {
            var goals = await db.FinancialGoals.AsNoTracking().Where(x => x.UserId == userId && !x.IsCompleted).Select(x => new { x.Name, x.TargetAmount, x.CurrentAmount }).ToListAsync(ct);
            if (goals.Count == 0) return new("goals", "Você não possui metas em andamento.", goals);
            var text = string.Join("; ", goals.Select(x => $"{x.Name}: {x.CurrentAmount:C} de {x.TargetAmount:C}"));
            return new("goals", text, goals);
        }
        if (ContainsAny(normalized, "planejamento", "orcamento", "orçamento", "utilizei"))
        {
            var budget = await planning.BudgetsAsync(userId, year, month, ct);
            return new("budget", $"Você utilizou {budget.TotalUsed:C} de {budget.TotalPlanned:C} planejados no mês.", budget);
        }
        if (ContainsAny(normalized, "alimentacao", "alimentação", "transporte", "educacao", "educação", "pets", "assinatura", "categoria", "gastei com"))
        {
            var summary = await summaries.GetAsync(userId, year, month, ct);
            var category = summary.ExpensesByCategory.FirstOrDefault(x => ContainsAny(RuleBasedCategorySuggestionService.Normalize(x.CategoryName), normalized));
            if (category is not null) return new("category", $"Você gastou {category.Amount:C} com {category.CategoryName} em {month}/{year}.", category);
            return new("category", $"Não encontrei gastos com essa categoria em {month}/{year}.", null);
        }
        if (ContainsAny(normalized, "cartao", "cartão", "fatura", "limite"))
        {
            var cards = await db.CreditCards.AsNoTracking().Where(x => x.UserId == userId && x.IsActive).Select(x => new { x.Name, x.Limit }).ToListAsync(ct);
            if (cards.Count == 0) return new("cards", "Você não possui cartões cadastrados.", cards);
            return new("cards", string.Join("; ", cards.Select(x => $"{x.Name}: limite {x.Limit:C}")), cards);
        }
        if (ContainsAny(normalized, "conta a pagar", "contas previstas", "vence", "proximas", "próximas"))
        {
            var weekEnd = today.AddDays(14);
            var bills = await db.ScheduledTransactions.AsNoTracking().Where(x => x.UserId == userId && x.DueDate >= today && x.DueDate <= weekEnd && x.Status == ScheduledTransactionStatus.Pending).Select(x => new { x.DueDate, x.Description, x.Amount }).OrderBy(x => x.DueDate).ToListAsync(ct);
            return new("bills", bills.Count == 0 ? "Nenhuma conta prevista nos próximos 14 dias." : $"Você tem {bills.Count} conta(s) prevista(s): " + string.Join("; ", bills.Select(x => $"{x.Description} ({x.Amount:C}) em {x.DueDate:dd/MM}")), bills);
        }
        return null;
    }

    private static bool ContainsAny(string text, params string[] terms) => terms.Any(term => text.Contains(term, StringComparison.Ordinal));
}

using System.Security.Claims;
using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Finance.Application;
using CentralContabil.Api.Modules.Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.Api.Endpoints;

public static class FinanceV5Endpoints
{
    public static void MapFinanceV5Api(this WebApplication app)
    {
        var finance = app.MapGroup("/api/v1/finance").RequireAuthorization().WithTags("Financial Planning V5");

        finance.MapGet("/budgets", GetBudgets);
        finance.MapPut("/budgets", UpsertBudget);
        finance.MapDelete("/budgets/{id:guid}", DeleteBudget);

        finance.MapGet("/goals", GetGoals);
        finance.MapPost("/goals", CreateGoal);
        finance.MapPut("/goals/{id:guid}", UpdateGoal);
        finance.MapDelete("/goals/{id:guid}", DeleteGoal);
        finance.MapPost("/goals/{id:guid}/contributions", AddContribution);
        finance.MapDelete("/goals/{goalId:guid}/contributions/{id:guid}", DeleteContribution);

        finance.MapGet("/installments", ListInstallments);
        finance.MapGet("/installments/{id:guid}", GetInstallment);
        finance.MapGet("/forecast", Forecast);
        finance.MapGet("/report", MonthlyReport);
        finance.MapGet("/export/excel", ExportExcel);

        finance.MapPost("/recurrences/{id:guid}/pause", (Guid id, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) => SetRecurrence(id, false, null, principal, db, ct));
        finance.MapPost("/recurrences/{id:guid}/resume", (Guid id, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) => SetRecurrence(id, true, null, principal, db, ct));
        finance.MapPost("/recurrences/{id:guid}/end", (Guid id, EndRecurrenceRequest body, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) => SetRecurrence(id, false, body.EndDate, principal, db, ct));
    }

    private static async Task<IResult> GetBudgets(int? year, int? month, ClaimsPrincipal principal, FinancePlanningService service, CancellationToken ct)
    {
        var period = Period(year, month); if (period.Error is not null) return period.Error;
        return Results.Ok(await service.BudgetsAsync(UserId(principal), period.Year, period.Month, ct));
    }

    private static async Task<IResult> UpsertBudget(BudgetRequest body, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var userId = UserId(principal);
        if (body.Year is < 2000 or > 2200 || body.Month is < 1 or > 12) return Validation("period", "Mês ou ano inválido.");
        if (body.PlannedAmount <= 0) return Validation("plannedAmount", "O valor planejado deve ser maior que zero.");
        var category = await db.FinancialCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == body.CategoryId && x.Type == FinancialTransactionType.Expense && x.IsActive && (x.UserId == null || x.UserId == userId), ct);
        if (category is null) return Validation("categoryId", "Escolha uma categoria de saída válida.");
        var item = await db.MonthlyBudgets.FirstOrDefaultAsync(x => x.UserId == userId && x.CategoryId == body.CategoryId && x.Year == body.Year && x.Month == body.Month, ct);
        if (item is null) { item = new MonthlyBudget { UserId = userId, CategoryId = body.CategoryId, Year = body.Year, Month = body.Month, PlannedAmount = decimal.Round(body.PlannedAmount, 2) }; db.MonthlyBudgets.Add(item); }
        else { item.PlannedAmount = decimal.Round(body.PlannedAmount, 2); item.UpdatedAt = DateTimeOffset.UtcNow; }
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { item.Id, item.CategoryId, categoryName = category.Name, item.Year, item.Month, item.PlannedAmount });
    }

    private static async Task<IResult> DeleteBudget(Guid id, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var item = await db.MonthlyBudgets.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId(principal), ct);
        if (item is null) return Results.NotFound(); db.MonthlyBudgets.Remove(item); await db.SaveChangesAsync(ct); return Results.NoContent();
    }

    private static async Task<IResult> GetGoals(ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var userId = UserId(principal);
        var rows = await db.FinancialGoals.AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.IsCompleted).ThenBy(x => x.TargetDate)
            .Select(x => new { x.Id, x.Name, x.TargetAmount, x.CurrentAmount, x.TargetDate, x.Description, x.IsCompleted,
                progressPercentage = x.TargetAmount > 0 ? decimal.Round(x.CurrentAmount / x.TargetAmount * 100, 2) : 0,
                contributions = x.Contributions.OrderByDescending(c => c.Date).Select(c => new { c.Id, c.Amount, c.Date, c.Description, c.CreatedAt }).ToArray() }).ToListAsync(ct);
        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateGoal(GoalRequest body, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var invalid = ValidateGoal(body); if (invalid is not null) return invalid;
        var item = new FinancialGoal { UserId = UserId(principal), Name = body.Name.Trim(), TargetAmount = decimal.Round(body.TargetAmount, 2), CurrentAmount = decimal.Round(Math.Max(body.CurrentAmount, 0), 2), TargetDate = body.TargetDate, Description = Clean(body.Description) };
        item.IsCompleted = item.CurrentAmount >= item.TargetAmount; db.FinancialGoals.Add(item); await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/finance/goals/{item.Id}", item);
    }

    private static async Task<IResult> UpdateGoal(Guid id, GoalRequest body, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var item = await db.FinancialGoals.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId(principal), ct); if (item is null) return Results.NotFound();
        var invalid = ValidateGoal(body); if (invalid is not null) return invalid;
        item.Name = body.Name.Trim(); item.TargetAmount = decimal.Round(body.TargetAmount, 2); item.TargetDate = body.TargetDate; item.Description = Clean(body.Description); item.IsCompleted = item.CurrentAmount >= item.TargetAmount; item.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct); return Results.Ok(item);
    }

    private static async Task<IResult> DeleteGoal(Guid id, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var item = await db.FinancialGoals.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId(principal), ct); if (item is null) return Results.NotFound();
        db.FinancialGoals.Remove(item); await db.SaveChangesAsync(ct); return Results.NoContent();
    }

    private static async Task<IResult> AddContribution(Guid id, ContributionRequest body, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var userId = UserId(principal); var goal = await db.FinancialGoals.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct); if (goal is null) return Results.NotFound();
        if (body.Amount <= 0) return Validation("amount", "O aporte deve ser maior que zero.");
        var item = new FinancialGoalContribution { GoalId = goal.Id, UserId = userId, Amount = decimal.Round(body.Amount, 2), Date = body.Date ?? DateOnly.FromDateTime(DateTime.Today), Description = Clean(body.Description) };
        goal.CurrentAmount += item.Amount; goal.IsCompleted = goal.CurrentAmount >= goal.TargetAmount; goal.UpdatedAt = DateTimeOffset.UtcNow; db.FinancialGoalContributions.Add(item); await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/finance/goals/{goal.Id}/contributions/{item.Id}", new { item.Id, item.Amount, item.Date, item.Description, currentAmount = goal.CurrentAmount, goal.IsCompleted });
    }

    private static async Task<IResult> DeleteContribution(Guid goalId, Guid id, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var userId = UserId(principal); var item = await db.FinancialGoalContributions.Include(x => x.Goal).FirstOrDefaultAsync(x => x.Id == id && x.GoalId == goalId && x.UserId == userId && x.Goal.UserId == userId, ct); if (item is null) return Results.NotFound();
        item.Goal.CurrentAmount = Math.Max(0, item.Goal.CurrentAmount - item.Amount); item.Goal.IsCompleted = item.Goal.CurrentAmount >= item.Goal.TargetAmount; item.Goal.UpdatedAt = DateTimeOffset.UtcNow; db.FinancialGoalContributions.Remove(item); await db.SaveChangesAsync(ct); return Results.NoContent();
    }

    private static async Task<IResult> ListInstallments(ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) => Results.Ok(await InstallmentQuery(db, UserId(principal)).ToListAsync(ct));

    private static async Task<IResult> GetInstallment(Guid id, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var userId = UserId(principal); var plan = await InstallmentQuery(db, userId).FirstOrDefaultAsync(x => x.Id == id, ct); if (plan is null) return Results.NotFound();
        var transactions = await db.FinancialTransactions.AsNoTracking().Where(x => x.UserId == userId && x.InstallmentPlanId == id).OrderBy(x => x.InstallmentNumber)
            .Select(x => new { x.Id, x.InstallmentNumber, x.InstallmentCount, x.Amount, x.TransactionDate, x.Notes }).ToListAsync(ct);
        return Results.Ok(new { plan, transactions });
    }

    private static IQueryable<InstallmentPlanRow> InstallmentQuery(AppDbContext db, Guid userId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return db.FinancialInstallmentPlans.AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.FirstDueDate)
            .Select(x => new InstallmentPlanRow(x.Id, x.Description, x.CategoryId, x.Category.Name, x.TotalAmount, x.InstallmentCount, x.FirstDueDate,
                x.Transactions.Count(t => t.TransactionDate <= today), x.Transactions.Where(t => t.TransactionDate > today).Sum(t => (decimal?)t.Amount) ?? 0,
                x.Transactions.Where(t => t.TransactionDate > today).OrderBy(t => t.TransactionDate).Select(t => (DateOnly?)t.TransactionDate).FirstOrDefault()));
    }

    private static async Task<IResult> Forecast(int? year, int? month, ClaimsPrincipal principal, FinanceForecastService service, CancellationToken ct)
    {
        var period = Period(year, month); if (period.Error is not null) return period.Error;
        return Results.Ok(await service.GetAsync(UserId(principal), period.Year, period.Month, ct));
    }

    private static async Task<IResult> MonthlyReport(int? year, int? month, ClaimsPrincipal principal, FinanceSummaryService summaries, FinancePlanningService planning, CancellationToken ct)
    {
        var period = Period(year, month); if (period.Error is not null) return period.Error; var userId = UserId(principal);
        var summary = await summaries.GetAsync(userId, period.Year, period.Month, ct); var budget = await planning.BudgetsAsync(userId, period.Year, period.Month, ct);
        var savingsRate = summary.TotalIncome > 0 ? decimal.Round(summary.Balance / summary.TotalIncome * 100, 2) : (decimal?)null;
        var alerts = budget.Categories.Where(x => x.IsExceeded).Select(x => $"Você ultrapassou o orçamento de {x.CategoryName}.").ToList();
        if (summary.TotalIncome > 0 && summary.TotalExpenses > summary.TotalIncome) alerts.Add("Suas saídas estão maiores que suas entradas neste mês.");
        return Results.Ok(new { period.Year, period.Month, summary.TotalIncome, summary.TotalExpenses, summary.Balance, savingsRate,
            largestExpenseCategory = summary.ExpensesByCategory.FirstOrDefault(), budget, alerts,
            insight = summary.Balance >= 0 ? "Você fechou o período no positivo." : "Revise as maiores categorias de saída para recuperar o equilíbrio." });
    }

    private static async Task<IResult> ExportExcel(int? year, int? month, DateOnly? startDate, DateOnly? endDate, FinancialTransactionType? type, Guid? categoryId,
        FinancialPaymentMethod? paymentMethod, string? search, decimal? minAmount, decimal? maxAmount, ClaimsPrincipal principal, IFinancialExportService exporter, CancellationToken ct)
    {
        DateOnly start; DateOnly end;
        if (startDate is not null || endDate is not null) { if (startDate is null || endDate is null || endDate < startDate) return Validation("period", "Informe um intervalo de datas válido."); start = startDate.Value; end = endDate.Value; }
        else { var period = Period(year, month); if (period.Error is not null) return period.Error; start = new DateOnly(period.Year, period.Month, 1); end = start.AddMonths(1).AddDays(-1); }
        if (minAmount is not null && maxAmount is not null && minAmount > maxAmount) return Validation("amount", "O valor mínimo não pode superar o máximo.");
        var bytes = await exporter.ExportAsync(UserId(principal), new(start, end, type, categoryId, paymentMethod, search, minAmount, maxAmount), ct);
        return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"controle-financeiro-{start:yyyy-MM-dd}-{end:yyyy-MM-dd}.xlsx");
    }

    private static async Task<IResult> SetRecurrence(Guid id, bool active, DateOnly? endDate, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var item = await db.FinancialRecurrences.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId(principal), ct); if (item is null) return Results.NotFound();
        item.IsActive = active; if (endDate is not null) item.EndDate = endDate; item.UpdatedAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return Results.Ok(new { item.Id, item.IsActive, item.EndDate, item.NextOccurrence });
    }

    private static IResult? ValidateGoal(GoalRequest body)
    {
        if (string.IsNullOrWhiteSpace(body.Name) || body.Name.Trim().Length > 100) return Validation("name", "Nome obrigatório com até 100 caracteres.");
        if (body.TargetAmount <= 0) return Validation("targetAmount", "O valor da meta deve ser maior que zero.");
        if (body.CurrentAmount < 0) return Validation("currentAmount", "O valor atual não pode ser negativo.");
        if (body.Description?.Length > 500) return Validation("description", "A descrição deve ter até 500 caracteres."); return null;
    }

    private static (int Year, int Month, IResult? Error) Period(int? year, int? month)
    {
        var today = DateOnly.FromDateTime(DateTime.Today); var selectedYear = year ?? today.Year; var selectedMonth = month ?? today.Month;
        return selectedYear is < 2000 or > 2200 || selectedMonth is < 1 or > 12 ? (selectedYear, selectedMonth, Validation("period", "Mês ou ano inválido.")) : (selectedYear, selectedMonth, null);
    }

    private static Guid UserId(ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static IResult Validation(string key, string message) => Results.ValidationProblem(new Dictionary<string, string[]> { [key] = [message] });
}

public sealed record BudgetRequest(Guid CategoryId, int Year, int Month, decimal PlannedAmount);
public sealed record GoalRequest(string Name, decimal TargetAmount, decimal CurrentAmount, DateOnly? TargetDate, string? Description);
public sealed record ContributionRequest(decimal Amount, DateOnly? Date, string? Description);
public sealed record EndRecurrenceRequest(DateOnly? EndDate);
public sealed record InstallmentPlanRow(Guid Id, string Description, Guid CategoryId, string CategoryName, decimal TotalAmount, int InstallmentCount, DateOnly FirstDueDate, int ElapsedInstallments, decimal RemainingAmount, DateOnly? NextDueDate);

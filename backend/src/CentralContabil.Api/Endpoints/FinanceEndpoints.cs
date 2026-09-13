using System.Security.Claims;
using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Finance.Application;
using CentralContabil.Api.Modules.Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.Api.Endpoints;

public static class FinanceEndpoints
{
    public static void MapFinanceApi(this WebApplication app)
    {
        var finance = app.MapGroup("/api/v1/finance").RequireAuthorization().WithTags("Personal Finance");

        finance.MapGet("/summary", Summary);
        finance.MapGet("/dashboard", Summary);
        finance.MapGet("/transactions", ListTransactions);
        finance.MapGet("/transactions/{id:guid}", async (Guid id, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) =>
        {
            var userId = UserId(principal);
            var item = await ProjectTransactions(db.FinancialTransactions.AsNoTracking().Where(x => x.Id == id && x.UserId == userId)).FirstOrDefaultAsync(ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });
        finance.MapPost("/transactions", CreateTransaction);
        finance.MapPut("/transactions/{id:guid}", UpdateTransaction);
        finance.MapDelete("/transactions/{id:guid}", async (Guid id, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) =>
        {
            var item = await db.FinancialTransactions.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId(principal), ct);
            if (item is null) return Results.NotFound();
            db.FinancialTransactions.Remove(item);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        finance.MapGet("/categories", async (ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) =>
        {
            var userId = UserId(principal);
            return Results.Ok(await db.FinancialCategories.AsNoTracking().Where(x => x.IsActive && (x.UserId == null || x.UserId == userId))
                .OrderBy(x => x.Type).ThenBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Type, x.Icon, x.IsDefault, isCustom = x.UserId != null }).ToListAsync(ct));
        });
        finance.MapPost("/categories", CreateCategory);
        finance.MapPut("/categories/{id:guid}", UpdateCategory);
        finance.MapDelete("/categories/{id:guid}", DeleteCategory);

        finance.MapGet("/recurrences", async (ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) =>
        {
            var userId = UserId(principal);
            return Results.Ok(await db.FinancialRecurrences.AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.NextOccurrence)
                .Select(x => new { x.Id, x.Type, x.CategoryId, categoryName = x.Category.Name, x.Description, x.Amount, x.PaymentMethod, x.Frequency, x.StartDate, x.EndDate, x.NextOccurrence, x.IsActive, x.Notes }).ToListAsync(ct));
        });
        finance.MapPost("/recurrences", CreateRecurrence);
        finance.MapPut("/recurrences/{id:guid}", UpdateRecurrence);
        finance.MapDelete("/recurrences/{id:guid}", async (Guid id, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) =>
        {
            var recurrence = await db.FinancialRecurrences.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId(principal), ct);
            if (recurrence is null) return Results.NotFound();
            recurrence.IsActive = false; recurrence.UpdatedAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return Results.NoContent();
        });
        finance.MapPost("/category-suggestion", async (CategorySuggestionRequest body, ClaimsPrincipal principal, ICategorySuggestionService service, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Description)) return Results.Ok(new { suggestions = Array.Empty<CategorySuggestion>() });
            return Results.Ok(new { suggestions = await service.SuggestAsync(UserId(principal), body.Type, body.Description, body.Notes, ct) });
        });
    }

    private static async Task<IResult> Summary(int? year, int? month, ClaimsPrincipal principal, FinanceSummaryService summaries, FinancialRecurrenceService recurrences, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var selectedYear = year ?? today.Year; var selectedMonth = month ?? today.Month;
        if (selectedYear is < 2000 or > 2200 || selectedMonth is < 1 or > 12) return Validation("period", "Mês ou ano inválido.");
        var userId = UserId(principal);
        await recurrences.MaterializeAsync(userId, new DateOnly(selectedYear, selectedMonth, 1).AddMonths(1).AddDays(-1), ct);
        return Results.Ok(await summaries.GetAsync(userId, selectedYear, selectedMonth, ct));
    }

    private static async Task<IResult> ListTransactions(int? year, int? month, DateOnly? startDate, DateOnly? endDate, FinancialTransactionType? type, Guid? categoryId,
        FinancialPaymentMethod? paymentMethod, string? search, decimal? minAmount, decimal? maxAmount, int? page, int? pageSize, ClaimsPrincipal principal,
        AppDbContext db, FinancialRecurrenceService recurrences, CancellationToken ct)
    {
        var userId = UserId(principal); var selectedPage = Math.Max(page ?? 1, 1); var size = Math.Clamp(pageSize ?? 20, 1, 100);
        if (year is not null && (year < 2000 || year > 2200) || month is not null && (month < 1 || month > 12)) return Validation("period", "Mês ou ano inválido.");
        if ((startDate is null) != (endDate is null) || startDate > endDate) return Validation("period", "Período personalizado inválido.");
        if (endDate is not null) await recurrences.MaterializeAsync(userId, endDate.Value, ct);
        else if (year is not null && month is not null) await recurrences.MaterializeAsync(userId, new DateOnly(year.Value, month.Value, 1).AddMonths(1).AddDays(-1), ct);
        var query = db.FinancialTransactions.AsNoTracking().Where(x => x.UserId == userId);
        if (startDate is not null && endDate is not null) query = query.Where(x => x.TransactionDate >= startDate && x.TransactionDate <= endDate);
        else if (year is not null && month is not null) { var start = new DateOnly(year.Value, month.Value, 1); var end = start.AddMonths(1); query = query.Where(x => x.TransactionDate >= start && x.TransactionDate < end); }
        else if (year is not null) query = query.Where(x => x.TransactionDate.Year == year);
        if (type is not null) query = query.Where(x => x.Type == type);
        if (categoryId is not null) query = query.Where(x => x.CategoryId == categoryId);
        if (paymentMethod is not null) query = query.Where(x => x.PaymentMethod == paymentMethod);
        if (minAmount is not null) query = query.Where(x => x.Amount >= minAmount);
        if (maxAmount is not null) query = query.Where(x => x.Amount <= maxAmount);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => EF.Functions.ILike(x.Description, $"%{search.Trim()}%") || x.Notes != null && EF.Functions.ILike(x.Notes, $"%{search.Trim()}%"));
        var total = await query.CountAsync(ct);
        var items = await ProjectTransactions(query.OrderByDescending(x => x.TransactionDate).ThenByDescending(x => x.CreatedAt).Skip((selectedPage - 1) * size).Take(size)).ToListAsync(ct);
        return Results.Ok(new { items, total, page = selectedPage, pageSize = size });
    }

    private static async Task<IResult> CreateTransaction(TransactionEditRequest body, ClaimsPrincipal principal, AppDbContext db, ICategorySuggestionService suggestions, FinancialInstallmentService installments, CancellationToken ct)
    {
        var userId = UserId(principal); var invalid = await ValidateTransaction(body, userId, db, ct); if (invalid is not null) return invalid;
        if (body.IsInstallment)
        {
            await suggestions.RememberAsync(userId, body.Type, body.Description, body.CategoryId, ct);
            var plan = await installments.CreateAsync(userId, body.CategoryId, body.Description, body.Amount, body.InstallmentCount!.Value,
                body.FirstInstallmentDate!.Value, body.PaymentMethod, Clean(body.Notes), ct, body.CreditCardId, body.AccountId);
            var first = await ProjectTransactions(db.FinancialTransactions.AsNoTracking().Where(x => x.InstallmentPlanId == plan.Id)
                .OrderBy(x => x.InstallmentNumber)).FirstAsync(ct);
            return Results.Created($"/api/v1/finance/installments/{plan.Id}", first);
        }
        Guid? recurrenceId = null;
        if (body.IsRecurring)
        {
            var recurrence = new FinancialRecurrence { UserId = userId, Type = body.Type, CategoryId = body.CategoryId, Description = body.Description.Trim(), Amount = body.Amount, PaymentMethod = body.PaymentMethod, Frequency = FinancialRecurrenceFrequency.Monthly, StartDate = body.TransactionDate, EndDate = body.RecurrenceEndDate, NextOccurrence = body.TransactionDate.AddMonths(1), Notes = Clean(body.Notes) };
            db.FinancialRecurrences.Add(recurrence); recurrenceId = recurrence.Id;
        }
        var item = new FinancialTransaction { UserId = userId, Type = body.Type, CategoryId = body.CategoryId, Description = body.Description.Trim(), Amount = body.Amount, TransactionDate = body.TransactionDate, PaymentMethod = body.PaymentMethod, IsRecurring = body.IsRecurring, RecurrenceId = recurrenceId, Notes = Clean(body.Notes), CreditCardId = body.CreditCardId, AccountId = body.AccountId };
        db.FinancialTransactions.Add(item); await suggestions.RememberAsync(userId, body.Type, body.Description, body.CategoryId, ct); await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/finance/transactions/{item.Id}", await ProjectTransactions(db.FinancialTransactions.AsNoTracking().Where(x => x.Id == item.Id)).SingleAsync(ct));
    }

    private static async Task<IResult> UpdateTransaction(Guid id, TransactionEditRequest body, ClaimsPrincipal principal, AppDbContext db, ICategorySuggestionService suggestions, CancellationToken ct)
    {
        var userId = UserId(principal); var item = await db.FinancialTransactions.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct); if (item is null) return Results.NotFound();
        if (item.InstallmentPlanId is not null && (body.Type != FinancialTransactionType.Expense || body.PaymentMethod != FinancialPaymentMethod.CreditCard || body.IsRecurring)) return Validation("installment", "Uma parcela deve continuar sendo uma saída no cartão de crédito.");
        var invalid = await ValidateTransaction(body, userId, db, ct); if (invalid is not null) return invalid;
        item.Type = body.Type; item.CategoryId = body.CategoryId; item.Description = body.Description.Trim(); item.Amount = body.Amount; item.TransactionDate = body.TransactionDate; item.PaymentMethod = body.PaymentMethod; item.Notes = Clean(body.Notes); item.CreditCardId = body.CreditCardId; item.AccountId = body.AccountId; item.UpdatedAt = DateTimeOffset.UtcNow;
        await suggestions.RememberAsync(userId, body.Type, body.Description, body.CategoryId, ct); await db.SaveChangesAsync(ct);
        return Results.Ok(await ProjectTransactions(db.FinancialTransactions.AsNoTracking().Where(x => x.Id == item.Id)).SingleAsync(ct));
    }

    private static async Task<IResult?> ValidateTransaction(TransactionEditRequest body, Guid userId, AppDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Description) || body.Description.Trim().Length > 160) return Validation("description", "Descrição obrigatória com até 160 caracteres.");
        if (body.Amount <= 0 || body.Amount > 999_999_999_999m) return Validation("amount", "O valor deve ser maior que zero.");
        if (body.TransactionDate == default) return Validation("transactionDate", "Data obrigatória.");
        if (body.Notes?.Length > 1000) return Validation("notes", "Observações devem ter até 1000 caracteres.");
        if (body.IsInstallment && body.Type != FinancialTransactionType.Expense) return Validation("isInstallment", "Parcelamento está disponível apenas para saídas.");
        if (body.IsInstallment && body.PaymentMethod != FinancialPaymentMethod.CreditCard) return Validation("paymentMethod", "Parcelamento exige cartão de crédito.");
        if (body.IsInstallment && body.IsRecurring) return Validation("isInstallment", "Um lançamento não pode ser parcelado e recorrente ao mesmo tempo.");
        if (body.IsInstallment && body.InstallmentCount is null or < 2 or > 360) return Validation("installmentCount", "Informe entre 2 e 360 parcelas.");
        if (body.IsInstallment && decimal.Round(body.Amount, 2) * 100 < body.InstallmentCount) return Validation("amount", "O total precisa permitir ao menos um centavo por parcela.");
        if (body.IsInstallment && body.FirstInstallmentDate is null) return Validation("firstInstallmentDate", "A data da primeira parcela é obrigatória.");
        var categoryExists = await db.FinancialCategories.AnyAsync(x => x.Id == body.CategoryId && x.Type == body.Type && x.IsActive && (x.UserId == null || x.UserId == userId), ct);
        if (!categoryExists) return Validation("categoryId", "Categoria inválida ou indisponível.");
        if (body.CreditCardId is not null && !await db.CreditCards.AnyAsync(x => x.Id == body.CreditCardId && x.UserId == userId, ct)) return Validation("creditCardId", "Cartão inválido.");
        if (body.AccountId is not null && !await db.FinancialAccounts.AnyAsync(x => x.Id == body.AccountId && x.UserId == userId, ct)) return Validation("accountId", "Conta inválida.");
        return null;
    }

    private static async Task<IResult> CreateCategory(CategoryRequest body, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var userId = UserId(principal); var name = body.Name?.Trim(); if (string.IsNullOrWhiteSpace(name) || name.Length > 80) return Validation("name", "Nome obrigatório com até 80 caracteres.");
        if (body.ChartColor is not null && !FinancialChartPalette.IsValid(body.ChartColor)) return Validation("chartColor", "Use uma cor no formato hexadecimal #RRGGBB.");
        if (await db.FinancialCategories.AnyAsync(x => x.UserId == userId && x.Type == body.Type && x.Name.ToLower() == name.ToLower(), ct)) return Results.Conflict(new { title = "Categoria já existente" });
        var item = new FinancialCategory { UserId = userId, Name = name, Type = body.Type, Icon = Clean(body.Icon), IsDefault = false };
        db.FinancialCategories.Add(item);
        if (body.ChartColor is not null) db.FinancialCategoryChartPreferences.Add(new FinancialCategoryChartPreference { UserId = userId, CategoryId = item.Id, ChartColor = FinancialChartPalette.Normalize(body.ChartColor) });
        await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/finance/categories/{item.Id}", new { item.Id, item.Name, item.Type, item.Icon, item.IsDefault, isCustom = true, chartColor = body.ChartColor });
    }

    private static async Task<IResult> UpdateCategory(Guid id, CategoryRequest body, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var item = await db.FinancialCategories.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId(principal), ct); if (item is null) return Results.NotFound();
        var name = body.Name?.Trim(); if (string.IsNullOrWhiteSpace(name) || name.Length > 80) return Validation("name", "Nome obrigatório com até 80 caracteres.");
        item.Name = name; item.Icon = Clean(body.Icon); item.Type = body.Type; await db.SaveChangesAsync(ct); return Results.Ok(new { item.Id, item.Name, item.Type, item.Icon, item.IsDefault, isCustom = true });
    }

    private static async Task<IResult> DeleteCategory(Guid id, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var item = await db.FinancialCategories.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId(principal), ct); if (item is null) return Results.NotFound();
        if (await db.FinancialTransactions.AnyAsync(x => x.CategoryId == id, ct) || await db.FinancialRecurrences.AnyAsync(x => x.CategoryId == id, ct)) item.IsActive = false; else db.FinancialCategories.Remove(item);
        await db.SaveChangesAsync(ct); return Results.NoContent();
    }

    private static async Task<IResult> CreateRecurrence(RecurrenceRequest body, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var userId = UserId(principal); var invalid = await ValidateRecurrence(body, userId, db, ct); if (invalid is not null) return invalid;
        var item = new FinancialRecurrence { UserId = userId, Type = body.Type, CategoryId = body.CategoryId, Description = body.Description.Trim(), Amount = body.Amount, PaymentMethod = body.PaymentMethod, Frequency = FinancialRecurrenceFrequency.Monthly, StartDate = body.StartDate, EndDate = body.EndDate, NextOccurrence = body.StartDate, Notes = Clean(body.Notes) };
        db.FinancialRecurrences.Add(item); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/finance/recurrences/{item.Id}", item);
    }

    private static async Task<IResult> UpdateRecurrence(Guid id, RecurrenceRequest body, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var userId = UserId(principal); var item = await db.FinancialRecurrences.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct); if (item is null) return Results.NotFound();
        var invalid = await ValidateRecurrence(body, userId, db, ct); if (invalid is not null) return invalid;
        item.Type = body.Type; item.CategoryId = body.CategoryId; item.Description = body.Description.Trim(); item.Amount = body.Amount; item.PaymentMethod = body.PaymentMethod; item.EndDate = body.EndDate; item.IsActive = body.IsActive; item.Notes = Clean(body.Notes); item.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct); return Results.Ok(item);
    }

    private static async Task<IResult?> ValidateRecurrence(RecurrenceRequest body, Guid userId, AppDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Description) || body.Amount <= 0 || body.StartDate == default) return Validation("recurrence", "Descrição, valor positivo e data inicial são obrigatórios.");
        if (body.EndDate < body.StartDate) return Validation("endDate", "A data final não pode ser anterior à inicial.");
        return await db.FinancialCategories.AnyAsync(x => x.Id == body.CategoryId && x.Type == body.Type && x.IsActive && (x.UserId == null || x.UserId == userId), ct) ? null : Validation("categoryId", "Categoria inválida.");
    }

    private static IQueryable<FinanceTransactionRow> ProjectTransactions(IQueryable<FinancialTransaction> query) => query.Select(x => new FinanceTransactionRow(x.Id, x.UserId, x.Type, x.CategoryId, x.Category.Name, x.Description, x.Amount, x.TransactionDate, x.PaymentMethod, x.IsRecurring, x.RecurrenceId, x.InstallmentPlanId, x.InstallmentNumber, x.InstallmentCount, x.Notes, x.CreatedAt, x.CreditCardId, x.AccountId));
    private static Guid UserId(ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static IResult Validation(string key, string message) => Results.ValidationProblem(new Dictionary<string, string[]> { [key] = [message] });
}

public sealed record FinanceTransactionRow(Guid Id, Guid UserId, FinancialTransactionType Type, Guid CategoryId, string CategoryName, string Description, decimal Amount, DateOnly TransactionDate, FinancialPaymentMethod PaymentMethod, bool IsRecurring, Guid? RecurrenceId, Guid? InstallmentPlanId, int? InstallmentNumber, int? InstallmentCount, string? Notes, DateTimeOffset CreatedAt, Guid? CreditCardId = null, Guid? AccountId = null);
public sealed record TransactionEditRequest(FinancialTransactionType Type, Guid CategoryId, string Description, decimal Amount, DateOnly TransactionDate, FinancialPaymentMethod PaymentMethod, bool IsRecurring, DateOnly? RecurrenceEndDate, string? Notes, bool IsInstallment = false, int? InstallmentCount = null, DateOnly? FirstInstallmentDate = null, Guid? CreditCardId = null, Guid? AccountId = null);
public sealed record CategoryRequest(string Name, FinancialTransactionType Type, string? Icon, string? ChartColor = null);
public sealed record RecurrenceRequest(FinancialTransactionType Type, Guid CategoryId, string Description, decimal Amount, FinancialPaymentMethod PaymentMethod, DateOnly StartDate, DateOnly? EndDate, bool IsActive, string? Notes);
public sealed record CategorySuggestionRequest(FinancialTransactionType Type, string Description, string? Notes);

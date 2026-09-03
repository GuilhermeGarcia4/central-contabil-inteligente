using System.Security.Claims;
using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Finance.Application;
using CentralContabil.Api.Modules.Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.Api.Endpoints;

public static class FinanceV6Endpoints
{
    public static void MapFinanceV6Api(this WebApplication app)
    {
        var finance = app.MapGroup("/api/v1/finance").RequireAuthorization().WithTags("Financial Planning V6");

        finance.MapGet("/cards", GetCards);
        finance.MapPost("/cards", CreateCard);
        finance.MapPut("/cards/{id:guid}", UpdateCard);
        finance.MapDelete("/cards/{id:guid}", DeleteCard);
        finance.MapGet("/cards/invoices", GetInvoices);

        finance.MapGet("/accounts", GetAccounts);
        finance.MapPost("/accounts", CreateAccount);
        finance.MapPut("/accounts/{id:guid}", UpdateAccount);
        finance.MapDelete("/accounts/{id:guid}", DeleteAccount);
        finance.MapPost("/accounts/transfer", Transfer);

        finance.MapGet("/scheduled", GetScheduled);
        finance.MapPost("/scheduled", CreateScheduled);
        finance.MapPut("/scheduled/{id:guid}", UpdateScheduled);
        finance.MapDelete("/scheduled/{id:guid}", DeleteScheduled);
        finance.MapPost("/scheduled/{id:guid}/status", SetScheduledStatus);

        finance.MapGet("/debts", GetDebts);
        finance.MapPost("/debts", CreateDebt);
        finance.MapPut("/debts/{id:guid}", UpdateDebt);
        finance.MapDelete("/debts/{id:guid}", DeleteDebt);

        finance.MapGet("/reserve", GetReserve);
        finance.MapPut("/reserve", UpsertReserve);

        finance.MapGet("/net-worth", GetNetWorth);
        finance.MapGet("/calendar", GetCalendar);
        finance.MapGet("/annual-report", GetAnnualReport);
    }

    private static async Task<IResult> GetCards(ClaimsPrincipal principal, CreditCardService service, CancellationToken ct) => Results.Ok(await service.ListAsync(UserId(principal), ct));

    private static async Task<IResult> CreateCard(CardRequest body, ClaimsPrincipal principal, CreditCardService service, CancellationToken ct)
    {
        var invalid = ValidateCard(body); if (invalid is not null) return invalid;
        var card = await service.CreateAsync(UserId(principal), body.Name, body.Bank, body.Limit, body.ClosingDay, body.DueDay, body.Last4, body.Color, ct);
        return Results.Created($"/api/v1/finance/cards/{card.Id}", card);
    }

    private static async Task<IResult> UpdateCard(Guid id, CardRequest body, ClaimsPrincipal principal, CreditCardService service, CancellationToken ct)
    {
        var invalid = ValidateCard(body); if (invalid is not null) return invalid;
        var card = await service.UpdateAsync(id, UserId(principal), body.Name, body.Bank, body.Limit, body.ClosingDay, body.DueDay, body.Last4, body.Color, ct);
        return card is null ? Results.NotFound() : Results.Ok(card);
    }

    private static async Task<IResult> DeleteCard(Guid id, ClaimsPrincipal principal, CreditCardService service, CancellationToken ct)
        => await service.DeleteAsync(id, UserId(principal), ct) ? Results.NoContent() : Results.NotFound();

    private static async Task<IResult> GetInvoices(int? year, int? month, ClaimsPrincipal principal, CreditCardService service, CancellationToken ct)
    {
        var period = Period(year, month); if (period.Error is not null) return period.Error;
        return Results.Ok(await service.InvoicesAsync(UserId(principal), period.Year, period.Month, ct));
    }

    private static async Task<IResult> GetAccounts(ClaimsPrincipal principal, FinancialAccountService service, CancellationToken ct) => Results.Ok(await service.ListAsync(UserId(principal), ct));

    private static async Task<IResult> CreateAccount(AccountRequest body, ClaimsPrincipal principal, FinancialAccountService service, CancellationToken ct)
    {
        var invalid = ValidateAccount(body); if (invalid is not null) return invalid;
        var account = await service.CreateAsync(UserId(principal), body.Name, body.Type, body.Balance, body.Color, ct);
        return Results.Created($"/api/v1/finance/accounts/{account.Id}", account);
    }

    private static async Task<IResult> UpdateAccount(Guid id, AccountRequest body, ClaimsPrincipal principal, FinancialAccountService service, CancellationToken ct)
    {
        var invalid = ValidateAccount(body); if (invalid is not null) return invalid;
        var account = await service.UpdateAsync(id, UserId(principal), body.Name, body.Type, body.Balance, body.Color, ct);
        return account is null ? Results.NotFound() : Results.Ok(account);
    }

    private static async Task<IResult> DeleteAccount(Guid id, ClaimsPrincipal principal, FinancialAccountService service, CancellationToken ct)
        => await service.DeleteAsync(id, UserId(principal), ct) ? Results.NoContent() : Results.NotFound();

    private static async Task<IResult> Transfer(TransferRequest body, ClaimsPrincipal principal, FinancialAccountService service, CancellationToken ct)
    {
        if (body.Amount <= 0) return Validation("amount", "O valor da transferência deve ser maior que zero.");
        var (ok, error) = await service.TransferAsync(UserId(principal), body.FromAccountId, body.ToAccountId, body.Amount, body.Date ?? DateOnly.FromDateTime(DateTime.Today), body.Notes, ct);
        return ok ? Results.Ok(new { message = "Transferência realizada." }) : Validation("transfer", error!);
    }

    private static async Task<IResult> GetScheduled(int? year, int? month, ScheduledTransactionStatus? status, ClaimsPrincipal principal, ScheduledTransactionService service, CancellationToken ct)
        => Results.Ok(await service.ListAsync(UserId(principal), year, month, status, ct));

    private static async Task<IResult> CreateScheduled(ScheduledRequest body, ClaimsPrincipal principal, ScheduledTransactionService service, AppDbContext db, CancellationToken ct)
    {
        var invalid = await ValidateScheduled(body, UserId(principal), db, ct); if (invalid is not null) return invalid;
        var item = await service.CreateAsync(UserId(principal), body.Type, body.CategoryId, body.Description, body.Amount, body.DueDate, body.AccountId, body.Notes, ct);
        return Results.Created($"/api/v1/finance/scheduled/{item.Id}", item);
    }

    private static async Task<IResult> UpdateScheduled(Guid id, ScheduledRequest body, ClaimsPrincipal principal, ScheduledTransactionService service, AppDbContext db, CancellationToken ct)
    {
        var invalid = await ValidateScheduled(body, UserId(principal), db, ct); if (invalid is not null) return invalid;
        var item = await service.UpdateAsync(id, UserId(principal), body.Type, body.CategoryId, body.Description, body.Amount, body.DueDate, body.AccountId, body.Notes, ct);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    private static async Task<IResult> DeleteScheduled(Guid id, ClaimsPrincipal principal, ScheduledTransactionService service, CancellationToken ct)
        => await service.DeleteAsync(id, UserId(principal), ct) ? Results.NoContent() : Results.NotFound();

    private static async Task<IResult> SetScheduledStatus(Guid id, StatusRequest body, ClaimsPrincipal principal, ScheduledTransactionService service, CancellationToken ct)
    {
        var item = await service.SetStatusAsync(id, UserId(principal), body.Status, ct);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    private static async Task<IResult> GetDebts(ClaimsPrincipal principal, DebtService service, CancellationToken ct) => Results.Ok(await service.ListAsync(UserId(principal), ct));

    private static async Task<IResult> CreateDebt(DebtRequest body, ClaimsPrincipal principal, DebtService service, CancellationToken ct)
    {
        var invalid = ValidateDebt(body); if (invalid is not null) return invalid;
        var debt = await service.CreateAsync(UserId(principal), body.Name, body.OriginalAmount, body.CurrentAmount, body.InstallmentCount, body.InterestRate, body.DueDate, body.Institution, body.Notes, ct);
        return Results.Created($"/api/v1/finance/debts/{debt.Id}", debt);
    }

    private static async Task<IResult> UpdateDebt(Guid id, DebtRequest body, ClaimsPrincipal principal, DebtService service, CancellationToken ct)
    {
        var invalid = ValidateDebt(body); if (invalid is not null) return invalid;
        var debt = await service.UpdateAsync(id, UserId(principal), body.Name, body.OriginalAmount, body.CurrentAmount, body.InstallmentCount, body.InterestRate, body.DueDate, body.Institution, body.Notes, ct);
        return debt is null ? Results.NotFound() : Results.Ok(debt);
    }

    private static async Task<IResult> DeleteDebt(Guid id, ClaimsPrincipal principal, DebtService service, CancellationToken ct)
        => await service.DeleteAsync(id, UserId(principal), ct) ? Results.NoContent() : Results.NotFound();

    private static async Task<IResult> GetReserve(ClaimsPrincipal principal, EmergencyReserveService service, CancellationToken ct) => Results.Ok(await service.GetAsync(UserId(principal), ct));

    private static async Task<IResult> UpsertReserve(ReserveRequest body, ClaimsPrincipal principal, EmergencyReserveService service, CancellationToken ct)
    {
        if (body.TargetAmount <= 0) return Validation("targetAmount", "A meta da reserva deve ser maior que zero.");
        if (body.CurrentAmount < 0) return Validation("currentAmount", "O valor atual não pode ser negativo.");
        return Results.Ok(await service.UpsertAsync(UserId(principal), body.TargetAmount, body.CurrentAmount, body.TargetMonths, body.Notes, ct));
    }

    private static async Task<IResult> GetNetWorth(ClaimsPrincipal principal, NetWorthService service, CancellationToken ct) => Results.Ok(await service.GetAsync(UserId(principal), ct));

    private static async Task<IResult> GetCalendar(int? year, int? month, ClaimsPrincipal principal, FinanceCalendarService service, CancellationToken ct)
    {
        var period = Period(year, month); if (period.Error is not null) return period.Error;
        return Results.Ok(await service.GetAsync(UserId(principal), period.Year, period.Month, ct));
    }

    private static async Task<IResult> GetAnnualReport(int? year, ClaimsPrincipal principal, AnnualReportService service, CancellationToken ct)
    {
        var selectedYear = year ?? DateOnly.FromDateTime(DateTime.Today).Year;
        if (selectedYear is < 2000 or > 2200) return Validation("year", "Ano inválido.");
        return Results.Ok(await service.GetAsync(UserId(principal), selectedYear, ct));
    }

    private static IResult? ValidateCard(CardRequest body)
    {
        if (string.IsNullOrWhiteSpace(body.Name) || body.Name.Trim().Length > 100) return Validation("name", "Nome obrigatório com até 100 caracteres.");
        if (body.Limit < 0) return Validation("limit", "O limite não pode ser negativo.");
        if (body.ClosingDay is < 1 or > 31) return Validation("closingDay", "O dia de fechamento deve estar entre 1 e 31.");
        if (body.DueDay is < 1 or > 31) return Validation("dueDay", "O dia de vencimento deve estar entre 1 e 31.");
        if (body.Last4?.Length > 4) return Validation("last4", "Informe apenas os 4 últimos dígitos."); return null;
    }

    private static IResult? ValidateAccount(AccountRequest body)
    {
        if (string.IsNullOrWhiteSpace(body.Name) || body.Name.Trim().Length > 100) return Validation("name", "Nome obrigatório com até 100 caracteres.");
        return null;
    }

    private static async Task<IResult?> ValidateScheduled(ScheduledRequest body, Guid userId, AppDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Description) || body.Description.Trim().Length > 160) return Validation("description", "Descrição obrigatória com até 160 caracteres.");
        if (body.Amount <= 0) return Validation("amount", "O valor deve ser maior que zero.");
        var category = await db.FinancialCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == body.CategoryId && x.Type == body.Type && x.IsActive && (x.UserId == null || x.UserId == userId), ct);
        if (category is null) return Validation("categoryId", "Escolha uma categoria válida.");
        if (body.AccountId is not null)
        {
            var account = await db.FinancialAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == body.AccountId && x.UserId == userId, ct);
            if (account is null) return Validation("accountId", "Conta inválida.");
        }
        return null;
    }

    private static IResult? ValidateDebt(DebtRequest body)
    {
        if (string.IsNullOrWhiteSpace(body.Name) || body.Name.Trim().Length > 100) return Validation("name", "Nome obrigatório com até 100 caracteres.");
        if (body.OriginalAmount <= 0) return Validation("originalAmount", "O valor original deve ser maior que zero.");
        if (body.CurrentAmount < 0) return Validation("currentAmount", "O valor atual não pode ser negativo.");
        if (body.InterestRate is < 0) return Validation("interestRate", "A taxa de juros não pode ser negativa.");
        if (body.Notes?.Length > 1000) return Validation("notes", "As observações devem ter até 1000 caracteres."); return null;
    }

    private static (int Year, int Month, IResult? Error) Period(int? year, int? month)
    {
        var today = DateOnly.FromDateTime(DateTime.Today); var selectedYear = year ?? today.Year; var selectedMonth = month ?? today.Month;
        return selectedYear is < 2000 or > 2200 || selectedMonth is < 1 or > 12 ? (selectedYear, selectedMonth, Validation("period", "Mês ou ano inválido.")) : (selectedYear, selectedMonth, null);
    }

    private static Guid UserId(ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static IResult Validation(string key, string message) => Results.ValidationProblem(new Dictionary<string, string[]> { [key] = [message] });
}

public sealed record CardRequest(string Name, string? Bank, decimal Limit, int ClosingDay, int DueDay, string? Last4, string? Color);
public sealed record AccountRequest(string Name, FinancialAccountType Type, decimal Balance, string? Color);
public sealed record TransferRequest(Guid FromAccountId, Guid ToAccountId, decimal Amount, DateOnly? Date, string? Notes);
public sealed record ScheduledRequest(FinancialTransactionType Type, Guid CategoryId, string Description, decimal Amount, DateOnly DueDate, Guid? AccountId, string? Notes);
public sealed record StatusRequest(ScheduledTransactionStatus Status);
public sealed record DebtRequest(string Name, decimal OriginalAmount, decimal CurrentAmount, int? InstallmentCount, decimal? InterestRate, DateOnly? DueDate, string? Institution, string? Notes);
public sealed record ReserveRequest(decimal TargetAmount, decimal CurrentAmount, int? TargetMonths, string? Notes);

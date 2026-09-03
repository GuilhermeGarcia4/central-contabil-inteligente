using System.Security.Claims;
using CentralContabil.Api.Modules.AI.Application;
using CentralContabil.Api.Modules.Finance.Application;
using CentralContabil.Api.Modules.Finance.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CentralContabil.Api.Endpoints;

public static class FinanceV7Endpoints
{
    public static void MapFinanceV7Api(this WebApplication app)
    {
        var finance = app.MapGroup("/api/v1/finance").RequireAuthorization().WithTags("Financial Intelligence V7");

        finance.MapGet("/understand-month", async (int? year, int? month, ClaimsPrincipal principal, FinanceInsightService service, CancellationToken ct) =>
        {
            var (y, m, error) = Period(year, month); if (error is not null) return error;
            return Results.Ok(await service.UnderstandMonthAsync(UserId(principal), y, m, ct));
        });
        finance.MapGet("/summary-weekly", async (ClaimsPrincipal principal, FinanceInsightService service, CancellationToken ct) => Results.Ok(await service.WeeklySummaryAsync(UserId(principal), ct)));
        finance.MapGet("/summary-monthly", async (int? year, int? month, ClaimsPrincipal principal, FinanceInsightService service, CancellationToken ct) =>
        {
            var (y, m, error) = Period(year, month); if (error is not null) return error;
            return Results.Ok(await service.MonthlySummaryAsync(UserId(principal), y, m, ct));
        });
        finance.MapGet("/insights", async (int? year, int? month, ClaimsPrincipal principal, FinanceInsightService service, CancellationToken ct) =>
        {
            var (y, m, error) = Period(year, month); if (error is not null) return error;
            return Results.Ok(await service.GenerateInsightsAsync(UserId(principal), y, m, ct));
        });

        finance.MapGet("/alerts", async (ClaimsPrincipal principal, FinancialAlertService service, CancellationToken ct) => Results.Ok(await service.GenerateAsync(UserId(principal), ct)));

        finance.MapGet("/preferences", async (ClaimsPrincipal principal, UserPreferenceService service, CancellationToken ct) => Results.Ok(await service.GetAsync(UserId(principal), ct)));
        finance.MapPut("/preferences", async (PreferenceRequest body, ClaimsPrincipal principal, UserPreferenceService service, CancellationToken ct) =>
            Results.Ok(await service.UpsertAsync(UserId(principal), body.ExplanationProfile, body.AlertBills, body.AlertInvoices, body.AlertBudget, body.AlertGoals, body.AlertInstallments, body.AlertWeeklySummary, body.AlertMonthlySummary, ct)));

        finance.MapPost("/imports/preview", async (IFormFile file, ClaimsPrincipal principal, FinancialImportService service, CancellationToken ct) =>
        {
            if (file is null || file.Length == 0) return Results.BadRequest(new { title = "Envie um arquivo." });
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension is not (".csv" or ".ofx")) return Results.BadRequest(new { title = "Formato não suportado. Envie um arquivo .csv ou .ofx." });
            if (file.Length > 5 * 1024 * 1024) return Results.BadRequest(new { title = "O arquivo excede o limite de 5 MB." });
            try { return Results.Ok(await service.ParseCsvAsync(UserId(principal), file.FileName, file.OpenReadStream(), ct)); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { title = ex.Message }); }
        }).DisableAntiforgery();
        finance.MapPost("/imports/confirm", async (ImportConfirmRequest body, ClaimsPrincipal principal, FinancialImportService service, CancellationToken ct) =>
        {
            if (body.Rows is null || body.Rows.Count == 0 || body.Rows.Count > 20000) return Results.BadRequest(new { title = "Nenhum lançamento para importar." });
            return Results.Ok(await service.ConfirmAsync(UserId(principal), body.Rows, ct));
        });

        var assistant = app.MapGroup("/api/v1/assistant").WithTags("Assistant V7");
        assistant.MapGet("/conversations", async (ClaimsPrincipal principal, AssistantConversationService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(UserId(principal), ct))).RequireAuthorization();
        assistant.MapGet("/conversations/{id:guid}", async (Guid id, ClaimsPrincipal principal, AssistantConversationService service, CancellationToken ct) =>
            Results.Ok(await service.GetAsync(id, UserId(principal), ct))).RequireAuthorization();
        assistant.MapPost("/conversations", async (NewConversationRequest body, ClaimsPrincipal principal, AssistantConversationService service, CancellationToken ct) =>
            Results.Ok(new { id = await service.CreateAsync(UserId(principal), body.Title ?? "", ct) })).RequireAuthorization();
        assistant.MapDelete("/conversations/{id:guid}", async (Guid id, ClaimsPrincipal principal, AssistantConversationService service, CancellationToken ct) =>
            await service.DeleteAsync(id, UserId(principal), ct) ? Results.NoContent() : Results.NotFound()).RequireAuthorization();
        assistant.MapDelete("/conversations", async (ClaimsPrincipal principal, AssistantConversationService service, CancellationToken ct) =>
            Results.Ok(new { removed = await service.ClearAsync(UserId(principal), ct) })).RequireAuthorization();
    }

    private static (int Year, int Month, IResult? Error) Period(int? year, int? month)
    {
        var today = DateOnly.FromDateTime(DateTime.Today); var y = year ?? today.Year; var m = month ?? today.Month;
        return y is < 2000 or > 2200 || m is < 1 or > 12 ? (y, m, Results.BadRequest(new { title = "Mês ou ano inválido." })) : (y, m, null);
    }

    private static Guid UserId(ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public sealed record PreferenceRequest(ExplanationProfile ExplanationProfile, bool AlertBills, bool AlertInvoices, bool AlertBudget, bool AlertGoals, bool AlertInstallments, bool AlertWeeklySummary, bool AlertMonthlySummary);
public sealed record ImportConfirmRequest(IReadOnlyList<ImportRowInput> Rows);
public sealed record NewConversationRequest(string? Title);

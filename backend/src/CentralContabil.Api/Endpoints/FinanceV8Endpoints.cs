using System.Security.Claims;
using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Finance.Application;
using CentralContabil.Api.Modules.Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.Api.Endpoints;

public static class FinanceV8Endpoints
{
    public static void MapFinanceV8Api(this WebApplication app)
    {
        var finance = app.MapGroup("/api/v1/finance").RequireAuthorization().WithTags("Financial Dashboard V8");

        finance.MapGet("/overview", async (DateOnly? startDate, DateOnly? endDate, ClaimsPrincipal principal,
            FinanceOverviewService overview, FinancialRecurrenceService recurrences, CancellationToken ct) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var start = startDate ?? new DateOnly(today.Year, today.Month, 1);
            var end = endDate ?? start.AddMonths(1).AddDays(-1);
            if (end < start || end.DayNumber - start.DayNumber > 1_826)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["period"] = ["Informe um período válido de no máximo cinco anos."]
                });

            var userId = UserId(principal);
            await recurrences.MaterializeAsync(userId, end, ct);
            return Results.Ok(await overview.GetAsync(userId, start, end, ct));
        });

        finance.MapGet("/category-colors", async (ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) =>
        {
            var userId = UserId(principal);
            var categories = await db.FinancialCategories.AsNoTracking()
                .Where(item => item.IsActive && (item.UserId == null || item.UserId == userId))
                .OrderBy(item => item.Type).ThenBy(item => item.Name)
                .Select(item => new { item.Id, item.Name, item.Type, item.Icon, item.IsDefault, IsCustom = item.UserId != null })
                .ToListAsync(ct);
            var ids = categories.Select(item => item.Id).ToArray();
            var preferences = await db.FinancialCategoryChartPreferences.AsNoTracking()
                .Where(item => item.UserId == userId && ids.Contains(item.CategoryId))
                .ToDictionaryAsync(item => item.CategoryId, item => item.ChartColor, ct);
            return Results.Ok(categories.Select(item => new
            {
                item.Id,
                item.Name,
                item.Type,
                item.Icon,
                item.IsDefault,
                item.IsCustom,
                ChartColor = preferences.GetValueOrDefault(item.Id) ?? FinancialChartPalette.Fallback(item.Id),
                HasCustomColor = preferences.ContainsKey(item.Id)
            }));
        });

        finance.MapPut("/categories/{categoryId:guid}/chart-color", async (Guid categoryId, ChartColorRequest body,
            ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) =>
        {
            var userId = UserId(principal);
            if (!FinancialChartPalette.IsValid(body.ChartColor))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["chartColor"] = ["Use uma cor no formato hexadecimal #RRGGBB."]
                });
            if (!await db.FinancialCategories.AnyAsync(item => item.Id == categoryId && item.IsActive &&
                (item.UserId == null || item.UserId == userId), ct)) return Results.NotFound();

            var color = FinancialChartPalette.Normalize(body.ChartColor);
            var preference = await db.FinancialCategoryChartPreferences
                .FirstOrDefaultAsync(item => item.UserId == userId && item.CategoryId == categoryId, ct);
            if (preference is null)
                db.FinancialCategoryChartPreferences.Add(new FinancialCategoryChartPreference
                    { UserId = userId, CategoryId = categoryId, ChartColor = color });
            else
            {
                preference.ChartColor = color;
                preference.UpdatedAt = DateTimeOffset.UtcNow;
            }
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { CategoryId = categoryId, ChartColor = color, HasCustomColor = true });
        });

        finance.MapDelete("/categories/{categoryId:guid}/chart-color", async (Guid categoryId,
            ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) =>
        {
            var userId = UserId(principal);
            if (!await db.FinancialCategories.AnyAsync(item => item.Id == categoryId && item.IsActive &&
                (item.UserId == null || item.UserId == userId), ct)) return Results.NotFound();
            var preference = await db.FinancialCategoryChartPreferences
                .FirstOrDefaultAsync(item => item.UserId == userId && item.CategoryId == categoryId, ct);
            if (preference is not null)
            {
                db.FinancialCategoryChartPreferences.Remove(preference);
                await db.SaveChangesAsync(ct);
            }
            return Results.Ok(new
            {
                CategoryId = categoryId,
                ChartColor = FinancialChartPalette.Fallback(categoryId),
                HasCustomColor = false
            });
        });
    }

    private static Guid UserId(ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public sealed record ChartColorRequest(string ChartColor);

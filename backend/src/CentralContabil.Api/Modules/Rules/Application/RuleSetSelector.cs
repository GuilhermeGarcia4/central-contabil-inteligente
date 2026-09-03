using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.Api.Modules.Rules.Application;

public interface IRuleSetSelector
{
    Task<CalculationRuleSet?> GetActiveAsync(string calculatorSlug, DateOnly referenceDate, CancellationToken ct);
}

public sealed class RuleSetSelector(AppDbContext db) : IRuleSetSelector
{
    public Task<CalculationRuleSet?> GetActiveAsync(string calculatorSlug, DateOnly referenceDate, CancellationToken ct) =>
        db.CalculationRuleSets.AsNoTracking()
          .Include(x => x.Calculator).Include(x => x.Parameters).Include(x => x.Source)
          .Where(x => x.IsActive && x.Calculator.IsActive && x.Calculator.Slug == calculatorSlug)
          .Where(x => x.ValidFrom <= referenceDate && (x.ValidUntil == null || x.ValidUntil >= referenceDate))
          .OrderByDescending(x => x.ValidFrom).ThenByDescending(x => x.CreatedAt)
          .FirstOrDefaultAsync(ct);
}


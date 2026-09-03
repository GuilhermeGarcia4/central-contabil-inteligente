using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Rules.Application;
using CentralContabil.Api.Modules.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.UnitTests;

public sealed class RuleSetSelectorTests
{
    [Fact] public async Task Selects_rule_whose_validity_contains_reference_date()
    {
        await using var db = Db(); var calculator = new Calculator { Name = "Teste", Slug = "teste" };
        db.AddRange(calculator,
            new CalculationRuleSet { Calculator = calculator, Name = "2025", Version = "1", ValidFrom = new(2025, 1, 1), ValidUntil = new(2025, 12, 31) },
            new CalculationRuleSet { Calculator = calculator, Name = "2026", Version = "2", ValidFrom = new(2026, 1, 1), ValidUntil = new(2026, 12, 31) });
        await db.SaveChangesAsync();
        var selected = await new RuleSetSelector(db).GetActiveAsync("teste", new DateOnly(2026, 8, 1), default);
        Assert.Equal("2", selected?.Version);
    }

    [Fact] public async Task Open_ended_rule_remains_active()
    {
        await using var db = Db(); var calculator = new Calculator { Name = "Teste", Slug = "teste" };
        db.Add(new CalculationRuleSet { Calculator = calculator, Name = "Atual", Version = "1", ValidFrom = new(2026, 1, 1), ValidUntil = null }); await db.SaveChangesAsync();
        Assert.NotNull(await new RuleSetSelector(db).GetActiveAsync("teste", new DateOnly(2030, 1, 1), default));
    }
    private static AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}


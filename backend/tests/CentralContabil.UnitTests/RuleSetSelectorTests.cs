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

    [Fact]
    public async Task Future_expired_and_inactive_rules_are_not_selected()
    {
        await using var db = Db();
        var calculator = new Calculator { Name = "Teste", Slug = "teste" };
        db.AddRange(
            new CalculationRuleSet { Calculator = calculator, Name = "Expirada", Version = "expired", ValidFrom = new(2025, 1, 1), ValidUntil = new(2025, 12, 31) },
            new CalculationRuleSet { Calculator = calculator, Name = "Futura", Version = "future", ValidFrom = new(2027, 1, 1) },
            new CalculationRuleSet { Calculator = calculator, Name = "Inativa", Version = "inactive", ValidFrom = new(2026, 1, 1), IsActive = false });
        await db.SaveChangesAsync();

        var selected = await new RuleSetSelector(db).GetActiveAsync("teste", new DateOnly(2026, 9, 1), default);

        Assert.Null(selected);
    }

    [Fact]
    public async Task Vacation_calculator_finds_current_active_rule()
    {
        await using var db = Db();
        var calculator = new Calculator { Name = "Férias", Slug = "ferias" };
        db.Add(new CalculationRuleSet
        {
            Calculator = calculator,
            Name = "Regra administrada",
            Version = "approved-1",
            ValidFrom = new(2026, 1, 1),
            Parameters =
            [
                new RuleParameter
                {
                    Key = "AdditionalVacationPercentage",
                    Value = "0.333333",
                    ValueType = ParameterValueType.Decimal
                }
            ]
        });
        await db.SaveChangesAsync();

        var selected = await new RuleSetSelector(db).GetActiveAsync("ferias", new DateOnly(2026, 9, 1), default);

        Assert.NotNull(selected);
        Assert.Equal("approved-1", selected.Version);
        Assert.Single(selected.Parameters);
    }

    [Fact]
    public async Task Missing_rule_returns_no_selection()
    {
        await using var db = Db();
        db.Add(new Calculator { Name = "Férias", Slug = "ferias" });
        await db.SaveChangesAsync();

        Assert.Null(await new RuleSetSelector(db).GetActiveAsync("ferias", new DateOnly(2026, 9, 1), default));
    }

    private static AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}

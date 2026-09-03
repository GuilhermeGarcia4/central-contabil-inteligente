using CentralContabil.Api.Modules.Calculators.Application;
using CentralContabil.Api.Modules.Shared.Domain;

namespace CentralContabil.UnitTests;

public sealed class CalculatorTests
{
    private static CalculationRuleSet Rule(string slug = "test") => new() { Name = "Regra teste", Version = "1", ValidFrom = new DateOnly(2026, 1, 1), Calculator = new Calculator { Name = slug, Slug = slug } };

    [Fact] public void Compound_interest_uses_decimal_and_monthly_compounding()
    {
        var result = new CompoundInterestCalculator().Calculate(new CompoundInterestInput(1000m, 1m, 12), Rule());
        Assert.Equal(1126.83m, result.Result);
        Assert.Equal(126.83m, result.Breakdown["totalJuros"]);
    }

    [Fact] public void Compound_interest_includes_end_of_period_contributions()
    {
        var result = new CompoundInterestCalculator().Calculate(new CompoundInterestInput(0m, 1m, 2, 100m), Rule());
        Assert.Equal(201m, result.Result);
        Assert.Equal(200m, result.Breakdown["totalAportado"]);
    }

    [Fact] public void Vacation_returns_separate_gross_components()
    {
        var rule = Rule("ferias"); rule.Parameters.Add(new RuleParameter { Key = "AdditionalVacationPercentage", Value = "0.333333", ValueType = ParameterValueType.Decimal });
        var result = new VacationCalculator().Calculate(new VacationInput(3000m, 30, false), rule);
        Assert.Equal(4000m, result.Result);
        Assert.Equal(1000m, result.Breakdown["tercoConstitucional"]);
    }

    [Fact] public void Thirteenth_salary_is_proportional_to_months()
    {
        var result = new ThirteenthSalaryCalculator().Calculate(new ThirteenthSalaryInput(3000m, 6), Rule());
        Assert.Equal(1500m, result.Result);
    }
}


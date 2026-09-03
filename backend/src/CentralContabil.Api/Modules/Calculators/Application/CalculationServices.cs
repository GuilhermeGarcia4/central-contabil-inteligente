using CentralContabil.Api.Modules.Shared.Domain;
using System.Globalization;

namespace CentralContabil.Api.Modules.Calculators.Application;

public sealed record RuleMetadata(Guid Id, string Name, string Version, DateOnly ValidFrom, DateOnly? ValidUntil, string? Source);
public sealed record CalculationResponse(decimal Result, IReadOnlyDictionary<string, decimal> Breakdown, string Formula, IReadOnlyList<string> Steps, RuleMetadata Rule);
public sealed record CompoundInterestInput(decimal InitialCapital, decimal MonthlyRatePercent, int Months, decimal MonthlyContribution = 0);
public sealed record VacationInput(decimal GrossSalary, int VacationDays, bool SellOneThird = false);
public sealed record ThirteenthSalaryInput(decimal Salary, int MonthsWorked);

public interface IFinancialCalculator
{
    string Slug { get; }
    CalculationResponse Calculate(object input, CalculationRuleSet ruleSet);
}

public sealed class CompoundInterestCalculator : IFinancialCalculator
{
    public string Slug => "juros-compostos";
    public CalculationResponse Calculate(object value, CalculationRuleSet ruleSet)
    {
        var input = (CompoundInterestInput)value;
        ArgumentOutOfRangeException.ThrowIfNegative(input.InitialCapital);
        ArgumentOutOfRangeException.ThrowIfNegative(input.MonthlyRatePercent);
        ArgumentOutOfRangeException.ThrowIfNegative(input.MonthlyContribution);
        if (input.Months is < 1 or > 1200) throw new ArgumentOutOfRangeException(nameof(input.Months));
        var rate = input.MonthlyRatePercent / 100m;
        var balance = input.InitialCapital;
        for (var month = 0; month < input.Months; month++) balance = balance * (1 + rate) + input.MonthlyContribution;
        var contributed = input.InitialCapital + input.MonthlyContribution * input.Months;
        var final = decimal.Round(balance, 2, MidpointRounding.ToEven);
        return Response(final, new Dictionary<string, decimal> {
            ["capitalInicial"] = input.InitialCapital, ["totalAportado"] = contributed,
            ["totalJuros"] = decimal.Round(final - contributed, 2), ["valorFinal"] = final },
            "M = C × (1 + i)^n + A × (((1 + i)^n − 1) / i)",
            [$"Taxa mensal: {input.MonthlyRatePercent:N4}%", $"Períodos: {input.Months}", $"Aportes ao fim de cada período: {input.MonthlyContribution:C}"], ruleSet);
    }
    internal static CalculationResponse Response(decimal result, IReadOnlyDictionary<string, decimal> breakdown, string formula, IReadOnlyList<string> steps, CalculationRuleSet rs) =>
        new(result, breakdown, formula, steps, new(rs.Id, rs.Name, rs.Version, rs.ValidFrom, rs.ValidUntil, rs.Source?.Url));
}

public sealed class VacationCalculator : IFinancialCalculator
{
    public string Slug => "ferias";
    public CalculationResponse Calculate(object value, CalculationRuleSet rs)
    {
        var input = (VacationInput)value;
        if (input.GrossSalary <= 0 || input.VacationDays is < 1 or > 30) throw new ArgumentOutOfRangeException(nameof(input));
        var additional = Parameter(rs, "AdditionalVacationPercentage", 0.333333m);
        var daily = input.GrossSalary / 30m;
        var vacation = daily * input.VacationDays;
        var constitutional = vacation * additional;
        var soldDays = input.SellOneThird ? Math.Min(10, input.VacationDays / 3) : 0;
        var allowance = daily * soldDays;
        var allowanceAdditional = allowance * additional;
        var total = decimal.Round(vacation + constitutional + allowance + allowanceAdditional, 2);
        return CompoundInterestCalculator.Response(total, new Dictionary<string, decimal> {
            ["remuneracaoFerias"] = decimal.Round(vacation, 2), ["tercoConstitucional"] = decimal.Round(constitutional, 2),
            ["abonoPecuniario"] = decimal.Round(allowance, 2), ["tercoSobreAbono"] = decimal.Round(allowanceAdditional, 2), ["totalBruto"] = total },
            "remuneração proporcional + 1/3 + eventual abono",
            ["Estimativa bruta, sem descontos.", "TODO: validar incidências e casos específicos com fonte oficial antes da produção."], rs);
    }
    private static decimal Parameter(CalculationRuleSet rs, string key, decimal fallback) =>
        decimal.TryParse(rs.Parameters.FirstOrDefault(x => x.Key == key)?.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : fallback;
}

public sealed class ThirteenthSalaryCalculator : IFinancialCalculator
{
    public string Slug => "decimo-terceiro";
    public CalculationResponse Calculate(object value, CalculationRuleSet rs)
    {
        var input = (ThirteenthSalaryInput)value;
        if (input.Salary <= 0 || input.MonthsWorked is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(input));
        var amount = decimal.Round(input.Salary / 12m * input.MonthsWorked, 2);
        return CompoundInterestCalculator.Response(amount, new Dictionary<string, decimal> {
            ["salarioBase"] = input.Salary, ["avos"] = input.MonthsWorked, ["valorBruto"] = amount },
            "salário ÷ 12 × meses computados",
            ["Estimativa bruta, sem descontos.", "TODO: validar critérios de mês computado e incidências com fonte oficial antes da produção."], rs);
    }
}

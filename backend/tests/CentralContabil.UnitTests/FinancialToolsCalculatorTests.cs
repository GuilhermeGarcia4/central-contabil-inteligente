using CentralContabil.Api.Modules.Calculators.Application;

namespace CentralContabil.UnitTests;

public sealed class FinancialToolsCalculatorTests
{
    private readonly FinancialToolsCalculator calculator = new();

    [Fact]
    public void Simple_interest_returns_principal_plus_linear_interest()
    {
        var result = calculator.Calculate("juros-simples", new Dictionary<string, decimal> { ["principal"] = 1000, ["monthlyRatePercent"] = 1, ["months"] = 12 });
        Assert.Equal(1120m, result.Result);
        Assert.Equal(120m, result.Breakdown["juros"]);
    }

    [Fact]
    public void Price_and_sac_have_expected_total_cost_and_amortization_schedule()
    {
        var input = new Dictionary<string, decimal> { ["principal"] = 10000, ["monthlyRatePercent"] = 1, ["months"] = 12 };
        var price = calculator.Calculate("financiamento-price", input);
        var sac = calculator.Calculate("financiamento-sac", input);

        Assert.Equal(10661.85m, price.Result);
        Assert.Equal(12, price.Schedule.Count);
        Assert.Equal(10650m, sac.Result);
        Assert.Equal(933.33m, sac.Breakdown["primeiraParcela"]);
        Assert.Equal(841.67m, sac.Breakdown["ultimaParcela"]);
    }

    [Fact]
    public void Regular_monthly_contributions_are_added_at_period_end()
    {
        var result = calculator.Calculate("investimento-aportes", new Dictionary<string, decimal> { ["initialCapital"] = 1000, ["monthlyContribution"] = 500, ["monthlyRatePercent"] = 0, ["months"] = 12 });
        Assert.Equal(7000m, result.Result);
        Assert.Equal(6000m, result.Breakdown["aportes"]);
    }

    [Fact]
    public void Compound_interest_calculator_does_not_require_or_store_a_user_account()
    {
        var result = calculator.Calculate("juros-compostos", new Dictionary<string, decimal> { ["initialCapital"] = 1000, ["monthlyContribution"] = 0, ["monthlyRatePercent"] = 1, ["months"] = 12 });
        Assert.Equal(1126.83m, result.Result);
        Assert.Equal("financial-tools-1.0", result.Version);
    }

    [Fact]
    public void Effective_cost_is_zero_when_payments_equal_net_disbursement()
    {
        var result = calculator.Calculate("cet", new Dictionary<string, decimal> { ["netAmount"] = 1000, ["upfrontCosts"] = 0, ["installmentValue"] = 100, ["months"] = 10 });
        Assert.Equal(0m, result.Breakdown["cetMensalPercentual"]);
        Assert.Equal(0m, result.Breakdown["cetAnualPercentual"]);
    }

    [Fact]
    public void Excessive_term_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Calculate("juros-simples", new Dictionary<string, decimal> { ["principal"] = 1000, ["monthlyRatePercent"] = 1, ["months"] = 1201 }));
    }

    [Fact]
    public void Excessive_monthly_rate_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Calculate("financiamento-price", new Dictionary<string, decimal> { ["principal"] = 1000, ["monthlyRatePercent"] = 1001, ["months"] = 12 }));
    }
}

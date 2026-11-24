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

    [Fact]
    public void Debt_renegotiation_uses_only_the_entered_assumptions()
    {
        var result = calculator.Calculate("renegociacao-divida", new Dictionary<string, decimal> { ["balance"] = 1000, ["discountPercent"] = 10, ["downPayment"] = 100, ["costs"] = 0, ["monthlyRatePercent"] = 0, ["months"] = 9 });
        Assert.Equal(900m, result.Breakdown["saldoNegociado"]);
        Assert.Equal(88.89m, result.Breakdown["parcelaEstimada"]);
        Assert.Contains(result.Steps, x => x.Contains("hipóteses digitadas", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Net_return_deducts_only_user_entered_tax_and_fees()
    {
        var result = calculator.Calculate("rendimento-liquido", new Dictionary<string, decimal> { ["principal"] = 1000, ["monthlyRatePercent"] = 1, ["months"] = 12, ["incomeTaxPercent"] = 15, ["iofPercent"] = 0, ["fees"] = 2 });
        Assert.Equal(1105.8m, result.Result);
    }

    [Fact]
    public void Revolving_card_scenario_caps_accumulated_interest_at_original_balance()
    {
        var result = calculator.Calculate("cartao-credito", new Dictionary<string, decimal> { ["balance"] = 1000, ["monthlyRatePercent"] = 100, ["paymentPercent"] = 1, ["months"] = 24, ["loanMonthlyRatePercent"] = 0 });
        Assert.True(result.Breakdown["encargosAcumuladosLimitados"] <= 1000m);
        Assert.Equal(1000m, result.Breakdown["encargosAcumuladosLimitados"]);
        Assert.Equal(24, result.Schedule.Count);
    }

    [Fact]
    public void Fixed_payment_solver_resolves_principal_and_monthly_rate()
    {
        var principal = calculator.SolvePrice("principal", new Dictionary<string, decimal> { ["installmentValue"] = 100, ["monthlyRatePercent"] = 0, ["months"] = 10 });
        Assert.Equal(1000m, principal.Result);
        var rate = calculator.SolvePrice("monthlyRatePercent", new Dictionary<string, decimal> { ["principal"] = 1000, ["installmentValue"] = 100, ["months"] = 10 });
        Assert.Equal(0m, rate.Result);
    }

    [Fact]
    public void Regular_investment_supports_beginning_of_period_contributions()
    {
        var result = calculator.Calculate("investimento-aportes", new Dictionary<string, decimal> { ["initialCapital"] = 0, ["monthlyContribution"] = 100, ["monthlyRatePercent"] = 10, ["months"] = 2, ["depositAtBeginning"] = 1 });
        Assert.Equal(231m, result.Result);
        Assert.True(result.Steps[0].Contains("início", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, result.Schedule.Count);
    }

    [Fact]
    public void Regular_investment_solver_resolves_contribution_and_term_at_zero_interest()
    {
        var contribution = calculator.SolveRegularInvestment("monthlyContribution", new Dictionary<string, decimal> { ["initialCapital"] = 1000, ["monthlyRatePercent"] = 0, ["months"] = 12, ["futureValue"] = 7000 });
        Assert.Equal(500m, contribution.Result);
        var term = calculator.SolveRegularInvestment("months", new Dictionary<string, decimal> { ["initialCapital"] = 0, ["monthlyContribution"] = 100, ["monthlyRatePercent"] = 0, ["futureValue"] = 1000 });
        Assert.Equal(10m, term.Result);
    }

    [Fact]
    public void Regular_investment_solver_resolves_monthly_rate_numerically()
    {
        var result = calculator.SolveRegularInvestment("monthlyRatePercent", new Dictionary<string, decimal> { ["initialCapital"] = 0, ["monthlyContribution"] = 100, ["months"] = 12, ["futureValue"] = 1268.25m });
        Assert.InRange(result.Result, 0.99m, 1.01m);
    }
}

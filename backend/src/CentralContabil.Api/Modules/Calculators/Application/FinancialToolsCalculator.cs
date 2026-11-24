namespace CentralContabil.Api.Modules.Calculators.Application;

public sealed record AmortizationRow(int Period, decimal Payment, decimal Principal, decimal Interest, decimal Balance);
public sealed record FinancialToolResponse(decimal Result, IReadOnlyDictionary<string, decimal> Breakdown, string Formula, IReadOnlyList<string> Steps, IReadOnlyList<AmortizationRow> Schedule, string Version);

public sealed class FinancialToolsCalculator
{
    private const int MaxPeriods = 1200;

    public FinancialToolResponse Calculate(string slug, IReadOnlyDictionary<string, decimal> input)
    {
        Validate(input);
        return slug switch
        {
            "juros-simples" => SimpleInterest(input),
            "juros-compostos" => CompoundInterest(input),
            "financiamento-fixas" or "financiamento-price" => Price(input),
            "financiamento-sac" => Sac(input),
            "simulacao-emprestimo" => Loan(input),
            "investimento-aportes" => RegularInvestment(input),
            "rentabilidade-investimento" => InvestmentReturn(input),
            "correcao-monetaria" => MonetaryCorrection(input),
            "reserva-emergencia" => EmergencyReserve(input),
            "planejamento-mensal" => MonthlyPlanning(input),
            "quitacao-antecipada" => EarlyPayoff(input),
            "portabilidade-divida" => DebtPortability(input),
            "consorcio-financiamento" => ConsortiumVsFinancing(input),
            "cet" => EffectiveCost(input),
            _ => throw new KeyNotFoundException("Calculadora não encontrada.")
        };
    }

    private static FinancialToolResponse SimpleInterest(IReadOnlyDictionary<string, decimal> x)
    {
        var principal = Required(x, "principal", 0.01m); var rate = Required(x, "monthlyRatePercent", 0); var months = Periods(x);
        var interest = principal * rate / 100m * months; var total = principal + interest;
        return Result(total, [ ("capitalInicial", principal), ("juros", interest), ("total", total) ], "J = C × i × n; M = C + J", $"Taxa simples mensal informada: {rate:N4}% por {months} meses.");
    }

    private static FinancialToolResponse CompoundInterest(IReadOnlyDictionary<string, decimal> x)
    {
        var initial = Optional(x, "initialCapital"); var contribution = Optional(x, "monthlyContribution"); var ratePercent = Required(x, "monthlyRatePercent", 0); var months = Periods(x); var rate = ratePercent / 100m;
        var balance = initial;
        for (var month = 0; month < months; month++) balance = balance * (1 + rate) + contribution;
        var paidIn = initial + contribution * months;
        return Result(balance, [("capitalInicial", initial), ("aportes", contribution * months), ("totalInvestido", paidIn), ("jurosEstimados", balance - paidIn), ("saldoFinalEstimado", balance)], "M = C × (1 + i)^n + A × (((1 + i)^n − 1) / i)", $"Capitalização mensal, com aportes ao fim do período; taxa informada {ratePercent:N4}%; sem impostos, tarifas ou oscilação de mercado.");
    }

    private static FinancialToolResponse Price(IReadOnlyDictionary<string, decimal> x)
    {
        var principal = Required(x, "principal", 0.01m); var ratePercent = Required(x, "monthlyRatePercent", 0); var months = Periods(x);
        var rate = ratePercent / 100m; var payment = Payment(principal, rate, months); var balance = principal; var schedule = new List<AmortizationRow>(months); var interestTotal = 0m;
        for (var period = 1; period <= months; period++)
        {
            var interest = balance * rate; var principalPart = Math.Min(balance, payment - interest); balance = Math.Max(0, balance - principalPart); interestTotal += interest;
            schedule.Add(new(period, decimal.Round(payment, 2), decimal.Round(principalPart, 2), decimal.Round(interest, 2), decimal.Round(balance, 2)));
        }
        var total = payment * months;
        return Result(total, [("valorFinanciado", principal), ("parcelaEstimada", payment), ("totalPago", total), ("totalJuros", interestTotal)], "PMT = PV × i / (1 − (1 + i)^−n)", $"Sistema Price; parcelas constantes; taxa mensal nominal informada {ratePercent:N4}%.", schedule);
    }

    private static FinancialToolResponse Sac(IReadOnlyDictionary<string, decimal> x)
    {
        var principal = Required(x, "principal", 0.01m); var ratePercent = Required(x, "monthlyRatePercent", 0); var months = Periods(x); var rate = ratePercent / 100m;
        var amortization = principal / months; var balance = principal; var total = 0m; var interestTotal = 0m; var schedule = new List<AmortizationRow>(months);
        for (var period = 1; period <= months; period++) { var interest = balance * rate; var principalPart = Math.Min(balance, amortization); var payment = principalPart + interest; balance = Math.Max(0, balance - principalPart); total += payment; interestTotal += interest; schedule.Add(new(period, decimal.Round(payment, 2), decimal.Round(principalPart, 2), decimal.Round(interest, 2), decimal.Round(balance, 2))); }
        return Result(total, [("valorFinanciado", principal), ("primeiraParcela", schedule[0].Payment), ("ultimaParcela", schedule[^1].Payment), ("totalPago", total), ("totalJuros", interestTotal)], "A = PV / n; parcela(k) = A + saldo(k−1) × i", $"Sistema SAC; amortização constante; taxa mensal informada {ratePercent:N4}%.", schedule);
    }

    private static FinancialToolResponse Loan(IReadOnlyDictionary<string, decimal> x)
    {
        var principal = Required(x, "principal", 0.01m); var fees = Optional(x, "fees") + Optional(x, "insurance") + Optional(x, "iof") + Optional(x, "otherCosts");
        var rate = Required(x, "monthlyRatePercent", 0) / 100m; var months = Periods(x); var financed = principal + fees; var payment = Payment(financed, rate, months); var total = payment * months;
        return Result(total, [("valorSolicitado", principal), ("encargosFinanciados", fees), ("saldoFinanciado", financed), ("parcelaEstimada", payment), ("totalPago", total), ("jurosEstimados", total - financed)], "Saldo financiado = valor solicitado + encargos; parcela pela fórmula Price", "Simulação considera todos os encargos informados financiados junto ao principal; confirme a forma de cobrança na proposta.");
    }

    private static FinancialToolResponse RegularInvestment(IReadOnlyDictionary<string, decimal> x)
    {
        var initial = Optional(x, "initialCapital"); var contribution = Required(x, "monthlyContribution", 0); var rate = Required(x, "monthlyRatePercent", 0) / 100m; var months = Periods(x);
        var balance = initial; for (var i = 0; i < months; i++) balance = balance * (1 + rate) + contribution;
        var contributed = initial + contribution * months;
        return Result(balance, [("capitalInicial", initial), ("aportes", contribution * months), ("totalInvestido", contributed), ("rendimentoEstimado", balance - contributed), ("saldoFinalEstimado", balance)], "M = C(1+i)^n + A × ((1+i)^n − 1) / i", $"Aportes ao fim de cada mês; taxa mensal informada {rate * 100:N4}%; sem impostos, taxas ou oscilação de mercado.");
    }

    private static FinancialToolResponse InvestmentReturn(IReadOnlyDictionary<string, decimal> x)
    {
        var initial = Required(x, "initialCapital", 0.01m); var final = Required(x, "finalValue", 0); var months = Periods(x); var returnPercent = (final / initial - 1) * 100m;
        var annualFactor = final == 0 ? 0d : Math.Pow((double)(final / initial), 12d / months);
        if (!double.IsFinite(annualFactor) || annualFactor > (double)decimal.MaxValue / 100d + 1d)
            throw new ArgumentOutOfRangeException("finalValue", "A taxa anualizada excede o limite numérico desta simulação.");
        var annualized = final == 0 ? -100m : ((decimal)annualFactor - 1) * 100m;
        return Result(returnPercent, [("valorInicial", initial), ("valorFinal", final), ("ganhoOuPerda", final - initial), ("rentabilidadeTotalPercentual", returnPercent), ("rentabilidadeAnualizadaPercentual", annualized)], "Rentabilidade = (valor final / valor inicial − 1) × 100", $"Período: {months} meses. A taxa anualizada pressupõe reinvestimento composto e não inclui aportes, impostos ou inflação.");
    }

    private static FinancialToolResponse MonetaryCorrection(IReadOnlyDictionary<string, decimal> x)
    {
        var principal = Required(x, "principal", 0); var cumulativeRate = Required(x, "cumulativeRatePercent", -100); if (cumulativeRate < -100) throw new ArgumentOutOfRangeException("cumulativeRatePercent");
        var corrected = principal * (1 + cumulativeRate / 100m);
        return Result(corrected, [("valorOriginal", principal), ("variacaoInformadaPercentual", cumulativeRate), ("valorCorrigidoEstimado", corrected), ("diferenca", corrected - principal)], "Valor corrigido = valor original × (1 + variação acumulada informada)", "Informe manualmente a variação acumulada do índice e do período desejados; nenhum índice é presumido.");
    }

    private static FinancialToolResponse EmergencyReserve(IReadOnlyDictionary<string, decimal> x)
    {
        var expenses = Required(x, "monthlyExpenses", 0.01m); var months = Periods(x); var existing = Optional(x, "currentReserve"); var target = expenses * months;
        return Result(target, [("despesasMensais", expenses), ("mesesDeCobertura", months), ("reservaAlvo", target), ("reservaAtual", existing), ("valorRestante", Math.Max(0, target - existing)), ("coberturaAtualEmMeses", existing / expenses)], "Reserva-alvo = despesas essenciais mensais × meses definidos", "O número de meses é uma escolha do usuário; resultado não constitui recomendação personalizada.");
    }

    private static FinancialToolResponse MonthlyPlanning(IReadOnlyDictionary<string, decimal> x)
    {
        var income = Required(x, "income", 0); var fixedCosts = Optional(x, "fixedExpenses"); var variable = Optional(x, "variableExpenses"); var debts = Optional(x, "debtPayments"); var goal = Optional(x, "savingsGoal");
        var expenses = fixedCosts + variable + debts; var balance = income - expenses; var needed = goal - balance;
        return Result(balance, [("renda", income), ("despesasFixas", fixedCosts), ("despesasVariaveis", variable), ("pagamentosDividas", debts), ("saldoAposDespesas", balance), ("metaDePoupanca", goal), ("valorAcimaOuAbaixoDaMeta", -needed)], "Saldo = renda − despesas fixas − despesas variáveis − pagamentos de dívidas", "Cálculo baseado somente nos valores informados; não representa análise individual nem aconselhamento.");
    }

    private static FinancialToolResponse EarlyPayoff(IReadOnlyDictionary<string, decimal> x)
    {
        var payment = Required(x, "installmentValue", 0.01m); var months = Periods(x); var rate = Required(x, "monthlyRatePercent", 0) / 100m; var fee = Optional(x, "fees");
        var balance = PresentValue(payment, rate, months); var payoff = balance + fee; var remaining = payment * months;
        return Result(payoff, [("saldoPresenteEstimado", balance), ("taxasInformadas", fee), ("valorDeQuitacaoEstimado", payoff), ("parcelasRestantesSemDesconto", remaining), ("diferencaEstimada", remaining - payoff)], "VP = PMT × (1 − (1+i)^−n) / i", "Estimativa de valor presente das parcelas fixas. Solicite à instituição o valor oficial de liquidação antecipada e os descontos aplicáveis.");
    }

    private static FinancialToolResponse DebtPortability(IReadOnlyDictionary<string, decimal> x)
    {
        var payment = Required(x, "installmentValue", 0.01m); var months = Periods(x); var oldRate = Required(x, "oldMonthlyRatePercent", 0) / 100m; var newRate = Required(x, "newMonthlyRatePercent", 0) / 100m; var costs = Optional(x, "transferCosts");
        var balance = PresentValue(payment, oldRate, months); var newPayment = Payment(balance, newRate, months); var newTotal = newPayment * months + costs; var oldTotal = payment * months;
        return Result(newTotal, [("saldoEstimadoAtual", balance), ("parcelaAtual", payment), ("novaParcelaEstimada", newPayment), ("totalRestanteAtual", oldTotal), ("totalEstimadoPortabilidade", newTotal), ("diferencaEstimada", oldTotal - newTotal)], "Saldo estimado = VP das parcelas atuais; nova parcela = Price(saldo, nova taxa, mesmo prazo)", "Pressupõe parcelas fixas e manutenção do prazo. Custos informados são somados; valide CET, seguros, tarifas e condições com as instituições.");
    }

    private static FinancialToolResponse ConsortiumVsFinancing(IReadOnlyDictionary<string, decimal> x)
    {
        var value = Required(x, "assetValue", 0.01m); var months = Periods(x); var administration = Required(x, "administrationPercent", 0) / 100m; var reserveFund = Required(x, "reserveFundPercent", 0) / 100m; var insurance = Optional(x, "monthlyInsurance");
        var consortiumPayment = value * (1 + administration + reserveFund) / months + insurance; var consortiumTotal = consortiumPayment * months;
        var financeRate = Required(x, "financingMonthlyRatePercent", 0) / 100m; var financePayment = Payment(value, financeRate, months); var financeTotal = financePayment * months;
        return Result(consortiumTotal - financeTotal, [("parcelaConsorcioEstimada", consortiumPayment), ("totalConsorcioEstimado", consortiumTotal), ("parcelaFinanciamentoEstimada", financePayment), ("totalFinanciamentoEstimado", financeTotal), ("diferencaConsorcioMenosFinanciamento", consortiumTotal - financeTotal)], "Consórcio: (crédito × (1 + taxa de administração + fundo reserva)) / prazo + seguro; financiamento: sistema Price", "Estimativa sem reajustes do crédito, lance, contemplação, tarifas adicionais, seguros do financiamento ou valor do dinheiro no tempo. Compare contratos completos.");
    }

    private static FinancialToolResponse EffectiveCost(IReadOnlyDictionary<string, decimal> x)
    {
        var amount = Required(x, "netAmount", 0.01m); var payment = Required(x, "installmentValue", 0.01m); var months = Periods(x); var costs = Optional(x, "upfrontCosts"); var received = amount - costs;
        if (received <= 0) throw new ArgumentOutOfRangeException("upfrontCosts", "Custos iniciais devem ser inferiores ao valor bruto.");
        decimal low = -0.999999m, high = 100m;
        for (var i = 0; i < 200; i++) { var mid = (low + high) / 2m; var pv = PresentValue(payment, mid, months); if (pv > received) low = mid; else high = mid; }
        var monthlyRate = (low + high) / 2m; var annualRate = ((decimal)Math.Pow((double)(1 + monthlyRate), 12) - 1) * 100m;
        return Result(annualRate, [("valorLiquidoRecebido", received), ("custosIniciais", costs), ("valorParcela", payment), ("totalParcelas", payment * months), ("cetMensalPercentual", monthlyRate * 100m), ("cetAnualPercentual", annualRate)], "Fluxo de caixa: valor líquido recebido = valor presente das parcelas; CET anual = (1 + CET mensal)^12 − 1", "Taxa interna estimada para parcelas mensais iguais e primeira parcela em um mês. O CET contratual pode incluir datas e despesas adicionais diferentes das informadas.");
    }

    private static decimal Payment(decimal principal, decimal monthlyRate, int months) => monthlyRate == 0 ? principal / months : principal * monthlyRate / (1 - (decimal)Math.Pow((double)(1 + monthlyRate), -months));
    private static decimal PresentValue(decimal payment, decimal monthlyRate, int months) => monthlyRate == 0 ? payment * months : payment * (1 - (decimal)Math.Pow((double)(1 + monthlyRate), -months)) / monthlyRate;
    private static int Periods(IReadOnlyDictionary<string, decimal> x) { var value = Required(x, "months", 1); if (value != decimal.Truncate(value) || value > MaxPeriods) throw new ArgumentOutOfRangeException("months", $"O prazo deve estar entre 1 e {MaxPeriods} meses."); return (int)value; }
    private static decimal Required(IReadOnlyDictionary<string, decimal> x, string key, decimal min) { if (!x.TryGetValue(key, out var value) || value < min) throw new ArgumentOutOfRangeException(key, $"Informe {key} com valor maior ou igual a {min}."); return value; }
    private static decimal Optional(IReadOnlyDictionary<string, decimal> x, string key) { var value = x.GetValueOrDefault(key); if (value < 0) throw new ArgumentOutOfRangeException(key, $"{key} não pode ser negativo."); return value; }
    private static FinancialToolResponse Result(decimal value, (string Key, decimal Value)[] fields, string formula, params string[] steps) => Result(value, fields, formula, steps, []);
    private static FinancialToolResponse Result(decimal value, (string Key, decimal Value)[] fields, string formula, string step, IReadOnlyList<AmortizationRow>? schedule = null) => Result(value, fields, formula, [step], schedule ?? []);
    private static FinancialToolResponse Result(decimal value, (string Key, decimal Value)[] fields, string formula, IReadOnlyList<string> steps, IReadOnlyList<AmortizationRow> schedule) =>
        new(decimal.Round(value, 2, MidpointRounding.ToEven), fields.ToDictionary(x => x.Key, x => decimal.Round(x.Value, 2, MidpointRounding.ToEven)), formula, steps, schedule, "financial-tools-1.0");
    private static void Validate(IReadOnlyDictionary<string, decimal> input)
    {
        if (input.Count > 32 || input.Values.Any(x => x > 1_000_000_000_000m)) throw new ArgumentOutOfRangeException(nameof(input), "Entrada acima do limite permitido.");
        if (input.Any(pair => pair.Key.Contains("RatePercent", StringComparison.OrdinalIgnoreCase) && pair.Value > 1000m))
            throw new ArgumentOutOfRangeException(nameof(input), "Taxas mensais acima de 1.000% excedem o limite da simulação.");
    }
}

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
            "renegociacao-divida" => DebtRenegotiation(input),
            "rendimento-liquido" => NetInvestmentReturn(input),
            "cartao-credito" => CreditCard(input),
            _ => throw new KeyNotFoundException("Calculadora não encontrada.")
        };
    }

    public FinancialToolResponse SolvePrice(string target, IReadOnlyDictionary<string, decimal> input)
    {
        Validate(input);
        var rate = input.GetValueOrDefault("monthlyRatePercent") / 100m;
        if (target == "principal")
        {
            var payment = Required(input, "installmentValue", 0.01m); var months = Periods(input);
            var principal = PresentValue(payment, rate, months);
            return Result(principal, [("valorFinanciadoResolvido", principal), ("parcelaInformada", payment), ("totalPago", payment * months)], "PV = PMT × (1 − (1+i)^−n) / i", "Principal resolvido para parcelas, taxa e prazo informados.");
        }
        if (target == "monthlyRatePercent")
        {
            var principal = Required(input, "principal", 0.01m); var payment = Required(input, "installmentValue", 0.01m); var months = Periods(input);
            if (payment + 0.000001m < principal / months) throw new ArgumentOutOfRangeException("installmentValue", "Não há taxa mensal não negativa que produza essa parcela.");
            decimal low = 0, high = 10;
            for (var i = 0; i < 200; i++) { var mid = (low + high) / 2; if (Payment(principal, mid, months) < payment) low = mid; else high = mid; }
            var solved = (low + high) / 2 * 100m;
            return Result(solved, [("taxaMensalResolvidaPercentual", solved), ("valorFinanciado", principal), ("parcelaInformada", payment), ("prazoMeses", months)], "PMT = PV × i / (1 − (1+i)^−n); i resolvido numericamente", "A taxa é uma aproximação numérica mensal; não inclui tarifas nem substitui o CET contratual.");
        }
        if (target == "months")
        {
            var principal = Required(input, "principal", 0.01m); var payment = Required(input, "installmentValue", 0.01m);
            var periods = rate == 0 ? (int)Math.Ceiling((double)(principal / payment)) : payment <= principal * rate ? throw new ArgumentOutOfRangeException("installmentValue", "A parcela deve superar os juros do primeiro período para quitar o saldo.") : (int)Math.Ceiling(Math.Log((double)(payment / (payment - principal * rate))) / Math.Log((double)(1 + rate)));
            if (periods < 1 || periods > MaxPeriods) throw new ArgumentOutOfRangeException("installmentValue", $"O prazo calculado deve ficar entre 1 e {MaxPeriods} meses.");
            return Result(periods, [("prazoResolvidoMeses", periods), ("valorFinanciado", principal), ("parcelaInformada", payment), ("taxaMensalPercentual", rate * 100m)], "n = ln(PMT / (PMT − PV × i)) / ln(1+i)", "Prazo arredondado para cima ao primeiro mês inteiro; a última parcela pode ser menor.");
        }
        throw new KeyNotFoundException("Variável a resolver não disponível.");
    }

    public FinancialToolResponse SolveRegularInvestment(string target, IReadOnlyDictionary<string, decimal> input)
    {
        Validate(input);
        if (target is not ("monthlyContribution" or "months" or "monthlyRatePercent")) throw new KeyNotFoundException("Variável a resolver não disponível.");
        var initial=Optional(input,"initialCapital");var contribution=Optional(input,"monthlyContribution");var ratePercent=Optional(input,"monthlyRatePercent");var atBeginning=Optional(input,"depositAtBeginning")==1;var targetValue=Required(input,"futureValue",0.01m);var resolved=input.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);
        decimal ValueAfter(int n,decimal monthlyRate,decimal amount){var balance=initial;for(var month=0;month<n;month++){if(atBeginning)balance+=amount;balance*=1+monthlyRate;if(!atBeginning)balance+=amount;}return balance;}
        decimal value;
        if(target=="monthlyContribution") {var months=Periods(input);var rate=ratePercent/100m;var factor=rate==0?months:(decimal)Math.Pow((double)(1+rate),months)-1;factor=rate==0?factor:factor/rate*(atBeginning?1+rate:1);var remainder=targetValue-initial*(decimal)Math.Pow((double)(1+rate),months);if(factor<=0||remainder<0)throw new ArgumentOutOfRangeException("futureValue","O valor futuro deve cobrir o capital inicial projetado.");value=remainder/factor;resolved["monthlyContribution"]=value;}
        else if(target=="months") {var rate=ratePercent/100m;var found=Enumerable.Range(1,MaxPeriods).FirstOrDefault(n=>ValueAfter(n,rate,contribution)>=targetValue);if(found==0)throw new ArgumentOutOfRangeException("futureValue",$"Meta não atingida em até {MaxPeriods} meses com as premissas informadas.");value=found;resolved["months"]=found;}
        else {var months=Periods(input);var low=0m;var high=10m;if(ValueAfter(months,low,contribution)>targetValue)throw new ArgumentOutOfRangeException("futureValue","A meta está abaixo do saldo projetado sem rendimento.");if(ValueAfter(months,high,contribution)<targetValue)throw new ArgumentOutOfRangeException("futureValue","A taxa necessária ultrapassa o limite de 1.000% ao mês.");for(var i=0;i<200;i++){var mid=(low+high)/2;if(ValueAfter(months,mid,contribution)<targetValue)low=mid;else high=mid;}value=(low+high)/2*100m;resolved["monthlyRatePercent"]=value;}
        var calculation=Calculate("investimento-aportes",resolved);var breakdown=calculation.Breakdown.ToDictionary(x=>x.Key,x=>x.Value);breakdown[$"{target}Resolvido"] = decimal.Round(value,2);return calculation with { Result=decimal.Round(value,2),Breakdown=breakdown,Steps=calculation.Steps.Append($"Variável {target} resolvida para o valor futuro informado com aportes ao {(atBeginning?"início":"fim")} do período.").ToArray() };
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
        var initial = Optional(x, "initialCapital"); var contribution = Required(x, "monthlyContribution", 0); var rate = Required(x, "monthlyRatePercent", 0) / 100m; var months = Periods(x); var atBeginning = Optional(x, "depositAtBeginning") == 1;
        var balance = initial; var schedule = new List<AmortizationRow>(months);
        for (var i = 1; i <= months; i++) { if (atBeginning) balance += contribution; var interest = balance * rate; balance += interest; if (!atBeginning) balance += contribution; schedule.Add(new(i, contribution, contribution, interest, balance)); }
        var contributed = initial + contribution * months;
        return Result(balance, [("capitalInicial", initial), ("aportes", contribution * months), ("totalInvestido", contributed), ("rendimentoEstimado", balance - contributed), ("saldoFinalEstimado", balance)], "M = C(1+i)^n + A × ((1+i)^n − 1) / i; aporte no início multiplica o fator de aportes por (1+i)", $"Aportes ao {(atBeginning ? "início" : "fim")} de cada mês; taxa mensal informada {rate * 100:N4}%; sem impostos, taxas ou oscilação de mercado.", schedule);
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
        var freeBidPercent=Optional(x,"freeBidPercent");var embeddedBidPercent=Optional(x,"embeddedBidPercent");if(freeBidPercent+embeddedBidPercent>100)throw new ArgumentOutOfRangeException("freeBidPercent","A soma dos lances não pode superar 100% do crédito.");
        var adjustment=Optional(x,"annualAdjustmentPercent")/100m;var discountRate=Optional(x,"discountMonthlyRatePercent")/100m;var initialPayment=value*(1+administration+reserveFund)/months+insurance;var consortiumTotal=0m;var consortiumPv=0m;var schedule=new List<AmortizationRow>(months);
        for(var month=1;month<=months;month++){var payment=initialPayment*(decimal)Math.Pow((double)(1+adjustment),(month-1)/12);consortiumTotal+=payment;consortiumPv+=payment/(decimal)Math.Pow((double)(1+discountRate),month);schedule.Add(new(month,payment,0,0,Math.Max(0,value*(1-embeddedBidPercent/100m))));}
        var financeRate=Required(x,"financingMonthlyRatePercent",0)/100m;var financed=value*(1-freeBidPercent/100m);var financePayment=Payment(financed,financeRate,months);var financeTotal=financePayment*months+value*freeBidPercent/100m;var financePv=value*freeBidPercent/100m;for(var month=1;month<=months;month++)financePv+=financePayment/(decimal)Math.Pow((double)(1+discountRate),month);
        return Result(consortiumTotal-financeTotal,[("parcelaConsorcioInicial",initialPayment),("totalConsorcioComReajuste",consortiumTotal),("creditoLiquidoAposLanceEmbutido",value*(1-embeddedBidPercent/100m)),("lanceLivreInformado",value*freeBidPercent/100m),("parcelaFinanciamentoEstimada",financePayment),("totalFinanciamentoComLanceLivre",financeTotal),("valorPresenteConsorcio",consortiumPv),("valorPresenteFinanciamento",financePv),("diferencaNominalConsorcioMenosFinanciamento",consortiumTotal-financeTotal)],"Consórcio: parcelas com reajuste anual informado; financiamento Price sobre crédito após lance livre",$"Lance embutido {embeddedBidPercent:N2}%; lance livre {freeBidPercent:N2}%; reajuste anual {adjustment*100:N2}%; desconto temporal mensal {discountRate*100:N3}%.",schedule);
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

    private static FinancialToolResponse DebtRenegotiation(IReadOnlyDictionary<string, decimal> x)
    {
        var balance = Required(x, "balance", 0.01m); var discount = Required(x, "discountPercent", 0); var down = Optional(x, "downPayment"); var costs = Optional(x, "costs"); var months = Periods(x); var rate = Required(x, "monthlyRatePercent", 0) / 100m;
        if (discount > 100) throw new ArgumentOutOfRangeException("discountPercent", "Desconto máximo: 100%.");
        var negotiated = balance * (1 - discount / 100m); if (down > negotiated) throw new ArgumentOutOfRangeException("downPayment", "A entrada não pode superar o saldo negociado.");
        var financed = negotiated - down + costs; var payment = Payment(financed, rate, months); var total = down + payment * months;
        return Result(total, [("saldoInformado", balance), ("descontoEstimado", balance - negotiated), ("saldoNegociado", negotiated), ("entrada", down), ("custosInformados", costs), ("parcelaEstimada", payment), ("totalRenegociado", total), ("economiaContraSaldoOriginal", balance - total)], "Saldo negociado = saldo informado × (1 − desconto); saldo financiado pela fórmula Price", "Desconto, entrada, taxa, prazo e custos são hipóteses digitadas. Solicite proposta de quitação oficial e confira encargos e datas com o credor.");
    }

    private static FinancialToolResponse NetInvestmentReturn(IReadOnlyDictionary<string, decimal> x)
    {
        var principal = Required(x, "principal", 0.01m); var rate = Required(x, "monthlyRatePercent", 0) / 100m; var months = Periods(x); var taxPercent = Required(x, "incomeTaxPercent", 0); var iofPercent = Required(x, "iofPercent", 0); var fees = Optional(x, "fees");
        if (taxPercent > 100 || iofPercent > 100) throw new ArgumentOutOfRangeException(nameof(x), "Impostos informados devem estar entre 0% e 100%.");
        var grossYield = principal * (decimal)Math.Pow((double)(1 + rate), months) - principal; var iof = grossYield * iofPercent / 100m; var incomeTax = Math.Max(0, grossYield - iof) * taxPercent / 100m; var net = principal + grossYield - iof - incomeTax - fees;
        return Result(net, [("aporteInicial", principal), ("rendimentoBrutoEstimado", grossYield), ("iofInformado", iof), ("impostoDeRendaInformado", incomeTax), ("taxasInformadas", fees), ("saldoLiquidoEstimado", net), ("rentabilidadeLiquidaPercentual", (net / principal - 1) * 100m)], "Saldo bruto = capital × (1 + taxa mensal)^prazo; saldo líquido deduz impostos e taxas informados", "As alíquotas de IR e IOF são fornecidas pelo usuário, sem inferência de regime tributário. Confirme modalidade, prazo e tributação na fonte oficial e no produto.");
    }

    private static FinancialToolResponse CreditCard(IReadOnlyDictionary<string, decimal> x)
    {
        var principal = Required(x, "balance", 0.01m); var monthlyRate = Required(x, "monthlyRatePercent", 0) / 100m; var paymentPercent = Required(x, "paymentPercent", 1); var months = Periods(x); var loanRate = Required(x, "loanMonthlyRatePercent", 0) / 100m;
        if (paymentPercent > 100) throw new ArgumentOutOfRangeException("paymentPercent", "O pagamento mensal máximo é 100% do saldo.");
        var balance = principal; var charged = 0m; var revolvingTotal = 0m; var schedule = new List<AmortizationRow>();
        for (var period = 1; period <= months && balance > 0.005m; period++) { var interest = Math.Min(balance * monthlyRate, Math.Max(0, principal - charged)); charged += interest; var due = balance + interest; var payment = Math.Min(due, Math.Max(due * paymentPercent / 100m, Math.Min(due, 0.01m))); balance = due - payment; revolvingTotal += payment; schedule.Add(new(period, decimal.Round(payment, 2), decimal.Round(payment - interest, 2), decimal.Round(interest, 2), decimal.Round(balance, 2))); }
        var loanPayment = Payment(principal, loanRate, months); var loanTotal = loanPayment * months;
        return Result(revolvingTotal, [("saldoOriginal", principal), ("encargosAcumuladosLimitados", charged), ("pagamentosTotaisNoCenarioRotativo", revolvingTotal), ("saldoRestanteNoFim", balance), ("parcelaEstimadaEmprestimo", loanPayment), ("totalEstimadoEmprestimo", loanTotal), ("prazoEmprestimo", months)], "Saldo mensal = saldo anterior + juros limitados a 100% do principal original − pagamento", "Cenário simplificado de crédito rotativo elegível às regras aplicáveis a operações desde 03/01/2024; o limite se refere ao total acumulado de juros e encargos, não à taxa. Taxas e pagamento percentual são premissas informadas; confira a regra e o contrato.", schedule);
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

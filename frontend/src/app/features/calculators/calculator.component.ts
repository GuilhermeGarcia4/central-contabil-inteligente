import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Meta, Title } from '@angular/platform-browser';
import { AuthService } from '../../core/auth/auth.service';
import { FinanceService } from '../finance/finance.service';

interface ScheduleRow { period: number; payment: number; principal: number; interest: number; balance: number; }
interface Response {
  result: number; breakdown: Record<string, number>; formula: string; steps: string[];
  rule?: { name: string; version: string; validFrom: string; validUntil: string | null; source: string | null };
  version?: string; schedule?: ScheduleRow[];
}
interface Field { key: string; label: string; min?: number; max?: number; step?: number; type?: 'number' | 'checkbox'; help?: string; }

const fields: Record<string, Field[]> = {
  'juros-simples': [{key:'principal',label:'Capital inicial (R$)',min:0.01},{key:'monthlyRatePercent',label:'Taxa mensal (%)',step:0.001},{key:'months',label:'Prazo (meses)',min:1,max:1200,step:1}],
  'financiamento-fixas': [{key:'principal',label:'Valor financiado (R$)',min:0.01},{key:'monthlyRatePercent',label:'Taxa mensal (%)',step:0.001},{key:'months',label:'Número de parcelas',min:1,max:1200,step:1}],
  'financiamento-sac': [{key:'principal',label:'Valor financiado (R$)',min:0.01},{key:'monthlyRatePercent',label:'Taxa mensal (%)',step:0.001},{key:'months',label:'Número de parcelas',min:1,max:1200,step:1}],
  'financiamento-price': [{key:'principal',label:'Valor financiado (R$)',min:0.01},{key:'monthlyRatePercent',label:'Taxa mensal (%)',step:0.001},{key:'months',label:'Número de parcelas',min:1,max:1200,step:1}],
  'simulacao-emprestimo': [{key:'principal',label:'Valor solicitado (R$)',min:0.01},{key:'monthlyRatePercent',label:'Taxa mensal (%)',step:0.001},{key:'months',label:'Número de parcelas',min:1,max:1200,step:1},{key:'iof',label:'IOF financiado (R$)'},{key:'fees',label:'Tarifas financiadas (R$)'},{key:'insurance',label:'Seguro financiado (R$)'},{key:'otherCosts',label:'Outros custos financiados (R$)'}],
  'investimento-aportes': [{key:'initialCapital',label:'Capital inicial (R$)'},{key:'monthlyContribution',label:'Aporte mensal (R$)'},{key:'monthlyRatePercent',label:'Taxa mensal informada (%)',step:0.001},{key:'months',label:'Prazo (meses)',min:1,max:1200,step:1},{key:'depositAtBeginning',label:'Aporte no início do mês',type:'checkbox'}],
  'rentabilidade-investimento': [{key:'initialCapital',label:'Valor inicial (R$)',min:0.01},{key:'finalValue',label:'Valor final (R$)'},{key:'months',label:'Período (meses)',min:1,max:1200,step:1}],
  'correcao-monetaria': [{key:'principal',label:'Valor original (R$)'},{key:'cumulativeRatePercent',label:'Variação acumulada informada (%)',min:-100,step:0.001}],
  'reserva-emergencia': [{key:'monthlyExpenses',label:'Despesas mensais essenciais (R$)',min:0.01},{key:'months',label:'Meses de cobertura desejados',min:1,max:1200,step:1},{key:'currentReserve',label:'Reserva atual (R$)'}],
  'planejamento-mensal': [{key:'income',label:'Renda mensal (R$)'},{key:'fixedExpenses',label:'Despesas fixas (R$)'},{key:'variableExpenses',label:'Despesas variáveis (R$)'},{key:'debtPayments',label:'Pagamentos de dívidas (R$)'},{key:'savingsGoal',label:'Meta de poupança (R$)'}],
  'quitacao-antecipada': [{key:'installmentValue',label:'Valor atual da parcela (R$)',min:0.01},{key:'months',label:'Parcelas restantes',min:1,max:1200,step:1},{key:'monthlyRatePercent',label:'Taxa mensal do contrato (%)',step:0.001},{key:'fees',label:'Custos informados (R$)'}],
  'portabilidade-divida': [{key:'installmentValue',label:'Parcela atual (R$)',min:0.01},{key:'months',label:'Parcelas restantes',min:1,max:1200,step:1},{key:'oldMonthlyRatePercent',label:'Taxa mensal atual (%)',step:0.001},{key:'newMonthlyRatePercent',label:'Nova taxa mensal (%)',step:0.001},{key:'transferCosts',label:'Custos da portabilidade (R$)'}],
  'consorcio-financiamento': [{key:'assetValue',label:'Valor do bem/carta (R$)',min:0.01},{key:'months',label:'Prazo (meses)',min:1,max:1200,step:1},{key:'administrationPercent',label:'Taxa de administração total (%)',step:0.001},{key:'reserveFundPercent',label:'Fundo de reserva total (%)',step:0.001},{key:'monthlyInsurance',label:'Seguro mensal do consórcio (R$)'},{key:'financingMonthlyRatePercent',label:'Juros mensais do financiamento (%)',step:0.001},{key:'freeBidPercent',label:'Lance livre (%)',min:0,max:100,step:0.01},{key:'embeddedBidPercent',label:'Lance embutido (%)',min:0,max:100,step:0.01},{key:'annualAdjustmentPercent',label:'Reajuste anual assumido (%)',min:0,step:0.01},{key:'discountMonthlyRatePercent',label:'Taxa mensal para valor presente (%)',min:0,step:0.01}],
  'renegociacao-divida': [{key:'balance',label:'Saldo da dívida informado (R$)',min:0.01},{key:'discountPercent',label:'Desconto proposto (%)',min:0,max:100,step:0.01},{key:'downPayment',label:'Entrada (R$)'},{key:'costs',label:'Custos da renegociação (R$)'},{key:'monthlyRatePercent',label:'Taxa mensal proposta (%)',step:0.001},{key:'months',label:'Parcelas',min:1,max:1200,step:1}],
  'rendimento-liquido': [{key:'principal',label:'Valor aplicado (R$)',min:0.01},{key:'monthlyRatePercent',label:'Taxa mensal bruta informada (%)',step:0.001},{key:'months',label:'Prazo (meses)',min:1,max:1200,step:1},{key:'incomeTaxPercent',label:'Alíquota IR informada (%)',min:0,max:100,step:0.01},{key:'iofPercent',label:'Alíquota IOF informada (%)',min:0,max:100,step:0.01},{key:'fees',label:'Taxas informadas (R$)'}],
  'cartao-credito': [{key:'balance',label:'Saldo da fatura (R$)',min:0.01},{key:'monthlyRatePercent',label:'Taxa mensal do rotativo informada (%)',step:0.001},{key:'paymentPercent',label:'Percentual pago mensalmente (%)',min:1,max:100,step:0.01},{key:'months',label:'Horizonte do cenário (meses)',min:1,max:1200,step:1},{key:'loanMonthlyRatePercent',label:'Taxa mensal do empréstimo comparável (%)',step:0.001}],
  cet: [{key:'netAmount',label:'Valor bruto liberado (R$)',min:0.01},{key:'upfrontCosts',label:'Custos iniciais (R$)'},{key:'installmentValue',label:'Valor de cada parcela (R$)',min:0.01},{key:'months',label:'Número de parcelas',min:1,max:1200,step:1}],
  'juros-compostos': [{key:'initialCapital',label:'Capital inicial (R$)'},{key:'monthlyRatePercent',label:'Taxa mensal (%)',step:0.001},{key:'months',label:'Período (meses)',min:1,max:1200,step:1},{key:'monthlyContribution',label:'Aporte mensal (opcional) (R$)'}],
  ferias: [{key:'grossSalary',label:'Salário bruto (R$)',min:0.01},{key:'vacationDays',label:'Dias de férias',min:1,max:30,step:1},{key:'sellOneThird',label:'Vender 1/3 das férias',type:'checkbox'}],
  'decimo-terceiro': [{key:'salary',label:'Salário (R$)',min:0.01},{key:'monthsWorked',label:'Meses trabalhados',min:1,max:12,step:1}]
};

const titles: Record<string,string> = {
  'juros-simples':'Juros simples','juros-compostos':'Juros compostos','financiamento-fixas':'Financiamento com parcelas fixas',
  'financiamento-sac':'Financiamento pelo sistema SAC','financiamento-price':'Financiamento pelo sistema Price',
  'simulacao-emprestimo':'Simulação de empréstimo','investimento-aportes':'Aplicação com depósitos regulares',
  'rentabilidade-investimento':'Rentabilidade de investimentos','correcao-monetaria':'Correção monetária',
  'reserva-emergencia':'Reserva de emergência','planejamento-mensal':'Planejamento financeiro mensal',
  'quitacao-antecipada':'Quitação antecipada de dívidas','portabilidade-divida':'Portabilidade de dívidas',
  'consorcio-financiamento':'Consórcio x financiamento','cet':'Custo Efetivo Total (CET)',
  'renegociacao-divida':'Renegociação de dívida','rendimento-liquido':'Rendimento líquido estimado','cartao-credito':'Cenários de dívida do cartão',
  'ferias':'Calculadora de férias','decimo-terceiro':'Calculadora de 13º salário'
};

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe, DecimalPipe],
  template: `
    <section class="page narrow calculator-detail">
      <a routerLink="/calculadoras">← Todas as calculadoras</a>
      <span class="eyebrow">SIMULAÇÃO EDUCATIVA</span>
      <h1>{{ title() }}</h1>
      <p class="lead">{{ description() }}</p>
      <div class="calculator-grid">
        <form class="panel" [formGroup]="form" (ngSubmit)="calculate()" novalidate>
          @for (field of visibleFields(); track field.key) {
            @if (field.type === 'checkbox') {
              <label class="check"><input type="checkbox" [formControlName]="field.key"> {{ field.label }}</label>
            } @else {
              <label>{{ field.label }}<input type="number" [formControlName]="field.key" [min]="field.min ?? 0" [max]="field.max ?? null" [step]="field.step ?? 0.01" [attr.aria-label]="field.label"></label>
            }
          }
          @if (slug() === 'financiamento-fixas' || slug() === 'financiamento-price') { <label>O que deseja resolver?<select [value]="solveFor()" (change)="solveFor.set($any($event.target).value)"><option value="payment">Parcela mensal</option><option value="principal">Valor financiado</option><option value="monthlyRatePercent">Taxa mensal</option><option value="months">Prazo</option></select></label> }
          @if (slug() === 'investimento-aportes') { <label>O que deseja resolver?<select [value]="investmentSolveFor()" (change)="investmentSolveFor.set($any($event.target).value)"><option value="futureValue">Saldo futuro</option><option value="monthlyContribution">Aporte mensal</option><option value="months">Prazo</option><option value="monthlyRatePercent">Taxa mensal</option></select></label> }
          @if (slug() === 'correcao-monetaria') { <fieldset class="index-lookup"><legend>Consultar índice oficial (opcional)</legend><label>Índice<select [value]="indexCode()" (change)="indexCode.set(+$any($event.target).value)"><option [value]="433">IPCA</option><option [value]="188">INPC</option><option [value]="189">IGP-M</option><option [value]="7811">TR (mês iniciado no dia 1º)</option><option [value]="196">Poupança (depósitos desde 2012, aniversário no dia 1º)</option><option [value]="4390">Selic acumulada no mês</option></select></label><label>Primeiro mês de referência<input type="date" [value]="indexFrom()" (change)="indexFrom.set($any($event.target).value)" /></label><label>Último mês de referência<input type="date" [value]="indexTo()" (change)="indexTo.set($any($event.target).value)" /></label><button type="button" class="secondary" (click)="applyOfficialIndex()">Buscar e aplicar variação</button><small>Aplicação composta dos meses completos publicados; não estima frações de mês nem meses ainda não divulgados.</small></fieldset> }
          @if (form.invalid) { <p class="hint" role="status">Revise os campos: use números dentro dos limites indicados.</p> }
          @if (auth.user() && (slug() === 'reserva-emergencia' || slug() === 'renegociacao-divida' || slug() === 'investimento-aportes')) { <button class="secondary" type="button" (click)="prefillFromFinance()">{{ slug() === 'reserva-emergencia' ? 'Usar minha reserva' : slug() === 'renegociacao-divida' ? 'Usar minha dívida' : 'Usar minha meta' }}</button> }
          <button class="primary" [disabled]="form.invalid || loading()">{{ loading() ? 'Calculando…' : 'Calcular' }}</button>
          @if (error()) { <p class="error" role="alert">{{ error() }}</p> }
        </form>
        <aside class="result" aria-live="polite">
          @if (result(); as r) {
            <span class="eyebrow">RESULTADO ESTIMADO</span>
            <p class="hint">Gerado em {{ generatedAt() }}</p><h2>Parâmetros</h2>@for(item of inputSummary();track item.key){<div class="line"><span>{{item.label}}</span><b>{{item.value}}</b></div>}
            @if (isPercentResult()) { <strong>{{ r.result | number:'1.2-4':'pt-BR' }}% {{ slug() === 'investimento-aportes' ? 'ao mês' : 'a.a.' }}</strong> }
            @else if (slug() === 'investimento-aportes' && investmentSolveFor() === 'months') { <strong>{{ r.result | number:'1.0-0':'pt-BR' }} meses</strong> }
            @else { <strong>{{ r.result | currency:'BRL':'symbol':'1.2-2':'pt-BR' }}</strong> }
            <h2>Detalhamento</h2>
            @for (item of entries(r.breakdown); track item[0]) { <div class="line"><span>{{ label(item[0]) }}</span><b>{{ isRate(item[0]) ? (item[1] | number:'1.2-4':'pt-BR') + '%' : (item[1] | currency:'BRL':'symbol':'1.2-2':'pt-BR') }}</b></div> }
            <h2>Como calculamos?</h2><code>{{ r.formula }}</code>
            @for (step of r.steps; track step) { <p>{{ step }}</p> }
            @if (r.schedule?.length) {
              <h2>Amortização</h2><p class="hint">Tabela com até 24 períodos; o total do resumo considera todo o prazo.</p>
              <div class="schedule-scroll"><table><caption>Estimativa de evolução do saldo</caption><thead><tr><th>Período</th><th>Prestação</th><th>Amortização</th><th>Juros</th><th>Saldo</th></tr></thead><tbody>
                @for (row of r.schedule!.slice(0,24); track row.period) { <tr><td>{{ row.period }}</td><td>{{ row.payment | currency:'BRL':'symbol':'1.2-2':'pt-BR' }}</td><td>{{ row.principal | currency:'BRL':'symbol':'1.2-2':'pt-BR' }}</td><td>{{ row.interest | currency:'BRL':'symbol':'1.2-2':'pt-BR' }}</td><td>{{ row.balance | currency:'BRL':'symbol':'1.2-2':'pt-BR' }}</td></tr> }
              </tbody></table></div>
              <h2>Saldo por período</h2><svg class="balance-chart" viewBox="0 0 520 150" role="img" aria-label="Gráfico do saldo estimado ao longo dos períodos"><line x1="12" y1="132" x2="508" y2="132" stroke="#b8c8d2"/><polyline [attr.points]="schedulePoints(r.schedule!)" fill="none" stroke="#26749a" stroke-width="3"/><text x="12" y="147">Início</text><text x="460" y="147">Fim</text></svg>
            }
            <h2>Fonte e limites</h2><p>{{ reportNote() }} <a [href]="reportSourceUrl()" target="_blank" rel="noopener noreferrer">Consultar fonte</a></p>
            @if (r.rule) { <div class="rule"><b>Regra utilizada</b><span>{{ r.rule.name }}</span><span>Versão: {{ r.rule.version }}</span><span>Vigência: {{ r.rule.validFrom }} — {{ r.rule.validUntil || 'em aberto' }}</span><span>Fonte: {{ r.rule.source || 'não informada' }}</span></div> }
            @if (r.version) { <div class="rule"><b>Versão da fórmula</b><span>{{ r.version }}</span></div> }
            <button class="secondary print-button" type="button" (click)="printResults()">Imprimir ou salvar em PDF</button>
            @if (auth.user()) { <button class="secondary print-button" type="button" (click)="saveSimulation()" [disabled]="saving()">{{ saving() ? 'Salvando…' : 'Salvar no meu histórico' }}</button> }
            @if (saveMessage()) { <p role="status">{{ saveMessage() }}</p> }
          } @else { <div class="empty">Preencha os campos para ver o resultado, a fórmula e as premissas utilizadas.</div> }
        </aside>
      </div>
      <p class="disclaimer">Simulação estimativa com os parâmetros digitados. Confira contratos, índices, impostos, tarifas e regras oficiais antes de tomar decisões.</p>
    </section>
  `,
  styles: [`
    .calculator-detail{max-width:1100px}.index-lookup{display:grid;gap:10px;margin:14px 0;padding:14px;border:1px solid var(--line);border-radius:8px}.index-lookup label{display:grid;gap:4px}.index-lookup small{color:var(--muted);line-height:1.5}.calculator-detail>a{display:inline-block;margin-bottom:28px;color:var(--blue)}.lead,.disclaimer{line-height:1.6;color:var(--muted)}.calculator-detail label input{min-height:42px}.calculator-detail label.check{display:flex;align-items:center;gap:10px}.hint{color:var(--muted);font-size:13px;line-height:1.5}.result h2{margin-top:25px;font:500 22px Georgia,serif}.result>strong{font-variant-numeric:tabular-nums}.balance-chart{display:block;width:100%;max-width:520px;height:150px;margin:12px 0}.balance-chart text{font-size:11px;fill:var(--muted)}.schedule-scroll{overflow:auto;max-height:360px;border:1px solid var(--line);border-radius:8px}table{width:100%;min-width:580px;border-collapse:collapse;background:white;font-size:13px}caption{text-align:left;padding:10px;color:var(--muted)}th,td{text-align:right;padding:9px;border-top:1px solid var(--line);white-space:nowrap}th:first-child,td:first-child{text-align:left}thead{position:sticky;top:0;background:var(--surface)}.secondary{margin-top:24px;border:1px solid var(--line);border-radius:8px;background:white;color:var(--navy);font-weight:700;padding:11px 15px;cursor:pointer}.disclaimer{margin-top:24px;font-size:13px}@media print{header,footer,.calculator-detail>a,.calculator-detail form,.print-button,.disclaimer{display:none!important}.page{max-width:none!important;padding:0!important}.calculator-grid{display:block!important}.result{border:0!important;box-shadow:none!important}.schedule-scroll{max-height:none!important;overflow:visible!important}}
  `]
})
export class CalculatorComponent {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  private readonly meta = inject(Meta);
  private readonly documentTitle = inject(Title);
  private readonly finance = inject(FinanceService);
  readonly auth = inject(AuthService);
  readonly slug = input.required<string>();
  readonly loading = signal(false);
  readonly error = signal('');
  readonly generatedAt = signal('');
  readonly result = signal<Response | null>(null);
  readonly saving = signal(false);
  readonly saveMessage = signal('');
  readonly solveFor = signal('payment');
  readonly investmentSolveFor = signal('futureValue');
  readonly indexCode = signal(433);
  readonly indexFrom = signal(`${new Date().getFullYear()-1}-01-01`);
  readonly indexTo = signal(new Date().toISOString().slice(0,10));
  readonly visibleFields = computed(() => {
    const rows = fields[this.slug()] ?? [];
    if (this.slug() === 'investimento-aportes') { const target=this.investmentSolveFor(); return target==='futureValue'?rows:[...rows.filter(x=>x.key!==target),{key:'futureValue',label:'Saldo futuro desejado (R$)',min:0.01,step:0.01}]; }
    if (this.slug() !== 'financiamento-fixas' && this.slug() !== 'financiamento-price') return rows;
    const target = this.solveFor();
    return [...rows.filter(x => x.key !== target), ...(target === 'payment' ? [] : [{key:'installmentValue',label:'Parcela informada (R$)',min:0.01,step:0.01}])];
  });
  readonly title = computed(() => titles[this.slug()] ?? 'Calculadora');
  readonly description = computed(() => this.slug() === 'juros-compostos'
    ? 'Projete a evolução do capital com aportes ao final de cada mês.'
    : 'Informe os parâmetros da simulação. Valores e taxas não são consultados nem presumidos pela ferramenta.');
  readonly form = this.fb.group({
    principal:[10000,[Validators.required,Validators.min(0.01)]], monthlyRatePercent:[1,[Validators.required,Validators.min(0)]], months:[12,[Validators.required,Validators.min(1),Validators.max(1200)]],
    initialCapital:[1000,[Validators.min(0)]], monthlyContribution:[500,[Validators.min(0)]], finalValue:[12000,[Validators.min(0)]], cumulativeRatePercent:[5,[Validators.min(-100)]],
    monthlyExpenses:[3000,[Validators.min(0.01)]], currentReserve:[0,[Validators.min(0)]], income:[7000,[Validators.min(0)]], fixedExpenses:[3000,[Validators.min(0)]],
    variableExpenses:[1500,[Validators.min(0)]], debtPayments:[500,[Validators.min(0)]], savingsGoal:[500,[Validators.min(0)]], installmentValue:[1000,[Validators.min(0.01)]],
    fees:[0,[Validators.min(0)]], insurance:[0,[Validators.min(0)]], iof:[0,[Validators.min(0)]], otherCosts:[0,[Validators.min(0)]],
    oldMonthlyRatePercent:[2,[Validators.min(0)]], newMonthlyRatePercent:[1.5,[Validators.min(0)]], transferCosts:[0,[Validators.min(0)]],
    assetValue:[50000,[Validators.min(0.01)]], administrationPercent:[15,[Validators.min(0)]], reserveFundPercent:[2,[Validators.min(0)]], monthlyInsurance:[0,[Validators.min(0)]], financingMonthlyRatePercent:[1.2,[Validators.min(0)]], freeBidPercent:[0,[Validators.min(0),Validators.max(100)]], embeddedBidPercent:[0,[Validators.min(0),Validators.max(100)]], annualAdjustmentPercent:[0,[Validators.min(0)]], discountMonthlyRatePercent:[0,[Validators.min(0)]],
    netAmount:[10000,[Validators.min(0.01)]], upfrontCosts:[100,[Validators.min(0)]],
    balance:[5000,[Validators.min(0.01)]], discountPercent:[10,[Validators.min(0),Validators.max(100)]], downPayment:[0,[Validators.min(0)]], costs:[0,[Validators.min(0)]], incomeTaxPercent:[15,[Validators.min(0),Validators.max(100)]], iofPercent:[0,[Validators.min(0),Validators.max(100)]], paymentPercent:[10,[Validators.min(1),Validators.max(100)]], loanMonthlyRatePercent:[2,[Validators.min(0)]],
    grossSalary:[3000,[Validators.required,Validators.min(0.01)]], vacationDays:[30,[Validators.required,Validators.min(1),Validators.max(30)]], sellOneThird:[false],
    salary:[3000,[Validators.required,Validators.min(0.01)]], monthsWorked:[12,[Validators.required,Validators.min(1),Validators.max(12)]], depositAtBeginning:[false]
    ,futureValue:[10000,[Validators.min(0.01)]]
  });

  constructor() {
    effect(() => {
      this.documentTitle.setTitle(`${this.title()} | Contabiliza Fácil`);
      this.meta.updateTag({ name: 'description', content: `${this.title()}: faça uma simulação educativa com fórmulas apresentadas e parâmetros informados por você.` });
    });
  }

  isPercentResult(): boolean { return this.slug() === 'rentabilidade-investimento' || this.slug() === 'cet' || (this.slug()==='investimento-aportes'&&this.investmentSolveFor()==='monthlyRatePercent'); }
  isRate(key: string): boolean { return key.toLowerCase().includes('percent'); }
  entries(value: Record<string, number>): [string, number][] { return Object.entries(value); }
  inputSummary(): {key:string;label:string;value:string}[] { const values=this.form.getRawValue() as Record<string,unknown>; return this.visibleFields().filter(x=>x.type!=='checkbox'||x.key==='depositAtBeginning').map(x=>({key:x.key,label:x.label,value:String(values[x.key]??'')})); }
  schedulePoints(rows:ScheduleRow[]):string { const values=rows.slice(0,24).map(x=>x.balance);if(!values.length)return '';const max=Math.max(1,...values),min=Math.min(0,...values);return values.map((value,index)=>`${12+index*(496/Math.max(1,values.length-1))},${128-((value-min)/Math.max(1,max-min))*112}`).join(' '); }
  reportNote():string { if(this.slug()==='rendimento-liquido')return 'Taxas e alíquotas tributárias foram informadas por você; confirme modalidade, prazo e regras vigentes.';if(this.slug()==='cartao-credito')return 'Cenário educativo; confira os limites e condições oficiais e o contrato do emissor.';if(this.slug()==='correcao-monetaria')return 'Variação composta sobre observações mensais completas da série escolhida; não há interpolação de meses parciais.';return 'Valores e premissas foram informados por você; resultado matemático não substitui contrato ou condição individual.'; }
  reportSourceUrl():string { if(this.slug()==='cartao-credito')return 'https://www.bcb.gov.br/meubc/faqs/p/limitacao-dos-juros-e-encargos-financeiros-no-saldo-devedor-da-fatura-do-cartao';if(this.slug()==='correcao-monetaria')return `https://api.bcb.gov.br/dados/serie/bcdata.sgs.${this.indexCode()}/dados`;return 'https://www.bcb.gov.br/meubc/calculadoradocidadao'; }
  label(value: string): string { return value.replace(/([A-Z])/g, ' $1').replace(/^./, char => char.toUpperCase()); }
  printResults(): void { window.print(); }
  applyOfficialIndex(): void {
    if (this.indexFrom() > this.indexTo()) { this.error.set('A data inicial deve anteceder a data final.'); return; }
    this.loading.set(true); this.error.set('');
    this.http.get<{observations:{date:string;value:number}[]}>(`/api/v1/market-rates/indicators/${this.indexCode()}`,{params:{from:this.indexFrom(),to:this.indexTo()}}).subscribe({next: value => {
      if (!value.observations.length) { this.error.set('Não há meses publicados nesse período.'); this.loading.set(false); return; }
      const factor=value.observations.reduce((total,row)=>total*(1+row.value/100),1);
      this.form.patchValue({cumulativeRatePercent:(factor-1)*100});
      this.saveMessage.set(`Aplicados ${value.observations.length} meses completos publicados pelo Banco Central.`); this.loading.set(false);
    },error:()=>{this.error.set('O índice não pôde ser consultado. Você pode informar uma variação acumulada manualmente.');this.loading.set(false);}});
  }
  prefillFromFinance(): void {
    const label = this.slug() === 'reserva-emergencia' ? 'reserva financeira' : this.slug()==='investimento-aportes'?'metas financeiras':'dívidas registradas';
    if (!this.auth.user() || !window.confirm(`Consultar ${label} da sua conta e preencher os campos desta simulação? Os valores não serão salvos na simulação até você pedir.`)) return;
    if (this.slug() === 'reserva-emergencia') this.finance.reserve().subscribe({ next: value => {
      if (value.targetMonths && value.targetMonths > 0) { this.form.patchValue({ months: value.targetMonths, monthlyExpenses: value.targetAmount / value.targetMonths, currentReserve: value.currentAmount }); }
      else { this.form.patchValue({ currentReserve: value.currentAmount }); this.saveMessage.set('Sua reserva foi preenchida. Informe meses e despesas para calcular o alvo.'); }
    }, error: () => this.saveMessage.set('Não foi possível consultar sua reserva.') });
    else if (this.slug()==='investimento-aportes') this.finance.goals().subscribe({next: goals=>{
      const goal=goals.find(x=>!x.isCompleted); if(!goal){this.saveMessage.set('Não há meta em aberto registrada na conta.');return;}
      const now=new Date();const target=goal.targetDate?new Date(goal.targetDate):null;const months=target?Math.max(1,(target.getFullYear()-now.getFullYear())*12+target.getMonth()-now.getMonth()):12;
      this.form.patchValue({initialCapital:goal.currentAmount, futureValue:goal.targetAmount, months});this.investmentSolveFor.set('monthlyContribution');this.saveMessage.set(`Preenchido com a meta “${goal.name}”. Confira o prazo e os valores antes de calcular.`);
    },error:()=>this.saveMessage.set('Não foi possível consultar suas metas.')});
    else this.finance.debts().subscribe({ next: debts => {
      const debt = debts.find(x => !x.isPaid);
      if (!debt) { this.saveMessage.set('Não há dívida em aberto registrada na conta.'); return; }
      this.form.patchValue({ balance: debt.currentAmount, monthlyRatePercent: debt.interestRate ?? this.form.controls['monthlyRatePercent'].value });
      this.saveMessage.set(`Preenchido com a dívida “${debt.name}”. Confira os valores antes de calcular.`);
    }, error: () => this.saveMessage.set('Não foi possível consultar suas dívidas.') });
  }
  saveSimulation(): void {
    const result = this.result();
    if (!result || !this.auth.user() || !window.confirm('Salvar os parâmetros e o resultado desta simulação no histórico da sua conta?')) return;
    this.saving.set(true); this.saveMessage.set('');
    const input = Object.fromEntries(this.visibleFields().filter(field => field.type !== 'checkbox').map(field => [field.key, Number((this.form.getRawValue() as Record<string, unknown>)[field.key] ?? 0)]));
    this.http.post('/api/v1/account/simulations', { title: this.title(), slug: this.slug(), inputJson: JSON.stringify(input), resultJson: JSON.stringify(result) }).subscribe({ next: () => { this.saveMessage.set('Simulação salva no seu histórico.'); this.saving.set(false); }, error: () => { this.saveMessage.set('Não foi possível salvar. Tente novamente após entrar na conta.'); this.saving.set(false); } });
  }

  calculate(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true); this.error.set(''); this.result.set(null);
    const slug = this.slug();
    if (slug === 'ferias' || slug === 'decimo-terceiro') {
      const endpoint = slug === 'ferias' ? 'vacation' : 'thirteenth-salary';
      this.http.post<Response>(`/api/v1/calculations/${endpoint}`, this.form.getRawValue()).subscribe(this.handlers());
      return;
    }
    const body = Object.fromEntries(this.visibleFields().filter(field => field.type !== 'checkbox' || field.key === 'depositAtBeginning').map(field => [field.key, Number((this.form.getRawValue() as Record<string, unknown>)[field.key] ?? 0)]));
    const inverse = ((slug === 'financiamento-fixas' || slug === 'financiamento-price') && this.solveFor() !== 'payment') || (slug==='investimento-aportes'&&this.investmentSolveFor()!=='futureValue');
    const target=slug==='investimento-aportes'?this.investmentSolveFor():this.solveFor();
    const url = inverse ? `/api/v1/calculations/financial-tools/${slug}/solve/${target}` : `/api/v1/calculations/financial-tools/${slug}`;
    this.http.post<Response>(url, body).subscribe(this.handlers());
  }

  private handlers() {
    return { next: (value: Response) => { this.result.set(value); this.generatedAt.set(new Date().toLocaleString('pt-BR')); this.loading.set(false); }, error: (error: {error?:{detail?:string;message?:string;errors?:Record<string,string[]>}}) => { const fieldError = Object.values(error.error?.errors ?? {}).flat()[0]; this.error.set(error.error?.detail ?? error.error?.message ?? fieldError ?? 'Não foi possível calcular. Revise os campos e tente novamente.'); this.loading.set(false); } };
  }
}

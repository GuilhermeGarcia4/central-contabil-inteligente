import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Meta, Title } from '@angular/platform-browser';

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
  'investimento-aportes': [{key:'initialCapital',label:'Capital inicial (R$)'},{key:'monthlyContribution',label:'Aporte mensal (R$)'},{key:'monthlyRatePercent',label:'Taxa mensal informada (%)',step:0.001},{key:'months',label:'Prazo (meses)',min:1,max:1200,step:1}],
  'rentabilidade-investimento': [{key:'initialCapital',label:'Valor inicial (R$)',min:0.01},{key:'finalValue',label:'Valor final (R$)'},{key:'months',label:'Período (meses)',min:1,max:1200,step:1}],
  'correcao-monetaria': [{key:'principal',label:'Valor original (R$)'},{key:'cumulativeRatePercent',label:'Variação acumulada informada (%)',min:-100,step:0.001}],
  'reserva-emergencia': [{key:'monthlyExpenses',label:'Despesas mensais essenciais (R$)',min:0.01},{key:'months',label:'Meses de cobertura desejados',min:1,max:1200,step:1},{key:'currentReserve',label:'Reserva atual (R$)'}],
  'planejamento-mensal': [{key:'income',label:'Renda mensal (R$)'},{key:'fixedExpenses',label:'Despesas fixas (R$)'},{key:'variableExpenses',label:'Despesas variáveis (R$)'},{key:'debtPayments',label:'Pagamentos de dívidas (R$)'},{key:'savingsGoal',label:'Meta de poupança (R$)'}],
  'quitacao-antecipada': [{key:'installmentValue',label:'Valor atual da parcela (R$)',min:0.01},{key:'months',label:'Parcelas restantes',min:1,max:1200,step:1},{key:'monthlyRatePercent',label:'Taxa mensal do contrato (%)',step:0.001},{key:'fees',label:'Custos informados (R$)'}],
  'portabilidade-divida': [{key:'installmentValue',label:'Parcela atual (R$)',min:0.01},{key:'months',label:'Parcelas restantes',min:1,max:1200,step:1},{key:'oldMonthlyRatePercent',label:'Taxa mensal atual (%)',step:0.001},{key:'newMonthlyRatePercent',label:'Nova taxa mensal (%)',step:0.001},{key:'transferCosts',label:'Custos da portabilidade (R$)'}],
  'consorcio-financiamento': [{key:'assetValue',label:'Valor do bem/carta (R$)',min:0.01},{key:'months',label:'Prazo (meses)',min:1,max:1200,step:1},{key:'administrationPercent',label:'Taxa de administração total (%)',step:0.001},{key:'reserveFundPercent',label:'Fundo de reserva total (%)',step:0.001},{key:'monthlyInsurance',label:'Seguro mensal do consórcio (R$)'},{key:'financingMonthlyRatePercent',label:'Juros mensais do financiamento (%)',step:0.001}],
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
          @if (form.invalid) { <p class="hint" role="status">Revise os campos: use números dentro dos limites indicados.</p> }
          <button class="primary" [disabled]="form.invalid || loading()">{{ loading() ? 'Calculando…' : 'Calcular' }}</button>
          @if (error()) { <p class="error" role="alert">{{ error() }}</p> }
        </form>
        <aside class="result" aria-live="polite">
          @if (result(); as r) {
            <span class="eyebrow">RESULTADO ESTIMADO</span>
            @if (isPercentResult()) { <strong>{{ r.result | number:'1.2-4':'pt-BR' }}% a.a.</strong> }
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
            }
            @if (r.rule) { <div class="rule"><b>Regra utilizada</b><span>{{ r.rule.name }}</span><span>Versão: {{ r.rule.version }}</span><span>Vigência: {{ r.rule.validFrom }} — {{ r.rule.validUntil || 'em aberto' }}</span><span>Fonte: {{ r.rule.source || 'não informada' }}</span></div> }
            @if (r.version) { <div class="rule"><b>Versão da fórmula</b><span>{{ r.version }}</span></div> }
            <button class="secondary print-button" type="button" (click)="printResults()">Imprimir ou salvar em PDF</button>
          } @else { <div class="empty">Preencha os campos para ver o resultado, a fórmula e as premissas utilizadas.</div> }
        </aside>
      </div>
      <p class="disclaimer">Simulação estimativa com os parâmetros digitados. Confira contratos, índices, impostos, tarifas e regras oficiais antes de tomar decisões.</p>
    </section>
  `,
  styles: [`
    .calculator-detail{max-width:1100px}.calculator-detail>a{display:inline-block;margin-bottom:28px;color:var(--blue);font-weight:700}.lead,.disclaimer{line-height:1.6;color:var(--muted)}.calculator-detail label input{min-height:42px}.calculator-detail label.check{display:flex;align-items:center;gap:10px}.hint{color:var(--muted);font-size:13px;line-height:1.5}.result h2{margin-top:25px;font:500 22px Georgia,serif}.result>strong{font-variant-numeric:tabular-nums}.schedule-scroll{overflow:auto;max-height:360px;border:1px solid var(--line);border-radius:8px}table{width:100%;min-width:580px;border-collapse:collapse;background:white;font-size:13px}caption{text-align:left;padding:10px;color:var(--muted)}th,td{text-align:right;padding:9px;border-top:1px solid var(--line);white-space:nowrap}th:first-child,td:first-child{text-align:left}thead{position:sticky;top:0;background:var(--surface)}.secondary{margin-top:24px;border:1px solid var(--line);border-radius:8px;background:white;color:var(--navy);font-weight:700;padding:11px 15px;cursor:pointer}.disclaimer{margin-top:24px;font-size:13px}@media print{header,footer,.calculator-detail>a,.calculator-detail form,.print-button,.disclaimer{display:none!important}.page{max-width:none!important;padding:0!important}.calculator-grid{display:block!important}.result{border:0!important;box-shadow:none!important}.schedule-scroll{max-height:none!important;overflow:visible!important}}
  `]
})
export class CalculatorComponent {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  private readonly meta = inject(Meta);
  private readonly documentTitle = inject(Title);
  readonly slug = input.required<string>();
  readonly loading = signal(false);
  readonly error = signal('');
  readonly result = signal<Response | null>(null);
  readonly visibleFields = computed(() => fields[this.slug()] ?? []);
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
    assetValue:[50000,[Validators.min(0.01)]], administrationPercent:[15,[Validators.min(0)]], reserveFundPercent:[2,[Validators.min(0)]], monthlyInsurance:[0,[Validators.min(0)]], financingMonthlyRatePercent:[1.2,[Validators.min(0)]],
    netAmount:[10000,[Validators.min(0.01)]], upfrontCosts:[100,[Validators.min(0)]],
    grossSalary:[3000,[Validators.required,Validators.min(0.01)]], vacationDays:[30,[Validators.required,Validators.min(1),Validators.max(30)]], sellOneThird:[false],
    salary:[3000,[Validators.required,Validators.min(0.01)]], monthsWorked:[12,[Validators.required,Validators.min(1),Validators.max(12)]]
  });

  constructor() {
    effect(() => {
      this.documentTitle.setTitle(`${this.title()} | Contabiliza Fácil`);
      this.meta.updateTag({ name: 'description', content: `${this.title()}: faça uma simulação educativa com fórmulas apresentadas e parâmetros informados por você.` });
    });
  }

  isPercentResult(): boolean { return this.slug() === 'rentabilidade-investimento' || this.slug() === 'cet'; }
  isRate(key: string): boolean { return key.toLowerCase().includes('percent'); }
  entries(value: Record<string, number>): [string, number][] { return Object.entries(value); }
  label(value: string): string { return value.replace(/([A-Z])/g, ' $1').replace(/^./, char => char.toUpperCase()); }
  printResults(): void { window.print(); }

  calculate(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true); this.error.set(''); this.result.set(null);
    const slug = this.slug();
    if (slug === 'ferias' || slug === 'decimo-terceiro') {
      const endpoint = slug === 'ferias' ? 'vacation' : 'thirteenth-salary';
      this.http.post<Response>(`/api/v1/calculations/${endpoint}`, this.form.getRawValue()).subscribe(this.handlers());
      return;
    }
    const body = Object.fromEntries(this.visibleFields().filter(field => field.type !== 'checkbox').map(field => [field.key, Number((this.form.getRawValue() as Record<string, unknown>)[field.key] ?? 0)]));
    this.http.post<Response>(`/api/v1/calculations/financial-tools/${slug}`, body).subscribe(this.handlers());
  }

  private handlers() {
    return { next: (value: Response) => { this.result.set(value); this.loading.set(false); }, error: (error: {error?:{detail?:string;message?:string;errors?:Record<string,string[]>}}) => { const fieldError = Object.values(error.error?.errors ?? {}).flat()[0]; this.error.set(error.error?.detail ?? error.error?.message ?? fieldError ?? 'Não foi possível calcular. Revise os campos e tente novamente.'); this.loading.set(false); } };
  }
}

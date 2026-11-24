import { DatePipe, DecimalPipe, SlicePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

interface Series { code: number; name: string; unit: string; periodicity: string; sourceUrl: string; }
interface Observation { date: string; value: number; }

@Component({
  standalone: true,
  imports: [FormsModule, DatePipe, DecimalPipe, SlicePipe],
  template: `
    <section class="page indicators" aria-labelledby="title">
      <span class="eyebrow">SÉRIES OFICIAIS</span><h1 id="title">Indicadores econômicos</h1>
      <p class="lead">Consulte séries históricas do Banco Central. Os valores são observações publicadas, não previsões.</p>
      @if (loading()) { <p role="status">Carregando catálogo…</p> }
      @else {
        <div class="controls"><label>Indicador<select [ngModel]="code()" (ngModelChange)="code.set(+$event)">@for (item of series(); track item.code) { <option [value]="item.code">{{ item.name }} ({{ item.unit }})</option> }</select></label>
        <label>Início<input type="date" [ngModel]="from()" (ngModelChange)="from.set($event)" /></label><label>Fim<input type="date" [ngModel]="to()" (ngModelChange)="to.set($event)" /></label><button class="primary" (click)="load()" [disabled]="!code()">Consultar</button></div>
        @if (error()) { <p role="alert">{{ error() }}</p> }
        @if (selected(); as item) { <p class="source">Unidade {{ item.unit }} · Periodicidade {{ item.periodicity }} · <a [href]="item.sourceUrl" target="_blank" rel="noopener noreferrer">Série do Banco Central</a></p> }
        @if (observations().length) { <p>Última atualização consultada: {{ retrievedAt() | date:'dd/MM/yyyy HH:mm' }}</p><div class="chart" role="img" aria-label="Gráfico dos valores observados"><div class="bars">@for (row of observations(); track row.date) { <div class="bar" [style.height.%]="barHeight(row.value)" [title]="row.date + ': ' + row.value"><span>{{ row.date | slice:0:7 }}</span></div> }</div></div><div class="table-wrap"><table><thead><tr><th>Referência</th><th>Valor ({{ selected()?.unit }})</th></tr></thead><tbody>@for (row of observations(); track row.date) { <tr><td>{{ row.date }}</td><td>{{ row.value | number:'1.0-4':'pt-BR' }}</td></tr> }</tbody></table></div> }
      }
      <p class="note">Indicadores são divulgados com defasagens próprias e podem ser revisados. Confira metodologia e unidade na fonte original antes de atualizar contratos.</p>
    </section>`,
  styles: [` .indicators{max-width:1050px}.lead{max-width:68ch;line-height:1.6}.controls{display:flex;align-items:end;gap:14px;flex-wrap:wrap;padding:20px;background:var(--surface);border-radius:12px}.controls label{display:grid;gap:6px}.controls input,.controls select{min-height:42px;padding:8px;border:1px solid var(--line);border-radius:7px;background:white}.source,.note{color:var(--muted);line-height:1.6}.source a{color:var(--blue)}.chart{height:240px;overflow:auto;margin:24px 0;padding:12px;border:1px solid var(--line);border-radius:9px}.bars{height:100%;min-width:650px;display:flex;align-items:end;gap:3px}.bar{position:relative;flex:1;min-width:7px;background:var(--blue-2);border-radius:3px 3px 0 0}.bar span{position:absolute;bottom:-21px;left:0;writing-mode:vertical-rl;font-size:8px;color:var(--muted)}.table-wrap{max-height:420px;overflow:auto}table{width:100%;border-collapse:collapse}td,th{text-align:left;padding:10px;border-bottom:1px solid var(--line)}@media(max-width:560px){.controls{align-items:stretch;flex-direction:column}}`]
})
export class EconomicIndicatorsComponent {
  private readonly http = inject(HttpClient);
  readonly loading = signal(true); readonly series = signal<Series[]>([]); readonly code = signal(433);
  readonly observations = signal<Observation[]>([]); readonly retrievedAt = signal(''); readonly error = signal('');
  readonly from = signal(`${new Date().getFullYear()-5}-01-01`); readonly to = signal(new Date().toISOString().slice(0,10));
  readonly selected = () => this.series().find(x => x.code === this.code());
  constructor() { this.http.get<Series[]>('/api/v1/market-rates/indicators').subscribe({next: x => {this.series.set(x);this.loading.set(false);this.load();},error:()=>{this.loading.set(false);this.error.set('Não foi possível carregar o catálogo oficial.')}}); }
  load(): void { this.error.set(''); this.observations.set([]); this.http.get<{observations:Observation[];retrievedAt:string}>(`/api/v1/market-rates/indicators/${this.code()}`, {params:{from:this.from(),to:this.to()}}).subscribe({next:x=>{this.observations.set(x.observations);this.retrievedAt.set(x.retrievedAt);},error:e=>this.error.set(e.error?.message??'Série indisponível no momento.')}); }
  barHeight(value:number):number { const rows=this.observations();const min=Math.min(0,...rows.map(x=>x.value));const max=Math.max(...rows.map(x=>x.value));return max===min?50:Math.max(3,((value-min)/(max-min))*95); }
}

@Component({
  standalone: true,
  template: `<section class="page glossary"><span class="eyebrow">EDUCAÇÃO FINANCEIRA</span><h1>Glossário financeiro</h1><p class="lead">Termos usados nas calculadoras e no comparador de propostas.</p><dl>@for (term of terms; track term.name) { <div><dt [id]="term.id">{{ term.name }}</dt><dd>{{ term.text }} <a [href]="term.href">Saiba mais</a></dd></div> }</dl></section>`,
  styles: [` .glossary{max-width:850px}.lead{color:var(--muted)}dl{display:grid;gap:12px;margin-top:28px}dl>div{padding:18px;border:1px solid var(--line);border-radius:10px;background:white}dt{font-weight:800;color:var(--navy);font-size:18px}dd{margin:6px 0 0;line-height:1.6}dd a{color:var(--blue);font-weight:700;white-space:nowrap}`]
})
export class FinancialGlossaryComponent {
  readonly terms=[
    {id:'cet',name:'CET — Custo Efetivo Total',text:'Taxa que reúne juros e custos considerados na operação de crédito, permitindo comparar o custo total em condições equivalentes. A estimativa da calculadora não substitui o CET do contrato.',href:'/calculadoras/cet'},
    {id:'price',name:'Sistema Price',text:'Sistema de amortização com parcelas iguais em condições fixas; a composição entre juros e amortização muda ao longo do tempo.',href:'/calculadoras/financiamento-price'},
    {id:'sac',name:'SAC',text:'Sistema de amortização constante; mantendo a taxa, as parcelas tendem a diminuir porque o saldo cai a cada período.',href:'/calculadoras/financiamento-sac'},
    {id:'ipca',name:'IPCA',text:'Índice oficial de preços ao consumidor amplo, publicado pelo IBGE; sua variação histórica não prevê inflação futura.',href:'/indicadores-economicos'},
    {id:'selic',name:'Selic',text:'Taxa básica de juros e conjunto de séries relacionadas publicadas pelo Banco Central; verifique qual série e periodicidade está sendo consultada.',href:'/indicadores-economicos'},
    {id:'rotativo',name:'Crédito rotativo',text:'Financiamento do saldo não pago da fatura de cartão. A taxa e as condições devem ser conferidas no contrato e na informação do emissor.',href:'/artigos'},
    {id:'consorcio',name:'Consórcio',text:'Grupo de autofinanciamento sujeito a regras contratuais e contemplação por sorteio ou lance, sem data individual garantida.',href:'/calculadoras/consorcio-financiamento'},
    {id:'juros-compostos',name:'Juros compostos',text:'Juros calculados sobre saldo acumulado, incluindo juros de períodos anteriores.',href:'/calculadoras/juros-compostos'}
  ];
}

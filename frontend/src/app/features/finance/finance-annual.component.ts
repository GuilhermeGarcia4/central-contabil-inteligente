import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { AnnualReport, FinanceService } from './finance.service';
import { FinanceNavComponent } from './finance-nav.component';

const MONTH_NAMES=['Jan','Fev','Mar','Abr','Mai','Jun','Jul','Ago','Set','Out','Nov','Dez'];

@Component({imports:[CurrencyPipe,FinanceNavComponent],template:`
<section class="page finance-page">
  <app-finance-nav/>
  <div class="finance-heading"><div><span class="eyebrow">MINHA CONTA</span><h1>Relatório anual</h1><p class="lead">Veja o resumo do ano e a evolução mês a mês.</p></div></div>
  @if(error()){<div class="notice error" role="alert">{{error()}}</div>}
  <div class="period" aria-label="Selecionar ano"><label><span>Ano</span><input type="number" [value]="year" min="2000" max="2200" (change)="selectYear($event)"></label></div>
  @if(report();as r){<div class="annual-stats"><article class="money-card income"><span>Entradas do ano</span><strong>{{r.totalIncome|currency:'BRL':'symbol':'1.2-2'}}</strong></article><article class="money-card expense"><span>Saídas do ano</span><strong>{{r.totalExpenses|currency:'BRL':'symbol':'1.2-2'}}</strong></article><article class="money-card balance"><span>Quanto sobrou</span><strong>{{r.balance|currency:'BRL':'symbol':'1.2-2'}}</strong></article></div>
  <section class="panel"><div class="section-head"><div><span class="eyebrow">EVOLUÇÃO</span><h2>Mês a mês</h2></div></div><div class="bar-chart">@for(m of r.months;track m.month){<div class="bar-col"><div class="bars"><i class="income" [style.height.%]="barHeight(m.income,r)"></i><i class="expense" [style.height.%]="barHeight(m.expenses,r)"></i></div><span>{{MONTH_NAMES[m.month-1]}}</span></div>}</div><div class="chart-legend"><span><i class="income"></i>Entradas</span><span><i class="expense"></i>Saídas</span></div></section>
  <section class="panel" style="margin-top:22px"><div class="section-head"><div><span class="eyebrow">DESTAQUES</span><h2>Maiores categorias de saída</h2></div></div>@for(cat of r.topCategories;track cat.name){<div class="finance-row"><div><b>{{cat.name}}</b></div><strong>{{cat.amount|currency:'BRL':'symbol':'1.2-2'}}</strong></div>}@empty{<div class="finance-empty"><p>Sem saídas no ano.</p></div>}</section>}
</section>`,
styles:[`.annual-stats{display:grid;grid-template-columns:repeat(3,1fr);gap:14px;margin-bottom:18px}.money-card{border:1px solid var(--line);border-radius:14px;padding:20px;background:#fff;display:grid;gap:6px;border-top:4px solid var(--navy)}.money-card strong{font-size:24px}.income{border-top-color:#168261}.expense{border-top-color:#a63434}.balance{border-top-color:#d39b00}.bar-chart{display:grid;grid-template-columns:repeat(12,1fr);gap:8px;align-items:end;height:220px;padding-top:10px}.bar-col{display:grid;grid-template-rows:1fr auto;gap:6px;height:100%}.bars{display:flex;align-items:flex-end;justify-content:center;gap:3px;height:100%}.bars i{width:12px;border-radius:4px 4px 0 0;min-height:2px}.bars i.income{background:#168261}.bars i.expense{background:#a63434}.bar-col span{text-align:center;font-size:11px;color:var(--muted)}.chart-legend{display:flex;gap:18px;justify-content:center;margin-top:14px;color:var(--muted);font-size:13px}.chart-legend i{display:inline-block;width:12px;height:12px;border-radius:3px;margin-right:6px}.chart-legend i.income{background:#168261}.chart-legend i.expense{background:#a63434}.finance-row{display:grid;grid-template-columns:1fr auto;gap:14px;align-items:center;padding:12px 0;border-bottom:1px solid var(--line)}.finance-row div{display:grid}@media(max-width:850px){.annual-stats{grid-template-columns:1fr}.bar-chart{grid-template-columns:repeat(6,1fr);height:auto;row-gap:18px}}`]
})
export class FinanceAnnualComponent{
  private readonly finance=inject(FinanceService);
  readonly report=signal<AnnualReport|null>(null);readonly error=signal('');readonly MONTH_NAMES=MONTH_NAMES;year=new Date().getFullYear();
  constructor(){this.load()}
  load(){this.finance.annualReport(this.year).subscribe({next:x=>this.report.set(x),error:()=>this.error.set('Não foi possível carregar o relatório anual.')})}
  selectYear(event:Event){const value=Number((event.target as HTMLInputElement).value);if(!value)return;this.year=value;this.load()}
  barHeight(value:number,report:AnnualReport){const max=Math.max(...report.months.map(m=>Math.max(m.income,m.expenses)),1);return Math.max(2,Math.round(value/max*100))}
}

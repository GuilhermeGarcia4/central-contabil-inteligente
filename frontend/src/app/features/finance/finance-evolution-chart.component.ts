import { CurrencyPipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { FinanceOverviewMonth } from './finance.service';

@Component({selector:'app-finance-evolution-chart',imports:[CurrencyPipe],template:`
<section class="evolution panel" aria-labelledby="evolution-title">
  <div><span class="eyebrow">TENDÊNCIA</span><h2 id="evolution-title">Evolução financeira</h2><p>Entradas, saídas e quanto sobrou ao longo do período.</p></div>
  @if(months.length){<div class="legend"><span><i class="income"></i>Entradas</span><span><i class="expense"></i>Saídas</span></div>
  <div class="bars" role="img" [attr.aria-label]="summary">@for(item of months;track item.year+'-'+item.month){<div class="month" [title]="tooltip(item)"><div class="bar-pair"><i class="income" [style.height.%]="height(item.income)"></i><i class="expense" [style.height.%]="height(item.expenses)"></i></div><b>{{monthName(item.month)}}</b><small [class.negative]="item.balance<0">{{item.balance|currency:'BRL':'symbol':'1.0-0':'pt-BR'}}</small></div>}</div>}
  @else{<p class="empty">Sem dados mensais para este período.</p>}
</section>`,styles:[`.evolution{margin:22px 0}.evolution h2{margin:3px 0}.evolution p{color:var(--muted)}.legend{display:flex;gap:18px;justify-content:flex-end}.legend span{display:flex;gap:6px;align-items:center}.legend i{width:10px;height:10px;border-radius:3px}.income{background:var(--green)}.expense{background:var(--blue-2)}.bars{height:240px;display:flex;align-items:end;gap:12px;padding:20px 6px 0;border-bottom:1px solid var(--line);overflow-x:auto}.month{min-width:64px;flex:1;display:grid;gap:5px;text-align:center}.bar-pair{height:155px;display:flex;align-items:end;justify-content:center;gap:5px}.bar-pair i{width:min(24px,35%);min-height:2px;border-radius:5px 5px 0 0}.month b{font-size:12px}.month small{color:var(--green)}.month small.negative{color:var(--danger)}.empty{padding:50px;text-align:center}`]})
export class FinanceEvolutionChartComponent{
  @Input() months:FinanceOverviewMonth[]=[];
  get maximum(){return Math.max(1,...this.months.flatMap(item=>[item.income,item.expenses]))}
  height(value:number){return Math.max(1,Math.min(100,value/this.maximum*100))}
  monthName(month:number){return new Intl.DateTimeFormat('pt-BR',{month:'short'}).format(new Date(2026,month-1,1)).replace('.','')}
  tooltip(item:FinanceOverviewMonth){return `${this.monthName(item.month)}: Entradas ${this.money(item.income)}; Saídas ${this.money(item.expenses)}; Quanto sobrou ${this.money(item.balance)}`}
  get summary(){return this.months.map(item=>this.tooltip(item)).join('. ')}
  private money(value:number){return value.toLocaleString('pt-BR',{style:'currency',currency:'BRL'})}
}

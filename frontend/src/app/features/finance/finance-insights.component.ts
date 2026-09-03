import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FinanceService, MonthUnderstanding, MonthlySummary, WeeklySummary } from './finance.service';

@Component({selector:'app-finance-insights',imports:[CurrencyPipe,DatePipe,RouterLink],template:`
@if(loading()){<div class="insights-skeleton" aria-label="Carregando inteligência financeira"><i></i><i></i></div>}
@if(error()){<div class="notice error" role="alert">{{error()}}</div>}
@if(monthUnderstanding();as m){
  <section class="insights-block">
    <div class="section-head"><div><span class="eyebrow">INTELIGÊNCIA FINANCEIRA</span><h2>Entenda meu mês</h2></div><a routerLink="/assistente">Perguntar sobre minhas finanças →</a></div>
    <div class="understand">
      <article><span>Entradas</span><strong>{{m.totalIncome|currency:'BRL':'symbol':'1.2-2'}}</strong></article>
      <article><span>Saídas</span><strong>{{m.totalExpenses|currency:'BRL':'symbol':'1.2-2'}}</strong></article>
      <article><span>Quanto sobrou</span><strong>{{m.balance|currency:'BRL':'symbol':'1.2-2'}}</strong></article>
      <article><span>Você guardou</span><strong>{{m.savingsRate===null?'—':m.savingsRate+'%'}}</strong></article>
    </div>
    @if(m.largestExpenseCategory){<p class="largest">Maior saída: <b>{{m.largestExpenseCategory}}</b> ({{m.largestExpenseAmount|currency:'BRL':'symbol':'1.2-2'}})</p>}
    @if(m.insights.length){<ul class="insight-list">@for(insight of m.insights;track insight){<li>{{insight}}</li>}</ul>}
  </section>
}
@if(weekly();as w){
  <section class="insights-block">
    <div class="section-head"><div><span class="eyebrow">RESUMO</span><h2>Esta semana</h2></div><span class="week-range">{{w.startDate|date:'dd/MM'}} – {{w.endDate|date:'dd/MM'}}</span></div>
    <div class="understand">
      <article><span>Entradas</span><strong>{{w.income|currency:'BRL':'symbol':'1.2-2'}}</strong></article>
      <article><span>Saídas</span><strong>{{w.expenses|currency:'BRL':'symbol':'1.2-2'}}</strong></article>
      <article><span>Quanto sobrou</span><strong>{{w.balance|currency:'BRL':'symbol':'1.2-2'}}</strong></article>
    </div>
    @if(w.topCategories.length){<p class="largest">Principais categorias: @for(cat of w.topCategories;track cat.categoryId){<b>{{cat.categoryName}}</b> ({{cat.amount|currency:'BRL':'symbol':'1.2-2'}})@if(!$last){, }}</p>}
    @if(w.upcomingBills.length){<p class="largest">Contas próximas: @for(bill of w.upcomingBills;track bill.dueDate+bill.description){<b>{{bill.description}}</b> ({{bill.amount|currency:'BRL':'symbol':'1.2-2'}}) em {{bill.dueDate|date:'dd/MM'}}@if(!$last){, }}</p>}
    @if(w.insights.length){<ul class="insight-list">@for(insight of w.insights;track insight){<li>{{insight}}</li>}</ul>}
  </section>
}
@if(monthly();as ms){
  <section class="insights-block">
    <div class="section-head"><div><span class="eyebrow">RESUMO</span><h2>Meu mês</h2></div><span class="week-range">{{monthName(ms.month)}} {{ms.year}}</span></div>
    <div class="understand">
      <article><span>Entradas</span><strong>{{ms.income|currency:'BRL':'symbol':'1.2-2'}}</strong></article>
      <article><span>Saídas</span><strong>{{ms.expenses|currency:'BRL':'symbol':'1.2-2'}}</strong></article>
      <article><span>Sobrou</span><strong>{{ms.balance|currency:'BRL':'symbol':'1.2-2'}}</strong></article>
      <article><span>Guardou</span><strong>{{ms.savingsRate===null?'—':ms.savingsRate+'%'}}</strong></article>
    </div>
    @if(ms.largestExpenseCategory){<p class="largest">Maior saída: <b>{{ms.largestExpenseCategory}}</b> ({{ms.largestExpenseAmount|currency:'BRL':'symbol':'1.2-2'}})</p>}
    @if(ms.goals.length){<p class="largest">Metas: @for(goal of ms.goals;track goal.id){<b>{{goal.name}}</b> {{goal.percentage}}%@if(!$last){, }}</p>}
    @if(ms.insights.length){<ul class="insight-list">@for(insight of ms.insights;track insight){<li>{{insight}}</li>}</ul>}
  </section>
}
`,styles:[`.insights-block{border:1px solid var(--line);border-radius:var(--radius);padding:24px;background:#fff;margin-top:22px}.insights-block .section-head{display:flex;align-items:center;justify-content:space-between;gap:12px;margin-bottom:16px}.insights-block .section-head a{color:var(--blue);text-decoration:none}.week-range{color:var(--muted);font-size:13px}.understand{display:grid;grid-template-columns:repeat(4,1fr);gap:12px}.understand article{border:1px solid var(--line);border-radius:12px;padding:16px;display:grid;gap:6px;background:#fafafa}.understand span{color:var(--muted);font-size:13px}.understand strong{font-size:20px}.largest{margin:16px 0 0;color:var(--muted)}.largest b{color:var(--navy)}.insight-list{margin:16px 0 0;padding-left:20px;display:grid;gap:8px}.insight-list li{color:var(--navy)}.insights-skeleton{display:grid;grid-template-columns:1fr 1fr;gap:18px;margin-top:22px}.insights-skeleton i{height:150px;border-radius:14px;background:linear-gradient(90deg,#f6edf2,#fff,#f6edf2);background-size:200% 100%;animation:pulse 1.3s infinite}@keyframes pulse{to{background-position:-200% 0}}@media(max-width:700px){.understand{grid-template-columns:1fr 1fr}.insights-skeleton{grid-template-columns:1fr}}`]})
export class FinanceInsightsComponent{
  private readonly finance=inject(FinanceService);
  readonly year=input.required<number>();readonly month=input.required<number>();
  readonly monthUnderstanding=signal<MonthUnderstanding|null>(null);readonly weekly=signal<WeeklySummary|null>(null);readonly monthly=signal<MonthlySummary|null>(null);
  readonly loading=signal(true);readonly error=signal('');
  constructor(){this.load()}
  load(){this.loading.set(true);this.error.set('');const y=this.year();const m=this.month();
    this.finance.understandMonth(y,m).subscribe({next:x=>this.monthUnderstanding.set(x),error:()=>this.error.set('Não foi possível carregar a inteligência financeira.')});
    this.finance.weeklySummary().subscribe({next:x=>this.weekly.set(x),error:()=>{}});
    this.finance.monthlySummary(y,m).subscribe({next:x=>this.monthly.set(x),error:()=>{}});
    this.loading.set(false);
  }
  monthName(month:number){return ['Janeiro','Fevereiro','Março','Abril','Maio','Junho','Julho','Agosto','Setembro','Outubro','Novembro','Dezembro'][month-1]??''}
}

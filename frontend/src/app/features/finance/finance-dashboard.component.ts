import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DonutChartComponent } from './donut-chart.component';
import { FinanceCategory, FinanceService, FinanceSummary } from './finance.service';
import { TransactionFormComponent } from './transaction-form.component';
import { FinanceNavComponent } from './finance-nav.component';
import { FinanceViewModeComponent } from './finance-view-mode.component';
import { FinanceHelpTermComponent } from './finance-help-term.component';
import { FinanceInsightsComponent } from './finance-insights.component';
import { FinanceAlertsComponent } from './finance-alerts.component';

@Component({imports:[CurrencyPipe,DatePipe,DecimalPipe,RouterLink,DonutChartComponent,TransactionFormComponent,FinanceNavComponent,FinanceViewModeComponent,FinanceHelpTermComponent,FinanceInsightsComponent,FinanceAlertsComponent],template:`
<section class="page finance-page">
  <app-finance-nav/>
  <app-finance-view-mode (detailedChange)="detailed.set($event)"/>
  @if(detailed()){<aside class="panel"><app-finance-help-term label="Quanto sobrou" explanation="É a diferença entre suas entradas e saídas no período."/> <span>Entradas − saídas = quanto sobrou.</span></aside>}
  <div class="finance-heading"><div><span class="eyebrow">MINHA CONTA</span><h1>Controle Financeiro</h1><p class="lead">Acompanhe suas entradas, saídas e saldo.</p></div><button class="primary" (click)="showForm.set(true)">+ Novo lançamento</button></div>
  <div class="period" aria-label="Selecionar período"><button aria-label="Mês anterior" (click)="moveMonth(-1)">‹</button><label><span>Período</span><input type="month" [value]="monthValue" (change)="selectMonth($event)"></label><button aria-label="Próximo mês" (click)="moveMonth(1)">›</button></div>
  @if(loading()){<div class="finance-skeleton" aria-label="Carregando dados financeiros"><i></i><i></i><i></i><i></i></div>}
  @if(error()){<div class="notice error" role="alert">{{error()}} <button class="link" (click)="load()">Tentar novamente</button></div>}
  @if(summary();as data){
    <div class="finance-stats">
      <article class="money-card income"><span>Entradas</span><strong>{{data.totalIncome|currency:'BRL':'symbol':'1.2-2'}}</strong><small>{{comparison(data.comparison.incomePercentage,'entradas')}}</small></article>
      <article class="money-card expense"><span>Saídas</span><strong>{{data.totalExpenses|currency:'BRL':'symbol':'1.2-2'}}</strong><small>{{comparison(data.comparison.expensePercentage,'saídas')}}</small></article>
      <article class="money-card balance"><span>Quanto sobrou</span><strong>{{data.balance|currency:'BRL':'symbol':'1.2-2'}}</strong><small>{{comparison(data.comparison.balancePercentage,'saldo')}}</small></article>
      <article class="money-card commitment"><span>Renda comprometida</span>@if(data.incomeCommitmentPercentage!==null){<strong>{{data.incomeCommitmentPercentage|number:'1.1-1'}}%</strong><small>{{data.transactionCount}} lançamentos no período</small>}@else{<strong>—</strong><small>Sem entradas cadastradas</small>}</article>
    </div>
    <div class="finance-charts"><app-donut-chart title="Saídas por categoria" [segments]="data.expensesByCategory" emptyMessage="Cadastre saídas para visualizar sua distribuição por categoria."/><app-donut-chart title="Entradas por categoria" [segments]="data.incomeByCategory" emptyMessage="Cadastre entradas para visualizar sua distribuição por categoria."/></div>
    <section class="recent panel"><div class="section-head"><div><span class="eyebrow">MOVIMENTAÇÃO</span><h2>Últimos lançamentos</h2></div><a routerLink="/minha-conta/financas/lancamentos">Ver todos →</a></div>
      @for(item of data.latestTransactions;track item.id){<div class="finance-row"><time>{{item.transactionDate|date:'dd/MM':'UTC'}}</time><div><b>{{item.description}}</b><span>{{item.categoryName}}</span></div><strong [class.income-value]="item.type==='Income'"><span class="sr-only">{{item.type==='Income'?'Entrada':'Saída'}}:</span>{{item.type==='Income'?'+':'−'}} {{item.amount|currency:'BRL':'symbol':'1.2-2'}}</strong></div>}@empty{<div class="finance-empty"><p>Você ainda não possui lançamentos neste mês.</p><button class="primary" (click)="showForm.set(true)">Adicionar primeiro lançamento</button></div>}
    </section>
    <app-finance-insights [year]="selYear" [month]="selMonth"/>
    <app-finance-alerts/>
  }
</section>
@if(showForm()){<app-transaction-form [categories]="categories()" (cancel)="showForm.set(false)" (saved)="afterSaved()"/>}`,
styles:[`.finance-page{max-width:1180px}.finance-heading{display:flex;align-items:end;justify-content:space-between;gap:20px}.finance-heading h1{margin-bottom:8px}.period{display:flex;justify-content:center;align-items:center;gap:12px;margin:35px 0}.period button{width:42px;height:42px;border:1px solid var(--line);border-radius:50%;background:#fff;font-size:25px;cursor:pointer}.period label{margin:0;text-align:center}.period label span{display:block;color:var(--muted);font-size:12px}.period input{border:0;color:var(--navy);font-weight:800;font-size:17px}.finance-stats,.finance-skeleton{display:grid;grid-template-columns:repeat(4,1fr);gap:14px}.money-card{border:1px solid var(--line);border-radius:14px;padding:22px;background:#fff;display:grid;gap:8px;border-top:4px solid var(--navy)}.money-card strong{font-size:27px}.money-card small{color:var(--muted)}.income{border-top-color:#168261}.expense{border-top-color:#a63434}.commitment{border-top-color:#d39b00}.finance-charts{display:grid;grid-template-columns:1fr 1fr;gap:18px;margin:22px 0}.recent{margin-top:22px}.recent .section-head{margin-bottom:8px}.finance-row{display:grid;grid-template-columns:58px 1fr auto;gap:14px;align-items:center;padding:14px 0;border-bottom:1px solid var(--line)}.finance-row time,.finance-row span{color:var(--muted)}.finance-row div{display:grid}.finance-row>strong{color:var(--danger)}.finance-row>strong.income-value{color:var(--green)}.finance-empty{text-align:center;padding:45px}.finance-skeleton i{height:120px;border-radius:14px;background:linear-gradient(90deg,#f6edf2,#fff,#f6edf2);background-size:200% 100%;animation:pulse 1.3s infinite}@keyframes pulse{to{background-position:-200% 0}}.sr-only{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0)}@media(max-width:850px){.finance-stats,.finance-skeleton,.finance-charts{grid-template-columns:1fr 1fr}}@media(max-width:560px){.finance-heading{align-items:start;flex-direction:column}.finance-stats,.finance-skeleton,.finance-charts{grid-template-columns:1fr}.finance-row{grid-template-columns:45px 1fr}.finance-row>strong{grid-column:2}}`]
})
export class FinanceDashboardComponent{
  private readonly finance=inject(FinanceService);readonly summary=signal<FinanceSummary|null>(null);readonly categories=signal<FinanceCategory[]>([]);readonly loading=signal(true);readonly error=signal('');readonly showForm=signal(false);readonly detailed=signal(false);private selected=new Date();
  constructor(){this.selected.setDate(1);this.finance.categories().subscribe({next:x=>this.categories.set(x),error:()=>this.error.set('Não foi possível carregar as categorias.')});this.load()}
  get monthValue(){return `${this.selected.getFullYear()}-${String(this.selected.getMonth()+1).padStart(2,'0')}`}
  get selYear(){return this.selected.getFullYear()}
  get selMonth(){return this.selected.getMonth()+1}
  load(){this.loading.set(true);this.error.set('');this.finance.summary(this.selected.getFullYear(),this.selected.getMonth()+1).subscribe({next:x=>{this.summary.set(x);this.loading.set(false)},error:()=>{this.loading.set(false);this.error.set('Não foi possível carregar o dashboard financeiro.')}})}
  moveMonth(delta:number){this.selected=new Date(this.selected.getFullYear(),this.selected.getMonth()+delta,1);this.load()}
  selectMonth(event:Event){const value=(event.target as HTMLInputElement).value;if(!value)return;const [year,month]=value.split('-').map(Number);this.selected=new Date(year,month-1,1);this.load()}
  afterSaved(){this.showForm.set(false);this.load()}
  comparison(value:number|null,label:string){if(value===null)return 'Sem base no mês anterior';if(value===0)return `${label} iguais ao mês anterior`;return `${Math.abs(value).toLocaleString('pt-BR',{maximumFractionDigits:1})}% ${value>0?'maior':'menor'} que o mês anterior`}
}

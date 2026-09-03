import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FinanceService, Forecast, InstallmentPlan } from './finance.service';
import { FinanceNavComponent } from './finance-nav.component';

@Component({imports:[FormsModule,CurrencyPipe,DatePipe,FinanceNavComponent],template:`<section class="page finance-v5"><app-finance-nav/><span class="eyebrow">COMPROMISSOS</span><h1>Parcelas, recorrências e previsão</h1><p class="lead">Veja o que já está previsto para os próximos meses.</p>
  <label class="period">Mês da previsão<input type="month" [(ngModel)]="period" (change)="loadForecast()"></label>
  @if(forecast();as f){<section class="forecast panel"><div><span>Entradas esperadas</span><b>{{f.expectedIncome|currency:'BRL'}}</b></div><div><span>Saídas esperadas</span><b>{{f.expectedExpenses|currency:'BRL'}}</b></div><div><span>Saldo esperado</span><b>{{f.expectedBalance|currency:'BRL'}}</b></div><small>{{f.disclaimer}}</small></section>}
  <h2>Compras parceladas</h2><div class="grid">@for(plan of installments();track plan.id){<article class="panel"><h3>{{plan.description}}</h3><p>{{plan.categoryName}} · {{plan.totalAmount|currency:'BRL'}}</p><b>{{plan.elapsedInstallments}} de {{plan.installmentCount}} parcelas no histórico</b><p>Restante futuro: {{plan.remainingAmount|currency:'BRL'}}</p>@if(plan.nextDueDate){<small>Próxima parcela: {{plan.nextDueDate|date:'dd/MM/yyyy':'UTC'}}</small>}</article>}@empty{<p>Nenhuma compra parcelada cadastrada.</p>}</div>
  <h2>Recorrências</h2><div class="grid">@for(item of recurrences();track item.id){<article class="panel"><h3>{{item.description}}</h3><p>{{item.amount|currency:'BRL'}} por mês · próxima em {{item.nextOccurrence|date:'dd/MM/yyyy':'UTC'}}</p><strong>{{item.isActive?'Ativa':'Pausada/encerrada'}}</strong><div class="actions">@if(item.isActive){<button (click)="action(item.id,'pause')">Pausar</button><button (click)="action(item.id,'end')">Encerrar</button>}@else{<button (click)="action(item.id,'resume')">Retomar</button>}</div></article>}@empty{<p>Nenhuma recorrência cadastrada.</p>}</div>
</section>`,styles:[`.finance-v5{max-width:1050px}.period{display:block;width:220px;margin:24px 0}.period input{display:block;width:100%;padding:10px;border:1px solid var(--line);border-radius:8px}.forecast{display:grid;grid-template-columns:repeat(3,1fr);gap:18px}.forecast div{display:grid}.forecast b{font-size:22px}.forecast small{grid-column:1/-1;color:var(--muted)}h2{margin-top:32px}.grid{display:grid;grid-template-columns:1fr 1fr;gap:14px}.grid h3{margin-top:0}.actions{display:flex;gap:8px;margin-top:12px}.actions button{padding:8px;border:1px solid var(--line);border-radius:7px;background:#fff}@media(max-width:700px){.forecast,.grid{grid-template-columns:1fr}.forecast small{grid-column:auto}}`]})
export class FinanceCommitmentsComponent{
  private finance=inject(FinanceService);readonly installments=signal<InstallmentPlan[]>([]);readonly recurrences=signal<any[]>([]);readonly forecast=signal<Forecast|null>(null);period=currentMonth();constructor(){this.load()}
  load(){this.finance.installments().subscribe(x=>this.installments.set(x));this.finance.recurrences().subscribe(x=>this.recurrences.set(x));this.loadForecast()}
  loadForecast(){const [year,month]=this.period.split('-').map(Number);this.finance.forecast(year,month).subscribe(x=>this.forecast.set(x))}
  action(id:string,action:'pause'|'resume'|'end'){this.finance.recurrenceAction(id,action).subscribe(()=>this.load())}
}
function currentMonth(){const now=new Date();return `${now.getFullYear()}-${String(now.getMonth()+1).padStart(2,'0')}`}

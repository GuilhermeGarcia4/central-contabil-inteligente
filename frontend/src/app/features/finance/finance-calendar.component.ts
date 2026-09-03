import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { CalendarDay, FinanceService } from './finance.service';
import { FinanceNavComponent } from './finance-nav.component';

const KIND_LABELS:Record<string,string>={entrada:'Entrada',saida:'Saída',previsto:'Previsto',fatura:'Fatura'};

@Component({imports:[CurrencyPipe,DatePipe,FinanceNavComponent],template:`
<section class="page finance-page">
  <app-finance-nav/>
  <div class="finance-heading"><div><span class="eyebrow">MINHA CONTA</span><h1>Calendário financeiro</h1><p class="lead">Veja o que entra, o que sai e o que vence em cada dia.</p></div></div>
  @if(error()){<div class="notice error" role="alert">{{error()}}</div>}
  <div class="period" aria-label="Selecionar período"><button aria-label="Mês anterior" (click)="moveMonth(-1)">‹</button><label><span>Período</span><input type="month" [value]="monthValue" (change)="selectMonth($event)"></label><button aria-label="Próximo mês" (click)="moveMonth(1)">›</button></div>
  @if(days();as list){<div class="calendar-grid">@for(day of list;track day.date){<article class="cal-day"><time>{{day.date|date:'dd/MM':'UTC'}}</time>@for(ev of day.events;track $index){<div class="cal-event" [class.income]="ev.kind==='entrada'||ev.kind==='previsto-entrada'" [class.fatura]="ev.kind==='fatura'"><span>{{KIND_LABELS[ev.kind]||ev.kind}}</span><b>{{ev.description}}</b><strong>{{ev.amount|currency:'BRL':'symbol':'1.2-2'}}</strong></div>}</article>}@empty{<div class="finance-empty"><p>Nenhum evento financeiro neste mês.</p></div>}</div>}
</section>`,
styles:[`.calendar-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(240px,1fr));gap:14px}.cal-day{border:1px solid var(--line);border-radius:14px;padding:16px;background:#fff;display:grid;gap:8px;align-content:start}.cal-day time{font-weight:800;color:var(--navy);font-size:15px}.cal-event{border-left:3px solid var(--danger);padding:6px 10px;background:#fdf6f6;border-radius:6px;display:grid;gap:2px}.cal-event.income{border-left-color:#168261;background:#f2faf6}.cal-event.fatura{border-left-color:#d39b00;background:#fdf9ec}.cal-event span{font-size:11px;color:var(--muted);font-weight:700;text-transform:uppercase}.cal-event b{font-size:14px}.cal-event strong{font-size:14px;color:var(--danger)}.cal-event.income strong{color:var(--green)}`]
})
export class FinanceCalendarComponent{
  private readonly finance=inject(FinanceService);
  readonly days=signal<CalendarDay[]>([]);readonly error=signal('');readonly KIND_LABELS=KIND_LABELS;private selected=new Date();
  constructor(){this.selected.setDate(1);this.load()}
  get monthValue(){return `${this.selected.getFullYear()}-${String(this.selected.getMonth()+1).padStart(2,'0')}`}
  load(){this.finance.calendar(this.selected.getFullYear(),this.selected.getMonth()+1).subscribe({next:x=>this.days.set(x),error:()=>this.error.set('Não foi possível carregar o calendário.')})}
  moveMonth(delta:number){this.selected=new Date(this.selected.getFullYear(),this.selected.getMonth()+delta,1);this.load()}
  selectMonth(event:Event){const value=(event.target as HTMLInputElement).value;if(!value)return;const [year,month]=value.split('-').map(Number);this.selected=new Date(year,month-1,1);this.load()}
}

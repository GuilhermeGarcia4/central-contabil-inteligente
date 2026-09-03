import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FinanceService, FinancialAlert } from './finance.service';

@Component({selector:'app-finance-alerts',imports:[RouterLink],template:`
<section class="alerts-block">
  <div class="section-head"><div><span class="eyebrow">ALERTAS</span><h2>Central de alertas</h2></div><a routerLink="/minha-conta/financas/preferencias">Preferências →</a></div>
  @if(loading()){<div class="alerts-skeleton" aria-label="Carregando alertas"><i></i><i></i></div>}
  @if(error()){<div class="notice error" role="alert">{{error()}}</div>}
  @if(alerts();as items){
    @if(items.length===0){<p class="alerts-empty">Nenhum alerta no momento. Continue acompanhando suas finanças.</p>}
    @for(alert of items;track alert.message){<div class="alert-item" [class]="alert.severity"><span class="dot" aria-hidden="true"></span><p>{{alert.message}}</p>@if(alert.link){<a [routerLink]="alert.link">Ver</a>}</div>}
  }
</section>`,styles:[`.alerts-block{border:1px solid var(--line);border-radius:var(--radius);padding:24px;background:#fff;margin-top:22px}.alerts-block .section-head{display:flex;align-items:center;justify-content:space-between;gap:12px;margin-bottom:16px}.alerts-block .section-head a{color:var(--blue);text-decoration:none}.alert-item{display:flex;align-items:center;gap:12px;padding:12px 14px;border-radius:10px;margin-bottom:8px;border:1px solid var(--line)}.alert-item p{flex:1;margin:0}.alert-item a{color:var(--blue);text-decoration:none;white-space:nowrap}.alert-item .dot{width:9px;height:9px;border-radius:50%;flex:none}.alert-item.danger{background:#fdecec;border-color:#f3c1c1}.alert-item.danger .dot{background:#a63434}.alert-item.warning{background:#fdf6e3;border-color:#f0dfae}.alert-item.warning .dot{background:#d39b00}.alert-item.info{background:#eef4fb;border-color:#c9ddf2}.alert-item.info .dot{background:#2f6fb2}.alerts-empty{color:var(--muted)}.alerts-skeleton{display:grid;gap:8px}.alerts-skeleton i{height:46px;border-radius:10px;background:linear-gradient(90deg,#f6edf2,#fff,#f6edf2);background-size:200% 100%;animation:pulse 1.3s infinite}@keyframes pulse{to{background-position:-200% 0}}`]})
export class FinanceAlertsComponent{
  private readonly finance=inject(FinanceService);
  readonly alerts=signal<FinancialAlert[]>([]);readonly loading=signal(true);readonly error=signal('');
  constructor(){this.load()}
  load(){this.loading.set(true);this.error.set('');this.finance.alerts().subscribe({next:x=>{this.alerts.set(x);this.loading.set(false)},error:()=>{this.loading.set(false);this.error.set('Não foi possível carregar os alertas.')}})}
}

import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FinanceNavComponent } from './finance-nav.component';
import { ExplanationProfile, FinanceService, UserPreferenceView } from './finance.service';

@Component({imports:[FormsModule,FinanceNavComponent],template:`
<section class="page finance-page">
  <app-finance-nav/>
  <div class="finance-heading"><div><span class="eyebrow">MINHA CONTA</span><h1>Preferências</h1><p class="lead">Como você prefere ver as informações e quais alertas deseja receber.</p></div></div>
  @if(loading()){<div class="notice" role="status">Carregando preferências...</div>}
  @if(error()){<div class="notice error" role="alert">{{error()}}</div>}
  @if(pref();as p){
    <form class="prefs" (ngSubmit)="save()">
      <fieldset><legend>Perfil de explicação</legend><p class="hint">Como você prefere ver as informações no assistente, artigos e finanças?</p>
        <label class="radio"><input type="radio" name="profile" [value]="'Simple'" [(ngModel)]="p.explanationProfile"><span><b>Simples</b><small>Linguagem fácil, explicações curtas e exemplos.</small></span></label>
        <label class="radio"><input type="radio" name="profile" [value]="'Detailed'" [(ngModel)]="p.explanationProfile"><span><b>Detalhado</b><small>Detalhes, termos técnicos, fórmulas, fontes e vigência.</small></span></label>
        <label class="radio"><input type="radio" name="profile" [value]="'Both'" [(ngModel)]="p.explanationProfile"><span><b>Os dois</b><small>Resumo simples com opção de ver os detalhes.</small></span></label>
      </fieldset>
      <fieldset><legend>Alertas</legend><p class="hint">Escolha quais alertas deseja ver na Central de alertas.</p>
        <label class="check"><input type="checkbox" [(ngModel)]="p.alertBills" name="alertBills"><span>Contas a pagar próximas</span></label>
        <label class="check"><input type="checkbox" [(ngModel)]="p.alertInvoices" name="alertInvoices"><span>Faturas de cartão</span></label>
        <label class="check"><input type="checkbox" [(ngModel)]="p.alertBudget" name="alertBudget"><span>Planejamento (orçamento)</span></label>
        <label class="check"><input type="checkbox" [(ngModel)]="p.alertGoals" name="alertGoals"><span>Metas</span></label>
        <label class="check"><input type="checkbox" [(ngModel)]="p.alertInstallments" name="alertInstallments"><span>Parcelas futuras</span></label>
        <label class="check"><input type="checkbox" [(ngModel)]="p.alertWeeklySummary" name="alertWeeklySummary"><span>Resumo semanal</span></label>
        <label class="check"><input type="checkbox" [(ngModel)]="p.alertMonthlySummary" name="alertMonthlySummary"><span>Resumo mensal</span></label>
      </fieldset>
      <button class="primary" [disabled]="saving()">{{saving()?'Salvando...':'Salvar preferências'}}</button>
      @if(saved()){<p class="saved" role="status">Preferências salvas.</p>}
    </form>
  }
</section>`,styles:[`.finance-page{max-width:820px}.finance-heading h1{margin-bottom:8px}.prefs{display:grid;gap:24px}.prefs fieldset{border:1px solid var(--line);border-radius:var(--radius);padding:22px;display:grid;gap:12px;background:#fff}.prefs legend{font-weight:800;color:var(--navy);padding:0 6px}.hint{color:var(--muted);margin:0 0 6px}.radio,.check{display:flex;align-items:flex-start;gap:12px;padding:10px;border:1px solid var(--line);border-radius:10px;cursor:pointer}.radio span{display:grid}.radio small{color:var(--muted)}.check span{color:var(--navy)}.prefs .primary{justify-self:start}.saved{color:var(--green);font-weight:600}`]})
export class FinancePreferencesComponent{
  private readonly finance=inject(FinanceService);
  readonly pref=signal<UserPreferenceView|null>(null);readonly loading=signal(true);readonly error=signal('');readonly saving=signal(false);readonly saved=signal(false);
  constructor(){this.finance.preferences().subscribe({next:x=>{this.pref.set(x);this.loading.set(false)},error:()=>{this.loading.set(false);this.error.set('Não foi possível carregar as preferências.')}})}
  save(){const p=this.pref();if(!p)return;this.saving.set(true);this.saved.set(false);this.finance.savePreferences(p).subscribe({next:x=>{this.pref.set(x);this.saving.set(false);this.saved.set(true)},error:()=>{this.saving.set(false);this.error.set('Não foi possível salvar as preferências.')}})}
}

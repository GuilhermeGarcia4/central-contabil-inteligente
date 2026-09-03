import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AccountType, FinanceService, FinancialAccount } from './finance.service';
import { FinanceNavComponent } from './finance-nav.component';

const ACCOUNT_TYPE_LABELS:Record<AccountType,string>={Checking:'Conta corrente',Digital:'Conta digital',Cash:'Dinheiro',Wallet:'Carteira',Savings:'Poupança'};

@Component({imports:[CurrencyPipe,FormsModule,FinanceNavComponent],template:`
<section class="page finance-page">
  <app-finance-nav/>
  <div class="finance-heading"><div><span class="eyebrow">MINHA CONTA</span><h1>Minhas contas</h1><p class="lead">Cadastre suas contas e faça transferências entre elas.</p></div><button class="primary" (click)="openNew()">+ Nova conta</button></div>
  @if(error()){<div class="notice error" role="alert">{{error()}}</div>}
  @if(accounts();as list){<div class="account-grid">@for(acc of list;track acc.id){<article class="account-card" [style.borderTopColor]="acc.color||'var(--navy)'"><span>{{ACCOUNT_TYPE_LABELS[acc.type]}}</span><b>{{acc.name}}</b><strong>{{acc.balance|currency:'BRL':'symbol':'1.2-2'}}</strong><div class="row-actions"><button class="link" (click)="edit(acc)">Editar</button><button class="link danger" (click)="remove(acc)">Excluir</button></div></article>}@empty{<div class="finance-empty"><p>Nenhuma conta cadastrada.</p><button class="primary" (click)="openNew()">Adicionar conta</button></div>}</div>}
  <section class="panel" style="margin-top:22px"><div class="section-head"><div><span class="eyebrow">TRANSFERÊNCIA</span><h2>Transferir entre contas</h2></div></div><form class="transfer-form" (ngSubmit)="doTransfer()"><div class="form-row"><label>De<input type="text" [value]="fromName()" readonly></label><label>Para<input type="text" [value]="toName()" readonly></label></div><div class="form-row"><label>Origem<select [(ngModel)]="fromId" name="fromId"><option [ngValue]="null" disabled>Escolha</option>@for(acc of accounts();track acc.id){<option [ngValue]="acc.id">{{acc.name}}</option>}</select></label><label>Destino<select [(ngModel)]="toId" name="toId"><option [ngValue]="null" disabled>Escolha</option>@for(acc of accounts();track acc.id){<option [ngValue]="acc.id">{{acc.name}}</option>}</select></label></div><div class="form-row"><label>Valor<input type="number" step="0.01" min="0.01" [(ngModel)]="transferAmount" name="transferAmount" required></label><label>Data<input type="date" [(ngModel)]="transferDate" name="transferDate" required></label></div><button class="primary" type="submit">Transferir</button></form></section>
</section>
@if(showForm()){<div class="modal-backdrop" (click)="close()"><form class="modal" (ngSubmit)="save()" (click)="$event.stopPropagation()"><h2>{{editing()?'Editar conta':'Nova conta'}}</h2><label>Nome da conta<input [(ngModel)]="form.name" name="name" required maxlength="100"></label><label>Tipo<select [(ngModel)]="form.type" name="type">@for(t of accountTypes;track t){<option [ngValue]="t">{{ACCOUNT_TYPE_LABELS[t]}}</option>}</select></label><label>Saldo atual<input type="number" step="0.01" [(ngModel)]="form.balance" name="balance" required></label><label>Cor (opcional)<input type="color" [(ngModel)]="form.color" name="color"></label><div class="modal-actions"><button type="button" class="ghost" (click)="close()">Cancelar</button><button class="primary" type="submit">Salvar</button></div></form></div>}`,
styles:[`.account-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(220px,1fr));gap:16px}.account-card{border:1px solid var(--line);border-top:4px solid var(--navy);border-radius:14px;padding:20px;background:#fff;display:grid;gap:6px}.account-card span{color:var(--muted);font-size:13px}.account-card b{font-size:17px}.account-card strong{font-size:24px}.row-actions{display:flex;gap:10px;margin-top:6px}.transfer-form{display:grid;gap:14px;max-width:560px}.form-row{display:grid;grid-template-columns:1fr 1fr;gap:12px}.transfer-form label{display:grid;gap:6px;font-weight:600;color:var(--navy)}.transfer-form input,.transfer-form select{border:1px solid var(--line);border-radius:8px;padding:10px 12px;font-size:15px}.modal-backdrop{position:fixed;inset:0;background:rgba(20,10,16,.5);display:grid;place-items:center;z-index:50;padding:20px}.modal{background:#fff;border-radius:16px;padding:26px;width:min(480px,100%);max-height:90vh;overflow:auto;display:grid;gap:14px}.modal h2{margin:0}.modal label{display:grid;gap:6px;font-weight:600;color:var(--navy)}.modal input,.modal select{border:1px solid var(--line);border-radius:8px;padding:10px 12px;font-size:15px}.modal-actions{display:flex;justify-content:flex-end;gap:10px;margin-top:6px}@media(max-width:560px){.form-row{grid-template-columns:1fr}}`]
})
export class FinanceAccountsComponent{
  private readonly finance=inject(FinanceService);
  readonly accounts=signal<FinancialAccount[]>([]);readonly error=signal('');readonly showForm=signal(false);readonly editing=signal<FinancialAccount|null>(null);
  readonly accountTypes:AccountType[]=['Checking','Digital','Cash','Wallet','Savings'];readonly ACCOUNT_TYPE_LABELS=ACCOUNT_TYPE_LABELS;
  readonly form={name:'',type:'Checking' as AccountType,balance:0,color:'#168261'};
  fromId:string|null=null;toId:string|null=null;transferAmount=0;transferDate=new Date().toISOString().slice(0,10);
  constructor(){this.load()}
  load(){this.finance.accounts().subscribe({next:x=>this.accounts.set(x),error:()=>this.error.set('Não foi possível carregar as contas.')})}
  fromName(){const a=this.accounts().find(x=>x.id===this.fromId);return a?a.name:'Escolha a origem'}
  toName(){const a=this.accounts().find(x=>x.id===this.toId);return a?a.name:'Escolha o destino'}
  openNew(){this.editing.set(null);this.form.name='';this.form.type='Checking';this.form.balance=0;this.form.color='#168261';this.showForm.set(true)}
  edit(acc:FinancialAccount){this.editing.set(acc);this.form.name=acc.name;this.form.type=acc.type;this.form.balance=acc.balance;this.form.color=acc.color||'#168261';this.showForm.set(true)}
  close(){this.showForm.set(false)}
  save(){const body={name:this.form.name,type:this.form.type,balance:this.form.balance,color:this.form.color||null};const op=this.editing()?this.finance.updateAccount(this.editing()!.id,body):this.finance.createAccount(body);op.subscribe({next:()=>{this.showForm.set(false);this.load()},error:()=>this.error.set('Não foi possível salvar a conta.')})}
  remove(acc:FinancialAccount){if(!confirm(`Excluir a conta "${acc.name}"?`))return;this.finance.removeAccount(acc.id).subscribe({next:()=>this.load(),error:()=>this.error.set('Não foi possível excluir a conta.')})}
  doTransfer(){if(!this.fromId||!this.toId){this.error.set('Escolha a conta de origem e destino.');return}this.finance.transfer(this.fromId,this.toId,this.transferAmount,this.transferDate,null).subscribe({next:()=>{this.error.set('');this.transferAmount=0;this.load()},error:(e:any)=>this.error.set(e.error?.errors?.transfer?.[0]||'Não foi possível transferir.')})}
}

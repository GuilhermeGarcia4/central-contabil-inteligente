import { CurrencyPipe } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { catchError, debounceTime, distinctUntilChanged, of, switchMap } from 'rxjs';
import { CreditCard, FinanceCategory, FinanceService, FinanceTransaction, FinancialAccount, PAYMENT_LABELS, PaymentMethod, TransactionPayload, TransactionType } from './finance.service';

@Component({selector:'app-transaction-form',imports:[ReactiveFormsModule,CurrencyPipe],template:`
<div class="dialog-backdrop" role="presentation" (click)="cancel.emit()"><section class="transaction-dialog" role="dialog" aria-modal="true" aria-labelledby="transaction-title" (click)="$event.stopPropagation()">
  <div class="dialog-head"><div><span class="eyebrow">CONTROLE FINANCEIRO</span><h2 id="transaction-title">{{transaction?'Editar':'Novo'}} lançamento</h2></div><button type="button" class="close" aria-label="Fechar" (click)="cancel.emit()">×</button></div>
  <form [formGroup]="form" (ngSubmit)="submit()">
    <fieldset><legend>Tipo</legend><div class="type-choice"><label><input type="radio" formControlName="type" value="Income"> Entrada</label><label><input type="radio" formControlName="type" value="Expense"> Saída</label></div></fieldset>
    <label>Descrição<input formControlName="description" maxlength="160" placeholder="Ex.: Notebook"></label>
    <label>Categoria<select formControlName="categoryId" (change)="categoryChanged()"><option value="">Selecione</option>@for(category of availableCategories;track category.id){<option [value]="category.id">{{category.name}}</option>}</select></label>
    @if(suggested()){<p class="suggested" role="status">✦ Categoria sugerida: <b>{{suggested()!.categoryName}}</b></p>}
    <div class="form-row"><label>Valor (R$)<input type="number" formControlName="amount" min="0.01" step="0.01" inputmode="decimal" placeholder="3.600,00"></label>@if(!isInstallment()){<label>Data<input type="date" formControlName="transactionDate"></label>}</div>
    <label>Forma de pagamento<select formControlName="paymentMethod">@for(option of paymentOptions;track option[0]){<option [value]="option[0]">{{option[1]}}</option>}</select></label>
    @if(showCardSelect()){<label>Cartão de crédito<select formControlName="creditCardId"><option [ngValue]="null">Sem cartão</option>@for(card of cards;track card.id){<option [ngValue]="card.id">{{card.name}}</option>}</select></label>}
    <label>Conta (opcional)<select formControlName="accountId"><option [ngValue]="null">Sem conta</option>@for(acc of accounts;track acc.id){<option [ngValue]="acc.id">{{acc.name}}</option>}</select></label>
    @if(canInstallment()){
      <label class="check"><input type="checkbox" formControlName="isInstallment"> Foi parcelado?</label>
      @if(isInstallment()){
        <div class="form-row"><label>Número de parcelas<input type="number" formControlName="installmentCount" min="2" max="360" step="1"></label><label>Primeira parcela<input type="date" formControlName="firstInstallmentDate"></label></div>
        @if(installmentSummary();as summary){<aside class="installment-summary"><b>Resumo</b><span>{{summary.count}}x de aproximadamente {{summary.value|currency:'BRL':'symbol':'1.2-2'}}</span><small>O total é distribuído em centavos sem diferença de arredondamento.</small></aside>}
      }
    }
    @if(!transaction&&!isInstallment()){<label class="check"><input type="checkbox" formControlName="isRecurring"> Repetir mensalmente</label>}
    <label>Observações<textarea formControlName="notes" maxlength="1000" rows="3"></textarea></label>
    @if(error()){<p class="error" role="alert">{{error()}}</p>}
    <div class="dialog-actions"><button type="button" class="link" (click)="cancel.emit()">Cancelar</button><button class="primary" [disabled]="form.invalid||saving()">{{saving()?'Salvando…':saveLabel}}</button></div>
  </form>
</section></div>`,styles:[`.dialog-backdrop{position:fixed;inset:0;background:rgba(52,21,34,.55);z-index:20;display:grid;place-items:center;padding:18px}.transaction-dialog{background:#fff;border-radius:18px;padding:26px;width:min(620px,100%);max-height:94vh;overflow:auto}.dialog-head{display:flex;justify-content:space-between;align-items:start}.dialog-head h2{margin:0 0 18px}.close{border:0;background:none;font-size:30px;cursor:pointer}.transaction-dialog input:not([type=radio]):not([type=checkbox]),.transaction-dialog select,.transaction-dialog textarea{display:block;width:100%;padding:11px;margin-top:6px;border:1px solid var(--line);border-radius:8px;background:#fff}.transaction-dialog fieldset{border:0;padding:0;margin:0 0 18px}.type-choice{display:flex;gap:20px}.type-choice label{margin:8px 0}.form-row{display:grid;grid-template-columns:1fr 1fr;gap:16px}.suggested,.installment-summary{background:#fff9cf;border-left:3px solid var(--navy);padding:12px;font-size:13px;margin:12px 0;display:grid;gap:5px}.installment-summary span{font-size:18px}.dialog-actions{display:flex;justify-content:flex-end;gap:12px;margin-top:22px}@media(max-width:520px){.form-row{grid-template-columns:1fr}}`]})
export class TransactionFormComponent implements OnInit,OnChanges{
  private readonly fb=inject(FormBuilder);private readonly finance=inject(FinanceService);
  @Input() categories:FinanceCategory[]=[];@Input() transaction:FinanceTransaction|null=null;@Input() cards:CreditCard[]=[];@Input() accounts:FinancialAccount[]=[];@Output() saved=new EventEmitter<void>();@Output() cancel=new EventEmitter<void>();
  readonly saving=signal(false);readonly error=signal('');readonly suggested=signal<{categoryId:string;categoryName:string}|null>(null);readonly paymentOptions=Object.entries(PAYMENT_LABELS) as [PaymentMethod,string][];private categoryWasManual=false;
  readonly form=this.fb.group({type:this.fb.nonNullable.control<TransactionType>('Expense'),categoryId:['',Validators.required],description:['',[Validators.required,Validators.maxLength(160)]],amount:[null as number|null,[Validators.required,Validators.min(.01)]],transactionDate:[localDate(),Validators.required],paymentMethod:this.fb.nonNullable.control<PaymentMethod>('Pix'),isRecurring:[false],isInstallment:[false],installmentCount:[null as number|null],firstInstallmentDate:[localDate()],notes:[''],creditCardId:[null as string|null],accountId:[null as string|null]});
  get availableCategories(){return this.categories.filter(x=>x.type===this.form.controls.type.value)}
  get saveLabel(){return `Salvar ${this.form.controls.type.value==='Income'?'entrada':'saída'}`}
  showCardSelect(){return this.form.controls.type.value==='Expense'&&this.form.controls.paymentMethod.value==='CreditCard'}
  canInstallment(){return !this.transaction&&this.form.controls.type.value==='Expense'&&this.form.controls.paymentMethod.value==='CreditCard'}
  isInstallment(){return !!this.form.controls.isInstallment.value}
  installmentSummary(){const count=Number(this.form.controls.installmentCount.value);const amount=Number(this.form.controls.amount.value);return count>=2&&amount>0?{count,value:amount/count}:null}
  ngOnInit(){
    this.form.controls.description.valueChanges.pipe(debounceTime(400),distinctUntilChanged(),switchMap(description=>{if(this.categoryWasManual||(description?.trim().length??0)<3)return of({suggestions:[]});return this.finance.suggest(this.form.controls.type.value,description!,this.form.controls.notes.value).pipe(catchError(()=>of({suggestions:[]})))})).subscribe(result=>{const first=result.suggestions[0];if(first&&first.confidence>=.75&&!this.categoryWasManual){this.form.controls.categoryId.setValue(first.categoryId);this.suggested.set(first)}else this.suggested.set(null)});
    this.form.controls.type.valueChanges.subscribe(()=>{this.form.controls.categoryId.setValue('');this.categoryWasManual=false;this.suggested.set(null);this.keepInstallmentValid()});
    this.form.controls.paymentMethod.valueChanges.subscribe(()=>this.keepInstallmentValid());
    this.form.controls.isInstallment.valueChanges.subscribe(checked=>{const count=this.form.controls.installmentCount,date=this.form.controls.firstInstallmentDate;if(checked){count.addValidators([Validators.required,Validators.min(2),Validators.max(360)]);date.addValidators(Validators.required);this.form.controls.isRecurring.setValue(false)}else{count.clearValidators();date.clearValidators()}count.updateValueAndValidity();date.updateValueAndValidity()});
  }
  ngOnChanges(changes:SimpleChanges){if(changes['transaction']&&this.transaction){this.form.patchValue({type:this.transaction.type,categoryId:this.transaction.categoryId,description:this.transaction.description,amount:this.transaction.amount,transactionDate:this.transaction.transactionDate,paymentMethod:this.transaction.paymentMethod,isRecurring:false,isInstallment:false,notes:this.transaction.notes??''},{emitEvent:false});this.categoryWasManual=true}}
  categoryChanged(){this.categoryWasManual=true;this.suggested.set(null)}
  submit(){if(this.form.invalid)return;this.saving.set(true);this.error.set('');const value=this.form.getRawValue();const installment=!!value.isInstallment;    const payload:TransactionPayload={type:value.type,categoryId:value.categoryId!,description:value.description!.trim(),amount:Number(value.amount),transactionDate:installment?value.firstInstallmentDate!:value.transactionDate!,paymentMethod:value.paymentMethod,isRecurring:!!value.isRecurring,recurrenceEndDate:null,notes:value.notes?.trim()||null,isInstallment:installment,installmentCount:installment?Number(value.installmentCount):null,firstInstallmentDate:installment?value.firstInstallmentDate:null,creditCardId:value.creditCardId||null,accountId:value.accountId||null};const request=this.transaction?this.finance.update(this.transaction.id,payload):this.finance.create(payload);request.subscribe({next:()=>{this.saving.set(false);this.saved.emit()},error:e=>{this.saving.set(false);this.error.set(firstValidationMessage(e.error)??e.error?.title??'Não foi possível salvar o lançamento.')}})}
  private keepInstallmentValid(){if(!this.canInstallment())this.form.controls.isInstallment.setValue(false)}
}
function localDate(){const now=new Date();return `${now.getFullYear()}-${String(now.getMonth()+1).padStart(2,'0')}-${String(now.getDate()).padStart(2,'0')}`}
function firstValidationMessage(error:any){const values=error?.errors?Object.values(error.errors).flat():[];return values.length?String(values[0]):null}

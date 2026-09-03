import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FinanceNavComponent } from './finance-nav.component';
import { FinanceCategory, FinanceService, ImportPreview, ImportPreviewRow, ImportResult } from './finance.service';

@Component({imports:[FormsModule,CurrencyPipe,DatePipe,FinanceNavComponent],template:`
<section class="page finance-page">
  <app-finance-nav/>
  <div class="finance-heading"><div><span class="eyebrow">MINHA CONTA</span><h1>Importar lançamentos</h1><p class="lead">Envie um arquivo CSV ou OFX. Nada é importado sem a sua revisão e confirmação.</p></div></div>
  @if(!preview()){
    <form class="upload" (ngSubmit)="upload()">
      <label for="import-file" class="drop">Selecione um arquivo .csv ou .ofx (máx. 5 MB)</label>
      <input id="import-file" type="file" accept=".csv,.ofx" (change)="onFile($event)">
      <button class="primary" [disabled]="!file()||uploading()">{{uploading()?'Analisando...':'Analisar arquivo'}}</button>
      @if(uploadError()){<p class="error" role="alert">{{uploadError()}}</p>}
    </form>
  }
  @if(preview();as p){
    <div class="preview-head">
      <div><b>{{p.fileName}}</b><span>Encontrados: {{p.total}} · Possíveis duplicados: {{p.duplicates}} · Sem categoria: {{p.uncategorized}}</span></div>
      <div class="preview-actions"><button class="ghost" (click)="reset()">Cancelar</button><button class="primary" [disabled]="importing()" (click)="confirm()">{{importing()?'Importando...':'Confirmar importação'}}</button></div>
    </div>
    @if(importError()){<p class="error" role="alert">{{importError()}}</p>}
    @if(result();as r){<div class="result" role="status"><b>Importação concluída.</b> Importados: {{r.imported}} · Duplicados ignorados: {{r.skippedDuplicates}} · Inválidos ignorados: {{r.skippedInvalid}}</div>}
    <div class="table-wrap">
      <table class="import-table">
        <thead><tr><th><input type="checkbox" [checked]="allSelected()" (change)="toggleAll($event)" aria-label="Selecionar todos"></th><th>Data</th><th>Descrição</th><th>Entrada/Saída</th><th>Categoria</th><th>Valor</th><th>Status</th></tr></thead>
        <tbody>@for(row of p.rows;track row.index){<tr [class.duplicate]="row.isDuplicate" [class.unselected]="!selected().has(row.index)">
          <td><input type="checkbox" [checked]="selected().has(row.index)" (change)="toggle(row.index,$event)" [attr.aria-label]="'Selecionar '+row.description"></td>
          <td>{{row.date|date:'dd/MM/yyyy'}}</td>
          <td>{{row.description}}</td>
          <td>{{row.type==='Income'?'Entrada':'Saída'}}</td>
          <td><select [value]="row.categoryId??''" (change)="setCategory(row,$event)"><option value="">Sem categoria</option>@for(cat of categories();track cat.id){<option [value]="cat.id">{{cat.name}}</option>}</select></td>
          <td>{{row.amount|currency:'BRL':'symbol':'1.2-2'}}</td>
          <td>@if(row.isDuplicate){<span class="badge dup">Possível duplicado</span>}@else if(row.categoryId){<span class="badge ok">Pronto</span>}@else{<span class="badge warn">Sem categoria</span>}</td>
        </tr>}</tbody>
      </table>
    </div>
    @if(p.duplicates>0){<p class="dup-note">Lançamentos marcados como possíveis duplicados podem ser desmarcados para não importar, ou mantidos para importar mesmo assim.</p>}
  }
</section>`,styles:[`.finance-page{max-width:1100px}.finance-heading h1{margin-bottom:8px}.upload{display:grid;gap:14px;max-width:520px;background:#fff;border:1px solid var(--line);border-radius:var(--radius);padding:26px}.drop{border:2px dashed var(--line);border-radius:12px;padding:34px;text-align:center;color:var(--muted);cursor:pointer}.upload input[type=file]{font-size:14px}.error{color:var(--danger);font-weight:600}.preview-head{display:flex;align-items:center;justify-content:space-between;gap:14px;margin:18px 0;flex-wrap:wrap}.preview-head b{display:block}.preview-head span{color:var(--muted);font-size:13px}.preview-actions{display:flex;gap:10px}.ghost{border:1px solid var(--line);background:#fff;border-radius:10px;padding:10px 16px;cursor:pointer}.table-wrap{overflow:auto;border:1px solid var(--line);border-radius:var(--radius);background:#fff}.import-table{width:100%;border-collapse:collapse;font-size:14px}.import-table th,.import-table td{padding:10px 12px;text-align:left;border-bottom:1px solid var(--line);white-space:nowrap}.import-table th{background:#fafafa;color:var(--muted);font-size:12px;text-transform:uppercase}.import-table tr.duplicate{background:#fdf6e3}.import-table tr.unselected{opacity:.5}.import-table select{border:1px solid var(--line);border-radius:8px;padding:6px;max-width:180px}.badge{display:inline-block;padding:3px 8px;border-radius:999px;font-size:12px}.badge.dup{background:#fdf0d0;color:#8a6d00}.badge.ok{background:#e8f4f2;color:var(--green)}.badge.warn{background:#fdecec;color:var(--danger)}.dup-note{color:var(--muted);font-size:13px;margin-top:10px}.result{border:1px solid #c9e6d8;background:#e8f4f2;color:var(--green);border-radius:10px;padding:12px 16px;margin-bottom:12px}`]})
export class FinanceImportComponent{
  private readonly finance=inject(FinanceService);
  readonly file=signal<File|null>(null);readonly preview=signal<ImportPreview|null>(null);readonly categories=signal<FinanceCategory[]>([]);
  readonly selected=signal<Set<number>>(new Set());readonly uploading=signal(false);readonly importing=signal(false);
  readonly uploadError=signal('');readonly importError=signal('');readonly result=signal<ImportResult|null>(null);
  constructor(){this.finance.categories().subscribe({next:x=>this.categories.set(x),error:()=>{}})}
  onFile(event:Event){const input=event.target as HTMLInputElement;const f=input.files?.[0];if(!f)return;this.file.set(f);this.uploadError.set('')}
  upload(){const f=this.file();if(!f)return;this.uploading.set(true);this.uploadError.set('');this.result.set(null);this.finance.importPreview(f).subscribe({next:p=>{this.preview.set(p);this.selected.set(new Set(p.rows.filter(r=>!r.isDuplicate).map(r=>r.index)));this.uploading.set(false)},error:(e:any)=>{this.uploading.set(false);this.uploadError.set(e.error?.title??'Não foi possível analisar o arquivo.')}})}
  toggle(index:number,event:Event){const checked=(event.target as HTMLInputElement).checked;const next=new Set(this.selected());if(checked)next.add(index);else next.delete(index);this.selected.set(next)}
  toggleAll(event:Event){const checked=(event.target as HTMLInputElement).checked;const p=this.preview();if(!p)return;this.selected.set(checked?new Set(p.rows.map(r=>r.index)):new Set())}
  allSelected(){const p=this.preview();return !!p&&p.rows.length>0&&this.selected().size===p.rows.length}
  setCategory(row:ImportPreviewRow,event:Event){const value=(event.target as HTMLInputElement).value;const p=this.preview();if(!p)return;const cat=this.categories().find(c=>c.id===value);row.categoryId=value?cat?.id??null:null;row.categoryName=value?cat?.name??null:null;this.preview.set({...p,rows:[...p.rows]})}
  confirm(){const p=this.preview();if(!p)return;const rows=p.rows.filter(r=>this.selected().has(r.index)).map(r=>({index:r.index,date:r.date,description:r.description,amount:r.amount,type:r.type,categoryId:r.categoryId}));this.importing.set(true);this.importError.set('');this.finance.importConfirm(rows).subscribe({next:r=>{this.result.set(r);this.importing.set(false)},error:(e:any)=>{this.importing.set(false);this.importError.set(e.error?.title??'Não foi possível concluir a importação.')}})}
  reset(){this.preview.set(null);this.file.set(null);this.result.set(null);this.importError.set('');this.uploadError.set('')}
}

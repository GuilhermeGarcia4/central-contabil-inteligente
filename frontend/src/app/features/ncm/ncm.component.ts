import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Subject, catchError, debounceTime, distinctUntilChanged, of, switchMap, tap } from 'rxjs';

interface Ncm { code:string; description:string; effectiveFrom:string|null; effectiveUntil:string|null; actType:string; actNumber:string; actYear:string; provider:string; fromCache:boolean }
@Component({imports:[FormsModule],template:`<section class="page narrow"><span class="eyebrow">NOMENCLATURA COMUM DO MERCOSUL</span><h1>Consulta NCM</h1><p class="lead">Pesquise por código de oito dígitos ou por palavras da descrição.</p><div class="search-page"><input name="ncm" [(ngModel)]="query" (ngModelChange)="changed($event)" placeholder="Ex.: 33051000 ou xampus" aria-label="Código ou descrição NCM"><button (click)="changed(query)">Pesquisar</button></div>@if(loading()){<p>Consultando tabela oficial…</p>}@if(error()){<div class="notice error">{{error()}}</div>}<div class="list ncm-list">@for(item of results();track item.code){<button class="ncm-item" (click)="selected.set(item)"><b>{{item.code}}</b><span>{{item.description}}</span></button>}@empty{@if(query.length>=2&&!loading()){<p>Nenhum NCM encontrado.</p>}}</div>@if(selected();as item){<section class="panel ncm-detail"><span class="eyebrow">DETALHES DO NCM</span><h2>{{item.code}}</h2><p>{{item.description}}</p><div class="line"><span>Vigência</span><b>{{item.effectiveFrom||'—'}} a {{item.effectiveUntil||'—'}}</b></div><div class="line"><span>Ato</span><b>{{item.actType}} {{item.actNumber}}/{{item.actYear}}</b></div><small>Fonte: {{item.provider}}{{item.fromCache?' · cache local':''}}</small></section>}</section>`})
export class NcmComponent{
  private readonly http=inject(HttpClient); private readonly destroyRef=inject(DestroyRef); private readonly terms=new Subject<string>();
  query='';results=signal<Ncm[]>([]);selected=signal<Ncm|null>(null);loading=signal(false);error=signal('');
  constructor(){this.terms.pipe(debounceTime(350),distinctUntilChanged(),tap(()=>{this.loading.set(true);this.error.set('')}),switchMap(query=>this.request(query).pipe(catchError((response:HttpErrorResponse)=>{this.error.set(response.error?.detail??'Não foi possível consultar NCM.');return of([])}))),takeUntilDestroyed(this.destroyRef)).subscribe(items=>{this.results.set(items);this.loading.set(false)})}
  changed(value:string){const query=value.trim();if(query.length>=2)this.terms.next(query);else this.results.set([])}
  private request(query:string){const digits=query.replace(/\D/g,'');return digits.length===8&&/^[\d.\s]+$/.test(query)?this.http.get<Ncm>(`/api/v2/ncm/${digits}`).pipe(switchMap(item=>of([item]))):this.http.get<Ncm[]>('/api/v2/ncm',{params:{query}})}
}

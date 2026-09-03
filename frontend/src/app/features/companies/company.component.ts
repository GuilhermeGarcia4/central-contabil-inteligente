import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { Component, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

interface Activity { code: string; description: string; isPrimary: boolean }
interface Company {
  cnpj: string; formattedCnpj: string; legalName: string; tradeName: string; registrationStatus: string;
  openedOn: string | null; companySize: string; legalNature: string;
  address: { street: string; number: string; complement: string; district: string; city: string; state: string; postalCode: string };
  primaryActivity: Activity | null; secondaryActivities: Activity[]; isSimpleNational: boolean | null; isMei: boolean | null;
  provider: string; retrievedAt: string; fromCache: boolean;
}

@Component({ imports: [FormsModule, DatePipe], template: `
<section class="page narrow company-page">
  <span class="eyebrow">CONSULTA EMPRESARIAL</span><h1>Empresas e CNPJ</h1>
  <p class="lead">Consulte dados cadastrais por CNPJ numérico ou alfanumérico. As letras nunca são removidas ou convertidas.</p>
  <form class="search-page" (ngSubmit)="submit()">
    <input name="cnpj" [(ngModel)]="query" autocomplete="off" placeholder="Ex.: 00.000.000/E08G-12" aria-label="CNPJ">
    <button [disabled]="loading()">{{loading() ? 'Consultando…' : 'Consultar'}}</button>
  </form>
  @if(error()){<div class="notice error" role="alert">{{error()}}</div>}
  @if(company(); as item){
    <section class="company-hero panel">
      <div><span class="eyebrow">{{item.registrationStatus || 'SITUAÇÃO NÃO INFORMADA'}}</span><h2>{{item.legalName}}</h2><p>{{item.tradeName}}</p></div>
      <div class="company-id"><small>CNPJ</small><strong>{{item.formattedCnpj}}</strong><span>{{item.fromCache ? 'Cache local' : item.provider}}</span></div>
    </section>
    <div class="detail-grid">
      <section class="panel"><h2>Dados cadastrais</h2><div class="line"><span>Abertura</span><b>{{item.openedOn || 'Não informada'}}</b></div><div class="line"><span>Porte</span><b>{{item.companySize || 'Não informado'}}</b></div><div class="line"><span>Natureza jurídica</span><b>{{item.legalNature || 'Não informada'}}</b></div><div class="line"><span>Simples Nacional</span><b>{{yesNo(item.isSimpleNational)}}</b></div><div class="line"><span>MEI</span><b>{{yesNo(item.isMei)}}</b></div></section>
      <section class="panel"><h2>Endereço</h2><p>{{item.address.street}}, {{item.address.number}} {{item.address.complement}}</p><p>{{item.address.district}} · {{item.address.city}}/{{item.address.state}}</p><p>CEP {{item.address.postalCode}}</p></section>
    </div>
    <section class="panel activities"><h2>Atividades econômicas</h2>@if(item.primaryActivity; as activity){<div class="activity primary-activity"><b>{{activity.code}}</b><span>{{activity.description}}</span><small>Principal</small></div>}@for(activity of item.secondaryActivities; track activity.code){<div class="activity"><b>{{activity.code}}</b><span>{{activity.description}}</span><small>Secundária</small></div>}</section>
    <p class="provenance">Fonte: {{item.provider}} · consultado em {{item.retrievedAt | date:'dd/MM/yyyy HH:mm'}}.</p>
  }
</section>` })
export class CompanyComponent {
  private readonly http = inject(HttpClient); private readonly router = inject(Router);
  readonly cnpj = input(''); query = ''; company = signal<Company | null>(null); loading = signal(false); error = signal('');
  ngOnInit(){ this.query = this.cnpj(); if(this.query) this.load(this.query); }
  submit(){ const normalized = this.query.toUpperCase().replace(/[.\/\-\s]/g, ''); if(normalized) void this.router.navigate(['/empresas', normalized]); }
  yesNo(value: boolean | null){ return value === null ? 'Não informado' : value ? 'Sim' : 'Não'; }
  private load(cnpj: string){ this.loading.set(true); this.error.set(''); this.http.get<Company>(`/api/v2/companies/${encodeURIComponent(cnpj)}`).subscribe({next:value=>{this.company.set(value);this.loading.set(false)},error:(response:HttpErrorResponse)=>{this.error.set(response.error?.detail ?? 'Não foi possível consultar o CNPJ.');this.loading.set(false)}}); }
}

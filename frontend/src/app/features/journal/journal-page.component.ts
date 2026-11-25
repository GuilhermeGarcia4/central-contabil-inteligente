import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { JournalNewsCardComponent } from './journal-news-card.component';
import { JOURNAL_CATEGORIES, JournalNewsResponse, JournalService } from './journal.service';

@Component({
  standalone: true,
  imports: [DatePipe, FormsModule, JournalNewsCardComponent],
  template: `<section class="page journal-page" aria-labelledby="journal-title">
    <span class="eyebrow">FONTES OFICIAIS E ATUALIZAÇÕES</span><h1 id="journal-title">Jornal Contábil</h1>
    <p class="lead">Notícias e comunicados de fontes oficiais para acompanhar assuntos contábeis. Consulte sempre a publicação original; este conteúdo não substitui orientação profissional individualizada.</p>
    <form class="journal-filters panel" (ngSubmit)="applyFilters()" aria-label="Buscar e filtrar notícias">
      <label class="search-field">Buscar por palavras-chave<input name="query" [(ngModel)]="query" placeholder="Ex.: Simples Nacional, tributos, MEI" maxlength="120"></label>
      <label>Categoria<select name="category" [(ngModel)]="category"><option value="">Todas as categorias</option>@for(item of categories;track item){<option [value]="item">{{item}}</option>}</select></label>
      <label>Ordenar<select name="order" [(ngModel)]="order"><option value="newest">Mais recentes</option><option value="oldest">Mais antigas</option></select></label>
      <button class="primary" type="submit">Buscar</button>
    </form>
    @if(loading()){<p role="status" aria-live="polite">Atualizando notícias…</p>}
    @if(error()){<div class="notice error" role="alert">{{error()}} <button class="link" type="button" (click)="load()">Tentar novamente</button></div>}
    @if(data();as response){
      @if(response.warnings.length){<div class="notice source-warning" role="status"><b>Fonte temporariamente indisponível.</b> {{response.warnings.join(' ')}} @if(response.items.length){As últimas notícias carregadas continuam disponíveis.}</div>}
      @if(response.items.length){<p class="result-count" aria-live="polite">{{response.total}} notícia(s) encontrada(s) · Consulta feita em {{response.generatedAt|date:'dd/MM/yyyy HH:mm':'UTC'}}</p><div class="journal-grid">@for(item of response.items;track item.id){<app-journal-news-card [item]="item"/>}</div>
        <nav class="pagination" aria-label="Paginação de notícias"><button type="button" (click)="changePage(response.page-1)" [disabled]="response.page<=1">Anterior</button><span>Página {{response.page}} de {{totalPages(response)}}</span><button type="button" (click)="changePage(response.page+1)" [disabled]="response.page>=totalPages(response)">Próxima</button></nav>
      } @else if(response.sourcesAvailable===0){<div class="notice error" role="status">Não foi possível consultar as fontes agora. Tente novamente; notícias exibidas anteriormente serão preservadas.</div>}
      @else{<div class="notice" role="status">Nenhuma notícia corresponde aos filtros. Altere a busca ou a categoria.</div>}
    }
  </section>`,
  styles: [`.journal-page{max-width:1180px}.journal-page h1{margin-bottom:12px}.journal-page>.lead{max-width:80ch;line-height:1.6}.journal-filters{display:grid;grid-template-columns:minmax(220px,2fr) minmax(170px,1fr) minmax(150px,1fr) auto;align-items:end;gap:14px;margin:26px 0}.journal-filters label{min-width:0;margin:0}.journal-filters input,.journal-filters select{display:block;width:100%;min-width:0;margin-top:6px;padding:11px 12px;border:1px solid #bccbd3;border-radius:7px;background:#fff;color:var(--ink)}.journal-filters .primary{min-height:42px;margin:0}.result-count{color:var(--muted)}.source-warning{max-width:none}.journal-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px}.pagination{display:flex;justify-content:center;align-items:center;gap:16px;margin:28px 0}.pagination button{padding:9px 14px;border:1px solid var(--line);border-radius:8px;background:#fff;color:var(--navy);font-weight:700;cursor:pointer}.pagination button:disabled{opacity:.5;cursor:not-allowed}.pagination button:focus-visible,.journal-filters :focus-visible{outline:3px solid var(--blue-2);outline-offset:2px}@media(max-width:800px){.journal-filters{grid-template-columns:repeat(2,minmax(0,1fr))}.journal-filters .search-field{grid-column:1/-1}.journal-grid{grid-template-columns:1fr}}@media(max-width:500px){.journal-filters{grid-template-columns:1fr}.journal-filters .search-field{grid-column:auto}.pagination{gap:8px;font-size:14px}}`]
})
export class JournalPageComponent {
  private readonly service = inject(JournalService);
  readonly categories = JOURNAL_CATEGORIES;
  readonly data = signal<JournalNewsResponse | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  query = '';
  category = '';
  order: 'newest' | 'oldest' = 'newest';

  constructor() { this.load(); }

  applyFilters() { this.load(1); }
  changePage(page: number) { this.load(page); }
  totalPages(response: JournalNewsResponse) { return Math.max(1, Math.ceil(response.total / response.pageSize)); }

  load(page = this.data()?.page ?? 1) {
    this.loading.set(true);
    this.error.set('');
    this.service.list({ query: this.query, category: this.category, order: this.order, page, pageSize: 12 }).subscribe({
      next: response => { this.data.set(response); this.loading.set(false); },
      error: () => { this.error.set('Não foi possível atualizar as notícias. As notícias já carregadas permanecem na tela.'); this.loading.set(false); }
    });
  }
}

import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { JournalNewsCardComponent } from './journal-news-card.component';
import { JournalNewsResponse, JournalService } from './journal.service';

@Component({
  selector: 'app-journal-home-section',
  standalone: true,
  imports: [RouterLink, JournalNewsCardComponent],
  template: `<section class="section journal-section" aria-labelledby="journal-home-title">
    <div class="section-head"><div><span class="eyebrow">ATUALIZAÇÕES DE FONTES OFICIAIS</span><h2 id="journal-home-title">Jornal Contábil</h2></div><a class="all-news" routerLink="/jornal-contabil">Ver todas as notícias <span aria-hidden="true">→</span></a></div>
    @if(loading()&&!data()){<div class="journal-skeleton" role="status" aria-live="polite"><span>Carregando notícias oficiais…</span><i></i><i></i></div>}
    @if(error()&&!data()){<div class="notice error" role="alert">{{error()}} <button class="link" type="button" (click)="load()">Tentar novamente</button></div>}
    @if(data();as response){
      @if(response.warnings.length){<p class="source-warning" role="status">Algumas fontes estão indisponíveis. As notícias carregadas continuam disponíveis.</p>}
      @if(response.items.length){<div class="journal-layout"><app-journal-news-card [item]="response.items[0]" [featured]="true"/><div class="recent-list" aria-label="Atualizações recentes">@for(item of response.items.slice(1);track item.id){<app-journal-news-card [item]="item"/>}</div></div>}
      @else if(response.sourcesAvailable===0){<div class="notice error" role="status">As fontes oficiais estão temporariamente indisponíveis. Tente novamente mais tarde.</div>}
      @else{<div class="notice" role="status">Nenhuma notícia recente foi encontrada nas fontes consultadas.</div>}
    }
  </section>`,
  styles: [`.journal-section{padding-top:54px;padding-bottom:54px}.journal-section .section-head{align-items:center}.journal-section h2{margin:0}.all-news{color:var(--blue);font-weight:800}.journal-layout{display:grid;grid-template-columns:minmax(0,1.2fr) minmax(0,1fr);gap:18px;align-items:stretch}.recent-list{display:grid;gap:12px}.source-warning{padding:10px 14px;border-left:3px solid #ba7a00;background:#fff8e7;color:#5f470d}.journal-skeleton{display:grid;grid-template-columns:1fr 1fr;gap:12px}.journal-skeleton span{grid-column:1/-1;color:var(--muted)}.journal-skeleton i{height:150px;border-radius:12px;background:linear-gradient(90deg,#f6edf2,#fff,#f6edf2);background-size:200% 100%;animation:journal-pulse 1.3s infinite}@keyframes journal-pulse{to{background-position:-200% 0}}@media(max-width:760px){.journal-layout{grid-template-columns:1fr}.journal-section .section-head{align-items:flex-start;flex-direction:column}.journal-skeleton{grid-template-columns:1fr}}@media(prefers-reduced-motion:reduce){.journal-skeleton i{animation:none}}`]
})
export class JournalHomeSectionComponent {
  private readonly service = inject(JournalService);
  readonly data = signal<JournalNewsResponse | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');

  constructor() { this.load(); }

  load() {
    this.loading.set(true);
    this.error.set('');
    this.service.list({ pageSize: 4 }).subscribe({
      next: response => { this.data.set(response); this.loading.set(false); },
      error: () => { this.loading.set(false); this.error.set('Não foi possível consultar as notícias agora.'); }
    });
  }
}

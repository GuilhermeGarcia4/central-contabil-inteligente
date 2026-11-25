import { CommonModule, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CommunityDiscussion, CommunityService } from '../community/community.service';
import { JournalNewsResponse, JournalService } from '../journal/journal.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, DatePipe, FormsModule, RouterLink],
  template: `
    <section class="welcome" aria-labelledby="welcome-title">
      <div><p class="eyebrow">CONTABILIZA FÁCIL</p><h1 id="welcome-title">Tudo para cuidar melhor das suas finanças e do seu negócio.</h1><p class="welcome-copy">Informação, ferramentas e uma comunidade para ajudar você a tomar decisões com mais clareza.</p></div>
      <form class="home-search" (ngSubmit)="search()" role="search"><label for="home-query">O que você procura?</label><div><input id="home-query" name="query" [(ngModel)]="query" placeholder="Ex.: empresa, tributo, educação financeira"><button type="submit">Buscar <span aria-hidden="true">→</span></button></div></form>
    </section>

    <div class="dashboard-grid">
      <section class="services" aria-labelledby="services-title">
        <div class="section-title"><div><p class="eyebrow">COMECE POR AQUI</p><h2 id="services-title">Acesso rápido</h2></div></div>
        <div class="service-grid">
          <a class="service-card" routerLink="/educacao-financeira"><span class="service-icon" aria-hidden="true"><svg viewBox="0 0 24 24"><path d="M3 5.5A2.5 2.5 0 0 1 5.5 3H20v16H5.5A2.5 2.5 0 0 1 3 16.5zM3 16.5A2.5 2.5 0 0 1 5.5 14H20M7 7h9M7 10h7"/></svg></span><span class="service-text"><strong>Educação financeira</strong><span>Aprenda com conteúdos práticos.</span><b>Explorar conteúdos <i aria-hidden="true">→</i></b></span></a>
          <a class="service-card" routerLink="/comparador-propostas"><span class="service-icon" aria-hidden="true"><svg viewBox="0 0 24 24"><path d="M4 6h16M4 12h16M4 18h16M8 3 5 6l3 3m8 0 3 3-3 3m-8 0-3 3 3 3"/></svg></span><span class="service-text"><strong>Comparar propostas</strong><span>Analise custos e condições.</span><b>Comparar agora <i aria-hidden="true">→</i></b></span></a>
          <a class="service-card" routerLink="/buscar"><span class="service-icon" aria-hidden="true"><svg viewBox="0 0 24 24"><circle cx="10.8" cy="10.8" r="6.8"/><path d="m16 16 5 5"/></svg></span><span class="service-text"><strong>Buscar informações</strong><span>Encontre respostas e referências.</span><b>Fazer uma busca <i aria-hidden="true">→</i></b></span></a>
          <a class="service-card" routerLink="/empresas"><span class="service-icon" aria-hidden="true"><svg viewBox="0 0 24 24"><path d="M4 21V5l8-2v18m0-13h8v13M2 21h20M7 7h2m-2 4h2m-2 4h2m7-2h2m-2 4h2"/></svg></span><span class="service-text"><strong>Empresas</strong><span>Consulte dados e informações.</span><b>Consultar empresa <i aria-hidden="true">→</i></b></span></a>
          <a class="service-card" routerLink="/minha-conta/financas"><span class="service-icon" aria-hidden="true"><svg viewBox="0 0 24 24"><rect x="3" y="5" width="18" height="15" rx="2"/><path d="M3 9h18m-5 5h2m-12-12h12"/></svg></span><span class="service-text"><strong>Finanças pessoais</strong><span>Organize seu dinheiro e suas metas.</span><b>Acessar finanças <i aria-hidden="true">→</i></b></span></a>
        </div>
        <nav class="more-tools" aria-label="Outras ferramentas"><span>Mais serviços:</span><a routerLink="/assistente">Assistente</a><a routerLink="/ncm">Consulta NCM</a><a routerLink="/calculadoras">Calculadoras</a><a routerLink="/indicadores-economicos">Indicadores</a><a routerLink="/glossario">Glossário</a><a routerLink="/novidades">Novidades</a></nav>
      </section>

      <aside class="updates-panel" aria-label="Comunidade e Jornal Contábil">
        <section class="feed-panel community-panel" aria-labelledby="community-title">
          <div class="feed-heading"><span class="feed-icon" aria-hidden="true">💬</span><div><p class="eyebrow">TROQUE EXPERIÊNCIAS</p><h2 id="community-title">Comunidade</h2></div></div>
          <p class="feed-intro">Tire dúvidas e converse com outros usuários.</p>
          @if(communityLoading()&&!communityLoaded()){<p class="feed-state" role="status">Carregando discussões…</p>}
          @if(communityError()){<p class="feed-state error" role="status">{{communityError()}} <button type="button" class="retry" (click)="loadCommunity()">Tentar novamente</button></p>}
          @if(communityLoaded()&&!communityError()&&communityItems().length===0){<p class="feed-state">Ainda não há discussões para mostrar. Você pode iniciar a primeira conversa.</p>}
          @if(communityItems().length){<ul class="feed-list">@for(item of communityItems();track item.id){<li><a [routerLink]="['/comunidade',item.id]">{{item.title}}</a><span>{{item.category}} · {{item.createdAt | date:'d MMM':'':'pt-BR'}}</span></li>}</ul>}
          <a class="feed-action" routerLink="/comunidade">Abrir Comunidade <span aria-hidden="true">→</span></a>
        </section>

        <section class="feed-panel journal-panel" aria-labelledby="journal-title">
          <div class="feed-heading"><span class="feed-icon journal-icon" aria-hidden="true">▤</span><div><p class="eyebrow">FONTES E ATUALIZAÇÕES</p><h2 id="journal-title">Jornal Contábil</h2></div></div>
          @if(journalLoading()&&!journalData()){<p class="feed-state" role="status">Carregando notícias…</p>}
          @if(journalError()&&!journalData()){<p class="feed-state error" role="status">{{journalError()}} <button type="button" class="retry" (click)="loadJournal()">Tentar novamente</button></p>}
          @if(journalData();as response){
            @if(response.warnings.length){<p class="feed-state warning" role="status">Algumas fontes estão indisponíveis. As notícias recebidas continuam disponíveis.</p>}
            @if(response.items.length){<ul class="feed-list journal-list">@for(item of response.items.slice(0,2);track item.id){<li><a [href]="item.sourceUrl" target="_blank" rel="noopener noreferrer">{{item.title}} <span class="sr-only">(abre em nova aba)</span></a><span>{{item.source}} · {{item.publishedAt | date:'d MMM y':'':'pt-BR'}} · {{item.category}}</span></li>}</ul>}
            @else if(response.sourcesAvailable===0){<p class="feed-state error" role="status">As fontes estão temporariamente indisponíveis. Tente novamente mais tarde.</p>}
            @else{<p class="feed-state">Nenhuma notícia recente encontrada nas fontes consultadas.</p>}
          }
          <a class="feed-action" routerLink="/jornal-contabil">Ver todas as notícias <span aria-hidden="true">→</span></a>
        </section>
      </aside>
    </div>
  `,
  styles: [`
    :host{display:block;max-width:1240px;margin:0 auto;padding:30px 28px 64px;color:#27151f}.welcome{display:grid;grid-template-columns:minmax(0,1fr) minmax(290px,.72fr);gap:36px;align-items:end;padding:20px 0 30px}.eyebrow{margin:0 0 8px;color:#9a155d;font-size:.73rem;font-weight:800;letter-spacing:.1em}.welcome h1{max-width:720px;margin:0;font-family:Georgia,serif;font-size:clamp(2rem,3.3vw,3rem);line-height:1.08;color:#65033e}.welcome-copy{max-width:650px;margin:12px 0 0;color:#62545e;font-size:1.05rem;line-height:1.55}.home-search{padding:16px;border:1px solid #f0dbe7;border-radius:14px;background:#fffafd;box-shadow:0 8px 24px #4c0b2810}.home-search label{display:block;margin-bottom:8px;font-size:.88rem;font-weight:700}.home-search>div{display:flex;gap:8px}.home-search input{min-width:0;flex:1;padding:12px;border:1px solid #d8c8d1;border-radius:8px;font:inherit}.home-search button{padding:0 16px;border:0;border-radius:8px;background:#79034d;color:#fff;font:inherit;font-weight:750;cursor:pointer}.dashboard-grid{display:grid;grid-template-columns:minmax(0,1.65fr) minmax(300px,.85fr);gap:24px;align-items:start}.section-title{margin:0 0 14px}.section-title h2,.feed-heading h2{margin:0;color:#560137;font-family:Georgia,serif;font-size:1.55rem}.service-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:14px}.service-card{display:flex;gap:14px;min-height:134px;padding:18px 16px;border:1px solid #ead8e2;border-radius:13px;background:#fff;color:inherit;text-decoration:none;transition:transform .15s ease,box-shadow .15s ease,border-color .15s ease}.service-card:hover{transform:translateY(-2px);border-color:#c45a91;box-shadow:0 10px 26px #4c0b2810}.service-card:focus-visible,.feed-panel a:focus-visible,.more-tools a:focus-visible{outline:3px solid #d99600;outline-offset:3px}.service-icon{display:grid;flex:0 0 42px;width:42px;height:42px;place-items:center;border-radius:11px;background:#fff2c6;color:#760047}.service-icon svg{width:23px;height:23px;fill:none;stroke:currentColor;stroke-width:1.7;stroke-linecap:round;stroke-linejoin:round}.service-text{display:grid;align-content:start;gap:5px}.service-text strong{color:#3e102d;font-size:1.02rem}.service-text>span{color:#6e6069;font-size:.9rem;line-height:1.4}.service-text b{margin-top:5px;color:#8c0756;font-size:.87rem}.service-text i{font-style:normal}.more-tools{display:flex;flex-wrap:wrap;gap:9px 15px;padding:19px 2px 0;color:#62545e;font-size:.87rem}.more-tools span{font-weight:700}.more-tools a{color:#79034d;font-weight:650}.updates-panel{display:grid;gap:14px}.feed-panel{padding:18px;border:1px solid #ead8e2;border-radius:14px;background:#fffafd}.community-panel{border-top:3px solid #ddbd3c}.journal-panel{border-top:3px solid #bd4e8b}.feed-heading{display:flex;align-items:center;gap:11px}.feed-icon{display:grid;width:40px;height:40px;place-items:center;border-radius:11px;background:#fff0c2;color:#79034d;font-size:1.25rem}.journal-icon{background:#f7e6ef}.feed-heading .eyebrow{margin-bottom:3px;font-size:.66rem}.feed-heading h2{font-size:1.35rem}.feed-intro{margin:9px 0 0;color:#695c65;font-size:.9rem}.feed-state{margin:13px 0;padding:10px;border-radius:8px;background:#fff;color:#695c65;font-size:.88rem;line-height:1.45}.feed-state.error{background:#fff1f1;color:#7a1e28}.feed-state.warning{background:#fff7dc;color:#624c11}.retry{display:block;margin-top:5px;padding:0;border:0;background:none;color:#79034d;text-decoration:underline;font:inherit;font-weight:700;cursor:pointer}.feed-list{display:grid;gap:11px;margin:13px 0;padding:0;list-style:none}.feed-list li{display:grid;gap:4px}.feed-list li+li{padding-top:10px;border-top:1px solid #efdee7}.feed-list a{color:#4d1636;font-size:.9rem;font-weight:750;line-height:1.4}.feed-list span{color:#756873;font-size:.76rem;line-height:1.4}.feed-action{display:flex;justify-content:space-between;align-items:center;margin-top:12px;padding-top:12px;border-top:1px solid #efdee7;color:#8c0756;font-size:.89rem;font-weight:800;text-decoration:none}.sr-only{position:absolute;width:1px;height:1px;padding:0;margin:-1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0}@media(max-width:900px){:host{padding:20px 20px 48px}.dashboard-grid{grid-template-columns:minmax(0,1.25fr) minmax(270px,.85fr);gap:16px}.welcome{gap:20px}.service-card{padding:14px 12px}}@media(max-width:700px){:host{padding:14px 16px 40px}.welcome{grid-template-columns:1fr;gap:18px;padding:12px 0 22px}.welcome h1{font-size:2.15rem}.dashboard-grid{grid-template-columns:1fr}.service-grid{grid-template-columns:1fr}.service-card{min-height:unset}.updates-panel{grid-row:2}.more-tools{gap:12px}.home-search{padding:13px}.home-search>div{align-items:stretch}.home-search button{padding:0 12px}}@media(prefers-reduced-motion:reduce){.service-card{transition:none}}
  `]
})
export class HomeComponent {
  private readonly community = inject(CommunityService);
  private readonly journal = inject(JournalService);
  private readonly router = inject(Router);
  query = '';
  readonly communityItems = signal<CommunityDiscussion[]>([]);
  readonly communityLoading = signal(true);
  readonly communityLoaded = signal(false);
  readonly communityError = signal('');
  readonly journalData = signal<JournalNewsResponse | null>(null);
  readonly journalLoading = signal(true);
  readonly journalError = signal('');

  constructor() { this.loadCommunity(); this.loadJournal(); }

  search() { const q = this.query.trim(); void this.router.navigate(['/buscar'], q ? { queryParams: { q } } : undefined); }

  loadCommunity() {
    this.communityLoading.set(true); this.communityError.set('');
    this.community.list('', '', 1).subscribe({
      next: page => { this.communityItems.set(page.items.slice(0, 3)); this.communityLoaded.set(true); this.communityLoading.set(false); },
      error: () => { this.communityError.set('Não foi possível consultar as discussões agora.'); this.communityLoading.set(false); this.communityLoaded.set(true); }
    });
  }

  loadJournal() {
    this.journalLoading.set(true); this.journalError.set('');
    this.journal.list({ page: 1, pageSize: 2, order: 'newest' }).subscribe({
      next: response => { this.journalData.set(response); this.journalLoading.set(false); },
      error: () => { this.journalError.set('Não foi possível consultar as notícias agora.'); this.journalLoading.set(false); }
    });
  }
}

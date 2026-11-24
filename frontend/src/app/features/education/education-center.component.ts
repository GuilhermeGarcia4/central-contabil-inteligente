import { HttpClient } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Meta } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

interface ArticleCard {
  id: string;
  title: string;
  slug: string;
  summary: string;
  category: string;
  publishedAt: string | null;
}

const isDemonstrationContent = (article: ArticleCard): boolean =>
  article.title.toLocaleLowerCase('pt-BR').includes('[conteúdo de demonstração]');

@Component({
  standalone: true,
  imports: [DatePipe, FormsModule, RouterLink],
  template: `
    <section class="page education-center" aria-labelledby="education-title">
      <span class="eyebrow">CONTEÚDO PARA DECISÕES MAIS CONSCIENTES</span>
      <h1 id="education-title">Educação financeira</h1>
      <p class="lead">Aprenda sobre dinheiro, crédito e planejamento com conteúdos claros e fontes identificadas.</p>

      <div class="education-search" role="search">
        <label for="education-query">Pesquisar conteúdos</label>
        <input id="education-query" name="query" type="search" autocomplete="off"
          placeholder="Ex.: juros, impostos ou planejamento"
          [ngModel]="query()" (ngModelChange)="query.set($event)">
        <label for="education-category">Categoria</label>
        <select id="education-category" name="category" [ngModel]="selectedCategory()"
          (ngModelChange)="selectedCategory.set($event)">
          <option value="all">Todas as categorias</option>
          @for (category of categories(); track category) {
            <option [value]="category">{{ category }}</option>
          }
        </select>
      </div>

      @if (loading()) {
        <p class="education-status" role="status">Carregando conteúdos…</p>
      } @else if (error()) {
        <div class="education-empty" role="alert">
          <h2>Não foi possível carregar os conteúdos</h2>
          <p>{{ error() }}</p>
          <button class="primary" type="button" (click)="loadArticles()">Tentar novamente</button>
        </div>
      } @else {
        <section class="education-results" aria-labelledby="articles-title">
          <div class="section-head">
            <div><span class="eyebrow">BIBLIOTECA</span><h2 id="articles-title">Artigos</h2></div>
            <span aria-live="polite">{{ filteredArticles().length }} conteúdo(s)</span>
          </div>

          @if (filteredArticles().length) {
            <div class="education-cards">
              @for (article of filteredArticles(); track article.id) {
                <a class="education-card" [routerLink]="['/artigos', article.slug]">
                  <span class="education-category">{{ article.category }}</span>
                  <h3>{{ article.title }}</h3>
                  <p>{{ article.summary }}</p>
                  @if (article.publishedAt) {
                    <time [attr.datetime]="article.publishedAt">Publicado em {{ article.publishedAt | date:'dd/MM/yyyy' }}</time>
                  }
                  <span class="education-card-link">Ler artigo →</span>
                </a>
              }
            </div>
          } @else {
            <div class="education-empty">
              <h2>Nenhum artigo disponível para esta busca</h2>
              <p>Os conteúdos de demonstração não aparecem nesta biblioteca. Tente outro termo ou categoria; novos artigos entram após revisão editorial.</p>
              @if (query() || selectedCategory() !== 'all') {
                <button class="secondary" type="button" (click)="clearFilters()">Limpar filtros</button>
              }
            </div>
          }
        </section>

        <section class="education-tools" aria-labelledby="tools-title">
          <div class="section-head">
            <div><span class="eyebrow">FERRAMENTAS EXISTENTES</span><h2 id="tools-title">Calculadoras</h2></div>
            <span>Simulações educativas com fórmulas e regras apresentadas.</span>
          </div>
          <div class="education-cards tools">
            <a class="education-card" routerLink="/comparador-propostas"><h3>Comparar propostas</h3><p>Compare CET, custo total, parcelas e encargos informados pelos bancos.</p><span class="education-card-link">Comparar →</span></a>
            <a class="education-card" routerLink="/calculadoras"><h3>Central de calculadoras</h3><p>Juros, financiamentos, investimentos, orçamento mensal e dívidas.</p><span class="education-card-link">Abrir ferramentas →</span></a>
            <a class="education-card" routerLink="/calculadoras/juros-compostos"><h3>Juros compostos</h3><p>Projete o crescimento do capital com aportes mensais.</p><span class="education-card-link">Simular →</span></a>
            <a class="education-card" routerLink="/calculadoras/ferias"><h3>Férias</h3><p>Consulte uma estimativa bruta de remuneração e abono.</p><span class="education-card-link">Calcular →</span></a>
            <a class="education-card" routerLink="/calculadoras/decimo-terceiro"><h3>13º salário</h3><p>Estime o valor bruto proporcional aos meses trabalhados.</p><span class="education-card-link">Calcular →</span></a>
          </div>
          <p class="education-disclaimer">As simulações são estimativas educativas e não substituem orientação profissional.</p>
        </section>
      }
    </section>
  `,
  styles: [`
    .education-center{max-width:1120px}
    .education-center>h1{max-width:18ch}
    .education-center>.lead{max-width:65ch;line-height:1.6}
    .education-search{display:grid;grid-template-columns:minmax(220px,1fr) minmax(220px,.55fr);gap:8px 18px;align-items:center;margin:32px 0 48px;padding:22px;border:1px solid var(--line);border-radius:var(--radius);background:var(--surface)}
    .education-search label{margin:0;font-weight:750}
    .education-search input,.education-search select{width:100%;min-height:46px;padding:10px 12px;border:1px solid #c9b2bf;border-radius:8px;background:white;color:var(--ink)}
    .education-search input:focus-visible,.education-search select:focus-visible,.education-card:focus-visible,button:focus-visible{outline:3px solid var(--blue-2);outline-offset:3px}
    .education-results .section-head,.education-tools .section-head{margin-bottom:20px}
    .education-cards{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:18px}
    .education-card{min-height:220px;display:flex;flex-direction:column;padding:24px;border:1px solid var(--line);border-radius:var(--radius);background:white;transition:transform .2s,box-shadow .2s,border-color .2s}
    .education-card:hover{transform:translateY(-3px);box-shadow:var(--shadow);border-color:#ca78a8}
    .education-card h3{font:500 24px/1.2 Georgia,serif;color:var(--navy);margin:12px 0 4px}
    .education-card p{color:var(--muted);line-height:1.55;margin:8px 0 18px}
    .education-category{width:max-content;padding:5px 10px;border-radius:999px;background:#fff0f7;color:var(--blue);font-size:12px;font-weight:750}
    .education-card time{color:var(--muted);font-size:12px;margin-top:auto}
    .education-card-link{color:var(--blue);font-weight:750;margin-top:auto;padding-top:16px}
    .education-empty{padding:30px;border:1px dashed var(--line);border-radius:var(--radius);background:var(--surface)}
    .education-empty h2{font-size:25px}
    .education-empty p,.education-status,.education-disclaimer{color:var(--muted);line-height:1.6}
    .education-empty .primary,.education-empty .secondary{margin-top:8px}
    .secondary{border:1px solid var(--line);border-radius:8px;padding:10px 16px;background:white;color:var(--navy);font-weight:700;cursor:pointer}
    .education-tools{margin-top:68px}
    .education-disclaimer{margin-top:18px;font-size:13px}
    @media(max-width:760px){.education-search{grid-template-columns:1fr;gap:8px}.education-search label:not(:first-child){margin-top:8px}.education-cards{grid-template-columns:repeat(2,minmax(0,1fr))}}
    @media(max-width:520px){.education-cards{grid-template-columns:1fr}.education-card{min-height:190px}.education-center{padding-top:50px}}
  `]
})
export class EducationCenterComponent {
  private readonly http = inject(HttpClient);
  private readonly meta = inject(Meta);

  readonly articles = signal<ArticleCard[]>([]);
  readonly query = signal('');
  readonly selectedCategory = signal('all');
  readonly loading = signal(true);
  readonly error = signal('');
  readonly categories = computed(() => [...new Set(this.articles().filter(article => !isDemonstrationContent(article)).map(article => article.category))]
    .sort((a, b) => a.localeCompare(b, 'pt-BR')));
  readonly filteredArticles = computed(() => {
    const query = this.query().trim().toLocaleLowerCase('pt-BR');
    const category = this.selectedCategory();
    return this.articles().filter(article => {
      if (isDemonstrationContent(article)) return false;
      const matchesQuery = !query || `${article.title} ${article.summary} ${article.category}`
        .toLocaleLowerCase('pt-BR').includes(query);
      return matchesQuery && (category === 'all' || article.category === category);
    });
  });

  constructor() {
    this.meta.updateTag({ name: 'description', content: 'Aprenda sobre educação financeira com artigos claros, fontes identificadas e calculadoras educativas.' });
    this.loadArticles();
  }

  loadArticles(): void {
    this.loading.set(true);
    this.error.set('');
    this.http.get<ArticleCard[]>('/api/v1/articles').subscribe({
      next: articles => {
        this.articles.set(articles);
        this.loading.set(false);
        const activeCategories = this.categories();
        if (this.selectedCategory() !== 'all' && !activeCategories.includes(this.selectedCategory())) {
          this.selectedCategory.set('all');
        }
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Verifique sua conexão e tente novamente.');
      }
    });
  }

  clearFilters(): void {
    this.query.set('');
    this.selectedCategory.set('all');
  }
}

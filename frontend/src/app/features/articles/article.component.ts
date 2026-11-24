import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, input, signal } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';

interface ArticleSource { name: string; url: string; isOfficial: boolean; lastVerifiedAt: string | null }
interface Article {
  id: string;
  title: string;
  slug: string;
  summary: string;
  simpleContent: string;
  technicalContent: string;
  category: string;
  contentVersion: number;
  publishedAt: string | null;
  lastReviewedAt: string | null;
  sources: ArticleSource[];
}
interface ArticleSummary { id: string; title: string; slug: string; summary: string; category: string }
type ArticleContentBlock =
  | { kind: 'heading'; text: string; level: number }
  | { kind: 'paragraph'; text: string }
  | { kind: 'list'; items: string[] }
  | { kind: 'faq'; question: string; answer: string };

@Component({
  standalone: true,
  imports: [DatePipe, RouterLink],
  template: `
    <article class="page article">
      @if (item(); as article) {
        <a class="back-link" routerLink="/educacao-financeira">← Voltar à educação financeira</a>
        <span class="eyebrow">{{ article.category }}</span>
        <h1>{{ article.title }}</h1>
        <p class="lead">{{ article.summary }}</p>
        <div class="article-meta">
          @if (article.lastReviewedAt) { <span>Revisado em <time [attr.datetime]="article.lastReviewedAt">{{ article.lastReviewedAt | date:'dd/MM/yyyy' }}</time></span> }
          <span>Versão {{ article.contentVersion }}</span>
        </div>
        <div class="tabs" role="tablist" aria-label="Nível de explicação">
          <button type="button" role="tab" [attr.aria-selected]="!technical()" [class.active]="!technical()" (click)="technical.set(false)">Explicação simples</button>
          <button type="button" role="tab" [attr.aria-selected]="technical()" [class.active]="technical()" (click)="technical.set(true)">Detalhes técnicos</button>
        </div>
        <section class="prose" aria-live="polite">
          @for (block of contentBlocks(technical() ? article.technicalContent : article.simpleContent); track $index) {
            @switch (block.kind) {
              @case ('heading') {
                @if (block.level === 3) { <h3>{{ block.text }}</h3> } @else { <h2>{{ block.text }}</h2> }
              }
              @case ('paragraph') { <p>{{ block.text }}</p> }
              @case ('list') { <ul>@for (line of block.items; track $index) { <li>{{ line }}</li> }</ul> }
              @case ('faq') { <details class="faq"><summary>{{ block.question }}</summary><p>{{ block.answer }}</p></details> }
            }
          }
        </section>
        <aside class="sources" aria-labelledby="source-title">
          <h2 id="source-title">Fontes e transparência</h2>
          @if (article.lastReviewedAt) { <p>Conteúdo revisado em {{ article.lastReviewedAt | date:'dd/MM/yyyy' }}. As regras podem mudar; confira a fonte oficial antes de agir.</p> }
          @for (source of article.sources; track source.url) {
            <a [href]="source.url" target="_blank" rel="noopener noreferrer">
              {{ source.name }}{{ source.isOfficial ? ' · fonte oficial' : '' }}
              @if (source.lastVerifiedAt) { <small>Verificada em {{ source.lastVerifiedAt | date:'dd/MM/yyyy' }}</small> }
            </a>
          } @empty { <p>Este artigo ainda não possui fontes cadastradas.</p> }
        </aside>
        @if (related().length) {
          <section class="related" aria-labelledby="related-title">
            <span class="eyebrow">CONTINUE APRENDENDO</span>
            <h2 id="related-title">Conteúdos relacionados</h2>
            <div class="related-list">
              @for (relatedArticle of related(); track relatedArticle.slug) {
                <a [routerLink]="['/artigos', relatedArticle.slug]"><b>{{ relatedArticle.title }}</b><span>{{ relatedArticle.summary }}</span></a>
              }
            </div>
          </section>
        }
      } @else if (error()) {
        <div class="article-error" role="alert"><h1>Artigo indisponível</h1><p>{{ error() }}</p><a class="primary" routerLink="/educacao-financeira">Ir para educação financeira</a></div>
      } @else { <p class="article-loading" role="status">Carregando artigo…</p> }
    </article>
  `,
  styles: [`
    .article{max-width:860px}
    .back-link{display:inline-block;margin-bottom:28px;color:var(--blue);font-weight:700}
    .article-meta{display:flex;flex-wrap:wrap;gap:12px;margin:20px 0;color:var(--muted);font-size:13px}
    .article-meta span+span:before{content:'·';margin-right:12px}
    .prose{font-size:17px;line-height:1.75;color:var(--ink)}
    .prose h2{font-size:29px;margin:32px 0 10px}
    .prose h3{font:600 21px/1.3 Georgia,serif;color:var(--navy);margin:26px 0 8px}
    .prose p{margin:0 0 18px}
    .prose ul{padding-left:24px;margin:0 0 20px}
    .prose li{padding-left:4px;margin:7px 0}
    .faq{margin:12px 0;border:1px solid var(--line);border-radius:9px;background:white}
    .faq summary{cursor:pointer;padding:14px 16px;font-weight:750;color:var(--navy)}
    .faq p{padding:0 16px 14px;margin:0}
    .sources{margin-top:38px;background:var(--surface)}
    .sources>a{display:grid;gap:4px;padding:10px 0;border-bottom:1px solid var(--line);font-weight:650}
    .sources small{color:var(--muted);font-weight:400}
    .sources p{color:var(--muted);line-height:1.5}
    .related{margin-top:45px}
    .related-list{display:grid;gap:10px}
    .related-list a{display:grid;gap:5px;padding:16px;border:1px solid var(--line);border-radius:9px}
    .related-list a:hover{border-color:#ca78a8;background:var(--surface)}
    .related-list span{color:var(--muted)}
    .article-error{padding:30px;border:1px solid var(--line);border-radius:var(--radius);background:var(--surface)}
    .article-error .primary{display:inline-block;margin-top:8px}
    @media(max-width:600px){.article{padding-top:44px}.prose{font-size:16px}.tabs button{flex:1;padding:12px 8px}}
  `]
})
export class ArticleComponent {
  private readonly http = inject(HttpClient);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);

  readonly slug = input.required<string>();
  readonly item = signal<Article | null>(null);
  readonly technical = signal(false);
  readonly related = signal<ArticleSummary[]>([]);
  readonly error = signal('');

  ngOnInit(): void {
    this.http.get<Article>(`/api/v1/articles/${this.slug()}`).subscribe({
      next: article => {
        this.item.set(article);
        this.title.setTitle(`${article.title} | Contabiliza Fácil`);
        this.meta.updateTag({ name: 'description', content: article.summary });
        this.loadRelated(article);
      },
      error: () => this.error.set('O conteúdo pode ter sido removido ou não está disponível no momento.')
    });
  }

  contentBlocks(content: string): ArticleContentBlock[] {
    const blocks: ArticleContentBlock[] = [];
    let paragraph: string[] = [];
    let list: string[] = [];
    let question: string | null = null;

    const flushParagraph = () => {
      if (paragraph.length) blocks.push({ kind: 'paragraph', text: paragraph.join(' ') });
      paragraph = [];
    };
    const flushList = () => {
      if (list.length) blocks.push({ kind: 'list', items: list });
      list = [];
    };
    const flush = () => { flushParagraph(); flushList(); };

    for (const rawLine of content.split(/\r?\n/)) {
      const line = rawLine.trim();
      if (!line) { flush(); continue; }

      const heading = /^(#{2,3})\s+(.+)$/.exec(line);
      if (heading) {
        flush();
        blocks.push({ kind: 'heading', text: heading[2], level: heading[1].length });
        continue;
      }

      if (line.startsWith('- ')) { flushParagraph(); list.push(line.slice(2)); continue; }
      const questionLine = /^P:\s*(.+)$/.exec(line);
      if (questionLine) { flush(); question = questionLine[1]; continue; }
      const answerLine = /^R:\s*(.+)$/.exec(line);
      if (answerLine && question) {
        flush();
        blocks.push({ kind: 'faq', question, answer: answerLine[1] });
        question = null;
        continue;
      }
      paragraph.push(line);
    }
    flush();
    if (question) blocks.push({ kind: 'paragraph', text: question });
    return blocks;
  }

  private loadRelated(article: Article): void {
    this.http.get<ArticleSummary[]>('/api/v1/articles').subscribe({
      next: articles => {
        const sameCategory = articles.filter(candidate => candidate.slug !== article.slug && candidate.category === article.category &&
          !candidate.title.toLocaleLowerCase('pt-BR').includes('[conteúdo de demonstração]')).slice(0, 3);
        const recommendations = sameCategory.length || article.slug !== 'mei-sem-complicacao'
          ? sameCategory
          : articles.filter(candidate => ['imposto-de-renda-sem-complicacao', 'beneficios-do-governo', 'fgts-e-inss'].includes(candidate.slug));
        this.related.set(recommendations);
      },
      error: () => this.related.set([])
    });
  }
}

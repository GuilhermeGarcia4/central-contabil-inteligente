import { DatePipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { JournalNewsItem } from './journal.service';

@Component({
  selector: 'app-journal-news-card',
  standalone: true,
  imports: [DatePipe],
  template: `<article class="journal-card" [class.featured]="featured">
    <div class="meta"><span class="category">{{item.category}}</span><span><time [attr.datetime]="item.publishedAt">Publicado em {{item.publishedAt|date:'dd/MM/yyyy':'UTC'}}</time> · Consultado em {{item.retrievedAt|date:'dd/MM/yyyy':'UTC'}}</span></div>
    <h3><a [href]="item.sourceUrl" target="_blank" rel="noopener noreferrer">{{item.title}} <span class="external" aria-hidden="true">↗</span><span class="sr-only"> (abre em nova aba)</span></a></h3>
    <p>{{item.summary}}</p>
    <div class="source-line"><span>Fonte: <b>{{item.source}}</b></span><a [href]="item.sourceUrl" target="_blank" rel="noopener noreferrer" [attr.aria-label]="'Ler publicação original: '+item.title">Ler publicação original ↗</a></div>
  </article>`,
  styles: [`.journal-card{min-width:0;height:100%;display:flex;flex-direction:column;gap:10px;padding:20px;border:1px solid var(--line);border-radius:12px;background:#fff}.journal-card.featured{padding:28px;border-left:4px solid var(--blue-2);box-shadow:var(--shadow)}.meta{display:flex;align-items:center;justify-content:space-between;gap:10px;color:var(--muted);font-size:13px}.category{display:inline-flex;border-radius:999px;padding:5px 10px;background:var(--surface);color:var(--navy);font-weight:750}.journal-card h3{margin:0;font-size:19px;line-height:1.35;color:var(--navy);overflow-wrap:anywhere}.featured h3{font:500 clamp(24px,3vw,34px)/1.2 Georgia,serif}.journal-card h3 a:hover,.source-line a:hover{text-decoration:underline}.external{color:var(--blue);font-size:.8em;white-space:nowrap}.journal-card p{margin:0;color:var(--muted);line-height:1.55;overflow-wrap:anywhere}.source-line{display:flex;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap;margin-top:auto;padding-top:12px;border-top:1px solid var(--line);font-size:13px}.source-line a{color:var(--blue);font-weight:750}.sr-only{position:absolute;width:1px;height:1px;padding:0;margin:-1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0}@media(max-width:560px){.journal-card,.journal-card.featured{padding:18px}.source-line{align-items:flex-start;flex-direction:column}}`]
})
export class JournalNewsCardComponent {
  @Input({ required: true }) item!: JournalNewsItem;
  @Input() featured = false;
}

import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

export const JOURNAL_CATEGORIES = ['Legislação', 'Receita Federal', 'Tributos', 'eSocial', 'MEI', 'Contabilidade', 'Negócios'] as const;
export type JournalCategory = typeof JOURNAL_CATEGORIES[number];

export interface JournalNewsItem {
  id: string;
  title: string;
  summary: string;
  category: JournalCategory;
  source: string;
  publishedAt: string;
  sourceUrl: string;
  retrievedAt: string;
}

export interface JournalNewsResponse {
  items: JournalNewsItem[];
  page: number;
  pageSize: number;
  total: number;
  generatedAt: string;
  sourcesAvailable: number;
  sourcesTotal: number;
  warnings: string[];
}

@Injectable({ providedIn: 'root' })
export class JournalService {
  private readonly http = inject(HttpClient);

  list(options: { query?: string; category?: string; page?: number; pageSize?: number; order?: 'newest' | 'oldest' }) {
    let params = new HttpParams()
      .set('page', options.page ?? 1)
      .set('pageSize', options.pageSize ?? 12)
      .set('order', options.order ?? 'newest');
    if (options.query?.trim()) params = params.set('q', options.query.trim());
    if (options.category) params = params.set('category', options.category);
    return this.http.get<JournalNewsResponse>('/api/v1/journal', { params });
  }
}

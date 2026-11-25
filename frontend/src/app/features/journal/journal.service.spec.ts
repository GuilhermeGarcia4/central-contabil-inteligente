import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { afterEach, describe, expect, it } from 'vitest';
import { JournalService } from './journal.service';

describe('JournalService', () => {
  let service: JournalService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(JournalService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('envia busca, categoria, ordenação e paginação à API do Jornal', () => {
    service.list({ query: 'Simples Nacional', category: 'Tributos', page: 2, pageSize: 10, order: 'oldest' }).subscribe();
    const request = http.expectOne(req => req.url === '/api/v1/journal');
    expect(request.request.params.get('q')).toBe('Simples Nacional');
    expect(request.request.params.get('category')).toBe('Tributos');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('10');
    expect(request.request.params.get('order')).toBe('oldest');
    request.flush({ items: [], page: 2, pageSize: 10, total: 0, generatedAt: '', sourcesAvailable: 2, sourcesTotal: 2, warnings: [] });
  });
});

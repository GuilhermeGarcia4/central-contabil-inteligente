import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it } from 'vitest';
import { JournalHomeSectionComponent } from './journal-home-section.component';

describe('JournalHomeSectionComponent', () => {
  it('mostra publicação oficial e link para o Jornal completo', () => {
    TestBed.configureTestingModule({
      imports: [JournalHomeSectionComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    const fixture = TestBed.createComponent(JournalHomeSectionComponent);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne(request => request.url === '/api/v1/journal' && request.params.get('pageSize') === '4').flush({
      items: [{ id: 'https://www.gov.br/noticia', title: 'Atualização contábil', summary: 'Resumo oficial.', category: 'Contabilidade', source: 'CFC', publishedAt: '2026-11-25T10:00:00Z', sourceUrl: 'https://cfc.org.br/noticia', retrievedAt: '2026-11-25T11:00:00Z' }],
      page: 1, pageSize: 4, total: 1, generatedAt: '2026-11-25T11:00:00Z', sourcesAvailable: 2, sourcesTotal: 2, warnings: []
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Atualização contábil');
    expect(fixture.nativeElement.querySelector('a[href="https://cfc.org.br/noticia"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('a[href="/jornal-contabil"]')).toBeTruthy();
    http.verify();
  });
});

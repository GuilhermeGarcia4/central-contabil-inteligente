import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Observable, Subject, of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { FinanceDashboardComponent } from './finance-dashboard.component';
import { FinanceOverview, FinanceService } from './finance.service';

const emptyOverview: FinanceOverview = {
  startDate: '2026-09-01', endDate: '2026-09-30', totalEntries: 0, totalExits: 0, balance: 0,
  entriesByCategory: [], exitsByCategory: [],
  comparison: { incomeDifference: 0, expenseDifference: 0, balanceDifference: 0, incomePercentage: null,
    expensePercentage: null, balancePercentage: null, largestIncreaseCategory: null, largestIncreaseAmount: null,
    largestReductionCategory: null, largestReductionAmount: null },
  evolution: [], planning: { planned: 0, used: 0, remaining: 0, nearLimits: [] },
  upcomingCommitments: [], goals: []
};

function create(overview: () => Observable<FinanceOverview>) {
  const finance = { categories: vi.fn(() => of([])), overview: vi.fn((_startDate: string, _endDate: string) => overview()) };
  const router = { navigate: vi.fn() };
  TestBed.configureTestingModule({ providers: [
    { provide: FinanceService, useValue: finance },
    { provide: Router, useValue: router }
  ] });
  const component = TestBed.runInInjectionContext(() => new FinanceDashboardComponent());
  return { component, finance, router };
}

describe('FinanceDashboardComponent V8', () => {
  it('mantém loading até o overview chegar e aceita resposta sem dados', () => {
    const response = new Subject<FinanceOverview>();
    const { component } = create(() => response);
    expect(component.loading()).toBe(true);
    response.next(emptyOverview);
    expect(component.loading()).toBe(false);
    expect(component.overview()?.entriesByCategory).toEqual([]);
  });

  it('mostra erro de overview sem preservar loading', () => {
    const { component } = create(() => throwError(() => new Error('offline')));
    expect(component.loading()).toBe(false);
    expect(component.error()).toContain('Visão Geral');
  });

  it('recarrega todos os dados ao trocar período', () => {
    const { component, finance } = create(() => of(emptyOverview));
    component.selectPreset('previous');
    expect(finance.overview).toHaveBeenCalledTimes(2);
    const [startDate, endDate] = finance.overview.mock.calls[1];
    expect(startDate).toMatch(/^\d{4}-\d{2}-01$/);
    expect(endDate).toMatch(/^\d{4}-\d{2}-\d{2}$/);
  });

  it('abre lançamentos com categoria e o mesmo intervalo', () => {
    const { component, router } = create(() => of(emptyOverview));
    component.startDate = '2026-08-01'; component.endDate = '2026-08-31';
    component.viewCategory('categoria-1');
    expect(router.navigate).toHaveBeenCalledWith(['/minha-conta/financas/lancamentos'], {
      queryParams: { categoryId: 'categoria-1', startDate: '2026-08-01', endDate: '2026-08-31' }
    });
  });
});

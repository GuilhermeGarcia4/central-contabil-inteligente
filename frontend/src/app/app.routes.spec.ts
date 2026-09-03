import { describe, expect, it } from 'vitest';
import { routes } from './app.routes';

describe('public routes', () => {
  it('exposes friendly calculator and article URLs', () => {
    expect(routes.some(route => route.path === 'calculadoras/:slug')).toBe(true);
    expect(routes.some(route => route.path === 'artigos/:slug')).toBe(true);
    expect(routes.some(route => route.path === 'empresas/:cnpj')).toBe(true);
    expect(routes.some(route => route.path === 'ncm')).toBe(true);
    expect(routes.some(route => route.path === 'buscar')).toBe(true);
    expect(routes.some(route => route.path === 'novidades')).toBe(true);
    expect(routes.some(route => route.path === 'assistente')).toBe(true);
  });

  it('protects account and admin routes', () => {
    expect(routes.find(route => route.path === 'minha-conta')?.canActivate?.length).toBeGreaterThan(0);
    expect(routes.find(route => route.path === 'minha-conta/financas/planejamento')?.canActivate?.length).toBeGreaterThan(0);
    expect(routes.find(route => route.path === 'minha-conta/financas/metas')?.canActivate?.length).toBeGreaterThan(0);
    expect(routes.find(route => route.path === 'minha-conta/financas/compromissos')?.canActivate?.length).toBeGreaterThan(0);
    expect(routes.find(route => route.path === 'minha-conta/financas/relatorio')?.canActivate?.length).toBeGreaterThan(0);
    expect(routes.find(route => route.path === 'admin')?.canActivate?.length).toBeGreaterThan(0);
    expect(routes.find(route => route.path === 'admin/integracoes')?.canActivate?.length).toBeGreaterThan(0);
    expect(routes.find(route => route.path === 'admin/knowledge')?.canActivate?.length).toBeGreaterThan(0);
    expect(routes.find(route => route.path === 'admin/ai')?.canActivate?.length).toBeGreaterThan(0);
  });
});

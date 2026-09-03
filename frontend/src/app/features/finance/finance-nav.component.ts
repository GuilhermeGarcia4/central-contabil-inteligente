import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({selector:'app-finance-nav',imports:[RouterLink,RouterLinkActive],template:`<nav class="finance-nav" aria-label="Controle financeiro">
  <a routerLink="/minha-conta/financas" routerLinkActive="active" [routerLinkActiveOptions]="{exact:true}">Visão geral</a>
  <a routerLink="/minha-conta/financas/lancamentos" routerLinkActive="active">Lançamentos</a>
  <a routerLink="/minha-conta/financas/planejamento" routerLinkActive="active">Planejamento</a>
  <a routerLink="/minha-conta/financas/metas" routerLinkActive="active">Metas</a>
  <a routerLink="/minha-conta/financas/compromissos" routerLinkActive="active">Compromissos</a>
  <a routerLink="/minha-conta/financas/cartoes" routerLinkActive="active">Cartões</a>
  <a routerLink="/minha-conta/financas/contas" routerLinkActive="active">Contas</a>
  <a routerLink="/minha-conta/financas/contas-a-pagar" routerLinkActive="active">A pagar/receber</a>
  <a routerLink="/minha-conta/financas/calendario" routerLinkActive="active">Calendário</a>
  <a routerLink="/minha-conta/financas/dividas" routerLinkActive="active">Dívidas</a>
  <a routerLink="/minha-conta/financas/reserva" routerLinkActive="active">Reserva</a>
  <a routerLink="/minha-conta/financas/relatorio" routerLinkActive="active">Relatório</a>
  <a routerLink="/minha-conta/financas/relatorio-anual" routerLinkActive="active">Anual</a>
  <a routerLink="/minha-conta/financas/importar" routerLinkActive="active">Importar</a>
  <a routerLink="/minha-conta/financas/preferencias" routerLinkActive="active">Preferências</a>
</nav>`,styles:[`.finance-nav{display:flex;gap:8px;overflow:auto;margin:0 0 26px;padding-bottom:8px}.finance-nav a{white-space:nowrap;padding:9px 12px;border:1px solid var(--line);border-radius:999px;color:var(--navy);background:#fff;text-decoration:none}.finance-nav a.active{background:var(--navy);color:var(--accent);border-color:var(--navy)}`]})
export class FinanceNavComponent{}

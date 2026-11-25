import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth/auth.service';

@Component({ selector: 'app-root', imports: [RouterLink, RouterOutlet], template: `
<a class="skip" href="#conteudo">Pular para o conteúdo</a>
<header>
  <a class="brand" routerLink="/" aria-label="Contabiliza Fácil — início" (click)="closeMenu()"><img src="/logo-contabiliza-facil.png" alt=""><span class="brand-copy"><b>Contabiliza</b><em>Fácil</em></span></a>
  <button class="menu-toggle" type="button" aria-controls="main-navigation" [attr.aria-expanded]="menuOpen()" (click)="toggleMenu()"><span aria-hidden="true">☰</span> Menu</button>
  <nav id="main-navigation" class="main-nav" [class.menu-open]="menuOpen()" aria-label="Principal">
    <a routerLink="/" (click)="closeMenu()">Início</a><a routerLink="/educacao-financeira" (click)="closeMenu()">Educação financeira</a><a routerLink="/comparador-propostas" title="Comparar propostas bancárias" (click)="closeMenu()">Comparar</a><a routerLink="/buscar" (click)="closeMenu()">Buscar</a><a routerLink="/comunidade" class="community-link" (click)="closeMenu()">Comunidade</a>
    <details class="nav-more"><summary>Mais serviços</summary><div class="nav-dropdown"><a routerLink="/assistente" (click)="closeMenu()">Assistente</a><a routerLink="/empresas" (click)="closeMenu()">Empresas</a><a routerLink="/ncm" (click)="closeMenu()">NCM</a><a routerLink="/jornal-contabil" (click)="closeMenu()">Jornal Contábil</a><a routerLink="/novidades" (click)="closeMenu()">Novidades</a>@if(auth.user()){<a routerLink="/minha-conta/financas" (click)="closeMenu()">Finanças</a><a routerLink="/minha-conta" (click)="closeMenu()">Minha conta</a>@if(auth.isAdmin()){<a routerLink="/admin" (click)="closeMenu()">Admin</a><a routerLink="/admin/knowledge" (click)="closeMenu()">Knowledge</a><a routerLink="/admin/ai" (click)="closeMenu()">IA</a>}}</div></details>
    @if(auth.user()){<button class="link sign-out" type="button" (click)="auth.logout()">Sair</button>}@else{<a class="nav-cta" routerLink="/entrar" (click)="closeMenu()">Entrar</a>}
  </nav>
</header>
<main id="conteudo"><router-outlet /></main>
<footer><strong>Contabiliza Fácil</strong><p>Informação clara, cálculo auditável e transparência sobre fontes.</p><small>Conteúdo educacional. Confirme decisões relevantes com profissional habilitado.</small></footer>` })
export class AppComponent {
  readonly auth = inject(AuthService);
  readonly menuOpen = signal(false);
  constructor() { void this.auth.restore(); }
  toggleMenu() { this.menuOpen.update(open => !open); }
  closeMenu() { this.menuOpen.set(false); }
}

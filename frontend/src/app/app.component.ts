import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth/auth.service';

@Component({ selector: 'app-root', imports: [RouterLink, RouterOutlet], template: `
<a class="skip" href="#conteudo">Pular para o conteúdo</a>
<header>
  <a class="brand" routerLink="/" aria-label="Contabiliza Fácil — início"><img src="/logo-contabiliza-facil.png" alt=""><span class="brand-copy"><b>Contabiliza</b><em>Fácil</em></span></a>
  <nav aria-label="Principal">
    <a routerLink="/">Início</a><a routerLink="/buscar">Buscar</a><a routerLink="/assistente">Assistente</a><a routerLink="/empresas">Empresas</a><a routerLink="/ncm">NCM</a><a routerLink="/novidades">Novidades</a>
    @if(auth.user()){<a routerLink="/minha-conta/financas">Finanças</a><a routerLink="/minha-conta">Minha conta</a>@if(auth.isAdmin()){<a routerLink="/admin">Admin</a><a routerLink="/admin/knowledge">Knowledge</a><a routerLink="/admin/ai">IA</a>}<button class="link" (click)="auth.logout()">Sair</button>}@else{<a class="nav-cta" routerLink="/entrar">Entrar</a>}
  </nav>
</header>
<main id="conteudo"><router-outlet /></main>
<footer><strong>Contabiliza Fácil</strong><p>Informação clara, cálculo auditável e transparência sobre fontes.</p><small>Conteúdo educacional. Confirme decisões relevantes com profissional habilitado.</small></footer>` })
export class AppComponent { readonly auth = inject(AuthService); constructor(){ void this.auth.restore(); } }

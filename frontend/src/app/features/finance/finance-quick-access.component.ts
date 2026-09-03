import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-finance-quick-access',
  imports: [],
  template: `
<section class="finance-quick-access" aria-labelledby="fqa-title">
  <div class="fqa-copy">
    <span class="eyebrow">MINHAS FINANÇAS</span>
    <h2 id="fqa-title">Tenha uma visão clara do seu dinheiro</h2>
    <p>Acompanhe suas entradas, saídas, metas e planejamento em um só lugar.</p>
    <a class="fqa-cta" [href]="ctaHref" (click)="go($event)">Acessar meu controle →</a>
    <ul class="fqa-tags" aria-label="O que você encontra">
      <li>Entradas</li><li>Saídas</li><li>Metas</li><li>Planejamento</li>
    </ul>
  </div>
  <div class="fqa-visual" aria-hidden="true">
    <svg viewBox="0 0 64 64" focusable="false">
      <rect x="5" y="13" width="54" height="38" rx="9" fill="#fff9cf" stroke="#b51f72" stroke-width="3"/>
      <rect x="5" y="13" width="54" height="15" rx="9" fill="#660240"/>
      <circle cx="46" cy="32" r="5.5" fill="#fffd74"/>
      <path d="M14 24h12" stroke="#fffd74" stroke-width="3" stroke-linecap="round"/>
      <path d="M14 42h8" stroke="#b51f72" stroke-width="3" stroke-linecap="round"/>
    </svg>
  </div>
</section>`,
  styles: [`
.finance-quick-access{max-width:1180px;margin:0 auto;padding:0 24px 8px;display:grid;grid-template-columns:1.5fr .7fr;gap:28px;align-items:center;background:linear-gradient(115deg,#fff9cf 0%,#fffafd 55%,#f9e9f2 100%);border:1px solid var(--line);border-radius:var(--radius);box-shadow:var(--shadow);overflow:hidden}
.fqa-copy{padding:34px 0 34px 34px}
.fqa-copy .eyebrow{margin-bottom:12px}
.fqa-copy h2{font-family:Georgia,serif;color:var(--navy);font-size:clamp(24px,3vw,34px);line-height:1.15;margin:0 0 12px;font-weight:500}
.fqa-copy p{color:var(--muted);font-size:17px;line-height:1.6;margin:0 0 22px;max-width:52ch}
.fqa-cta{display:inline-block;background:var(--navy);color:var(--accent);border-radius:8px;padding:13px 22px;font-weight:750;transition:.2s}
.fqa-cta:hover{background:#7a084e}
.fqa-cta:focus-visible{outline:3px solid var(--blue-2);outline-offset:2px}
.fqa-tags{display:flex;flex-wrap:wrap;gap:8px;list-style:none;margin:22px 0 0;padding:0}
.fqa-tags li{font-size:13px;font-weight:700;color:var(--navy);background:#fff;border:1px solid var(--line);border-radius:999px;padding:6px 12px}
.fqa-visual{display:grid;place-items:center;align-self:stretch;background:radial-gradient(circle at 70% 30%,rgba(181,31,114,.10),transparent 60%)}
.fqa-visual svg{width:min(150px,60%);height:auto;filter:drop-shadow(0 10px 18px rgba(102,2,64,.18))}
@media(max-width:900px){.finance-quick-access{grid-template-columns:1fr;gap:0}.fqa-copy{padding:30px}.fqa-visual{display:none}}
@media(max-width:560px){.finance-quick-access{padding:0 16px}.fqa-copy{padding:26px 20px}.fqa-cta{display:block;text-align:center}.fqa-tags{gap:6px}.fqa-tags li{padding:5px 10px}}
`]
})
export class FinanceQuickAccessComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  get ctaHref(): string {
    return this.auth.user() ? '/minha-conta/financas' : '/entrar?returnUrl=' + encodeURIComponent('/minha-conta/financas');
  }
  go(event: Event): void {
    event.preventDefault();
    if (this.auth.user()) { void this.router.navigate(['/minha-conta/financas']); }
    else { void this.router.navigate(['/entrar'], { queryParams: { returnUrl: '/minha-conta/financas' } }); }
  }
}

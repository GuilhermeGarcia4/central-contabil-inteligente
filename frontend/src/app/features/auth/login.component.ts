import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  imports: [ReactiveFormsModule],
  template: `<section class="page auth">
    <div><span class="eyebrow">SUA CONTA</span><h1>{{registering() ? 'Crie sua conta' : 'Bem-vindo de volta'}}</h1><p>Salve cálculos, favorite conteúdos e consulte seu histórico.</p></div>
    <form class="panel" [formGroup]="form" (ngSubmit)="submit()">
      <button type="button" class="google" [disabled]="!googleEnabled()" (click)="loginWithGoogle()"><span aria-hidden="true">G</span> Continuar com Google</button>
      @if (!googleEnabled() && !googleChecking()) { <small class="google-note">Login Google aguardando configuração no servidor.</small> }
      <div class="divider"><span>ou use seu e-mail</span></div>
      @if (registering()) { <label>Nome<input formControlName="displayName" autocomplete="name"></label> }
      <label>E-mail<input type="email" formControlName="email" autocomplete="email"></label>
      <label>Senha<input type="password" formControlName="password" [autocomplete]="registering() ? 'new-password' : 'current-password'"></label>
      <button class="primary" [disabled]="form.invalid">{{registering() ? 'Criar conta' : 'Entrar'}}</button>
      @if (error()) { <p class="error">{{error()}}</p> }
      <button type="button" class="link" (click)="registering.set(!registering())">{{registering() ? 'Já tenho uma conta' : 'Quero me cadastrar'}}</button>
    </form>
  </section>`,
  styles: [`.google{width:100%;display:flex;align-items:center;justify-content:center;gap:.65rem;padding:.78rem;border:1px solid #c9d1d9;border-radius:.55rem;background:#fff;color:#202124;font-weight:600;cursor:pointer}.google:disabled{cursor:not-allowed;opacity:.55}.google span{font-size:1.15rem;color:#4285f4}.google-note{display:block;margin:.55rem 0;color:#667085}.divider{display:flex;align-items:center;gap:.75rem;margin:1rem 0;color:#667085;font-size:.8rem}.divider::before,.divider::after{content:'';height:1px;background:#d8dee4;flex:1}`]
})
export class LoginComponent implements OnInit {
  private fb = inject(FormBuilder); private auth = inject(AuthService); private router = inject(Router); private route = inject(ActivatedRoute);
  registering = signal(false); error = signal(''); googleEnabled = signal(false); googleChecking = signal(true);
  form = this.fb.nonNullable.group({displayName: [''], email: ['', [Validators.required, Validators.email]], password: ['', [Validators.required, Validators.minLength(10)]]});

  ngOnInit() {
    if (this.route.snapshot.queryParamMap.has('googleError')) this.error.set('Não foi possível entrar com o Google. Tente novamente.');
    this.auth.googleStatus().subscribe({next: status => { this.googleEnabled.set(status.enabled); this.googleChecking.set(false); }, error: () => this.googleChecking.set(false)});
  }

  loginWithGoogle() { if (this.googleEnabled()) this.auth.loginWithGoogle(this.safeReturnUrl()); }
  submit() { const value = this.form.getRawValue(); this.error.set(''); if (this.registering()) { this.auth.register(value.displayName, value.email, value.password).subscribe({next: () => this.doLogin(value.email, value.password), error: e => this.error.set(e.error?.title ?? 'Não foi possível cadastrar.')}); } else this.doLogin(value.email, value.password); }
  private doLogin(email: string, password: string) { this.auth.login(email.trim(), password).subscribe({next: result => { this.auth.accept(result); const requested=this.safeReturnUrl();void this.router.navigateByUrl(requested!=='/minha-conta'||!this.auth.isAdmin()?requested:'/admin'); }, error: response => { if (response.status === 429) this.error.set('Muitas tentativas. Aguarde um minuto e tente novamente.'); else if (response.status === 0) this.error.set('Não foi possível conectar ao servidor.'); else this.error.set('E-mail ou senha inválidos.'); }}); }
  private safeReturnUrl(){const value=this.route.snapshot.queryParamMap.get('returnUrl');return value?.startsWith('/')&&!value.startsWith('//')?value:'/minha-conta'}
}

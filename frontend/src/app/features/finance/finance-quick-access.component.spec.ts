import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { signal } from '@angular/core';
import { vi } from 'vitest';
import { FinanceQuickAccessComponent } from './finance-quick-access.component';
import { AuthService } from '../../core/auth/auth.service';

describe('FinanceQuickAccessComponent',()=>{
  let component:FinanceQuickAccessComponent;
  let router:Router;
  const user=signal<any>(null);
  const authMock={user};

  function setup(){
    TestBed.configureTestingModule({
      providers:[
        {provide:AuthService,useValue:authMock},
        {provide:Router,useValue:{navigate:()=>Promise.resolve(true)}}
      ]
    });
    component=TestBed.createComponent(FinanceQuickAccessComponent).componentInstance;
    router=TestBed.inject(Router);
  }

  it('deslogado aponta o CTA para o login preservando a rota pretendida',()=>{
    setup();user.set(null);
    expect(component.ctaHref).toBe('/entrar?returnUrl='+encodeURIComponent('/minha-conta/financas'));
    const spy=vi.spyOn(router,'navigate');
    component.go(new Event('click'));
    expect(spy).toHaveBeenCalledWith(['/entrar'],{queryParams:{returnUrl:'/minha-conta/financas'}});
  });

  it('logado aponta o CTA direto para o controle financeiro',()=>{
    setup();user.set({id:'1',email:'a@b.c',displayName:'A'});
    expect(component.ctaHref).toBe('/minha-conta/financas');
    const spy=vi.spyOn(router,'navigate');
    component.go(new Event('click'));
    expect(spy).toHaveBeenCalledWith(['/minha-conta/financas']);
  });
});

import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { apiUrl } from '../http/api-url';
export interface User { id:string; email:string; displayName:string }
interface LoginResult { accessToken:string; expiresAt:string; user:User }
interface GoogleStatus { enabled:boolean; callbackUrl:string|null }
@Injectable({providedIn:'root'}) export class AuthService {
  private readonly http=inject(HttpClient); private readonly router=inject(Router); private accessToken:string|null=null; private restorePromise:Promise<void>|null=null; private stateVersion=0;
  readonly user=signal<User|null>(null); readonly roles=signal<string[]>([]); readonly isAdmin=computed(()=>this.roles().includes('Admin'));
  token(){return this.accessToken}
  login(email:string,password:string){return this.http.post<LoginResult>('/api/v1/auth/login',{email,password},{withCredentials:true})}
  accept(result:LoginResult){this.stateVersion++;this.accessToken=result.accessToken;this.user.set(result.user);this.roles.set(this.readRoles(result.accessToken))}
  register(displayName:string,email:string,password:string){return this.http.post('/api/v1/auth/register',{displayName,email,password})}
  googleStatus(){return this.http.get<GoogleStatus>('/api/v1/auth/google/status')}
  loginWithGoogle(returnUrl='/minha-conta'){window.location.assign(apiUrl(`/api/v1/auth/google/start?returnUrl=${encodeURIComponent(returnUrl)}`))}
  restore(){if(this.accessToken&&this.user())return Promise.resolve();return this.restorePromise??=this.restoreInternal()}
  private async restoreInternal(){const version=this.stateVersion;try{const r=await firstValueFrom(this.http.post<{accessToken:string}>('/api/v1/auth/refresh',{}, {withCredentials:true}));if(version!==this.stateVersion)return;this.accessToken=r.accessToken;this.roles.set(this.readRoles(r.accessToken));const payload=this.payload(r.accessToken);this.user.set({id:payload['sub'],email:payload['email'],displayName:payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']??payload['email']})}catch{/* visitor */}}
  logout(){this.http.post('/api/v1/auth/logout',{}, {withCredentials:true}).subscribe({complete:()=>{this.stateVersion++;this.restorePromise=null;this.accessToken=null;this.user.set(null);this.roles.set([]);void this.router.navigateByUrl('/')}})}
  private readRoles(token:string):string[]{const p=this.payload(token);const r=p['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];return r?Array.isArray(r)?r:[r]:[]}
  private payload(token:string):Record<string,any>{let value=token.split('.')[1].replace(/-/g,'+').replace(/_/g,'/');value+='='.repeat((4-value.length%4)%4);return JSON.parse(atob(value))}
}

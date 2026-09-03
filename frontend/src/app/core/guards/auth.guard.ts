import { inject } from '@angular/core'; import { CanActivateFn, Router } from '@angular/router'; import { AuthService } from '../auth/auth.service';
export const authGuard:CanActivateFn=(_route,state)=>{const a=inject(AuthService),router=inject(Router);return a.restore().then(()=>a.user()?true:router.createUrlTree(['/entrar'],{queryParams:{returnUrl:state.url}}))};
export const adminGuard:CanActivateFn=(_route,state)=>{const a=inject(AuthService),router=inject(Router);return a.restore().then(()=>a.isAdmin()?true:router.createUrlTree(['/entrar'],{queryParams:{returnUrl:state.url}}))};

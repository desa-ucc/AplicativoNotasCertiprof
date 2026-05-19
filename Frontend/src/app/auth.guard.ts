import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';

export const authGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
  const router = inject(Router);
  const token = localStorage.getItem('token');
  const menuStr = localStorage.getItem('menu');

  if (!token) {
    router.navigate(['/login']);
    return false;
  }

  const targetPath = '/' + route.url.join('/');
  if (targetPath === '/login' || targetPath === '/') {
      return true;
  }

  if (menuStr) {
      try {
          const menu = JSON.parse(menuStr);
          const hasAccess = menu.some((m: any) => m.path === targetPath);

          if (!hasAccess) {
              // Try to redirect to the first available module if history isn't allowed, else login
              if (menu.length > 0) {
                 router.navigate([menu[0].path]);
              } else {
                 router.navigate(['/login']);
              }
              return false;
          }
      } catch (e) {
          router.navigate(['/login']);
          return false;
      }
  }

  return true;
};

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

  let targetPath = '/';
  if (route.url.length > 0) {
      targetPath = '/' + route.url.map(segment => segment.path).join('/');
  } else if (state.url) {
      targetPath = state.url.split('?')[0]; // fallback to state url if route url is empty
  }

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

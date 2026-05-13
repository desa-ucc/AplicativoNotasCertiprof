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
              console.warn(`AuthGuard: User doesn't have access to ${targetPath}. Redirecting...`);
              if (menu.length > 0) {
                 router.navigate([menu[0].path]);
              } else {
                 router.navigate(['/login']);
              }
              return false;
          }

          return true; // Explicitly allow if access is found
      } catch (e) {
          console.error("AuthGuard: Failed to parse menu", e);
          router.navigate(['/login']);
          return false;
      }
  }

  console.warn("AuthGuard: No menu found, rejecting access.");
  router.navigate(['/login']);
  return false;
};
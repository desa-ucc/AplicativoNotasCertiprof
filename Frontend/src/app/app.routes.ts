import { SecurityComponent } from './security/security.component';
import { Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { CertiprofComponent } from './certiprof/certiprof.component';
import { HistoryComponent } from './history/history.component';
import { authGuard } from './auth.guard';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';

export const routes: Routes = [
  // 1. Ruta Pública (Pantalla completa, sin menú lateral)
  { path: 'login', component: LoginComponent },

  // 2. Rutas Privadas (Protegidas por Guard y envueltas en el Layout del menú)
  {
      path: '',
      component: MainLayoutComponent,
      canActivate: [authGuard],
      children: [
          { path: '', redirectTo: 'history', pathMatch: 'full' },
          { path: 'upload', component: CertiprofComponent },
          { path: 'history', component: HistoryComponent },
          { path: 'security', component: SecurityComponent }
      ]
  },

  // 3. Comodín para rutas no encontradas
  { path: '**', redirectTo: '/login' }
];

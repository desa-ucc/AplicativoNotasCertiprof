import { Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { CertiprofComponent } from './certiprof/certiprof.component';
import { HistoryComponent } from './history/history.component';
import { authGuard } from './auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'upload', component: CertiprofComponent, canActivate: [authGuard] },
  { path: 'history', component: HistoryComponent, canActivate: [authGuard] },
  { path: '', redirectTo: '/login', pathMatch: 'full' }
];

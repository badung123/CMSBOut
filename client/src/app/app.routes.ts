import { Routes } from '@angular/router';
import { authGuard, adminGuard, guestGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent),
    canActivate: [guestGuard]
  },
  {
    path: '',
    loadComponent: () => import('./layout/main-layout/main-layout.component').then(m => m.MainLayoutComponent),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'bank-out',
        loadComponent: () => import('./features/bankout/bankout.component').then(m => m.BankoutComponent)
      },
      {
        path: 'agents',
        loadComponent: () => import('./features/agents/agents.component').then(m => m.AgentsComponent),
        canActivate: [adminGuard]
      }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];

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
      },
      {
        path: 'security',
        loadComponent: () => import('./features/security/security.component').then(m => m.SecurityComponent)
      },
      {
        path: 'redirect/page-rule',
        loadComponent: () => import('./features/redirect/page-rule/page-rule.component').then(m => m.PageRuleComponent)
      },
      {
        path: 'redirect/301',
        loadComponent: () => import('./features/redirect/redirect-301/redirect-301.component').then(m => m.Redirect301Component)
      },
      {
        path: 'redirect/cache',
        loadComponent: () => import('./features/redirect/cache/cache.component').then(m => m.CacheComponent)
      },
      {
        path: 'redirect/security-rule',
        loadComponent: () => import('./features/redirect/welcome/welcome.component').then(m => m.RedirectWelcomeComponent),
        data: { title: 'securityRule' }
      }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];

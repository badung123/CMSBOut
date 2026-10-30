import { Component, HostListener, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss'
})
export class MainLayoutComponent {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  sidebarOpen = false;
  redirectOpen = false;

  constructor() {
    this.redirectOpen = this.isRedirectUrl(this.router.url);
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => {
        this.closeSidebar();
        if (this.isRedirectUrl(event.urlAfterRedirects)) {
          this.redirectOpen = true;
        }
      });
  }

  get redirectActive(): boolean {
    return this.isRedirectUrl(this.router.url);
  }

  toggleRedirect(): void {
    this.redirectOpen = !this.redirectOpen;
  }

  toggleSidebar(): void {
    this.sidebarOpen = !this.sidebarOpen;
  }

  closeSidebar(): void {
    this.sidebarOpen = false;
  }

  logout(): void {
    this.auth.logout();
  }

  @HostListener('window:resize')
  onResize(): void {
    if (window.innerWidth >= 1024) {
      this.sidebarOpen = false;
    }
  }

  private isRedirectUrl(url: string): boolean {
    const path = url.split('?')[0].split('#')[0];
    return path === '/redirect' || path.startsWith('/redirect/');
  }
}

import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  get userMenu(): any[] {
    const menuStr = localStorage.getItem('menu');
    if (menuStr) {
        try {
            return JSON.parse(menuStr);
        } catch (e) {
            return [];
        }
    }
    return [];
  }
  title = 'Frontend';

  constructor(private router: Router) {}

  get userRole(): string | null {
    return localStorage.getItem('role');
  }

  isLoggedIn(): boolean {
    return !!localStorage.getItem('token');
  }

  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('role');
    localStorage.removeItem('menu');
    this.router.navigate(['/login']);
  }

  isActive(path: string): boolean {
    return this.router.url === path;
  }
}

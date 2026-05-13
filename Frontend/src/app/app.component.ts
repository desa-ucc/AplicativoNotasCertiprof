import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  userMenu: any[] = [];
  title = 'Frontend';

  constructor(private router: Router) {}

  ngOnInit() {
    this.refreshMenu();
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.refreshMenu();
    });
  }

  refreshMenu() {
    const menuStr = localStorage.getItem('menu');
    if (menuStr) {
        try {
            this.userMenu = JSON.parse(menuStr);
        } catch (e) {
            this.userMenu = [];
        }
    } else {
        this.userMenu = [];
    }
  }

  get userRole(): string | null {
    return localStorage.getItem('role');
  }

  isLoggedIn(): boolean {
    return !!localStorage.getItem('token');
  }

  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('role');
    this.router.navigate(['/login']);
  }
}

import { Component, OnInit } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { CommonModule } from '@angular/common';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './main-layout.component.html',
  styleUrls: ['./main-layout.component.css']
})
export class MainLayoutComponent implements OnInit {
  menuItems: any[] = [];
  userRole: string | null = null;
  loggedIn: boolean = false;
  nombreUsuarioLogueado: string = 'Usuario';
  isSidebarOpen: boolean = false;

  constructor(private router: Router) {
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.updateState();
      this.closeSidebar();
    });
  }

  ngOnInit() {
    this.updateState();
  }

  toggleSidebar() {
    this.isSidebarOpen = !this.isSidebarOpen;
  }

  closeSidebar() {
    this.isSidebarOpen = false;
  }

  updateState() {
    this.loggedIn = !!localStorage.getItem('token');
    this.userRole = localStorage.getItem('role');
    this.nombreUsuarioLogueado = localStorage.getItem('username') || 'Usuario';

    const menuStr = localStorage.getItem('menu');
    if (menuStr) {
      try {
        this.menuItems = JSON.parse(menuStr);
      } catch (e) {
        this.menuItems = [];
      }
    } else {
        this.menuItems = [];
    }
  }

  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('role');
    localStorage.removeItem('menu');
    localStorage.removeItem('username');
    this.updateState();
    this.router.navigate(['/login']);
  }
}

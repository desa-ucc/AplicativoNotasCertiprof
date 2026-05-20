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
  title = 'Frontend';
  menuItems: any[] = [];
  userRole: string | null = null;
  loggedIn: boolean = false;

  constructor(private router: Router) {
    // Update state on navigation changes (like login redirect)
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.updateState();
    });
  }

  ngOnInit() {
    this.updateState();
  }

  updateState() {
    this.loggedIn = !!localStorage.getItem('token');
    this.userRole = localStorage.getItem('role');

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
    this.updateState();
    this.router.navigate(['/login']);
  }
}

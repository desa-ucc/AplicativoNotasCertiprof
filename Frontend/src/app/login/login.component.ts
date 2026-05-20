import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  username = '';
  passwordPlain = '';
  errorMessage = '';

  constructor(private http: HttpClient, private router: Router) {}

  login() {
    if (!this.username || !this.passwordPlain) {
      this.errorMessage = 'Debe ingresar usuario y contraseña';
      return;
    }

    this.http.post<any>('/api/auth/login', { username: this.username, passwordPlain: this.passwordPlain })
      .subscribe({
        next: (res) => {
          localStorage.setItem('token', res.token);
          localStorage.setItem('role', res.role);
          if (res.menu && res.menu.length > 0) {
             localStorage.setItem('menu', JSON.stringify(res.menu));
             this.router.navigate([res.menu[0].path]);
          } else {
             this.router.navigate(['/upload']);
          }
        },
        error: () => {
          this.errorMessage = 'Credenciales inválidas';
        }
      });
  }
}

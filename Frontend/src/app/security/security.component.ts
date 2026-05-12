import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-security',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './security.component.html',
  styleUrls: ['./security.component.css']
})
export class SecurityComponent implements OnInit {
  users: any[] = [];
  roles: any[] = [];

  isEditing = false;
  isCreating = false;
  userForm: any = { username: '', password: '', rolId: 1 };

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.fetchRoles();
    this.fetchUsers();
  }

  fetchRoles() {
    this.http.get<any[]>('/api/users/roles')
      .subscribe({
        next: (data) => this.roles = data,
        error: (err) => console.error('Error fetching roles:', err)
      });
  }

  fetchUsers() {
    this.http.get<any[]>('/api/users')
      .subscribe({
        next: (data) => this.users = data,
        error: (err) => console.error('Error fetching users:', err)
      });
  }

  openCreateModal() {
    this.isCreating = true;
    this.isEditing = false;
    this.userForm = { username: '', password: '', rolId: this.roles.length > 0 ? this.roles[0].id : 1 };
  }

  openEditModal(user: any) {
    this.isCreating = false;
    this.isEditing = true;
    this.userForm = { id: user.id, username: user.username, password: '', rolId: user.rol_id };
  }

  closeModal() {
    this.isCreating = false;
    this.isEditing = false;
    this.userForm = { username: '', password: '', rolId: 1 };
  }

  saveUser() {
    if (this.isCreating) {
      if (!this.userForm.username || !this.userForm.password) {
        alert('Por favor complete todos los campos');
        return;
      }
      this.http.post('/api/users', { username: this.userForm.username, password: this.userForm.password, rolId: parseInt(this.userForm.rolId) })
        .subscribe({
          next: () => {
            this.closeModal();
            this.fetchUsers();
          },
          error: (err) => {
            console.error('Error creating user:', err);
            alert('Error al crear usuario.');
          }
        });
    } else if (this.isEditing) {
      if (!this.userForm.username) {
        alert('Por favor ingrese el nombre de usuario');
        return;
      }
      this.http.put('/api/users', { id: this.userForm.id, username: this.userForm.username, password: this.userForm.password, rolId: parseInt(this.userForm.rolId) })
        .subscribe({
          next: () => {
            this.closeModal();
            this.fetchUsers();
          },
          error: (err) => {
            console.error('Error updating user:', err);
            alert('Error al actualizar usuario.');
          }
        });
    }
  }

  deleteUser(id: number) {
    if (confirm('¿Está seguro de que desea eliminar este usuario?')) {
      this.http.delete(`/api/users/${id}`)
        .subscribe({
          next: () => {
            this.fetchUsers();
          },
          error: (err) => {
            console.error('Error al eliminar:', err);
            alert('Hubo un error al eliminar el usuario.');
          }
        });
    }
  }
}

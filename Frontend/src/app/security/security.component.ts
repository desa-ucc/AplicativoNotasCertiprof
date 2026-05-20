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

  // New Role Modal State
  isRoleModalOpen = false;
  newRoleName = '';
  selectedNewRoleModules: Set<number> = new Set();

  abrirModalNuevoRol() {
    this.isRoleModalOpen = true;
    this.newRoleName = '';
    this.selectedNewRoleModules.clear();
  }

  cerrarModalNuevoRol() {
    this.isRoleModalOpen = false;
    this.newRoleName = '';
    this.selectedNewRoleModules.clear();
  }

  toggleNewRoleModule(moduleId: number, event: Event) {
    const isChecked = (event.target as HTMLInputElement).checked;
    if (isChecked) {
      this.selectedNewRoleModules.add(moduleId);
    } else {
      this.selectedNewRoleModules.delete(moduleId);
    }
  }

  guardarRol() {
    if (!this.newRoleName.trim()) {
      alert('El nombre del rol es obligatorio.');
      return;
    }

    const payload = {
      nombreRol: this.newRoleName,
      moduleIds: Array.from(this.selectedNewRoleModules)
    };

    this.http.post('/api/security/roles', payload).subscribe({
      next: () => {
        alert('Rol creado exitosamente.');
        this.cerrarModalNuevoRol();
        this.fetchRoles();
      },
      error: (err) => {
        console.error('Error al crear rol:', err);
        alert('Error al crear el rol.');
      }
    });
  }


  activeTab: 'users' | 'roles' = 'users';

  users: any[] = [];
  roles: any[] = [];
  modules: any[] = [];
  modulosDisponibles: any[] = [];

  // User Modal State
  isEditing = false;
  isCreating = false;
  userForm: any = { username: '', password: '', rolId: 1 };

  // Roles/Permissions Modal State
  isEditingPermissions = false;
  selectedRole: any = null;
  rolePermissions: Set<number> = new Set();

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.fetchRoles();
    this.fetchUsers();
    this.fetchModules();
  }

  setTab(tab: 'users' | 'roles') {
    this.activeTab = tab;
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

  fetchModules() {
    this.http.get<any[]>('/api/security/modules')
      .subscribe({
        next: (res) => {
            this.modules = res;
            this.modulosDisponibles = res;
            console.log("Módulos cargados:", res);
        },
        error: (err) => console.error('Error trayendo módulos', err)
      });
  }

  // --- Users Management ---

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
        alert('Por favor complete todos los campos requeridos');
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

  // --- Roles & Permissions Management ---

  openPermissionsModal(role: any) {
    this.selectedRole = role;
    this.isEditingPermissions = true;
    this.rolePermissions.clear();

    // Fetch existing permissions for this role
    this.http.get<any[]>(`/api/security/roles/${role.id}/modules`)
      .subscribe({
        next: (data) => {
          data.forEach(m => this.rolePermissions.add(m.id));
        },
        error: (err) => console.error('Error fetching role permissions:', err)
      });
  }

  closePermissionsModal() {
    this.isEditingPermissions = false;
    this.selectedRole = null;
    this.rolePermissions.clear();
  }

  togglePermission(moduleId: number, event: Event) {
    const isChecked = (event.target as HTMLInputElement).checked;
    if (isChecked) {
      this.rolePermissions.add(moduleId);
    } else {
      this.rolePermissions.delete(moduleId);
    }
  }

  hasPermission(moduleId: number): boolean {
    return this.rolePermissions.has(moduleId);
  }

  savePermissions() {
    if (!this.selectedRole) return;

    this.http.put(`/api/security/roles/${this.selectedRole.id}/permissions`, {
      moduleIds: Array.from(this.rolePermissions)
    }).subscribe({
      next: () => {
        alert('Permisos actualizados. Tenga en cuenta que los usuarios deben volver a iniciar sesión para ver los cambios en su menú principal.');
        this.closePermissionsModal();
      },
      error: (err) => {
        console.error('Error updating permissions:', err);
        alert('Error al actualizar permisos.');
      }
    });
  }
}

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import * as ExcelJS from 'exceljs';
import * as saveAs from 'file-saver';

@Component({
  selector: 'app-security',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './security.component.html',
  styleUrls: ['./security.component.css']
})
export class SecurityComponent implements OnInit {

  activeTab: 'users' | 'roles' = 'users';

  users: any[] = [];
  roles: any[] = [];
  modules: any[] = [];

  // Users Tab Logic
  mostrarFiltrosUsuarios: boolean = false;
  terminoBusquedaUsuario: string = '';
  paginaActualUsuarios: number = 1;
  itemsPorPaginaUsuarios: number = 5;

  get usuariosPaginados() {
      let filtrados = this.users || [];
      if (this.terminoBusquedaUsuario) {
          filtrados = filtrados.filter(u =>
              u.username?.toLowerCase().includes(this.terminoBusquedaUsuario.toLowerCase()) ||
              u.role_name?.toLowerCase().includes(this.terminoBusquedaUsuario.toLowerCase())
          );
      }
      const inicio = (this.paginaActualUsuarios - 1) * this.itemsPorPaginaUsuarios;
      return filtrados.slice(inicio, inicio + this.itemsPorPaginaUsuarios);
  }

  get totalPaginasUsuarios() {
      let filtrados = this.users || [];
      if (this.terminoBusquedaUsuario) {
          filtrados = filtrados.filter(u => u.username?.toLowerCase().includes(this.terminoBusquedaUsuario.toLowerCase()));
      }
      return Math.ceil(filtrados.length / this.itemsPorPaginaUsuarios) || 1;
  }

  cambiarPaginaUsuarios(delta: number) {
      const nuevaPagina = this.paginaActualUsuarios + delta;
      if (nuevaPagina >= 1 && nuevaPagina <= this.totalPaginasUsuarios) {
          this.paginaActualUsuarios = nuevaPagina;
      }
  }

  async exportarUsuariosExcel() {
      if (!this.users || this.users.length === 0) return;

      const workbook = new ExcelJS.Workbook();
      const worksheet = workbook.addWorksheet('Usuarios');

      worksheet.columns = [
          { header: 'Usuario', key: 'usuario', width: 30 },
          { header: 'Rol Asignado', key: 'rol', width: 25 }
      ];

      const headerRow = worksheet.getRow(1);
      headerRow.eachCell((cell) => {
          cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF006971' } };
          cell.font = { color: { argb: 'FFFFFFFF' }, bold: true };
      });

      this.users.forEach(u => {
          worksheet.addRow({ usuario: u.username, rol: u.role_name });
      });

      const buffer = await workbook.xlsx.writeBuffer();
      saveAs.saveAs(new Blob([buffer]), 'Directorio_Usuarios.xlsx');
  }

  abrirFiltrosUsuarios() {
    this.mostrarFiltrosUsuarios = !this.mostrarFiltrosUsuarios;
  }

  // New Role Modal State
  isRoleModalOpen = false;
  newRoleName = '';
  selectedNewRoleModules: Set<number> = new Set();
  modulosDisponibles: any[] = [];

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
      error: (err: any) => {
        console.error('Error al crear rol:', err);
        alert('Error al crear el rol.');
      }
    });
  }

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
        next: (data: any[]) => this.roles = data,
        error: (err: any) => console.error('Error fetching roles:', err)
      });
  }

  fetchUsers() {
    this.http.get<any[]>('/api/users')
      .subscribe({
        next: (data: any[]) => this.users = data,
        error: (err: any) => console.error('Error fetching users:', err)
      });
  }

  fetchModules() {
    this.http.get<any[]>('/api/security/modules')
      .subscribe({
        next: (res: any[]) => {
            this.modules = res;
            this.modulosDisponibles = res;
            console.log("Módulos cargados:", res);
        },
        error: (err: any) => console.error('Error trayendo módulos', err)
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
          error: (err: any) => {
            console.error('Error creating user:', err);
            alert('Error al crear usuario.');
          }
        });
    } else if (this.isEditing) {
      if (!this.userForm.username) {
        alert('Por favor ingrese el nombre de usuario');
        return;
      }
      this.http.put(`/api/users/${this.userForm.id}`, { id: this.userForm.id, username: this.userForm.username, password: this.userForm.password, rolId: parseInt(this.userForm.rolId) })
        .subscribe({
          next: () => {
            this.closeModal();
            this.fetchUsers();
          },
          error: (err: any) => {
            console.error('Error updating user:', err);
            alert(err.error?.message || 'Error al actualizar usuario.');
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
          error: (err: any) => {
            console.error('Error al eliminar:', err);
            alert('Hubo un error al eliminar el usuario.');
          }
        });
    }
  }

  // --- Roles & Permissions Management ---

  abrirModalConfiguracion(rol: any) {
    this.openPermissionsModal(rol);
  }

  editarNombreRol(rol: any) {
    const nuevoNombre = window.prompt("Ingrese el nuevo nombre para el rol:", rol.nombre_rol);
    if (nuevoNombre && nuevoNombre.trim() !== "" && nuevoNombre !== rol.nombre_rol) {
        this.http.put(`/api/security/roles/${rol.id}`, { nombre: nuevoNombre }).subscribe({
            next: () => {
                this.fetchRoles();
            },
            error: (err: any) => alert("Error al actualizar el nombre del rol")
        });
    }
  }

  eliminarRol(rol: any) {
    if (window.confirm(`¿Seguro que desea eliminar el rol ${rol.nombre_rol}?`)) {
        this.http.delete(`/api/security/roles/${rol.id}`).subscribe({
            next: () => {
                this.fetchRoles();
            },
            error: (err: any) => alert("Error al eliminar el rol")
        });
    }
  }

  openPermissionsModal(role: any) {
    this.selectedRole = role;
    this.isEditingPermissions = true;
    this.rolePermissions.clear();

    // Fetch existing permissions for this role
    this.http.get<any[]>(`/api/security/roles/${role.id}/modules`)
      .subscribe({
        next: (data: any[]) => {
          data.forEach(m => this.rolePermissions.add(m.id));
        },
        error: (err: any) => console.error('Error fetching role permissions:', err)
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

    const payload = {
      moduleIds: Array.from(this.rolePermissions)
    };

    this.http.put(`/api/security/roles/${this.selectedRole.id}/permissions`, payload).subscribe({
      next: () => {
        alert('Permisos actualizados. Tenga en cuenta que los usuarios deben volver a iniciar sesión para ver los cambios en su menú principal.');
        this.closePermissionsModal();
      },
      error: (err: any) => {
        console.error('Error updating permissions:', err);
        alert('Error al actualizar permisos.');
      }
    });
  }
}

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { NgxChartsModule } from '@swimlane/ngx-charts';

@Component({
  selector: 'app-process2',
  standalone: true,
  imports: [CommonModule, FormsModule, NgxChartsModule],
  templateUrl: './process2.component.html',
  styleUrls: ['./process2.component.css']
})
export class Process2Component implements OnInit {
  registros: any[] = [];
  registrosFiltrados: any[] = [];

  // Permisos
  puedeVer: boolean = false;
  puedeLeer: boolean = false;
  puedeEditar: boolean = false;

  // Filtros
  searchTerm: string = '';

  // Edición
  isEditing = false;
  editRecord: any = null;
  listaEstados = ['Approved', 'Failed', 'Pending'];

  // Modal de Detalle
  isDetailOpen = false;
  detailRecord: any = null;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.verificarPermisos();
    this.cargarDatos();
  }

  verificarPermisos() {
      // Mock de verificación de permisos por roles
      // En la vida real, se evaluaría un JWT o respuesta del backend
      const userRole = localStorage.getItem('role') || '';

      if (userRole.includes('Administrador')) {
          this.puedeVer = true;
          this.puedeLeer = true;
          this.puedeEditar = true;
      } else if (userRole.includes('Operador') || userRole.includes('Docente')) {
          this.puedeVer = true;
          this.puedeLeer = true;
          this.puedeEditar = false;
      } else {
          // Asumir nivel de vista básico
          this.puedeVer = true;
          this.puedeLeer = false;
          this.puedeEditar = false;
      }
  }

  cargarDatos() {
      this.http.get<any[]>('/api/process2').subscribe({
          next: (data) => {
              this.registros = data;
              this.registrosFiltrados = [...this.registros];
          },
          error: (err) => {
              console.error('Error cargando datos de Proceso 2:', err);
              // Fallback for development
              this.registrosFiltrados = [
                  { id: 1, cedula: '111111111', first_name: 'Juan', last_name: 'Pérez', status: 'Approved', percentage: 85, certification_name: 'Cert 1', created_at: new Date() },
                  { id: 2, cedula: '222222222', first_name: 'María', last_name: 'Gómez', status: 'Pending', percentage: 60, certification_name: 'Cert 2', created_at: new Date() }
              ];
          }
      });
  }

  filtrarRegistros() {
      if (!this.searchTerm) {
          this.registrosFiltrados = [...this.registros];
          return;
      }

      const term = this.searchTerm.toLowerCase();
      this.registrosFiltrados = this.registros.filter(r =>
          (r.first_name + ' ' + r.last_name).toLowerCase().includes(term) ||
          r.cedula?.toLowerCase().includes(term) ||
          r.certification_name?.toLowerCase().includes(term)
      );
  }

  // --- Acciones ---

  abrirDetalle(registro: any) {
      if (!this.puedeLeer && !this.puedeEditar) return;
      this.detailRecord = registro;
      this.isDetailOpen = true;
  }

  cerrarDetalle() {
      this.isDetailOpen = false;
      this.detailRecord = null;
  }

  abrirEdicion(registro: any) {
      if (!this.puedeEditar) return;
      this.editRecord = { ...registro };
      this.isEditing = true;
  }

  cerrarEdicion() {
      this.isEditing = false;
      this.editRecord = null;
  }

  guardarEdicion() {
      if (!this.editRecord) return;

      this.http.post('/api/process2/edit', this.editRecord).subscribe({
          next: () => {
              alert('Registro actualizado');
              this.cargarDatos();
              this.cerrarEdicion();
          },
          error: (err) => {
              console.error('Error:', err);
              alert('Error actualizando registro');
          }
      });
  }
}

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { timeout, catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';

@Component({
  selector: 'app-process2',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './process2.component.html',
  styleUrls: ['./process2.component.css']
})
export class Process2Component implements OnInit {

  registros: any[] = [];
  proyectosAgrupados: any[] = [];
  proyectosFiltrados: any[] = [];
  proyectosExpandidos: { [key: string]: boolean } = {};


  // Tabs
  activeTab: 'metas' | 'proyectos' = 'metas';

  // Proyectos Umbrella
  umbrellaProjects: any[] = [];
  umbrellaProjectsFiltrados: any[] = [];
  searchProjectTerm: string = '';

  // Permisos

  puedeVer: boolean = false;
  puedeLeer: boolean = false;
  puedeEditar: boolean = false; // Equivalente a Puede Cargar (Upload)

  // Filtros
  searchTerm: string = '';

  // Upload state
  isDragging = false;
  selectedFile: File | null = null;
  isUploading = false;

  // Modal de Resultado
  uploadResultModalOpen = false;
  isUploadSuccess = false;
  uploadMessage = '';

  // Modal de Detalle
  isDetailOpen = false;
  detailRecord: any = null;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.verificarPermisos();
    if (this.puedeVer) {
        this.cargarHistorial();
    }
  }

  verificarPermisos() {
      // Logic from local storage to define permissions.
      const userRole = localStorage.getItem('role') || '';

      if (userRole.includes('Administrador')) {
          this.puedeVer = true;
          this.puedeLeer = true;
          this.puedeEditar = true;
      } else if (userRole.includes('Operador') || userRole.includes('Docente')) {
          this.puedeVer = true;
          this.puedeLeer = true;
          this.puedeEditar = false; // Solo Administrador puede cargar
      } else {
          this.puedeVer = true;
          this.puedeLeer = false;
          this.puedeEditar = false;
      }
  }


  cargarHistorial() {
      this.http.get<any[]>('/api/plannermetas/history').subscribe({
          next: (data) => {
              this.registros = data;
              this.agruparProyectos();
          },
          error: (err) => {
              console.error('Error cargando historial de Metas:', err);
              this.registros = [];
              this.agruparProyectos();
          }
      });
  }

  agruparProyectos() {
      const grupos = this.registros.reduce((acc, curr) => {
          const key = curr.codigo_proyecto || curr.proyecto_umbrella || 'Sin Proyecto';
          if (!acc[key]) {
              acc[key] = {
                  codigo_proyecto: key,
                  nombre_proyecto: curr.nombre_proyecto || 'N/A',
                  area: curr.area || 'N/A',
                  fecha_carga: curr.fecha_carga, // Toma la primera
                  total_tareas: 0,
                  tareas: []
              };
          }
          acc[key].tareas.push(curr);
          acc[key].total_tareas++;

          // Mantener la fecha más reciente si hay varias
          if (curr.fecha_carga && (!acc[key].fecha_carga || curr.fecha_carga > acc[key].fecha_carga)) {
               acc[key].fecha_carga = curr.fecha_carga;
          }

          return acc;
      }, {});

      this.proyectosAgrupados = Object.values(grupos);
      this.proyectosFiltrados = [...this.proyectosAgrupados];
  }

  filtrarRegistros() {
      if (!this.searchTerm) {
          this.proyectosFiltrados = [...this.proyectosAgrupados];
          return;
      }

      const term = this.searchTerm.toLowerCase();
      this.proyectosFiltrados = this.proyectosAgrupados.filter(p =>
          (p.codigo_proyecto && p.codigo_proyecto.toLowerCase().includes(term)) ||
          (p.nombre_proyecto && p.nombre_proyecto.toLowerCase().includes(term)) ||
          (p.area && p.area.toLowerCase().includes(term)) ||
          // Buscar también dentro de las tareas del proyecto
          p.tareas.some((t: any) =>
              (t.responsable && t.responsable.toLowerCase().includes(term)) ||
              (t.id_planner_task && t.id_planner_task.toLowerCase().includes(term))
          )
      );
  }

  toggleProyecto(codigo: string) {
      this.proyectosExpandidos[codigo] = !this.proyectosExpandidos[codigo];
  }

  switchTab(tab: 'metas' | 'proyectos') {
      this.activeTab = tab;
      if (tab === 'proyectos' && this.umbrellaProjects.length === 0) {
          this.cargarProyectosUmbrella();
      }
  }

  cargarProyectosUmbrella() {
      this.http.get<any[]>('/api/plannermetas/projects').subscribe({
          next: (data) => {
              this.umbrellaProjects = data;
              this.umbrellaProjectsFiltrados = [...this.umbrellaProjects];
          },
          error: (err) => {
              console.error('Error cargando proyectos umbrella:', err);
              this.umbrellaProjects = [];
              this.umbrellaProjectsFiltrados = [];
          }
      });
  }

  filtrarProyectosUmbrella() {
      if (!this.searchProjectTerm) {
          this.umbrellaProjectsFiltrados = [...this.umbrellaProjects];
          return;
      }
      const term = this.searchProjectTerm.toLowerCase();
      this.umbrellaProjectsFiltrados = this.umbrellaProjects.filter(p =>
          (p.codigo_proyecto && p.codigo_proyecto.toLowerCase().includes(term)) ||
          (p.nombre_proyecto && p.nombre_proyecto.toLowerCase().includes(term)) ||
          (p.area && p.area.toLowerCase().includes(term))
      );
  }

  actualizarEstadoProyecto(codigo: string, nuevoEstado: string) {
      if (!this.puedeEditar) return;

      this.http.put(`/api/plannermetas/projects/${encodeURIComponent(codigo)}/status`, { estado: nuevoEstado }).subscribe({
          next: () => {
              // Update local state
              const p = this.umbrellaProjects.find(x => x.codigo_proyecto === codigo);
              if (p) p.estado_global = nuevoEstado;
              this.showResultModal(true, `El estado del proyecto ${codigo} se actualizó a ${nuevoEstado}.`);
          },
          error: (err) => {
              console.error('Error actualizando estado:', err);
              this.showResultModal(false, 'No se pudo actualizar el estado del proyecto.');
              // Refresh to revert UI state
              this.cargarProyectosUmbrella();
          }
      });
  }

  // --- Upload Logic ---


  onDragOver(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;

    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      const file = event.dataTransfer.files[0];
      if (this.isValidFileType(file)) {
        this.selectedFile = file;
      } else {
        this.showResultModal(false, 'Formato inválido. Solo se permite un archivo .xlsx consolidado.');
      }
    }
  }

  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      if (this.isValidFileType(file)) {
        this.selectedFile = file;
      } else {
        this.showResultModal(false, 'Formato inválido. Solo se permite un archivo .xlsx consolidado.');
        event.target.value = ''; // Reset input
      }
    }
  }

  isValidFileType(file: File): boolean {
    return file.name.toLowerCase().endsWith('.xlsx') || file.type === 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';
  }

  clearFile(event: Event) {
    event.preventDefault();
    this.selectedFile = null;
    const input = document.getElementById('file-upload') as HTMLInputElement;
    if (input) input.value = '';
  }


  uploadFile() {
    if (!this.selectedFile) return;

    this.isUploading = true;
    const formData = new FormData();
    formData.append('file', this.selectedFile);

    this.http.post<any>('/api/plannermetas/upload', formData).pipe(
        timeout(30000),
        catchError(err => {
            return throwError(() => err);
        })
    ).subscribe({
      next: (res) => {
        const jobId = res.jobId;
        if (jobId) {
            // Show immediate feedback to user that the file was accepted
            this.showResultModal(true, res.message || 'El archivo se está procesando en segundo plano.');
            this.pollJobStatus(jobId, "Procesamiento finalizado con éxito.");
        } else {
            this.isUploading = false;
            this.showResultModal(true, 'El archivo se está procesando en segundo plano. Puede continuar usando el sistema.');
            this.selectedFile = null;
        }
      },
      error: (err) => {
        this.isUploading = false;
        console.error('Error uploading file:', err);
        let errorMsg = 'Ocurrió un error inesperado al procesar el archivo.';
        if (err.name === 'TimeoutError') {
             errorMsg = 'La petición tomó demasiado tiempo. Intente de nuevo más tarde.';
        } else if (err.status === 409 || err.status === 400) {
             errorMsg = err.error?.message || 'El archivo ya fue procesado o es inválido.';
        } else if (err.error?.message) {
             errorMsg = err.error.message;
        }
        this.showResultModal(false, errorMsg);
      }
    });
  }

  pollJobStatus(jobId: string, successMessage: string = "Carga completada") {
    const intervalId = setInterval(() => {
        this.http.get<any>(`/api/plannermetas/status/${jobId}`).subscribe({
            next: (res) => {
                if (res.status === 'Completado' || res.status === 'Error') {
                    clearInterval(intervalId);
                    this.isUploading = false;
                    this.selectedFile = null;
                    if (res.status === 'Completado') {
                        this.showResultModal(true, successMessage);
                        this.cargarHistorial();
                    } else {
                        this.showResultModal(false, 'Ocurrió un error al procesar el archivo en segundo plano.');
                    }
                }
            },
            error: () => {
                clearInterval(intervalId);
                this.isUploading = false;
            }
        });
    }, 2000);
  }
  showResultModal(success: boolean, message: string) {
      this.isUploadSuccess = success;
      this.uploadMessage = message;
      this.uploadResultModalOpen = true;
  }

  closeResultModal() {
      this.uploadResultModalOpen = false;
  }

  // --- Acciones de Vista ---

  abrirDetalle(registro: any) {
      if (!this.puedeLeer) return;
      this.detailRecord = registro;
      this.isDetailOpen = true;
  }

  cerrarDetalle() {
      this.isDetailOpen = false;
      this.detailRecord = null;
  }
}

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-process2',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './process2.component.html',
  styleUrls: ['./process2.component.css']
})
export class Process2Component implements OnInit {
  registros: any[] = [];
  registrosFiltrados: any[] = [];

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
              this.registrosFiltrados = [...this.registros];
          },
          error: (err) => {
              console.error('Error cargando historial de Metas:', err);
              // Fallback dev data si no existe SP o tabla
              this.registros = [];
              this.registrosFiltrados = [];
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
          (r.area && r.area.toLowerCase().includes(term)) ||
          (r.responsable && r.responsable.toLowerCase().includes(term)) ||
          (r.codigo_proyecto && r.codigo_proyecto.toLowerCase().includes(term)) ||
          (r.nombre_proyecto && r.nombre_proyecto.toLowerCase().includes(term))
      );
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

    this.http.post<any>('/api/plannermetas/upload', formData).subscribe({
      next: (res) => {
        this.isUploading = false;
        this.showResultModal(true, res.message || 'El archivo se procesó correctamente.');
        this.selectedFile = null;
        this.cargarHistorial(); // Refrescar tabla
      },
      error: (err) => {
        this.isUploading = false;
        console.error('Error uploading file:', err);
        const errorMsg = err.error?.message || err.error?.detail || 'Ocurrió un error inesperado al procesar el archivo.';
        this.showResultModal(false, errorMsg);
      }
    });
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

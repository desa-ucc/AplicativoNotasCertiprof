import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './history.component.html',
  styleUrls: ['./history.component.css']
})
export class HistoryComponent implements OnInit {
  histories: any[] = [];
  registrosFiltrados: any[] = [];
  listaCertificaciones: string[] = [];
  certSeleccionada: string = '';
  textoBusqueda: string = '';

  isEditing = false;
  editRecord: any = null;
  userRole: string | null = null;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.userRole = localStorage.getItem('role');
    this.fetchHistory();
  }

  fetchHistory() {
    this.http.get<any[]>('/api/certiprof/history')
      .subscribe({
        next: (data) => {
          this.histories = data;
          this.cargarOpcionesFiltro();
          this.aplicarFiltros();
        },
        error: (err) => {
          console.error('Error fetching history:', err);
        }
      });
  }

  cargarOpcionesFiltro() {
    const todosLosNombres = this.histories.map(r => r.certification_name);
    // Obtener valores únicos, filtrar nulos y ordenar alfabéticamente
    this.listaCertificaciones = [...new Set(todosLosNombres)].filter(c => c).sort() as string[];
  }

  aplicarFiltros() {
    this.registrosFiltrados = this.histories.filter(r => {
        const cumpleCert = this.certSeleccionada ? r.certification_name === this.certSeleccionada : true;

        let cumpleTexto = true;
        if (this.textoBusqueda) {
            const term = this.textoBusqueda.toLowerCase();
            cumpleTexto = (r.email?.toLowerCase().includes(term)) || (r.cedula?.includes(term));
        }
        return cumpleCert && cumpleTexto;
    });
  }

  openEditModal(record: any) {
    // Clone the record so we don't modify the table row until save
    this.editRecord = { ...record };
    this.isEditing = true;
  }

  closeEditModal() {
    this.isEditing = false;
    this.editRecord = null;
  }

  saveRecord() {
    if (!this.editRecord || !this.editRecord.id) {
      alert('Error: ID de registro no válido.');
      return;
    }

    // Validate no critical fields are fully null or completely empty (though COALESCE handles nulls on backend, we want to prevent sending explicitly blank strings if they were filled before, but we send what's in the form).
    if (this.editRecord.first_name === '' || this.editRecord.last_name === '' || this.editRecord.certification_name === '') {
       alert('Por favor complete todos los campos de texto requeridos.');
       return;
    }

    this.http.post('/api/certiprof/edit', this.editRecord)
      .subscribe({
        next: () => {
          this.closeEditModal();
          this.fetchHistory(); // Refresh the table automatically
        },
        error: (err) => {
          console.error('Error al editar:', err);
          alert('Hubo un error al actualizar el registro.');
        }
      });
  }


  deleteRecord(id: number) {
    if (confirm('¿Está seguro de que desea eliminar este registro?')) {
      this.http.delete(`/api/certiprof/${id}`)
        .subscribe({
          next: () => {
            this.fetchHistory();
          },
          error: (err) => {
            console.error('Error al eliminar:', err);
            alert('Hubo un error al eliminar el registro.');
          }
        });
    }
  }

  downloadActa(uploadId: number, courseCode: string) {
    this.http.get(`/api/certiprof/export-avatar/${uploadId}`, { responseType: 'blob' })
      .subscribe({
        next: (response: Blob) => {
          const url = window.URL.createObjectURL(response);
          const a = document.createElement('a');
          a.href = url;
          a.download = `Acta_Auxiliar_${courseCode}.xlsx`;
          a.click();
          window.URL.revokeObjectURL(url);
        },
        error: (err) => {
          console.error('Error downloading act:', err);
          alert('Error al descargar el acta.');
        }
      });
  }
}

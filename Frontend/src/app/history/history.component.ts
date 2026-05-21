import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import * as XLSX from 'xlsx';

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './history.component.html',
  styleUrls: ['./history.component.css']
})
export class HistoryComponent implements OnInit {



  searchTerm = '';
  isFiltersModalOpen = false;
  filtroAvanzado = {
    cedula: '',
    fechaInicio: '',
    fechaFin: '',
    certificacion: ''
  };

  histories: any[] = [];
  registrosFiltrados: any[] = [];

  abrirFiltros() {
    this.isFiltersModalOpen = true;
  }

  cerrarFiltros() {
    this.isFiltersModalOpen = false;
  }

  aplicarFiltrosAvanzados() {
    this.registrosFiltrados = this.histories.filter(record => {
      let pass = true;

      // Exact match by Cedula
      if (this.filtroAvanzado.cedula && record.cedula !== this.filtroAvanzado.cedula) {
         pass = false;
      }

      // Date Range (Fecha Inicio - Fecha Fin)
      if (this.filtroAvanzado.fechaInicio && record.created_at) {
         const recordDate = new Date(record.created_at).getTime();
         const startDate = new Date(this.filtroAvanzado.fechaInicio).getTime();
         if (recordDate < startDate) pass = false;
      }
      if (this.filtroAvanzado.fechaFin && record.created_at) {
         const recordDate = new Date(record.created_at).getTime();
         // Make endDate include the whole day
         const endDate = new Date(this.filtroAvanzado.fechaFin).getTime() + 86400000;
         if (recordDate >= endDate) pass = false;
      }

      // Certification match
      if (this.filtroAvanzado.certificacion && record.certification_name) {
         if (!record.certification_name.toLowerCase().includes(this.filtroAvanzado.certificacion.toLowerCase())) {
            pass = false;
         }
      }

      return pass;
    });

    this.cerrarFiltros();
  }

  aplicarFiltros() {
    if (!this.searchTerm.trim()) {
      this.registrosFiltrados = [...this.histories];
      return;
    }

    const searchLower = this.searchTerm.toLowerCase();
    this.registrosFiltrados = this.histories.filter(record =>
      (record.cedula && record.cedula.toLowerCase().includes(searchLower)) ||
      (record.email && record.email.toLowerCase().includes(searchLower)) ||
      (record.first_name && record.first_name.toLowerCase().includes(searchLower)) ||
      (record.last_name && record.last_name.toLowerCase().includes(searchLower))
    );
  }

  exportarExcel() {
    if (this.registrosFiltrados.length === 0) {
      alert("No hay datos para exportar.");
      return;
    }
    const ws: XLSX.WorkSheet = XLSX.utils.json_to_sheet(this.registrosFiltrados);
    const wb: XLSX.WorkBook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Historial');
    XLSX.writeFile(wb, 'Historial_Certificaciones.xlsx');
  }

  fetchHistory() {
    this.http.get<any[]>('/api/certiprof/history')
      .subscribe({
        next: (data) => {
          this.histories = data;
          this.registrosFiltrados = [...data];
        },
        error: (err) => {
          console.error('Error fetching history:', err);
        }
      });
  }

  isEditing = false;
  editRecord: any = null;
  userRole: string | null = null;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.userRole = localStorage.getItem('role');
    this.fetchHistory();
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

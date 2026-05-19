import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [CommonModule, FormsModule],
  providers: [DatePipe],
  templateUrl: './history.component.html',
  styleUrls: ['./history.component.css']
})
export class HistoryComponent implements OnInit {
  histories: any[] = [];

  // Filtering properties
  filterStartDate: string = '';
  filterEndDate: string = '';
  filterUserName: string = '';
  filterCedula: string = '';
  filterCertificationName: string = '';

  isEditing = false;
  editRecord: any = null;
  userRole: string | null = null;

  constructor(private http: HttpClient, private datePipe: DatePipe) {}

  ngOnInit(): void {
    this.userRole = localStorage.getItem('role');
    this.fetchHistory();
  }

  fetchHistory() {
    this.http.get<any[]>('/api/certiprof/history')
      .subscribe({
        next: (data) => {
          this.histories = data;
        },
        error: (err) => {
          console.error('Error fetching history:', err);
        }
      });
  }

  get uniqueCertifications(): string[] {
    const certs = this.histories.map(h => h.certification_name).filter(Boolean);
    return [...new Set(certs)].sort();
  }

  get filteredHistories(): any[] {
    return this.histories.filter(record => {
      // Date Filter
      if (this.filterStartDate || this.filterEndDate) {
        const recordDate = new Date(record.created_at);
        recordDate.setHours(0, 0, 0, 0); // normalize for comparison

        if (this.filterStartDate) {
          const startDate = new Date(this.filterStartDate);
          startDate.setHours(0, 0, 0, 0);
          if (recordDate < startDate) return false;
        }
        if (this.filterEndDate) {
          const endDate = new Date(this.filterEndDate);
          endDate.setHours(23, 59, 59, 999);
          if (recordDate > endDate) return false;
        }
      }

      // Username Filter
      if (this.filterUserName) {
        const fullName = `${record.first_name} ${record.last_name}`.toLowerCase();
        if (!fullName.includes(this.filterUserName.toLowerCase())) {
          return false;
        }
      }

      // Cedula Filter
      if (this.filterCedula) {
        const cedulaStr = record.cedula ? String(record.cedula).toLowerCase() : '';
        if (!cedulaStr.includes(this.filterCedula.toLowerCase())) {
          return false;
        }
      }

      // Certification Name Filter
      if (this.filterCertificationName) {
        if (record.certification_name !== this.filterCertificationName) {
          return false;
        }
      }

      return true;
    });
  }

  clearFilters() {
      this.filterStartDate = '';
      this.filterEndDate = '';
      this.filterUserName = '';
      this.filterCedula = '';
      this.filterCertificationName = '';
  }

  downloadCSV() {
    const dataToExport = this.filteredHistories;
    if (!dataToExport || dataToExport.length === 0) {
      alert('No hay datos para descargar.');
      return;
    }

    const headers = ['Cédula', 'Email', 'Nombres', 'Apellidos', 'Certificación', 'Porcentaje', 'Estado', 'Fecha'];

    const rows = dataToExport.map(row => {
      const cedula = row.cedula && row.cedula !== 'No está dentro del registro' ? row.cedula : 'No Registrado';
      const email = row.email || '';
      const firstName = row.first_name || '';
      const lastName = row.last_name || '';
      const certName = row.certification_name || '';
      const percentage = row.percentage || 0;
      const status = row.status || 'Reprobado';
      const date = this.datePipe.transform(row.created_at, 'dd/MM/yyyy HH:mm') || '';

      // Escape quotes and wrap in quotes for CSV
      return [
        `"${cedula}"`,
        `"${email}"`,
        `"${firstName}"`,
        `"${lastName}"`,
        `"${certName}"`,
        `"${percentage}"`,
        `"${status}"`,
        `"${date}"`
      ].join(',');
    });

    const csvContent = "data:text/csv;charset=utf-8,\uFEFF" + [headers.join(','), ...rows].join('\n');
    const encodedUri = encodeURI(csvContent);
    const link = document.createElement("a");
    link.setAttribute("href", encodedUri);
    link.setAttribute("download", `historial_certificaciones_${this.datePipe.transform(new Date(), 'yyyyMMdd_HHmm')}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
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

    // Validate no critical fields are fully null or completely empty
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
}

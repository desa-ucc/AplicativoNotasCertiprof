import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import * as ExcelJS from 'exceljs';
import * as saveAs from 'file-saver';

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


  histories: any[] = [];
  registrosFiltrados: any[] = [];
  fechaInicio: string = '';
  fechaFin: string = '';

  listaCertificaciones: string[] = [];
  listaEstados: string[] = [];

  abrirFiltros() {
    this.isFiltersModalOpen = true;
  }

  cerrarFiltros() {
    this.isFiltersModalOpen = false;
  }


  filtroAvanzado = {
    cedula: '',
    fechaInicio: '',
    fechaFin: '',
    certificacion: '',
    estado: ''
  };

  limpiarFiltros(event?: Event) {
    if (event) event.preventDefault();
    this.filtroAvanzado = {
      cedula: '',
      fechaInicio: '',
      fechaFin: '',
      certificacion: '',
      estado: ''
    };
    this.aplicarFiltrosAvanzados();
  }

  cargarOpcionesFiltro() {
    // Certificaciones únicas
    const certs = this.histories.map(r => r.certification_name || r.certificacion);
    this.listaCertificaciones = [...new Set(certs)].filter(c => c).sort();

    // Estados únicos
    const estados = this.histories.map(r => r.status);
    this.listaEstados = [...new Set(estados)].filter(e => e).sort();
  }

  aplicarFiltrosAvanzados(event?: Event) {
    if (event) event.preventDefault();
    this.registrosFiltrados = this.histories.filter(record => {
      // 1. Filtro Cédula (Exacto)
      let coincideCedula = true;
      if (this.filtroAvanzado.cedula && this.filtroAvanzado.cedula.trim() !== '') {
          coincideCedula = record.cedula === this.filtroAvanzado.cedula.trim();
      }

      // 2. Filtro Certificación (Coincidencia Parcial Segura)
      let coincideCert = true;
      if (this.filtroAvanzado.certificacion && this.filtroAvanzado.certificacion.trim() !== '') {
          const certFiltro = this.filtroAvanzado.certificacion.toLowerCase().trim();
          const certRecord = (record.certification_name || record.certificacion || '').toLowerCase();
          coincideCert = certRecord.includes(certFiltro);
      }

      // 3. Filtro Estado (Exacto pero case-insensitive)
      let coincideEstado = true;
      if (this.filtroAvanzado.estado && this.filtroAvanzado.estado !== '') {
          const estadoRecord = (record.status || '').toLowerCase().trim();
          const estadoFiltro = this.filtroAvanzado.estado.toLowerCase().trim();
          coincideEstado = estadoRecord === estadoFiltro;
      }

      // 4. Filtro Fechas
      let coincideFechas = true;
      const rawDate = record.created_at || record.fecha;
      if (!rawDate && (this.filtroAvanzado.fechaInicio || this.filtroAvanzado.fechaFin)) {
        coincideFechas = false;
      } else if (rawDate) {
        const fechaNormalizada = rawDate.toString().replace('T', ' ').split(' ')[0];
        if (this.filtroAvanzado.fechaInicio && this.filtroAvanzado.fechaFin) {
          coincideFechas = fechaNormalizada >= this.filtroAvanzado.fechaInicio && fechaNormalizada <= this.filtroAvanzado.fechaFin;
        } else if (this.filtroAvanzado.fechaInicio) {
          coincideFechas = fechaNormalizada >= this.filtroAvanzado.fechaInicio;
        } else if (this.filtroAvanzado.fechaFin) {
          coincideFechas = fechaNormalizada <= this.filtroAvanzado.fechaFin;
        }
      }

      // 5. Búsqueda por texto general
      let coincideTexto = true;
      if (this.searchTerm) {
          const term = this.searchTerm.toLowerCase();
          coincideTexto = (record.email?.toLowerCase().includes(term)) || (record.cedula?.includes(term));
      }

      return coincideCedula && coincideCert && coincideEstado && coincideFechas && coincideTexto;
    });

    this.calcularMetricas(this.registrosFiltrados);
    this.cerrarFiltros();
  }

  aplicarFiltros() {
    this.aplicarFiltrosAvanzados();
  }

  async exportarExcel() {
    if (this.registrosFiltrados.length === 0) {
      alert("No hay datos para exportar.");
      return;
    }

    const workbook = new ExcelJS.Workbook();
    const worksheet = workbook.addWorksheet('Historial de Cargas');

    worksheet.columns = [
        { header: 'CÉDULA', key: 'cedula', width: 15 },
        { header: 'USUARIO', key: 'estudiante', width: 35 },
        { header: 'CERTIFICACIÓN', key: 'certificacion', width: 45 },
        { header: 'NOTA', key: 'nota', width: 10 },
        { header: 'ESTADO', key: 'estado', width: 15 },
        { header: 'FECHA', key: 'fecha', width: 15 },
        { header: 'EMAIL', key: 'email', width: 35 }
    ];

    // Estilo del Encabezado: Verde Institucional (#76BC21)
    const headerRow = worksheet.getRow(1);
    headerRow.eachCell((cell) => {
        cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF76BC21' } };
        cell.font = { color: { argb: 'FFFFFFFF' }, bold: true, size: 11 };
        cell.alignment = { vertical: 'middle', horizontal: 'center' };
        cell.border = {
            top: { style: 'thin' }, left: { style: 'thin' },
            bottom: { style: 'thin' }, right: { style: 'thin' }
        };
    });
    headerRow.height = 25;

    // Inyectar Datos y dar formato de celda
    this.registrosFiltrados.forEach(r => {
        const row = worksheet.addRow({
            cedula: r.cedula,
            estudiante: `${r.first_name} ${r.last_name}`,
            certificacion: r.certification_name || r.certificacion,
            nota: r.percentage || r.nota,
            estado: r.status,
            fecha: r.created_at ? new Date(r.created_at).toLocaleDateString() : '',
            email: r.email
        });

        // Bordes para todas las celdas
        row.eachCell((cell) => {
            cell.border = {
                top: { style: 'thin', color: { argb: 'FFDDDDDD'} },
                left: { style: 'thin', color: { argb: 'FFDDDDDD'} },
                bottom: { style: 'thin', color: { argb: 'FFDDDDDD'} },
                right: { style: 'thin', color: { argb: 'FFDDDDDD'} }
            };
        });
        row.getCell('nota').alignment = { horizontal: 'center' };
        row.getCell('estado').alignment = { horizontal: 'center' };
    });

    const buffer = await workbook.xlsx.writeBuffer();
    saveAs.saveAs(new Blob([buffer]), 'Reporte_Historial.xlsx');
  }


  usuariosRegistradosCount: number = 0;
  tasaAprobacion: string = '0';
  totalCertificaciones: number = 0;


  calcularMetricas(data: any[]) {
    this.totalCertificaciones = data.length;

    // 1. Calcular usuarios únicos según las cédulas o correos en la tabla
    this.usuariosRegistradosCount = new Set(data.map(r => r.cedula || r.email)).size;

    // 2. Calcular la tasa de aprobación real
    const aprobados = data.filter(r => r.status?.toLowerCase() === 'approved' || r.status?.toLowerCase() === 'aprobado' || r.percentage >= 70).length;
    this.tasaAprobacion = data.length > 0 ? ((aprobados / data.length) * 100).toFixed(1) : '0';
  }

  estadosDisponibles: string[] = [];

  fetchHistory() {
    this.http.get<any[]>('/api/certiprof/history')
      .subscribe({
        next: (data) => {
          this.histories = data;
          this.cargarOpcionesFiltro();
          this.aplicarFiltros();

          const estadosExtraidos = data
                .map(r => r.status)
                .filter(status => status !== null && status !== undefined && status.toString().trim() !== '');
          this.estadosDisponibles = [...new Set(estadosExtraidos)];

          this.calcularMetricas(this.registrosFiltrados);
        },
        error: (err) => {
          console.error("Error al obtener historial:", err);
          this.histories = [];
          this.registrosFiltrados = [];
          if(err.status === 403) alert("No tienes permisos en el backend para ver esta data.");
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
          this.fetchHistory();
          alert("Registro actualizado con éxito");
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

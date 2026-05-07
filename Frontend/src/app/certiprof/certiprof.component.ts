import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import * as Papa from 'papaparse';
import { NgxChartsModule, Color, ScaleType, LegendPosition } from '@swimlane/ngx-charts';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-certiprof',
  standalone: true,
  imports: [CommonModule, NgxChartsModule, FormsModule],
  templateUrl: './certiprof.component.html',
  styleUrls: ['./certiprof.component.css']
})
export class CertiprofComponent {
  selectedFile: File | null = null;
  previewData: any[] = [];
  isDragging = false;
  isProcessing = false;

  courseCode: string = '';
  certificationName: string = '';
  uploadId: number | null = null;

  // Charts config
  chartData: any[] = [];
  view: [number, number] = [700, 400];
  gradient: boolean = true;
  showLegend: boolean = true;
  legendPosition: LegendPosition = LegendPosition.Below;
  showLabels: boolean = true;
  isDoughnut: boolean = false;
  colorScheme: Color = {
    name: 'custom',
    selectable: true,
    group: ScaleType.Ordinal,
    domain: ['#10B981', '#EF4444'] // Green for pass, Red for fail
  };

  constructor(private http: HttpClient) {}

  onDragOver(event: DragEvent) {
    event.preventDefault();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    this.isDragging = false;
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    this.isDragging = false;

    if (event.dataTransfer?.files.length) {
      this.handleFile(event.dataTransfer.files[0]);
    }
  }

  onFileSelected(event: any) {
    if (event.target.files.length) {
      this.handleFile(event.target.files[0]);
    }
  }

  handleFile(file: File) {
    this.selectedFile = file;
    this.previewData = [];
    this.chartData = [];

    // Parse for preview via backend
    const formData = new FormData();
    formData.append('file', file);

    this.http.post<any[]>('http://localhost:5000/api/certiprof/parse-excel', formData)
      .subscribe({
        next: (results) => {
          this.previewData = results;
          this.generateChartData(this.previewData);
        },
        error: (err) => {
          console.error('Error parsing file via backend:', err);
          alert('Error al leer el archivo. Asegúrese de que el formato sea correcto.');
        }
      });
  }

  generateChartData(data: any[]) {
    let passCount = 0;
    let failCount = 0;

    data.forEach(row => {
      // Adjusted based on actual return from parse-excel
      const gradeStr = row.percentage || '0';
      const grade = parseFloat(gradeStr);
      if (!isNaN(grade) && grade >= 60) {
        passCount++;
      } else {
        failCount++;
      }
    });

    this.chartData = [
      { name: 'Aprobados', value: passCount },
      { name: 'Reprobados', value: failCount }
    ];
  }

  parseGrade(rawGrade: any): string {
    const gradeStr = String(rawGrade || '0').trim();
    const grade = parseFloat(gradeStr);
    return isNaN(grade) ? '0' : grade.toString();
  }

  processFile() {
    // Disabled while previewData has no elements or misses Cedula? Wait, Cedula might be added during processing or parsing.
    // The instruction says: "El botón de "Guardar" debe permanecer deshabilitado hasta que el flujo de validación obtenga la Cédula desde la base de datos institucional."
    // So the previewData must have elements, and at least one element should have Cedula if validation was successful. Or just checking if previewData is loaded.
    const hasCedula = this.previewData.length > 0 && this.previewData.some(row => !!row.cedula);
    if (!this.selectedFile || !this.courseCode || !this.certificationName || !hasCedula) return;

    this.isProcessing = true;
    const formData = new FormData();
    formData.append('file', this.selectedFile);
    formData.append('courseCode', this.courseCode);
    formData.append('certificationName', this.certificationName);

    // Call backend API
    this.http.post<any>('http://localhost:5000/api/certiprof/process-report', formData)
      .subscribe({
        next: (response) => {
          this.isProcessing = false;
          this.uploadId = response.uploadId;
          alert('Archivo procesado con éxito. Ahora puede generar el acta.');
        },
        error: (err) => {
          this.isProcessing = false;
          console.error('Error processing file:', err);
          alert('Error processing file. See console for details.');
        }
      });
  }

  generateAvatarAct() {
    if (!this.uploadId) return;

    this.http.get(`http://localhost:5000/api/certiprof/export-avatar/${this.uploadId}`, { responseType: 'blob' })
      .subscribe({
        next: (response: Blob) => {
          // Trigger download
          const url = window.URL.createObjectURL(response);
          const a = document.createElement('a');
          a.href = url;
          a.download = `Acta_Auxiliar_${this.courseCode}.xlsx`;
          a.click();
          window.URL.revokeObjectURL(url);

          // Reset state after successful flow
          this.selectedFile = null;
          this.previewData = [];
          this.uploadId = null;
          this.courseCode = '';
          this.certificationName = '';
        },
        error: (err) => {
          console.error('Error generating act:', err);
          alert('Error al generar el acta.');
        }
      });
  }
}

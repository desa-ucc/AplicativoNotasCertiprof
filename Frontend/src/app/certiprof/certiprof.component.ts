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

  isValidated = false;
  isValidating = false;
  emailToCedulaMap: { [key: string]: string } = {};

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
    this.isValidated = false;
    this.emailToCedulaMap = {};
    this.previewData = [];

    const formData = new FormData();
    formData.append('file', file);

    this.http.post<any[]>('http://localhost:5000/api/certiprof/parse-excel', formData)
      .subscribe({
        next: (data) => {
          this.previewData = data;
          this.generateChartData(this.previewData);
          this.validateEmails(this.previewData);
        },
        error: (err) => {
          console.error('Error parsing excel:', err);
          alert('Error al leer el archivo Excel.');
        }
      });
  }

  validateEmails(data: any[]) {
    const emails = data.map(row => row.email || row.Email).filter(e => !!e);
    if (emails.length === 0) {
      alert('No se encontraron correos en el archivo.');
      return;
    }

    this.isValidating = true;
    this.http.post<{ [key: string]: string }>('http://localhost:5000/api/certiprof/validate-emails', emails)
      .subscribe({
        next: (res) => {
          this.isValidating = false;
          this.emailToCedulaMap = res;

          // Check if all extracted emails have a matched cedula
          const allFound = emails.every(email => !!this.emailToCedulaMap[email]);

          if (allFound) {
            this.isValidated = true;
          } else {
            this.isValidated = false;
            alert('Algunos correos no se encontraron en la base de datos institucional. Por favor, verifique.');
          }
        },
        error: (err) => {
          this.isValidating = false;
          this.isValidated = false;
          console.error('Error validating emails:', err);
          alert('Error al validar los correos con la base de datos.');
        }
      });
  }

  generateChartData(data: any[]) {
    let passCount = 0;
    let failCount = 0;

    data.forEach(row => {
      // Assuming a grade logic, adjust as per real Certiprof data structure
      const gradeStr = row.percentage || row.Percentage || row.notas || row.Notas || row.Grade || '0';
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
    if (!this.selectedFile || !this.courseCode || !this.certificationName) return;

    this.isProcessing = true;
    const formData = new FormData();
    formData.append('file', this.selectedFile);
    formData.append('courseCode', this.courseCode);
    formData.append('certificationName', this.certificationName);

    formData.append('emailMapJson', JSON.stringify(this.emailToCedulaMap));

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

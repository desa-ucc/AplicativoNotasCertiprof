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

    // Parse for preview
    Papa.parse(file, {
      header: true,
      skipEmptyLines: true,
      complete: (results) => {
        this.previewData = results.data;
        this.generateChartData(this.previewData);
      }
    });
  }

  generateChartData(data: any[]) {
    let passCount = 0;
    let failCount = 0;

    data.forEach(row => {
      // Assuming a grade logic, adjust as per real Certiprof data structure
      const gradeStr = row.notas || row.Notas || row.Grade || '0';
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

  processFile() {
    if (!this.selectedFile || !this.courseCode || !this.certificationName) return;

    this.isProcessing = true;
    const formData = new FormData();
    formData.append('file', this.selectedFile);
    formData.append('courseCode', this.courseCode);
    formData.append('certificationName', this.certificationName);

    // Call backend API
    // In a real application, the token should be dynamically acquired
    // Since this is a demo, we will generate a valid token on the backend to use or rely on interceptors.
    // For now we will pass a placeholder token to hit the endpoint.
    const token = 'placeholder_token_for_demo';
    const headers = new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });

    this.http.post('http://localhost:5000/api/certiprof/process-report', formData, { headers, responseType: 'blob' })
      .subscribe({
        next: (response: Blob) => {
          this.isProcessing = false;
          // Trigger download
          const url = window.URL.createObjectURL(response);
          const a = document.createElement('a');
          a.href = url;
          a.download = `Acta_Auxiliar_CE0501.xlsx`;
          a.click();
          window.URL.revokeObjectURL(url);
          this.selectedFile = null;
          this.previewData = [];
        },
        error: (err) => {
          this.isProcessing = false;
          console.error('Error processing file:', err);
          alert('Error processing file. See console for details.');
        }
      });
  }
}

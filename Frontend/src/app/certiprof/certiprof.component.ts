import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import * as Papa from 'papaparse';
import { NgxChartsModule, Color, ScaleType, LegendPosition } from '@swimlane/ngx-charts';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

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
  mostrarModalResultados: boolean = false;
  esErrorFatal: boolean = false;
  mensajeErrorFatal: string = '';
  resultadosCarga = { exitosos: 0, fallidos: 0, total: 0 };

  constructor(private http: HttpClient, private router: Router) {}

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

  successMessage: string = '';

  handleFile(file: File) {
    this.selectedFile = file;
    this.successMessage = '';
  }

  processFile() {
    if (!this.selectedFile) return;

    this.isProcessing = true;
    this.successMessage = '';
    const formData = new FormData();
    formData.append('file', this.selectedFile);

    // Call backend API directly to process and save
    this.http.post<any>('/api/certiprof/process-report', formData)
      .subscribe({
        next: (res: any) => {
          this.isProcessing = false;
          this.resultadosCarga = {
              exitosos: res.exitosos || res.successCount || 0,
              fallidos: res.fallidos || res.errorCount || 0,
              total: (res.exitosos || res.successCount || 0) + (res.fallidos || res.errorCount || 0)
          };
          this.esErrorFatal = false;
          this.mostrarModalResultados = true;
          this.selectedFile = null;
        },
        error: (err: any) => {
          this.isProcessing = false;
          this.esErrorFatal = true;
          this.mensajeErrorFatal = err.error?.mensaje || err.error?.message || err.error?.Message || 'Error de conexión o formato no soportado.';
          this.mostrarModalResultados = true;
          this.selectedFile = null;
        }
      });
  }

  cerrarModalResultados() {
    this.mostrarModalResultados = false;
    if (!this.esErrorFatal) {
      this.router.navigate(['/history']);
    }
  }
}

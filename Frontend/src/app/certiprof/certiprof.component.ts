import { Component, OnInit } from '@angular/core';
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
export class CertiprofComponent implements OnInit {
  selectedFile: File | null = null;
  previewData: any[] = [];
  isDragging = false;
  isProcessing = false;

  constructor(private http: HttpClient, private router: Router) {}

  ngOnInit() {
    this.obtenerUltimaActualizacion();
  }

  obtenerUltimaActualizacion() {
    this.http.get<any>('/api/certiprof/last-update').subscribe({
      next: (res) => {
        if (res.lastUpdateDate) {
          this.fechaUltimaActualizacion = new Date(res.lastUpdateDate);
        }
      },
      error: (err) => console.error('Error obteniendo la última fecha', err)
    });
  }

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
        next: (response) => {
          this.isProcessing = false;
          this.selectedFile = null;
          this.successMessage = 'Carga exitosa';

          // Wait briefly to show the success message, then navigate to history
          setTimeout(() => {
            this.router.navigate(['/history']);
          }, 1500);
        },
        error: (err) => {
          this.isProcessing = false;
          console.error('Error processing file:', err);

          let errorMsg = 'Error al procesar el archivo.';
          if (err.error && err.error.message) {
            errorMsg = `Error: ${err.error.message}`;
          }
          alert(errorMsg);
        }
      });
  }
}

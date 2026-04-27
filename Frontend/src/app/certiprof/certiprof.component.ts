import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import * as Papa from 'papaparse'; // Let's try to preview the CSV using papaparse, will install it.

@Component({
  selector: 'app-certiprof',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './certiprof.component.html',
  styleUrls: ['./certiprof.component.css']
})
export class CertiprofComponent {
  selectedFile: File | null = null;
  previewData: any[] = [];
  isDragging = false;
  isProcessing = false;

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
      }
    });
  }

  processFile() {
    if (!this.selectedFile) return;

    this.isProcessing = true;
    const formData = new FormData();
    formData.append('file', this.selectedFile);

    // Call backend API
    this.http.post('http://localhost:5000/api/certiprof/process-report', formData, { responseType: 'blob' })
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

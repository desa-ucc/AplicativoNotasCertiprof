import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './history.component.html',
  styleUrls: ['./history.component.css']
})
export class HistoryComponent implements OnInit {
  histories: any[] = [];

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.fetchHistory();
  }

  fetchHistory() {
    this.http.get<any[]>('http://localhost:5000/api/certiprof/history')
      .subscribe({
        next: (data) => {
          this.histories = data;
        },
        error: (err) => {
          console.error('Error fetching history:', err);
        }
      });
  }

  downloadActa(uploadId: number, courseCode: string) {
    this.http.get(`http://localhost:5000/api/certiprof/export-avatar/${uploadId}`, { responseType: 'blob' })
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

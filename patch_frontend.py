import re

with open('Frontend/src/app/process2/process2.component.ts', 'r') as f:
    content = f.read()

content = content.replace("import { HttpClient } from '@angular/common/http';", "import { HttpClient } from '@angular/common/http';\nimport { timeout, catchError } from 'rxjs/operators';\nimport { throwError } from 'rxjs';")

new_uploadFile = """
  uploadFile() {
    if (!this.selectedFile) return;

    this.isUploading = true;
    const formData = new FormData();
    formData.append('file', this.selectedFile);

    this.http.post<any>('/api/plannermetas/upload', formData).pipe(
        timeout(30000), // Generous 30s timeout for the initial request
        catchError(err => {
            return throwError(() => err);
        })
    ).subscribe({
      next: (res) => {
        this.isUploading = false;
        // Check for 202 Accepted message pattern or just assume success if it lands here
        this.showResultModal(true, res.message || 'El archivo se está procesando en segundo plano. Puede continuar usando el sistema.');
        this.selectedFile = null;
      },
      error: (err) => {
        this.isUploading = false;
        console.error('Error uploading file:', err);
        let errorMsg = 'Ocurrió un error inesperado al procesar el archivo.';
        if (err.name === 'TimeoutError') {
             errorMsg = 'La petición tomó demasiado tiempo. Intente de nuevo más tarde.';
        } else if (err.error?.message) {
             errorMsg = err.error.message;
        }
        this.showResultModal(false, errorMsg);
      }
    });
  }
"""

content = re.sub(r'  uploadFile\(\) \{.*?(?=  showResultModal)', new_uploadFile, content, flags=re.DOTALL)

with open('Frontend/src/app/process2/process2.component.ts', 'w') as f:
    f.write(content)

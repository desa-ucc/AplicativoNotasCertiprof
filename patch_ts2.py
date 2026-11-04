import re

with open('Frontend/src/app/process2/process2.component.ts', 'r') as f:
    content = f.read()

tabs_logic = """
  // Tabs
  activeTab: 'metas' | 'proyectos' = 'metas';

  // Proyectos Umbrella
  umbrellaProjects: any[] = [];
  umbrellaProjectsFiltrados: any[] = [];
  searchProjectTerm: string = '';
"""

content = re.sub(r'  // Permisos', tabs_logic + "\n  // Permisos", content)

projects_logic = """
  switchTab(tab: 'metas' | 'proyectos') {
      this.activeTab = tab;
      if (tab === 'proyectos' && this.umbrellaProjects.length === 0) {
          this.cargarProyectosUmbrella();
      }
  }

  cargarProyectosUmbrella() {
      this.http.get<any[]>('/api/plannermetas/projects').subscribe({
          next: (data) => {
              this.umbrellaProjects = data;
              this.umbrellaProjectsFiltrados = [...this.umbrellaProjects];
          },
          error: (err) => {
              console.error('Error cargando proyectos umbrella:', err);
              this.umbrellaProjects = [];
              this.umbrellaProjectsFiltrados = [];
          }
      });
  }

  filtrarProyectosUmbrella() {
      if (!this.searchProjectTerm) {
          this.umbrellaProjectsFiltrados = [...this.umbrellaProjects];
          return;
      }
      const term = this.searchProjectTerm.toLowerCase();
      this.umbrellaProjectsFiltrados = this.umbrellaProjects.filter(p =>
          (p.codigo_proyecto && p.codigo_proyecto.toLowerCase().includes(term)) ||
          (p.nombre_proyecto && p.nombre_proyecto.toLowerCase().includes(term)) ||
          (p.area && p.area.toLowerCase().includes(term))
      );
  }

  actualizarEstadoProyecto(codigo: string, nuevoEstado: string) {
      if (!this.puedeEditar) return;

      this.http.put(`/api/plannermetas/projects/${encodeURIComponent(codigo)}/status`, { estado: nuevoEstado }).subscribe({
          next: () => {
              // Update local state
              const p = this.umbrellaProjects.find(x => x.codigo_proyecto === codigo);
              if (p) p.estado_global = nuevoEstado;
              this.showResultModal(true, `El estado del proyecto ${codigo} se actualizó a ${nuevoEstado}.`);
          },
          error: (err) => {
              console.error('Error actualizando estado:', err);
              this.showResultModal(false, 'No se pudo actualizar el estado del proyecto.');
              // Refresh to revert UI state
              this.cargarProyectosUmbrella();
          }
      });
  }

  // --- Upload Logic ---
"""

content = re.sub(r'  // --- Upload Logic ---', projects_logic, content)

with open('Frontend/src/app/process2/process2.component.ts', 'w') as f:
    f.write(content)

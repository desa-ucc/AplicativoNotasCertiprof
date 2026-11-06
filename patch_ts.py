import re

with open('Frontend/src/app/process2/process2.component.ts', 'r') as f:
    content = f.read()

grouping_logic = """
  registros: any[] = [];
  proyectosAgrupados: any[] = [];
  proyectosFiltrados: any[] = [];
  proyectosExpandidos: { [key: string]: boolean } = {};

  // Permisos
"""

content = re.sub(r'  registros: any\[\] = \[\];\n  registrosFiltrados: any\[\] = \[\];\n  \n  // Permisos', grouping_logic, content)


cargar_historial = """
  cargarHistorial() {
      this.http.get<any[]>('/api/plannermetas/history').subscribe({
          next: (data) => {
              this.registros = data;
              this.agruparProyectos();
          },
          error: (err) => {
              console.error('Error cargando historial de Metas:', err);
              this.registros = [];
              this.agruparProyectos();
          }
      });
  }

  agruparProyectos() {
      const grupos = this.registros.reduce((acc, curr) => {
          const key = curr.codigo_proyecto || curr.proyecto_umbrella || 'Sin Proyecto';
          if (!acc[key]) {
              acc[key] = {
                  codigo_proyecto: key,
                  nombre_proyecto: curr.nombre_proyecto || 'N/A',
                  area: curr.area || 'N/A',
                  fecha_carga: curr.fecha_carga, // Toma la primera
                  total_tareas: 0,
                  tareas: []
              };
          }
          acc[key].tareas.push(curr);
          acc[key].total_tareas++;

          // Mantener la fecha más reciente si hay varias
          if (curr.fecha_carga && (!acc[key].fecha_carga || curr.fecha_carga > acc[key].fecha_carga)) {
               acc[key].fecha_carga = curr.fecha_carga;
          }

          return acc;
      }, {});

      this.proyectosAgrupados = Object.values(grupos);
      this.proyectosFiltrados = [...this.proyectosAgrupados];
  }

  filtrarRegistros() {
      if (!this.searchTerm) {
          this.proyectosFiltrados = [...this.proyectosAgrupados];
          return;
      }

      const term = this.searchTerm.toLowerCase();
      this.proyectosFiltrados = this.proyectosAgrupados.filter(p =>
          (p.codigo_proyecto && p.codigo_proyecto.toLowerCase().includes(term)) ||
          (p.nombre_proyecto && p.nombre_proyecto.toLowerCase().includes(term)) ||
          (p.area && p.area.toLowerCase().includes(term)) ||
          // Buscar también dentro de las tareas del proyecto
          p.tareas.some((t: any) =>
              (t.responsable && t.responsable.toLowerCase().includes(term)) ||
              (t.id_planner_task && t.id_planner_task.toLowerCase().includes(term))
          )
      );
  }

  toggleProyecto(codigo: string) {
      this.proyectosExpandidos[codigo] = !this.proyectosExpandidos[codigo];
  }
"""

content = re.sub(r'  cargarHistorial\(\) \{.*?(?=  // --- Upload Logic ---)', cargar_historial, content, flags=re.DOTALL)

with open('Frontend/src/app/process2/process2.component.ts', 'w') as f:
    f.write(content)

import re

with open('Frontend/src/app/process2/process2.component.html', 'r') as f:
    content = f.read()

tabs_html = """
        <!-- Tabs -->
        <div class="border-b border-outline-variant/30" *ngIf="puedeVer">
            <nav class="-mb-px flex space-x-8" aria-label="Tabs">
                <button (click)="switchTab('metas')" [ngClass]="activeTab === 'metas' ? 'border-primary text-primary' : 'border-transparent text-on-surface-variant hover:border-outline-variant/50 hover:text-on-surface'" class="whitespace-nowrap border-b-2 py-4 px-1 text-sm font-medium transition-colors">
                    Historial de Metas
                </button>
                <button (click)="switchTab('proyectos')" [ngClass]="activeTab === 'proyectos' ? 'border-primary text-primary' : 'border-transparent text-on-surface-variant hover:border-outline-variant/50 hover:text-on-surface'" class="whitespace-nowrap border-b-2 py-4 px-1 text-sm font-medium transition-colors">
                    Gestión de Proyectos Umbrella
                </button>
            </nav>
        </div>

        <div *ngIf="puedeVer && activeTab === 'metas'" class="space-y-6">
"""

content = content.replace('        <div *ngIf="puedeVer" class="space-y-6">', tabs_html)

proyectos_view = """
        </div>

        <!-- Vista de Proyectos Umbrella -->
        <div *ngIf="puedeVer && activeTab === 'proyectos'" class="space-y-6">
            <div class="bg-white rounded-2xl shadow-sm border border-outline-variant/30 overflow-hidden">
                <div class="px-6 py-5 border-b border-outline-variant/20 flex justify-between items-center bg-surface-container-lowest">
                    <h3 class="text-lg font-bold text-on-surface flex items-center gap-2">
                        <span class="material-symbols-outlined text-primary">folder_managed</span>
                        Proyectos Umbrella
                    </h3>
                    <div class="relative w-full md:w-64">
                        <span class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                            <span class="material-symbols-outlined text-outline">search</span>
                        </span>
                        <input type="text" [(ngModel)]="searchProjectTerm" (ngModelChange)="filtrarProyectosUmbrella()" class="block w-full pl-10 pr-3 py-1.5 border border-outline-variant rounded-xl leading-5 bg-surface placeholder-on-surface-variant focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary sm:text-sm transition-shadow" placeholder="Buscar proyecto...">
                    </div>
                </div>

                <div class="overflow-x-auto">
                    <table class="min-w-full divide-y divide-outline-variant/20">
                        <thead class="bg-surface-container-lowest">
                            <tr>
                                <th scope="col" class="py-3.5 pl-4 pr-3 text-left text-xs font-semibold text-on-surface-variant uppercase tracking-wider sm:pl-6">Código Proyecto</th>
                                <th scope="col" class="px-3 py-3.5 text-left text-xs font-semibold text-on-surface-variant uppercase tracking-wider">Nombre</th>
                                <th scope="col" class="px-3 py-3.5 text-left text-xs font-semibold text-on-surface-variant uppercase tracking-wider">Área</th>
                                <th scope="col" class="px-3 py-3.5 text-left text-xs font-semibold text-on-surface-variant uppercase tracking-wider">Estado Global</th>
                            </tr>
                        </thead>
                        <tbody class="divide-y divide-outline-variant/20 bg-white">
                            <tr *ngFor="let p of umbrellaProjectsFiltrados" class="hover:bg-surface-container-lowest/50 transition-colors">
                                <td class="whitespace-nowrap py-4 pl-4 pr-3 text-sm font-bold text-on-surface sm:pl-6">{{ p.codigo_proyecto }}</td>
                                <td class="whitespace-nowrap px-3 py-4 text-sm text-on-surface-variant">
                                    <div class="truncate max-w-[400px]" title="{{ p.nombre_proyecto }}">{{ p.nombre_proyecto || 'N/A' }}</div>
                                </td>
                                <td class="whitespace-nowrap px-3 py-4 text-sm text-on-surface-variant">{{ p.area || 'N/A' }}</td>
                                <td class="whitespace-nowrap px-3 py-4 text-sm">
                                    <!-- Si puede editar, muestra dropdown -->
                                    <select *ngIf="puedeEditar"
                                            [ngModel]="p.estado_global"
                                            (ngModelChange)="actualizarEstadoProyecto(p.codigo_proyecto, $event)"
                                            class="block w-full max-w-[150px] rounded-lg border-outline-variant text-sm focus:border-primary focus:ring-1 focus:ring-primary py-1.5 px-3"
                                            [ngClass]="p.estado_global === 'Terminado' ? 'bg-green-50 text-green-700' : 'bg-surface text-on-surface'">
                                        <option value="Activo">Activo</option>
                                        <option value="Terminado">Terminado</option>
                                    </select>

                                    <!-- Si no puede editar, muestra etiqueta -->
                                    <span *ngIf="!puedeEditar"
                                          class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                                          [ngClass]="p.estado_global === 'Terminado' ? 'bg-green-100 text-green-800' : 'bg-surface-container text-on-surface-variant'">
                                        {{ p.estado_global }}
                                    </span>
                                </td>
                            </tr>
                            <tr *ngIf="umbrellaProjectsFiltrados.length === 0">
                                <td colspan="4" class="px-6 py-12 text-center text-on-surface-variant text-sm bg-surface-container-lowest">
                                    <div class="flex flex-col items-center justify-center">
                                        <span class="material-symbols-outlined text-4xl mb-2 text-outline-variant/50">find_in_page</span>
                                        <p>No se encontraron proyectos Umbrella.</p>
                                    </div>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>

    </div>
</div>
"""

content = re.sub(r'        </div>\n    </div>\n</div>\n\n<!-- Modal de Detalle Completo', proyectos_view + "\n<!-- Modal de Detalle Completo", content)

with open('Frontend/src/app/process2/process2.component.html', 'w') as f:
    f.write(content)

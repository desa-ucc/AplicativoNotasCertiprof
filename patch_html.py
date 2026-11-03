import re

with open('Frontend/src/app/process2/process2.component.html', 'r') as f:
    content = f.read()

history_table = """
            <!-- History Table (Accordion) -->
            <div class="bg-white rounded-2xl shadow-sm border border-outline-variant/30 overflow-hidden">
                <div class="px-6 py-5 border-b border-outline-variant/20 flex justify-between items-center bg-surface-container-lowest">
                    <h3 class="text-lg font-bold text-on-surface flex items-center gap-2">
                        <span class="material-symbols-outlined text-primary">account_tree</span>
                        Proyectos y Metas Planner
                    </h3>
                </div>

                <div class="overflow-x-auto">
                    <table class="min-w-full divide-y divide-outline-variant/20">
                        <thead class="bg-surface-container-lowest">
                            <tr>
                                <th scope="col" class="py-3.5 pl-4 pr-3 text-left text-xs font-semibold text-on-surface-variant uppercase tracking-wider sm:pl-6 w-10"></th>
                                <th scope="col" class="px-3 py-3.5 text-left text-xs font-semibold text-on-surface-variant uppercase tracking-wider">Proyecto</th>
                                <th scope="col" class="px-3 py-3.5 text-left text-xs font-semibold text-on-surface-variant uppercase tracking-wider">Área</th>
                                <th scope="col" class="px-3 py-3.5 text-center text-xs font-semibold text-on-surface-variant uppercase tracking-wider">Total Tareas</th>
                                <th scope="col" class="px-3 py-3.5 text-left text-xs font-semibold text-on-surface-variant uppercase tracking-wider">Última Carga</th>
                            </tr>
                        </thead>
                        <tbody class="divide-y divide-outline-variant/20 bg-white">
                            <ng-container *ngFor="let proyecto of proyectosFiltrados">
                                <!-- Master Row -->
                                <tr class="hover:bg-surface-container-lowest/50 transition-colors cursor-pointer" (click)="toggleProyecto(proyecto.codigo_proyecto)">
                                    <td class="whitespace-nowrap py-4 pl-4 pr-3 text-sm font-medium text-on-surface sm:pl-6 text-center">
                                        <span class="material-symbols-outlined text-outline-variant transition-transform duration-200" [ngClass]="{'rotate-90': proyectosExpandidos[proyecto.codigo_proyecto]}">chevron_right</span>
                                    </td>
                                    <td class="whitespace-nowrap px-3 py-4 text-sm font-bold text-on-surface">
                                        <div class="truncate max-w-[300px]" title="{{ proyecto.nombre_proyecto }}">{{ proyecto.codigo_proyecto }} - {{ proyecto.nombre_proyecto }}</div>
                                    </td>
                                    <td class="whitespace-nowrap px-3 py-4 text-sm text-on-surface-variant">{{ proyecto.area }}</td>
                                    <td class="whitespace-nowrap px-3 py-4 text-sm text-center">
                                        <span class="inline-flex items-center justify-center bg-primary/10 text-primary font-bold rounded-full w-6 h-6 text-xs">{{ proyecto.total_tareas }}</span>
                                    </td>
                                    <td class="whitespace-nowrap px-3 py-4 text-sm text-on-surface-variant">
                                        <span *ngIf="proyecto.fecha_carga">{{ proyecto.fecha_carga | date:'medium' }}</span>
                                        <span *ngIf="!proyecto.fecha_carga" class="italic text-outline-variant">-</span>
                                    </td>
                                </tr>

                                <!-- Detail Row -->
                                <tr *ngIf="proyectosExpandidos[proyecto.codigo_proyecto]" class="bg-surface-container-lowest/30">
                                    <td colspan="5" class="p-0 border-b-2 border-primary/20">
                                        <div class="px-8 py-4">
                                            <table class="min-w-full divide-y divide-outline-variant/10">
                                                <thead>
                                                    <tr>
                                                        <th scope="col" class="py-2 pl-2 pr-3 text-left text-[11px] font-semibold text-on-surface-variant uppercase tracking-wider">Planner ID</th>
                                                        <th scope="col" class="px-2 py-2 text-left text-[11px] font-semibold text-on-surface-variant uppercase tracking-wider">Responsable</th>
                                                        <th scope="col" class="px-2 py-2 text-left text-[11px] font-semibold text-on-surface-variant uppercase tracking-wider">Mes Cierre</th>
                                                        <th scope="col" class="px-2 py-2 text-left text-[11px] font-semibold text-on-surface-variant uppercase tracking-wider">Estado</th>
                                                        <th scope="col" class="relative py-2 pl-3 pr-2 text-right" *ngIf="puedeLeer">
                                                            <span class="sr-only">Acciones</span>
                                                        </th>
                                                    </tr>
                                                </thead>
                                                <tbody class="divide-y divide-outline-variant/10">
                                                    <tr *ngFor="let tarea of proyecto.tareas" class="hover:bg-white transition-colors">
                                                        <td class="whitespace-nowrap py-2 pl-2 pr-3 text-xs font-medium text-on-surface">
                                                            <div class="flex items-center gap-1.5">
                                                                <span class="w-1.5 h-1.5 rounded-full" [ngClass]="tarea.id_planner_task ? 'bg-green-500' : 'bg-gray-300'"></span>
                                                                <span class="truncate max-w-[120px]">{{ tarea.id_planner_task || 'Sin asignar' }}</span>
                                                            </div>
                                                        </td>
                                                        <td class="whitespace-nowrap px-2 py-2 text-xs text-on-surface-variant">{{ tarea.responsable || 'N/A' }}</td>
                                                        <td class="whitespace-nowrap px-2 py-2 text-xs text-on-surface-variant">{{ tarea.mes_de_cierre || 'N/A' }}</td>
                                                        <td class="whitespace-nowrap px-2 py-2 text-xs text-on-surface-variant">
                                                            <span class="inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-medium bg-surface-container text-on-surface">{{ tarea.estado_bucket || 'Desconocido' }}</span>
                                                        </td>
                                                        <td class="relative whitespace-nowrap py-2 pl-3 pr-2 text-right text-xs font-medium" *ngIf="puedeLeer">
                                                            <button (click)="abrirDetalle(tarea); $event.stopPropagation();" class="text-primary hover:text-primary-container inline-flex items-center gap-1 transition-colors">
                                                                Detalle <span class="material-symbols-outlined text-[14px]">open_in_new</span>
                                                            </button>
                                                        </td>
                                                    </tr>
                                                </tbody>
                                            </table>
                                        </div>
                                    </td>
                                </tr>
                            </ng-container>

                            <tr *ngIf="proyectosFiltrados.length === 0">
                                <td colspan="5" class="px-6 py-12 text-center text-on-surface-variant text-sm bg-surface-container-lowest">
                                    <div class="flex flex-col items-center justify-center">
                                        <span class="material-symbols-outlined text-4xl mb-2 text-outline-variant/50">find_in_page</span>
                                        <p>No se encontraron proyectos de metas.</p>
                                    </div>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
"""

content = re.sub(r'            <!-- History Table -->.*?</div>\n            </div>', history_table, content, flags=re.DOTALL)

with open('Frontend/src/app/process2/process2.component.html', 'w') as f:
    f.write(content)

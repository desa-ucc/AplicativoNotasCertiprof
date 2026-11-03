using System;
using System.Linq;
using System.Threading.Tasks;
using Backend.Data;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using ClosedXML.Excel;
using System.IO;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PlannerMetasController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly IPlannerService _plannerService;

        public PlannerMetasController(AppDbContext dbContext, IPlannerService plannerService)
        {
            _dbContext = dbContext;
            _plannerService = plannerService;
        }

        [Backend.Attributes.PermissionAuthorize("/process2")]
        [HttpGet("history")]
        public async Task<IActionResult> GetMetasHistory()
        {
            var records = new System.Collections.Generic.List<object>();
            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT id, area, codigo_proyecto, url, nombre_proyecto, mes_de_cierre, responsable, etapa_actividad_tarea, mes, mes_ud, anio, solicitudes_cambio, id_planner_task, estado_bucket, fecha_expectativa, fecha_cierre_real, proyecto_umbrella, correos_autorizados, fecha_carga, usuario_carga FROM [dbo].[fact_metas_planner] ORDER BY fecha_carga DESC";
                    command.CommandType = System.Data.CommandType.Text;

                    if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        await _dbContext.Database.OpenConnectionAsync();
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            records.Add(new
                            {
                                id = reader.GetInt32(reader.GetOrdinal("id")),
                                id_planner_task = reader.IsDBNull(reader.GetOrdinal("id_planner_task")) ? null : reader.GetString(reader.GetOrdinal("id_planner_task")),
                                area = reader.IsDBNull(reader.GetOrdinal("area")) ? null : reader.GetString(reader.GetOrdinal("area")),
                                codigo_proyecto = reader.IsDBNull(reader.GetOrdinal("codigo_proyecto")) ? null : reader.GetString(reader.GetOrdinal("codigo_proyecto")),
                                url = reader.IsDBNull(reader.GetOrdinal("url")) ? null : reader.GetString(reader.GetOrdinal("url")),
                                nombre_proyecto = reader.IsDBNull(reader.GetOrdinal("nombre_proyecto")) ? null : reader.GetString(reader.GetOrdinal("nombre_proyecto")),
                                mes_de_cierre = reader.IsDBNull(reader.GetOrdinal("mes_de_cierre")) ? null : reader.GetString(reader.GetOrdinal("mes_de_cierre")),
                                responsable = reader.IsDBNull(reader.GetOrdinal("responsable")) ? null : reader.GetString(reader.GetOrdinal("responsable")),
                                etapa_actividad_tarea = reader.IsDBNull(reader.GetOrdinal("etapa_actividad_tarea")) ? null : reader.GetString(reader.GetOrdinal("etapa_actividad_tarea")),
                                fecha_carga = reader.IsDBNull(reader.GetOrdinal("fecha_carga")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("fecha_carga")),
                                usuario_carga = reader.IsDBNull(reader.GetOrdinal("usuario_carga")) ? null : reader.GetString(reader.GetOrdinal("usuario_carga"))
                            });
                        }
                    }
                }
                return Ok(records);
            }
            catch (Exception ex)
            {
                // Fallback for dev if table doesn't exist
                if (ex.Message.Contains("Invalid object name"))
                {
                    return Ok(new object[] { });
                }
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Backend.Attributes.PermissionAuthorize("/process2")]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "No se ha proporcionado un archivo válido." });

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx")
                return BadRequest(new { Message = "Solo se permiten archivos Excel (.xlsx)." });

            int processedCount = 0;
            var usuarioCarga = User.Identity?.Name ?? "Sistema";

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rowCount = worksheet.RowsUsed().Count();

                        if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                        {
                            await _dbContext.Database.OpenConnectionAsync();
                        }

                        for (int i = 2; i <= rowCount; i++)
                        {
                            var row = worksheet.Row(i);

                            var area = row.Cell(1).GetString();
                            var codigoProyecto = row.Cell(2).GetString();
                            var url = row.Cell(3).GetString();
                            var nombreProyecto = row.Cell(4).GetString();
                            var mesDeCierre = row.Cell(5).GetString();
                            var responsable = row.Cell(6).GetString();
                            var etapaActividadTarea = row.Cell(7).GetString();
                            var mes = row.Cell(8).GetString();
                            var mesUd = row.Cell(9).GetString();
                            var anio = row.Cell(10).GetString();
                            var solicitudesCambio = row.Cell(11).GetString();

                            if (string.IsNullOrWhiteSpace(area) && string.IsNullOrWhiteSpace(codigoProyecto)) continue;

                            // Process 1 requirement: Generate Planner Task ID
                            var plannerTaskId = await _plannerService.CreateTaskAsync(nombreProyecto);

                            using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                            {
                                command.CommandText = "sp_InsertarMetaPlanner";
                                command.CommandType = System.Data.CommandType.StoredProcedure;

                                command.Parameters.Add(new SqlParameter("@Area", string.IsNullOrWhiteSpace(area) ? DBNull.Value : area));
                                command.Parameters.Add(new SqlParameter("@CodigoProyecto", string.IsNullOrWhiteSpace(codigoProyecto) ? DBNull.Value : codigoProyecto));
                                command.Parameters.Add(new SqlParameter("@Url", string.IsNullOrWhiteSpace(url) ? DBNull.Value : url));
                                command.Parameters.Add(new SqlParameter("@NombreProyecto", string.IsNullOrWhiteSpace(nombreProyecto) ? DBNull.Value : nombreProyecto));
                                command.Parameters.Add(new SqlParameter("@MesDeCierre", string.IsNullOrWhiteSpace(mesDeCierre) ? DBNull.Value : mesDeCierre));
                                command.Parameters.Add(new SqlParameter("@Responsable", string.IsNullOrWhiteSpace(responsable) ? DBNull.Value : responsable));
                                command.Parameters.Add(new SqlParameter("@EtapaActividadTarea", string.IsNullOrWhiteSpace(etapaActividadTarea) ? DBNull.Value : etapaActividadTarea));
                                command.Parameters.Add(new SqlParameter("@Mes", string.IsNullOrWhiteSpace(mes) ? DBNull.Value : mes));
                                command.Parameters.Add(new SqlParameter("@MesUd", string.IsNullOrWhiteSpace(mesUd) ? DBNull.Value : mesUd));
                                command.Parameters.Add(new SqlParameter("@Anio", string.IsNullOrWhiteSpace(anio) ? DBNull.Value : anio));
                                command.Parameters.Add(new SqlParameter("@SolicitudesCambio", string.IsNullOrWhiteSpace(solicitudesCambio) ? DBNull.Value : solicitudesCambio));
                                command.Parameters.Add(new SqlParameter("@IdPlannerTask", plannerTaskId));
                                command.Parameters.Add(new SqlParameter("@EstadoBucket", "En blanco")); // Initial status
                                command.Parameters.Add(new SqlParameter("@FechaExpectativa", DBNull.Value));
                                command.Parameters.Add(new SqlParameter("@FechaCierreReal", DBNull.Value));
                                command.Parameters.Add(new SqlParameter("@ProyectoUmbrella", DBNull.Value));
                                command.Parameters.Add(new SqlParameter("@CorreosAutorizados", DBNull.Value));
                                command.Parameters.Add(new SqlParameter("@UsuarioCarga", usuarioCarga));

                                try
                                {
                                    await command.ExecuteNonQueryAsync();
                                    processedCount++;
                                }
                                catch (Exception ex)
                                {
                                    if(ex.Message.Contains("Could not find stored procedure")) break;
                                }
                            }
                        }
                    }
                }

                return Ok(new { Message = $"Se han procesado {processedCount} registros correctamente en Planner Metas.", ProcessedCount = processedCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error interno al procesar el archivo.", Detail = ex.Message });
            }
        }

        public class N8nSyncPayload
        {
            public string IdPlannerTask { get; set; } = string.Empty;
            public string? Area { get; set; }
            public string? CodigoProyecto { get; set; }
            public string? Url { get; set; }
            public string? NombreProyecto { get; set; }
            public string? MesDeCierre { get; set; }
            public string? Responsable { get; set; }
            public string? EtapaActividadTarea { get; set; }
            public string? Mes { get; set; }
            public string? MesUd { get; set; }
            public string? Anio { get; set; }
            public string? SolicitudesCambio { get; set; }
            public string? EstadoBucket { get; set; }
            public DateTime? FechaExpectativa { get; set; }
            public DateTime? FechaCierreReal { get; set; }
            public string? ProyectoUmbrella { get; set; }
            public string? CorreosAutorizados { get; set; }
        }

        [Backend.Attributes.PermissionAuthorize("/process2")]
        [HttpPost("sync-n8n")]
        public async Task<IActionResult> SyncN8nWebhook([FromBody] N8nSyncPayload payload)
        {
            if (string.IsNullOrWhiteSpace(payload.IdPlannerTask))
            {
                return BadRequest(new { Message = "id_planner_task es requerido para sincronización." });
            }

            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "sp_UpsertMetaPlanner";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@IdPlannerTask", payload.IdPlannerTask));
                    command.Parameters.Add(new SqlParameter("@Area", string.IsNullOrWhiteSpace(payload.Area) ? DBNull.Value : payload.Area));
                    command.Parameters.Add(new SqlParameter("@CodigoProyecto", string.IsNullOrWhiteSpace(payload.CodigoProyecto) ? DBNull.Value : payload.CodigoProyecto));
                    command.Parameters.Add(new SqlParameter("@Url", string.IsNullOrWhiteSpace(payload.Url) ? DBNull.Value : payload.Url));
                    command.Parameters.Add(new SqlParameter("@NombreProyecto", string.IsNullOrWhiteSpace(payload.NombreProyecto) ? DBNull.Value : payload.NombreProyecto));
                    command.Parameters.Add(new SqlParameter("@MesDeCierre", string.IsNullOrWhiteSpace(payload.MesDeCierre) ? DBNull.Value : payload.MesDeCierre));
                    command.Parameters.Add(new SqlParameter("@Responsable", string.IsNullOrWhiteSpace(payload.Responsable) ? DBNull.Value : payload.Responsable));
                    command.Parameters.Add(new SqlParameter("@EtapaActividadTarea", string.IsNullOrWhiteSpace(payload.EtapaActividadTarea) ? DBNull.Value : payload.EtapaActividadTarea));
                    command.Parameters.Add(new SqlParameter("@Mes", string.IsNullOrWhiteSpace(payload.Mes) ? DBNull.Value : payload.Mes));
                    command.Parameters.Add(new SqlParameter("@MesUd", string.IsNullOrWhiteSpace(payload.MesUd) ? DBNull.Value : payload.MesUd));
                    command.Parameters.Add(new SqlParameter("@Anio", string.IsNullOrWhiteSpace(payload.Anio) ? DBNull.Value : payload.Anio));
                    command.Parameters.Add(new SqlParameter("@SolicitudesCambio", string.IsNullOrWhiteSpace(payload.SolicitudesCambio) ? DBNull.Value : payload.SolicitudesCambio));
                    command.Parameters.Add(new SqlParameter("@EstadoBucket", string.IsNullOrWhiteSpace(payload.EstadoBucket) ? DBNull.Value : payload.EstadoBucket));
                    command.Parameters.Add(new SqlParameter("@FechaExpectativa", payload.FechaExpectativa.HasValue ? payload.FechaExpectativa.Value : DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@FechaCierreReal", payload.FechaCierreReal.HasValue ? payload.FechaCierreReal.Value : DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@ProyectoUmbrella", string.IsNullOrWhiteSpace(payload.ProyectoUmbrella) ? DBNull.Value : payload.ProyectoUmbrella));
                    command.Parameters.Add(new SqlParameter("@CorreosAutorizados", string.IsNullOrWhiteSpace(payload.CorreosAutorizados) ? DBNull.Value : payload.CorreosAutorizados));
                    command.Parameters.Add(new SqlParameter("@UsuarioCarga", "n8n_Automation"));

                    if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        await _dbContext.Database.OpenConnectionAsync();
                    }

                    await command.ExecuteNonQueryAsync();
                }
                return Ok(new { Message = "Sincronización de n8n procesada exitosamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error interno al sincronizar.", Detail = ex.Message });
            }
        }
    }
}

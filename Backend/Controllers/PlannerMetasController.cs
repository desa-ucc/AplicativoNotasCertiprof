using Microsoft.Extensions.DependencyInjection;
using System.Threading;
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
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _jobStatus = new();
        private static string _lastFileHash = string.Empty;
        private readonly AppDbContext _dbContext;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _serviceScopeFactory;
        private readonly IPlannerService _plannerService;

        public PlannerMetasController(AppDbContext dbContext, IPlannerService plannerService, IBackgroundTaskQueue taskQueue, Microsoft.Extensions.DependencyInjection.IServiceScopeFactory serviceScopeFactory)
        {
            _dbContext = dbContext;
            _plannerService = plannerService;
            _taskQueue = taskQueue;
            _serviceScopeFactory = serviceScopeFactory;
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
                    command.CommandText = "SELECT id, area, codigo_proyecto, url, nombre_proyecto, mes_de_cierre, responsable, etapa_actividad_tarea, mes, mes_ud, anio, solicitudes_cambio, id_planner_task, estado_bucket, fecha_expectativa, fecha_cierre_real, proyecto_umbrella, correos_autorizados, fecha_carga, usuario_carga, estado_proyecto FROM [dbo].[pry_metas_planner] ORDER BY fecha_carga DESC";
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
                                estado_proyecto = reader.IsDBNull(reader.GetOrdinal("estado_proyecto")) ? null : reader.GetString(reader.GetOrdinal("estado_proyecto")),
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

            // Read file into memory
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            // Calculate Hash
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(fileBytes);
            var hashString = Convert.ToBase64String(hashBytes);

            if (hashString == _lastFileHash)
            {
                return Conflict(new { Message = "Este archivo ya fue subido recientemente." });
            }
            _lastFileHash = hashString;

            // Pre-count rows
            int validRows = 0;
            try
            {
                using var stream = new MemoryStream(fileBytes);
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 0;

                for (int i = 2; i <= rowCount; i++)
                {
                    var row = worksheet.Row(i);
                    var area = row.Cell(1).GetString();
                    var codigoProyecto = row.Cell(2).GetString();
                    if (!string.IsNullOrWhiteSpace(area) || !string.IsNullOrWhiteSpace(codigoProyecto))
                    {
                        validRows++;
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Error al leer el archivo Excel.", Detail = ex.Message });
            }

            var usuarioCarga = User.Identity?.Name ?? "Sistema";
            var jobId = Guid.NewGuid().ToString();
            _jobStatus[jobId] = "Procesando";

            _taskQueue.QueueBackgroundWorkItem(async token =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var scopedPlannerService = scope.ServiceProvider.GetRequiredService<IPlannerService>();

                try {
                    await ProcessExcelInBackground(fileBytes, usuarioCarga, scopedDbContext, scopedPlannerService, token);
                    _jobStatus[jobId] = "Completado";
                } catch(Exception) {
                    _jobStatus[jobId] = "Error";
                }
            });

            return Accepted(new { Message = $"El archivo fue recibido. Se procesarán {validRows} metas en segundo plano.", JobId = jobId });
        }

        [HttpGet("status/{jobId}")]
        public IActionResult GetStatus(string jobId)
        {
            if (_jobStatus.TryGetValue(jobId, out var status))
            {
                return Ok(new { Status = status });
            }
            return NotFound();
        }


        private string ParseYear(IXLCell cell)
        {
            if (cell.IsEmpty()) return null;

            // 1. Try if it's explicitly a DateTime in Excel
            if (cell.DataType == XLDataType.DateTime)
            {
                try
                {
                    return cell.GetDateTime().Year.ToString("0000");
                }
                catch
                {
                    // Fallback to text parsing
                }
            }

            var textValue = cell.GetString()?.Trim();
            if (string.IsNullOrEmpty(textValue)) return null;

            // 2. Parse number string
            if (int.TryParse(textValue, out int yearNumber))
            {
                // JavaScript Date.getYear() bug -> 126 means 2026
                if (yearNumber >= 100 && yearNumber < 200)
                {
                    return (1900 + yearNumber).ToString();
                }
                // Two digit years (e.g. 26 -> 2026)
                if (yearNumber > 0 && yearNumber < 100)
                {
                    return (2000 + yearNumber).ToString();
                }
                // It's already a full year
                if (yearNumber >= 1900 && yearNumber <= 2100)
                {
                    return yearNumber.ToString("0000");
                }
            }

            // 3. Fallback: it might be a date string like "2026-01-01" or "01/01/2026"
            if (DateTime.TryParse(textValue, out DateTime parsedDate))
            {
                return parsedDate.Year.ToString("0000");
            }

            // If all else fails, just return what we have
            return textValue;
        }


        private DateTime? TryParseDueDateTime(string mesDeCierre, string anio)
        {
            if (string.IsNullOrWhiteSpace(anio) || string.IsNullOrWhiteSpace(mesDeCierre))
                return null;

            // Map Spanish months to integers if necessary, or just attempt a parse if it's already a number
            int month = 12; // default to december if we can't parse the month name
            var mesLower = mesDeCierre.ToLower().Trim();

            if (mesLower.StartsWith("ene")) month = 1;
            else if (mesLower.StartsWith("feb")) month = 2;
            else if (mesLower.StartsWith("mar")) month = 3;
            else if (mesLower.StartsWith("abr")) month = 4;
            else if (mesLower.StartsWith("may")) month = 5;
            else if (mesLower.StartsWith("jun")) month = 6;
            else if (mesLower.StartsWith("jul")) month = 7;
            else if (mesLower.StartsWith("ago")) month = 8;
            else if (mesLower.StartsWith("sep")) month = 9;
            else if (mesLower.StartsWith("oct")) month = 10;
            else if (mesLower.StartsWith("nov")) month = 11;
            else if (mesLower.StartsWith("dic")) month = 12;
            else int.TryParse(mesLower, out month);

            if (month < 1 || month > 12) month = 12;

            if (int.TryParse(anio, out int year))
            {
                 try
                 {
                     // Set due date to the last day of the target month
                     int daysInMonth = DateTime.DaysInMonth(year, month);
                     return new DateTime(year, month, daysInMonth, 23, 59, 59, DateTimeKind.Utc);
                 }
                 catch
                 {
                     return null;
                 }
            }
            return null;
        }

        private async Task ProcessExcelInBackground(byte[] fileBytes, string usuarioCarga, AppDbContext dbContext, IPlannerService plannerService, CancellationToken token)


        {
            try
            {
                using var stream = new MemoryStream(fileBytes);
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var rowCount = worksheet.RowsUsed().Count();

                if (dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                {
                    await dbContext.Database.OpenConnectionAsync(token);
                }

                for (int i = 2; i <= rowCount; i++)
                {
                    if (token.IsCancellationRequested) break;

                    var row = worksheet.Row(i);
                    var area = row.Cell(1).GetString();
                    var codigoProyecto = row.Cell(2).GetString();

                    if (string.IsNullOrWhiteSpace(area) && string.IsNullOrWhiteSpace(codigoProyecto)) continue;

                    var url = row.Cell(3).GetString();
                    var nombreProyecto = row.Cell(4).GetString();
                    var mesDeCierre = row.Cell(5).GetString();
                    var responsable = row.Cell(6).GetString();
                    var etapaActividadTarea = row.Cell(7).GetString();
                    var mes = row.Cell(8).GetString();
                    var mesUd = row.Cell(9).GetString();
                    var anio = ParseYear(row.Cell(10));
                    var solicitudesCambio = row.Cell(11).GetString();

                    string plannerTaskId = null;

                    // Check for existing task to prevent duplicates
                    using (var checkCmd = dbContext.Database.GetDbConnection().CreateCommand())
                    {
                        checkCmd.CommandText = "SELECT id_planner_task FROM pry_metas_planner WHERE codigo_proyecto = @codigo AND etapa_actividad_tarea = @etapa AND nombre_proyecto = @nombre";
                        checkCmd.CommandType = System.Data.CommandType.Text;
                        checkCmd.Parameters.Add(new SqlParameter("@codigo", string.IsNullOrWhiteSpace(codigoProyecto) ? DBNull.Value : codigoProyecto));
                        checkCmd.Parameters.Add(new SqlParameter("@etapa", string.IsNullOrWhiteSpace(etapaActividadTarea) ? DBNull.Value : etapaActividadTarea));
                        checkCmd.Parameters.Add(new SqlParameter("@nombre", string.IsNullOrWhiteSpace(nombreProyecto) ? DBNull.Value : nombreProyecto));
                        var result = await checkCmd.ExecuteScalarAsync(token);
                        if (result != null && result != DBNull.Value)
                        {
                            plannerTaskId = result.ToString();
                        }
                    }

                    int maxRetries = 3;
                    for (int retry = 0; retry < maxRetries && string.IsNullOrEmpty(plannerTaskId); retry++)
                    {
                        try
                        {
                            // Parse dueDateTime
                            var dueDateTime = TryParseDueDateTime(mesDeCierre, anio);

                            // Process 1 requirement: Generate Planner Task ID with retry logic
                            plannerTaskId = await plannerService.CreateTaskAsync(nombreProyecto, null, dueDateTime);
                            break;
                        }
                        catch (Exception)
                        {
                            if (retry == maxRetries - 1)
                            {
                                // Log failure, in a real app we might write to an error log table
                                Console.WriteLine($"Failed to create Planner task for project {nombreProyecto} after {maxRetries} retries.");
                            }
                            await Task.Delay(1000 * (retry + 1), token); // exponential-ish backoff
                        }
                    }

                    using (var command = dbContext.Database.GetDbConnection().CreateCommand())
                    {
                        command.CommandText = "sp_pry_GestionMetas";
                        command.CommandType = System.Data.CommandType.StoredProcedure;

                        command.Parameters.Add(new SqlParameter("@Area", string.IsNullOrWhiteSpace(area) ? DBNull.Value : area));
                        command.Parameters.Add(new SqlParameter("@CodigoProyecto", string.IsNullOrWhiteSpace(codigoProyecto) ? DBNull.Value : codigoProyecto));
                        command.Parameters.Add(new SqlParameter("@Url", string.IsNullOrWhiteSpace(url) ? DBNull.Value : url));
                        command.Parameters.Add(new SqlParameter("@NombreProyecto", string.IsNullOrWhiteSpace(nombreProyecto) ? DBNull.Value : nombreProyecto));
                        command.Parameters.Add(new SqlParameter("@MesDeCierre", string.IsNullOrWhiteSpace(mesDeCierre) ? DBNull.Value : mesDeCierre));
                        command.Parameters.Add(new SqlParameter("@Responsable", string.IsNullOrWhiteSpace(responsable) ? DBNull.Value : responsable));
                        command.Parameters.Add(new SqlParameter("@EtapaActividadTarea", string.IsNullOrWhiteSpace(etapaActividadTarea) ? DBNull.Value : etapaActividadTarea));
                        command.Parameters.Add(new SqlParameter("@Accion", "UPSERT_META"));
                        command.Parameters.Add(new SqlParameter("@Mes", string.IsNullOrWhiteSpace(mes) ? DBNull.Value : mes));
                        command.Parameters.Add(new SqlParameter("@MesUd", string.IsNullOrWhiteSpace(mesUd) ? DBNull.Value : mesUd));
                        command.Parameters.Add(new SqlParameter("@Anio", string.IsNullOrWhiteSpace(anio) ? DBNull.Value : anio));
                        command.Parameters.Add(new SqlParameter("@SolicitudesCambio", string.IsNullOrWhiteSpace(solicitudesCambio) ? DBNull.Value : solicitudesCambio));
                        command.Parameters.Add(new SqlParameter("@IdPlannerTask", string.IsNullOrWhiteSpace(plannerTaskId) ? DBNull.Value : plannerTaskId));
                        command.Parameters.Add(new SqlParameter("@EstadoBucket", string.IsNullOrWhiteSpace(plannerTaskId) ? "Pendiente" : "En blanco"));
                        command.Parameters.Add(new SqlParameter("@FechaExpectativa", DBNull.Value));
                        command.Parameters.Add(new SqlParameter("@FechaCierreReal", DBNull.Value));
                        command.Parameters.Add(new SqlParameter("@ProyectoUmbrella", DBNull.Value));
                        command.Parameters.Add(new SqlParameter("@CorreosAutorizados", DBNull.Value));
                        command.Parameters.Add(new SqlParameter("@UsuarioCarga", usuarioCarga));

                        try
                        {
                            await command.ExecuteNonQueryAsync(token);
                        }
                        catch (Exception ex)
                        {
                            if(ex.Message.Contains("Could not find stored procedure")) break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Background processing error: {ex.Message}");
            }
        }



        [Backend.Attributes.PermissionAuthorize("/process2")]
        [HttpGet("projects")]
        public async Task<IActionResult> GetUmbrellaProjects()
        {
            var records = new System.Collections.Generic.List<object>();
            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "sp_pry_ObtenerProyectosUmbrella";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

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
                                codigo_proyecto = reader.IsDBNull(reader.GetOrdinal("codigo_proyecto")) ? null : reader.GetString(reader.GetOrdinal("codigo_proyecto")),
                                nombre_proyecto = reader.IsDBNull(reader.GetOrdinal("nombre_proyecto")) ? null : reader.GetString(reader.GetOrdinal("nombre_proyecto")),
                                area = reader.IsDBNull(reader.GetOrdinal("area")) ? null : reader.GetString(reader.GetOrdinal("area")),
                                estado_global = reader.IsDBNull(reader.GetOrdinal("estado_global")) ? "Activo" : reader.GetString(reader.GetOrdinal("estado_global"))
                            });
                        }
                    }
                }
                return Ok(records);
            }
            catch (Exception ex)
            {
                // Fallback for dev if table doesn't exist
                if (ex.Message.Contains("Could not find stored procedure") || ex.Message.Contains("Invalid object name"))
                {
                    return Ok(new object[] { });
                }
                return BadRequest(new { Message = ex.Message });
            }
        }

        public class UpdateProjectStatusRequest
        {
            public string Estado { get; set; } = string.Empty;
        }

        [Backend.Attributes.PermissionAuthorize("/process2")]
        [HttpPut("projects/{codigo}/status")]
        public async Task<IActionResult> UpdateProjectStatus(string codigo, [FromBody] UpdateProjectStatusRequest request)
        {
            if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(request.Estado))
                return BadRequest(new { Message = "Datos inválidos." });

            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "sp_ActualizarEstadoProyecto";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@CodigoProyecto", codigo));
                    command.Parameters.Add(new SqlParameter("@Estado", request.Estado));

                    if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        await _dbContext.Database.OpenConnectionAsync();
                    }

                    await command.ExecuteNonQueryAsync();
                }

                return Ok(new { Message = "Estado actualizado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error interno al actualizar estado.", Detail = ex.Message });
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
                    command.CommandText = "sp_pry_GestionMetas";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@IdPlannerTask", payload.IdPlannerTask));
                    command.Parameters.Add(new SqlParameter("@Area", string.IsNullOrWhiteSpace(payload.Area) ? DBNull.Value : payload.Area));
                    command.Parameters.Add(new SqlParameter("@CodigoProyecto", string.IsNullOrWhiteSpace(payload.CodigoProyecto) ? DBNull.Value : payload.CodigoProyecto));
                    command.Parameters.Add(new SqlParameter("@Url", string.IsNullOrWhiteSpace(payload.Url) ? DBNull.Value : payload.Url));
                    command.Parameters.Add(new SqlParameter("@NombreProyecto", string.IsNullOrWhiteSpace(payload.NombreProyecto) ? DBNull.Value : payload.NombreProyecto));
                    command.Parameters.Add(new SqlParameter("@MesDeCierre", string.IsNullOrWhiteSpace(payload.MesDeCierre) ? DBNull.Value : payload.MesDeCierre));
                    command.Parameters.Add(new SqlParameter("@Responsable", string.IsNullOrWhiteSpace(payload.Responsable) ? DBNull.Value : payload.Responsable));
                    command.Parameters.Add(new SqlParameter("@EtapaActividadTarea", string.IsNullOrWhiteSpace(payload.EtapaActividadTarea) ? DBNull.Value : payload.EtapaActividadTarea));
                        command.Parameters.Add(new SqlParameter("@Accion", "UPSERT_META"));
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
                    command.Parameters.Add(new SqlParameter("@EstadoProyecto", DBNull.Value));

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

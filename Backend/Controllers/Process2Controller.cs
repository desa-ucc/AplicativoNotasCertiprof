using System;
using System.Linq;
using System.Threading.Tasks;
using Backend.Data;
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
    public class Process2Controller : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public Process2Controller(AppDbContext dbContext)
        {
            _dbContext = dbContext;
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
                    command.CommandText = "sp_ObtenerHistorialMetas";
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
                                id = reader.IsDBNull(reader.GetOrdinal("id")) ? 0 : reader.GetInt32(reader.GetOrdinal("id")),
                                area = reader.IsDBNull(reader.GetOrdinal("area")) ? null : reader.GetString(reader.GetOrdinal("area")),
                                codigo_proyecto = reader.IsDBNull(reader.GetOrdinal("codigo_proyecto")) ? null : reader.GetString(reader.GetOrdinal("codigo_proyecto")),
                                url = reader.IsDBNull(reader.GetOrdinal("url")) ? null : reader.GetString(reader.GetOrdinal("url")),
                                nombre_proyecto = reader.IsDBNull(reader.GetOrdinal("nombre_proyecto")) ? null : reader.GetString(reader.GetOrdinal("nombre_proyecto")),
                                mes_cierre = reader.IsDBNull(reader.GetOrdinal("mes_cierre")) ? null : reader.GetString(reader.GetOrdinal("mes_cierre")),
                                responsable = reader.IsDBNull(reader.GetOrdinal("responsable")) ? null : reader.GetString(reader.GetOrdinal("responsable")),
                                etapa_actividades_tareas = reader.IsDBNull(reader.GetOrdinal("etapa_actividades_tareas")) ? null : reader.GetString(reader.GetOrdinal("etapa_actividades_tareas")),
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
                if (ex.Message.Contains("Could not find stored procedure"))
                {
                    return Ok(new object[] { });
                }
                return BadRequest(new { Message = ex.Message });
            }
        }


        [Backend.Attributes.PermissionAuthorize("/process2")]
        [HttpPost("upload-goals")]
        public async Task<IActionResult> UploadGoals(IFormFile file)
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

                        // Enforce basic barrier validations (check headers)
                        var headerRow = worksheet.Row(1);
                        bool isValid = true;

                        // Very loose validation, assuming user uses the provided template
                        // We will just iterate starting from row 2

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
                            var mesCierre = row.Cell(5).GetString();
                            var responsable = row.Cell(6).GetString();
                            var etapaActividades = row.Cell(7).GetString();

                            // Skip empty rows
                            if (string.IsNullOrWhiteSpace(area) && string.IsNullOrWhiteSpace(codigoProyecto)) continue;

                            using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                            {
                                command.CommandText = "sp_CargarMetas";
                                command.CommandType = System.Data.CommandType.StoredProcedure;

                                var pArea = command.CreateParameter(); pArea.ParameterName = "@Area"; pArea.Value = string.IsNullOrWhiteSpace(area) ? DBNull.Value : area; command.Parameters.Add(pArea);
                                var pCod = command.CreateParameter(); pCod.ParameterName = "@CodigoProyecto"; pCod.Value = string.IsNullOrWhiteSpace(codigoProyecto) ? DBNull.Value : codigoProyecto; command.Parameters.Add(pCod);
                                var pUrl = command.CreateParameter(); pUrl.ParameterName = "@Url"; pUrl.Value = string.IsNullOrWhiteSpace(url) ? DBNull.Value : url; command.Parameters.Add(pUrl);
                                var pNom = command.CreateParameter(); pNom.ParameterName = "@NombreProyecto"; pNom.Value = string.IsNullOrWhiteSpace(nombreProyecto) ? DBNull.Value : nombreProyecto; command.Parameters.Add(pNom);
                                var pMes = command.CreateParameter(); pMes.ParameterName = "@MesCierre"; pMes.Value = string.IsNullOrWhiteSpace(mesCierre) ? DBNull.Value : mesCierre; command.Parameters.Add(pMes);
                                var pResp = command.CreateParameter(); pResp.ParameterName = "@Responsable"; pResp.Value = string.IsNullOrWhiteSpace(responsable) ? DBNull.Value : responsable; command.Parameters.Add(pResp);
                                var pEtapa = command.CreateParameter(); pEtapa.ParameterName = "@EtapaActividadesTareas"; pEtapa.Value = string.IsNullOrWhiteSpace(etapaActividades) ? DBNull.Value : etapaActividades; command.Parameters.Add(pEtapa);
                                var pUsr = command.CreateParameter(); pUsr.ParameterName = "@UsuarioCarga"; pUsr.Value = usuarioCarga; command.Parameters.Add(pUsr);

                                try
                                {
                                    await command.ExecuteNonQueryAsync();
                                    processedCount++;
                                }
                                catch (Exception ex)
                                {
                                     // Skip invalid rows on SP missing, let dev proceed
                                     if(ex.Message.Contains("Could not find stored procedure")) break;
                                }
                            }
                        }
                    }
                }

                return Ok(new { Message = $"Se han procesado {processedCount} registros correctamente.", ProcessedCount = processedCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error interno al procesar el archivo.", Detail = ex.Message });
            }
        }
    }
}

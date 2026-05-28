using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Backend.Services;
using System.Threading.Tasks;
using System;

namespace Backend.Controllers
{
    using Microsoft.AspNetCore.Authorization;

    using Microsoft.EntityFrameworkCore;

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CertiprofController : ControllerBase
    {
        private readonly IFileProcessingService _fileProcessingService;

        public CertiprofController(IFileProcessingService fileProcessingService)
        {
            _fileProcessingService = fileProcessingService;
        }

        [Backend.Attributes.PermissionAuthorize("/upload")]
        [HttpPost("process-report")]
        public async Task<IActionResult> ProcessReport(IFormFile file)
        {
            try
            {
                // Retrieve user from token
                var uploadedBy = User.Identity?.Name ?? "Unknown_User";

                var result = await _fileProcessingService.ProcessReportAsync(file, uploadedBy, string.Empty, string.Empty);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Backend.Attributes.PermissionAuthorize("/history")]
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromServices] Backend.Data.AppDbContext dbContext)
        {
            var records = new System.Collections.Generic.List<object>();

            using (var command = dbContext.Database.GetDbConnection().CreateCommand())
            {
                // The House Way: Execute the requested SP instead of a direct SELECT
                command.CommandText = "sp_ConsultarHistorialCertificaciones";
                command.CommandType = System.Data.CommandType.StoredProcedure;

                if (dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                {
                    dbContext.Database.OpenConnection();
                }

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        // Map directly according to the strict instruction:
                        // "cedula, email, certification_name, first_name, last_name, percentage, status"
                        decimal? percentageVal = null;
                        if (!reader.IsDBNull(reader.GetOrdinal("percentage")))
                        {
                            var rawPct = reader.GetValue(reader.GetOrdinal("percentage"));
                            if (decimal.TryParse(rawPct.ToString(), out decimal parsedPct))
                            {
                                percentageVal = parsedPct;
                            }
                        }

                        records.Add(new
                        {
                            id = reader.IsDBNull(reader.GetOrdinal("id")) ? 0 : reader.GetInt32(reader.GetOrdinal("id")),
                            cedula = reader.IsDBNull(reader.GetOrdinal("cedula")) ? null : reader.GetValue(reader.GetOrdinal("cedula")).ToString(),
                            email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetValue(reader.GetOrdinal("email")).ToString(),
                            certification_name = reader.IsDBNull(reader.GetOrdinal("certification_name")) ? null : reader.GetValue(reader.GetOrdinal("certification_name")).ToString(),
                            first_name = reader.IsDBNull(reader.GetOrdinal("first_name")) ? null : reader.GetValue(reader.GetOrdinal("first_name")).ToString(),
                            last_name = reader.IsDBNull(reader.GetOrdinal("last_name")) ? null : reader.GetValue(reader.GetOrdinal("last_name")).ToString(),
                            percentage = percentageVal,
                            status = reader.IsDBNull(reader.GetOrdinal("status")) ? null : reader.GetValue(reader.GetOrdinal("status")).ToString(),
                            // Including created_at for frontend as requested
                            created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ? (System.DateTime?)null : reader.GetDateTime(reader.GetOrdinal("created_at"))
                        });
                    }
                }
            }

            return Ok(records);
        }

        public class EditRecordRequest
        {
            [System.Text.Json.Serialization.JsonPropertyName("id")]
            public int Id { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("first_name")]
            public string? FirstName { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("last_name")]
            public string? LastName { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("percentage")]
            public decimal? Percentage { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("status")]
            public string? Status { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("certification_name")]
            public string? CertificationName { get; set; }
        }

        [Backend.Attributes.PermissionAuthorize("/upload")]
        [HttpPost("edit")]
        public async Task<IActionResult> EditRecord([FromBody] EditRecordRequest request, [FromServices] Backend.Data.AppDbContext dbContext)
        {
            try
            {
                using (var command = dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "sp_EditarRegistroCertificacion";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    var pId = command.CreateParameter(); pId.ParameterName = "@Id"; pId.Value = request.Id; command.Parameters.Add(pId);
                    var pFirst = command.CreateParameter(); pFirst.ParameterName = "@FirstName"; pFirst.Value = (object)request.FirstName ?? DBNull.Value; command.Parameters.Add(pFirst);
                    var pLast = command.CreateParameter(); pLast.ParameterName = "@LastName"; pLast.Value = (object)request.LastName ?? DBNull.Value; command.Parameters.Add(pLast);
                    var pPct = command.CreateParameter(); pPct.ParameterName = "@Percentage"; pPct.Value = (object)request.Percentage ?? DBNull.Value; command.Parameters.Add(pPct);
                    var pStatus = command.CreateParameter(); pStatus.ParameterName = "@Status"; pStatus.Value = (object)request.Status ?? DBNull.Value; command.Parameters.Add(pStatus);
                    var pCert = command.CreateParameter(); pCert.ParameterName = "@CertificationName"; pCert.Value = (object)request.CertificationName ?? DBNull.Value; command.Parameters.Add(pCert);

                    if (dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        dbContext.Database.OpenConnection();
                    }

                    await command.ExecuteNonQueryAsync();
                }

                return Ok(new { Message = "Registro actualizado exitosamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }


        [Backend.Attributes.PermissionAuthorize("/upload")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecord(int id, [FromServices] Backend.Data.AppDbContext dbContext)
        {
            try
            {
                using (var command = dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "sp_EliminarRegistroCertificacion";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    var pId = command.CreateParameter();
                    pId.ParameterName = "@Id";
                    pId.Value = id;
                    command.Parameters.Add(pId);

                    if (dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        dbContext.Database.OpenConnection();
                    }

                    await command.ExecuteNonQueryAsync();
                }

                return Ok(new { Message = "Registro eliminado exitosamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("export-avatar/{uploadId}")]
        public async Task<IActionResult> ExportAvatar(int uploadId)
        {
            try
            {
                var result = await _fileProcessingService.GenerateAvatarActAsync(uploadId);

                if (result.FileBytes == null)
                    return BadRequest("Error generating report.");

                return File(result.FileBytes, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}

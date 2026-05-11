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
    [Authorize(Policy = "DocentePolicy")]
    public class CertiprofController : ControllerBase
    {
        private readonly IFileProcessingService _fileProcessingService;

        public CertiprofController(IFileProcessingService fileProcessingService)
        {
            _fileProcessingService = fileProcessingService;
        }

        [AllowAnonymous]
        [HttpPost("process-report")]
        public async Task<IActionResult> ProcessReport(IFormFile file)
        {
            try
            {
                // Retrieve user from token
                var uploadedBy = User.Identity?.Name ?? "Unknown_User";

                var uploadId = await _fileProcessingService.ProcessReportAsync(file, uploadedBy, string.Empty, string.Empty);

                return Ok(new { UploadId = uploadId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

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

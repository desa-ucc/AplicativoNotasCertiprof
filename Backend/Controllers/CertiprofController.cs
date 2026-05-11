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
            // Simple SELECT * FROM cert_registros mapping as requested
            // Because we don't have PKs and map it via HasNoKey, we can execute raw sql to fetch them simply,
            // or just use FromSqlRaw.

            var records = new System.Collections.Generic.List<object>();

            using (var command = dbContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "SELECT cert_status, cert_percentage, cert_first_name, cert_last_name, cert_email, cert_certification_name, cert_created_at, cert_cedula FROM cert_registros";

                if (dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                {
                    dbContext.Database.OpenConnection();
                }

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        records.Add(new
                        {
                            status = reader.IsDBNull(0) ? null : reader.GetValue(0).ToString(),
                            percentage = reader.IsDBNull(1) ? null : reader.GetValue(1).ToString(),
                            first_name = reader.IsDBNull(2) ? null : reader.GetValue(2).ToString(),
                            last_name = reader.IsDBNull(3) ? null : reader.GetValue(3).ToString(),
                            email = reader.IsDBNull(4) ? null : reader.GetValue(4).ToString(),
                            certification_name = reader.IsDBNull(5) ? null : reader.GetValue(5).ToString(),
                            created_at = reader.IsDBNull(6) ? (System.DateTime?)null : reader.GetDateTime(6),
                            cedula = reader.IsDBNull(7) ? null : reader.GetValue(7).ToString()
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

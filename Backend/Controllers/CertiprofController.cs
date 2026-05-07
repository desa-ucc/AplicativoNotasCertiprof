using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Backend.Services;
using System.Threading.Tasks;
using System;

namespace Backend.Controllers
{
    using Microsoft.AspNetCore.Authorization;

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

        [HttpPost("parse-excel")]
        public async Task<IActionResult> ParseExcel(IFormFile file)
        {
            try
            {
                var records = await _fileProcessingService.ParseFileForPreviewAsync(file);
                return Ok(records);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("process-report")]
        public async Task<IActionResult> ProcessReport(IFormFile file, [FromForm] string courseCode, [FromForm] string certificationName)
        {
            try
            {
                // Retrieve user from token
                var uploadedBy = User.Identity?.Name ?? "Unknown_User";

                var uploadId = await _fileProcessingService.ProcessReportAsync(file, uploadedBy, courseCode, certificationName);

                return Ok(new { UploadId = uploadId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromServices] Backend.Repositories.IUploadHistoryRepository repository)
        {
            var user = User.Identity?.Name;
            var role = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            if (role == "Admin")
            {
                var allHistory = await repository.GetAllAsync();
                return Ok(allHistory);
            }
            else
            {
                var userHistory = await repository.GetByUsernameAsync(user ?? "");
                return Ok(userHistory);
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

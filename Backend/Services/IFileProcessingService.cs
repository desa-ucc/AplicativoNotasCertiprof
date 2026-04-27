using Microsoft.AspNetCore.Http;
using Backend.Models;

namespace Backend.Services
{
    public class ProcessResult
    {
        public byte[]? FileBytes { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public UploadHistory? History { get; set; }
    }

    public interface IFileProcessingService
    {
        Task<ProcessResult> ProcessReportAsync(IFormFile file, string uploadedBy);
    }
}

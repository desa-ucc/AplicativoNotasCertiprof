using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Backend.Models;
using Backend.Data;

namespace Backend.Services
{
    public class CertiprofCsvRecord
    {
        public string email { get; set; } = string.Empty;
        public string first_name { get; set; } = string.Empty;
        public string last_name { get; set; } = string.Empty;
        public string certification_name { get; set; } = string.Empty;
        public string notas { get; set; } = string.Empty;
    }

    public class FileProcessingService : IFileProcessingService
    {
        private readonly AppDbContext _dbContext;

        public FileProcessingService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ProcessResult> ProcessReportAsync(IFormFile file, string uploadedBy)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or not provided.");

            var records = new List<CertiprofCsvRecord>();

            // Parse CSV
            using (var stream = file.OpenReadStream())
            using (var reader = new StreamReader(stream))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true, MissingFieldFound = null, HeaderValidated = null }))
            {
                records = csv.GetRecords<CertiprofCsvRecord>().ToList();
            }

            // Filter specific course
            var filteredRecords = records
                .Where(r => r.certification_name.Contains("Generative AI Professional Certification", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var uploadHistory = new UploadHistory
            {
                UploadedBy = uploadedBy,
                CourseCode = "CE0501", // Mapped course code
                ProcessedRecordsCount = filteredRecords.Count,
                UploadDate = DateTime.UtcNow
            };

            foreach (var rec in filteredRecords)
            {
                uploadHistory.Records.Add(new CertiprofRecord
                {
                    Email = rec.email,
                    FirstName = rec.first_name,
                    LastName = rec.last_name,
                    CertificationName = rec.certification_name,
                    Grade = rec.notas
                });
            }

            // Save to DB
            _dbContext.UploadHistories.Add(uploadHistory);
            await _dbContext.SaveChangesAsync();

            // Generate Excel (Acta Auxiliar)
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Acta Auxiliar");

            // Add Headers
            worksheet.Cell(1, 1).Value = "Identificación (Email)";
            worksheet.Cell(1, 2).Value = "Nombre Completo";
            worksheet.Cell(1, 3).Value = "Nota Final";

            var row = 2;
            foreach (var rec in filteredRecords)
            {
                worksheet.Cell(row, 1).Value = rec.email;
                worksheet.Cell(row, 2).Value = $"{rec.first_name} {rec.last_name}";
                worksheet.Cell(row, 3).Value = rec.notas;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);

            return new ProcessResult
            {
                FileBytes = memoryStream.ToArray(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = $"Acta_Auxiliar_CE0501_{DateTime.Now:yyyyMMddHHmmss}.xlsx",
                History = uploadHistory
            };
        }
    }
}

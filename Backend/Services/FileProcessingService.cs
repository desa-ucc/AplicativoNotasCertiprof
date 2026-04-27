using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Backend.Models;
using Backend.Data;
using Backend.Repositories;

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
        private readonly IUploadHistoryRepository _repository;

        public FileProcessingService(IUploadHistoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<ProcessResult> ProcessReportAsync(IFormFile file, string uploadedBy, string courseCode, string certificationName)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or not provided.");

            if (string.IsNullOrWhiteSpace(courseCode) || string.IsNullOrWhiteSpace(certificationName))
                throw new ArgumentException("Course code and Certification name must be provided for intelligent filtering.");

            var records = new List<CertiprofCsvRecord>();

            // Parse CSV
            using (var stream = file.OpenReadStream())
            using (var reader = new StreamReader(stream))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true, MissingFieldFound = null, HeaderValidated = null }))
            {
                records = csv.GetRecords<CertiprofCsvRecord>().ToList();
            }

            // Intelligent Filtering based on user selection
            var filteredRecords = records
                .Where(r => !string.IsNullOrEmpty(r.certification_name) &&
                            r.certification_name.Contains(certificationName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var uploadHistory = new UploadHistory
            {
                UploadedBy = uploadedBy,
                CourseCode = courseCode,
                ProcessedRecordsCount = filteredRecords.Count,
                UploadDate = DateTime.UtcNow
            };

            foreach (var rec in filteredRecords)
            {
                // Normalization: trim spaces and handle casing
                var normalizedEmail = rec.email?.Trim().ToLowerInvariant() ?? "";
                var normalizedFirstName = rec.first_name?.Trim() ?? "";
                var normalizedLastName = rec.last_name?.Trim() ?? "";

                uploadHistory.Records.Add(new CertiprofRecord
                {
                    Email = normalizedEmail,
                    FirstName = normalizedFirstName,
                    LastName = normalizedLastName,
                    CertificationName = rec.certification_name?.Trim() ?? "",
                    Grade = rec.notas?.Trim() ?? ""
                });
            }

            // Save to DB via Repository
            await _repository.AddAsync(uploadHistory);
            await _repository.SaveChangesAsync();

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
                FileName = $"Acta_Auxiliar_{courseCode}_{DateTime.Now:yyyyMMddHHmmss}.xlsx",
                History = uploadHistory
            };
        }
    }
}

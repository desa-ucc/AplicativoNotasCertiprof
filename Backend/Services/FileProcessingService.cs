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

        public async Task<int> ProcessReportAsync(IFormFile file, string uploadedBy, string courseCode, string certificationName)
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
                ProcessedRecordsCount = filteredRecords.Count, // will be updated if upsert drops count, but we are saving to history the parsed items
                UploadDate = DateTime.UtcNow
            };

            foreach (var rec in filteredRecords)
            {
                // Normalization: trim spaces and handle casing
                var normalizedEmail = rec.email?.Trim().ToLowerInvariant() ?? "";
                var normalizedFirstName = rec.first_name?.Trim() ?? "";
                var normalizedLastName = rec.last_name?.Trim() ?? "";
                var normalizedCertName = rec.certification_name?.Trim() ?? "";

                // Parse the grade
                string rawGrade = rec.notas?.Trim() ?? "";
                decimal? finalGrade = null;

                if (decimal.TryParse(rawGrade, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedGrade))
                {
                    finalGrade = parsedGrade;
                }

                // Upsert logic inside the current UploadHistory (to avoid duplicates in the same batch)
                // Real DB upsert against existing records would require querying the DB.
                // For this demo, we ensure no duplicates within the same UploadHistory.
                var existingRecord = uploadHistory.Records.FirstOrDefault(r => r.Email == normalizedEmail && r.CertificationName == normalizedCertName);

                if (existingRecord != null)
                {
                    // Update existing
                    existingRecord.Grade = finalGrade;
                    existingRecord.FirstName = normalizedFirstName;
                    existingRecord.LastName = normalizedLastName;
                }
                else
                {
                    uploadHistory.Records.Add(new CertiprofRecord
                    {
                        Email = normalizedEmail,
                        FirstName = normalizedFirstName,
                        LastName = normalizedLastName,
                        CertificationName = normalizedCertName,
                        Grade = finalGrade
                    });
                }
            }

            uploadHistory.ProcessedRecordsCount = uploadHistory.Records.Count;

            // Save to DB via Repository
            await _repository.AddAsync(uploadHistory);
            await _repository.SaveChangesAsync();

            return uploadHistory.Id;
        }

        public async Task<ProcessResult> GenerateAvatarActAsync(int uploadId)
        {
            var uploadHistory = await _repository.GetByIdAsync(uploadId);
            if (uploadHistory == null)
            {
                throw new ArgumentException("Upload history not found.");
            }

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Acta Auxiliar");

            // Mapping Dictionary Logic
            string headerText = "Acta Auxiliar";
            string courseTitle = uploadHistory.CourseCode;

            // Simplified mapping logic based on prompt instructions
            if (uploadHistory.Records.Any() && uploadHistory.Records.First().CertificationName.Contains("Generative AI Professional Certification - GAIPC", StringComparison.OrdinalIgnoreCase))
            {
                courseTitle = "CE0501 – Fundamentos de IA Generativa";
            }

            worksheet.Cell(1, 1).Value = $"Curso: {courseTitle}";
            worksheet.Range("A1:C1").Merge();
            worksheet.Cell(1, 1).Style.Font.Bold = true;

            // Add Headers
            worksheet.Cell(2, 1).Value = "Identificación";
            worksheet.Cell(2, 2).Value = "Nombre Completo";
            worksheet.Cell(2, 3).Value = "Nota Final";
            worksheet.Range("A2:C2").Style.Font.Bold = true;

            var row = 3;
            foreach (var rec in uploadHistory.Records)
            {
                // Identification: fallback to FirstName if Email is empty
                string id = !string.IsNullOrWhiteSpace(rec.Email) ? rec.Email : $"{rec.FirstName} {rec.LastName}".Trim();

                worksheet.Cell(row, 1).Value = id;
                worksheet.Cell(row, 2).Value = $"{rec.FirstName} {rec.LastName}".Trim();

                if (rec.Grade.HasValue)
                {
                    worksheet.Cell(row, 3).Value = rec.Grade.Value;
                }
                else
                {
                    worksheet.Cell(row, 3).Value = ""; // Or "N/A"
                }

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);

            return new ProcessResult
            {
                FileBytes = memoryStream.ToArray(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = $"Acta_Auxiliar_{uploadHistory.CourseCode}_{DateTime.Now:yyyyMMddHHmmss}.xlsx",
                History = uploadHistory
            };
        }
    }
}

using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Backend.Models;
using Backend.Data;
using Backend.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Backend.Services
{
    public class CertiprofCsvRecord
    {
        public string email { get; set; } = string.Empty;
        public string first_name { get; set; } = string.Empty;
        public string last_name { get; set; } = string.Empty;
        public string certification_name { get; set; } = string.Empty;
        public string percentage { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public string created_at { get; set; } = string.Empty;
    }

    public class FileProcessingService : IFileProcessingService
    {
        private readonly IUploadHistoryRepository _repository;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public FileProcessingService(IUploadHistoryRepository repository, AppDbContext context, IConfiguration configuration)
        {
            _repository = repository;
            _context = context;
            _configuration = configuration;
        }

        public async Task<Dictionary<string, string>> ValidateEmailsAsync(List<string> emails)
        {
            var result = new Dictionary<string, string>();
            if (emails == null || !emails.Any()) return result;

            var queryTemplate = _configuration["LookupQuery"];
            if (string.IsNullOrWhiteSpace(queryTemplate))
                throw new Exception("LookupQuery is not configured.");

            using var command = _context.Database.GetDbConnection().CreateCommand();

            // We'll execute the query for each email individually to avoid SQL injection
            // and handle the lookup robustly based on the configured template.
            await _context.Database.OpenConnectionAsync();
            try
            {
                foreach (var email in emails.Distinct())
                {
                    command.CommandText = queryTemplate;
                    command.Parameters.Clear();

                    var emailParam = command.CreateParameter();
                    emailParam.ParameterName = "@email";
                    emailParam.Value = email;
                    command.Parameters.Add(emailParam);

                    using var reader = await command.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        var cedula = reader.GetString(0);
                        if (!string.IsNullOrWhiteSpace(cedula))
                        {
                            result[email] = cedula;
                        }
                    }
                }
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }

            return result;
        }

        public Task<List<CertiprofCsvRecord>> ParseExcelAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or not provided.");

            var records = new List<CertiprofCsvRecord>();

            using (var stream = file.OpenReadStream())
            using (var workbook = new XLWorkbook(stream))
            {
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header

                // Find column indices by header name
                var headerRow = worksheet.FirstRowUsed();
                var headers = headerRow.Cells().ToDictionary(c => c.Value.ToString().Trim().ToLowerInvariant(), c => c.Address.ColumnNumber);

                foreach (var row in rows)
                {
                    var record = new CertiprofCsvRecord();

                    if (headers.TryGetValue("status", out int statusCol)) record.status = row.Cell(statusCol).Value.ToString();
                    if (headers.TryGetValue("percentage", out int percentageCol)) record.percentage = row.Cell(percentageCol).Value.ToString();
                    if (headers.TryGetValue("first_name", out int fNameCol)) record.first_name = row.Cell(fNameCol).Value.ToString();
                    if (headers.TryGetValue("last_name", out int lNameCol)) record.last_name = row.Cell(lNameCol).Value.ToString();
                    if (headers.TryGetValue("email", out int emailCol)) record.email = row.Cell(emailCol).Value.ToString();
                    if (headers.TryGetValue("certification_name", out int certCol)) record.certification_name = row.Cell(certCol).Value.ToString();
                    if (headers.TryGetValue("created_at", out int createdCol)) record.created_at = row.Cell(createdCol).Value.ToString();

                    records.Add(record);
                }
            }

            return Task.FromResult(records);
        }

        public async Task<int> ProcessReportAsync(IFormFile file, string uploadedBy, string courseCode, string certificationName, Dictionary<string, string> emailToCedulaMap)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or not provided.");

            if (string.IsNullOrWhiteSpace(courseCode) || string.IsNullOrWhiteSpace(certificationName))
                throw new ArgumentException("Course code and Certification name must be provided for intelligent filtering.");

            var caseInsensitiveEmailMap = new Dictionary<string, string>(emailToCedulaMap ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);

            var records = await ParseExcelAsync(file);

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
                string rawGrade = rec.percentage?.Trim() ?? "";
                decimal? finalGrade = null;

                if (decimal.TryParse(rawGrade, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedGrade))
                {
                    finalGrade = parsedGrade;
                }

                string status = rec.status?.Trim() ?? "";

                // Try parse CreatedAt
                DateTime createdAt = DateTime.UtcNow;
                if (DateTime.TryParse(rec.created_at, out DateTime parsedDate))
                {
                    createdAt = parsedDate;
                }

                caseInsensitiveEmailMap.TryGetValue(normalizedEmail, out var cedula);

                // Upsert logic inside the current UploadHistory (to avoid duplicates in the same batch)
                // Real DB upsert against existing records would require querying the DB.
                // For this demo, we ensure no duplicates within the same UploadHistory.
                var existingRecord = uploadHistory.Records.FirstOrDefault(r => r.Email == normalizedEmail && r.CertificationName == normalizedCertName);

                if (existingRecord != null)
                {
                    // Update existing
                    existingRecord.Percentage = finalGrade;
                    existingRecord.FirstName = normalizedFirstName;
                    existingRecord.LastName = normalizedLastName;
                    existingRecord.Status = status;
                    existingRecord.CreatedAt = createdAt;
                    existingRecord.Cedula = cedula;
                }
                else
                {
                    uploadHistory.Records.Add(new CertiprofRecord
                    {
                        Email = normalizedEmail,
                        FirstName = normalizedFirstName,
                        LastName = normalizedLastName,
                        CertificationName = normalizedCertName,
                        Percentage = finalGrade,
                        Status = status,
                        CreatedAt = createdAt,
                        Cedula = cedula
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

                if (rec.Percentage.HasValue)
                {
                    worksheet.Cell(row, 3).Value = rec.Percentage.Value;
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

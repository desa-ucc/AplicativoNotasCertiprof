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
    using Microsoft.EntityFrameworkCore;
    using System.Linq;

    public class FileProcessingService : IFileProcessingService
    {
        private readonly IUploadHistoryRepository _repository;
        private readonly AppDbContext _dbContext;

        public FileProcessingService(IUploadHistoryRepository repository, AppDbContext dbContext)
        {
            _repository = repository;
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<object>> ParseFileForPreviewAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or not provided.");

            var records = new List<dynamic>();

            var extension = Path.GetExtension(file.FileName).ToLower();

            if (extension == ".csv")
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true, MissingFieldFound = null, HeaderValidated = null });
                var csvRecords = csv.GetRecords<dynamic>().ToList();

                foreach (IDictionary<string, object> rec in csvRecords)
                {
                    records.Add(MapRecord(rec));
                }
            }
            else if (extension == ".xlsx")
            {
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed().RowsUsed();

                var headerRow = rows.First();
                var headers = new List<string>();
                foreach (var cell in headerRow.Cells())
                {
                    headers.Add(cell.Value.ToString().ToLower().Trim());
                }

                foreach (var row in rows.Skip(1))
                {
                    var dict = new Dictionary<string, object>();
                    int colIdx = 1;
                    foreach (var header in headers)
                    {
                        dict[header] = row.Cell(colIdx).Value.ToString();
                        colIdx++;
                    }
                    records.Add(MapRecord(dict));
                }
            }
            else
            {
                throw new ArgumentException("Unsupported file type.");
            }

            // Database validation for institutional Cedula
            // Collect emails to fetch all related users in a single query to avoid N+1 query problem
            var emails = new List<string>();
            foreach (var rec in records)
            {
                string email = rec.email ?? "";
                if (!string.IsNullOrEmpty(email))
                {
                    emails.Add(email);
                }
            }

            var users = await _dbContext.Users.Where(u => u.Email != null && emails.Contains(u.Email)).ToDictionaryAsync(u => u.Email!);

            var result = new List<object>();
            foreach (var rec in records)
            {
                string email = rec.email ?? "";
                string cedula = "";

                if (!string.IsNullOrEmpty(email) && users.TryGetValue(email, out var user))
                {
                    cedula = user.Cedula ?? "";
                }

                result.Add(new
                {
                    status = rec.status,
                    percentage = rec.percentage,
                    first_name = rec.first_name,
                    last_name = rec.last_name,
                    email = rec.email,
                    certification_name = rec.certification_name,
                    created_at = rec.created_at,
                    cedula = cedula
                });
            }

            return result;
        }

        private dynamic MapRecord(IDictionary<string, object> rawRec)
        {
            return new
            {
                status = GetValueDict(rawRec, new[] { "status", "Estado" }),
                percentage = GetValueDict(rawRec, new[] { "percentage", "notas", "Notas", "Grade" }),
                first_name = GetValueDict(rawRec, new[] { "first_name", "first name", "Nombres" }),
                last_name = GetValueDict(rawRec, new[] { "last_name", "last name", "Apellidos" }),
                email = GetValueDict(rawRec, new[] { "email", "Email" }),
                certification_name = GetValueDict(rawRec, new[] { "certification_name", "certification name", "Certificación" }),
                created_at = GetValueDict(rawRec, new[] { "created_at", "created at", "Fecha de creación" })
            };
        }

        private string GetValueDict(IDictionary<string, object> dict, string[] keys)
        {
            foreach (var key in keys)
            {
                if (dict.ContainsKey(key))
                {
                    return dict[key]?.ToString() ?? string.Empty;
                }
                var caseInsensitiveKey = dict.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
                if (caseInsensitiveKey != null)
                {
                    return dict[caseInsensitiveKey]?.ToString() ?? string.Empty;
                }
            }
            return string.Empty;
        }

        public async Task<int> ProcessReportAsync(IFormFile file, string uploadedBy, string courseCode, string certificationName)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or not provided.");

            if (string.IsNullOrWhiteSpace(courseCode) || string.IsNullOrWhiteSpace(certificationName))
                throw new ArgumentException("Course code and Certification name must be provided for intelligent filtering.");

            var parsedObjects = await ParseFileForPreviewAsync(file);

            // Cast dynamic objects to a common dictionary shape
            var parsedDicts = parsedObjects.Select(o => {
                var props = o.GetType().GetProperties();
                var dict = new Dictionary<string, string>();
                foreach (var p in props)
                {
                    dict[p.Name] = p.GetValue(o)?.ToString() ?? "";
                }
                return dict;
            }).ToList();

            // Intelligent Filtering based on user selection
            var filteredRecords = parsedDicts
                .Where(r => r.ContainsKey("certification_name") && !string.IsNullOrEmpty(r["certification_name"]) &&
                            r["certification_name"].Contains(certificationName, StringComparison.OrdinalIgnoreCase))
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
                var normalizedEmail = rec.ContainsKey("email") ? rec["email"].Trim().ToLowerInvariant() : "";
                var normalizedFirstName = rec.ContainsKey("first_name") ? rec["first_name"].Trim() : "";
                var normalizedLastName = rec.ContainsKey("last_name") ? rec["last_name"].Trim() : "";
                var normalizedCertName = rec.ContainsKey("certification_name") ? rec["certification_name"].Trim() : "";
                var status = rec.ContainsKey("status") ? rec["status"].Trim() : "";
                var cedula = rec.ContainsKey("cedula") ? rec["cedula"].Trim() : "";

                // Parse the grade
                string rawGrade = rec.ContainsKey("percentage") ? rec["percentage"].Trim() : "";
                decimal? finalGrade = null;

                if (decimal.TryParse(rawGrade, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedGrade))
                {
                    finalGrade = parsedGrade;
                }

                // Only save the record if the user's Cedula was found
                if (!string.IsNullOrEmpty(cedula))
                {
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
                        existingRecord.Percentage = rawGrade;
                        existingRecord.Status = status;
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
                            Grade = finalGrade,
                            Percentage = rawGrade,
                            Status = status,
                            Cedula = cedula
                        });
                    }
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

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

                // ClosedXML rows/cols are 1-indexed. Sometimes RangeUsed can be sparse,
                // but Cells() provides the actual filled cells.
                // Best approach for a standard header row is to iterate by column index.
                int lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 1;

                for (int i = 1; i <= lastCol; i++)
                {
                    headers.Add(headerRow.Cell(i).Value.ToString().ToLower().Trim());
                }

                foreach (var row in rows.Skip(1))
                {
                    var dict = new Dictionary<string, object>();
                    for (int i = 1; i <= lastCol; i++)
                    {
                        var header = headers[i - 1];
                        dict[header] = row.Cell(i).Value.ToString();
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
            var result = new List<object>();

            var emails = new List<string>();
            foreach (var rec in records)
            {
                string email = rec.email ?? "";
                if (!string.IsNullOrEmpty(email))
                {
                    emails.Add(email);
                }
            }

            var cedulasDict = new Dictionary<string, string>();

            if (emails.Any())
            {
                var parameters = new List<string>();
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    for (int i = 0; i < emails.Count; i++)
                    {
                        var p = command.CreateParameter();
                        p.ParameterName = $"@p{i}";
                        p.Value = emails[i];
                        command.Parameters.Add(p);
                        parameters.Add($"@p{i}");
                    }

                    var emailListStr = string.Join(",", parameters);
                    command.CommandText = $"SELECT m12emi, M12CAR FROM M12ARC WHERE m12emi IN ({emailListStr})";

                    _dbContext.Database.OpenConnection();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            cedulasDict[reader.GetString(0)] = reader.GetString(1);
                        }
                    }
                }

                var missingEmails = emails.Where(e => !cedulasDict.ContainsKey(e)).ToList();
                if (missingEmails.Any())
                {
                    var missingParameters = new List<string>();
                    using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                    {
                        for (int i = 0; i < missingEmails.Count; i++)
                        {
                            var p = command.CreateParameter();
                            p.ParameterName = $"@mp{i}";
                            p.Value = missingEmails[i];
                            command.Parameters.Add(p);
                            missingParameters.Add($"@mp{i}");
                        }

                        var missingEmailListStr = string.Join(",", missingParameters);
                        command.CommandText = $"SELECT pla20emi, pla20ced FROM PLA20ARC WHERE pla20emi IN ({missingEmailListStr})";

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                cedulasDict[reader.GetString(0)] = reader.GetString(1);
                            }
                        }
                    }
                }
            }

            foreach (var rec in records)
            {
                string email = rec.email ?? "";
                string cedula = "No está dentro del registro";

                if (!string.IsNullOrEmpty(email) && cedulasDict.TryGetValue(email, out var foundCedula))
                {
                    cedula = foundCedula;
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

            // Directly parsing the file avoiding the N+1 query validation again,
            // as this is specifically the DB persistence step and we can validate on-the-fly.
            var records = new List<dynamic>();
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (extension == ".csv")
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true, MissingFieldFound = null, HeaderValidated = null });
                var csvRecords = csv.GetRecords<dynamic>().ToList();
                foreach (IDictionary<string, object> rec in csvRecords) records.Add(MapRecord(rec));
            }
            else if (extension == ".xlsx")
            {
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed().RowsUsed();

                var headerRow = rows.First();
                var headers = new List<string>();
                int lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 1;

                for (int i = 1; i <= lastCol; i++)
                {
                    headers.Add(headerRow.Cell(i).Value.ToString().ToLower().Trim());
                }

                foreach (var row in rows.Skip(1))
                {
                    var dict = new Dictionary<string, object>();
                    for (int i = 1; i <= lastCol; i++)
                    {
                        var header = headers[i - 1];
                        dict[header] = row.Cell(i).Value.ToString();
                    }
                    records.Add(MapRecord(dict));
                }
            }

            var emails = new List<string>();
            foreach (var rec in records)
            {
                string email = rec.email ?? "";
                if (!string.IsNullOrEmpty(email)) emails.Add(email);
            }

            var cedulasDict = new Dictionary<string, string>();

            if (emails.Any())
            {
                var parameters = new List<string>();
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    for (int i = 0; i < emails.Count; i++)
                    {
                        var p = command.CreateParameter();
                        p.ParameterName = $"@p{i}";
                        p.Value = emails[i];
                        command.Parameters.Add(p);
                        parameters.Add($"@p{i}");
                    }

                    var emailListStr = string.Join(",", parameters);
                    command.CommandText = $"SELECT m12emi, M12CAR FROM M12ARC WHERE m12emi IN ({emailListStr})";

                    _dbContext.Database.OpenConnection();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read()) cedulasDict[reader.GetString(0)] = reader.GetString(1);
                    }
                }

                var missingEmails = emails.Where(e => !cedulasDict.ContainsKey(e)).ToList();
                if (missingEmails.Any())
                {
                    var missingParameters = new List<string>();
                    using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                    {
                        for (int i = 0; i < missingEmails.Count; i++)
                        {
                            var p = command.CreateParameter();
                            p.ParameterName = $"@mp{i}";
                            p.Value = missingEmails[i];
                            command.Parameters.Add(p);
                            missingParameters.Add($"@mp{i}");
                        }

                        var missingEmailListStr = string.Join(",", missingParameters);
                        command.CommandText = $"SELECT pla20emi, pla20ced FROM PLA20ARC WHERE pla20emi IN ({missingEmailListStr})";

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read()) cedulasDict[reader.GetString(0)] = reader.GetString(1);
                        }
                    }
                }
            }

            var uploadHistory = new UploadHistory
            {
                UploadedBy = uploadedBy,
                CourseCode = courseCode,
                ProcessedRecordsCount = records.Count, // will be updated if upsert drops count
                UploadDate = DateTime.UtcNow
            };

            foreach (var rec in records)
            {
                // Normalization: trim spaces and handle casing
                var normalizedEmail = (rec.email ?? "").Trim().ToLowerInvariant();
                var normalizedFirstName = (rec.first_name ?? "").Trim();
                var normalizedLastName = (rec.last_name ?? "").Trim();
                var normalizedCertName = (rec.certification_name ?? "").Trim();
                var status = (rec.status ?? "").Trim();
                var cedula = cedulasDict.ContainsKey(rec.email ?? "") ? cedulasDict[rec.email ?? ""] : "";

                // Parse the grade
                string rawGrade = (rec.percentage ?? "").Trim();
                decimal? finalGrade = null;

                if (decimal.TryParse(rawGrade, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedGrade))
                {
                    finalGrade = parsedGrade;
                }

                // Only save the record if the user's Cedula was found
                if (!string.IsNullOrEmpty(cedula))
                {
                    var existingRecord = uploadHistory.Records.FirstOrDefault(r => r.Email == normalizedEmail && r.CertificationName == normalizedCertName);

                    if (existingRecord != null)
                    {
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
            // Save history record first
            await _repository.AddAsync(uploadHistory);
            await _repository.SaveChangesAsync();

            // We use direct SQL insert for cert_registros as HasNoKey makes it hard for EF Core Tracking to insert it as a child collection
            foreach (var record in uploadHistory.Records)
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO cert_registros (cert_status, cert_percentage, cert_first_name, cert_last_name, cert_email, cert_certification_name, cert_created_at, cert_cedula, cert_upload_history_id)
                        VALUES (@status, @percentage, @first_name, @last_name, @email, @certification_name, @created_at, @cedula, @upload_id)";

                    var p1 = command.CreateParameter(); p1.ParameterName = "@status"; p1.Value = (object)record.Status ?? DBNull.Value; command.Parameters.Add(p1);
                    var p2 = command.CreateParameter(); p2.ParameterName = "@percentage"; p2.Value = (object)record.Percentage ?? DBNull.Value; command.Parameters.Add(p2);
                    var p3 = command.CreateParameter(); p3.ParameterName = "@first_name"; p3.Value = (object)record.FirstName ?? DBNull.Value; command.Parameters.Add(p3);
                    var p4 = command.CreateParameter(); p4.ParameterName = "@last_name"; p4.Value = (object)record.LastName ?? DBNull.Value; command.Parameters.Add(p4);
                    var p5 = command.CreateParameter(); p5.ParameterName = "@email"; p5.Value = (object)record.Email ?? DBNull.Value; command.Parameters.Add(p5);
                    var p6 = command.CreateParameter(); p6.ParameterName = "@certification_name"; p6.Value = (object)record.CertificationName ?? DBNull.Value; command.Parameters.Add(p6);
                    var p7 = command.CreateParameter(); p7.ParameterName = "@created_at"; p7.Value = record.CreatedAt; command.Parameters.Add(p7);
                    var p8 = command.CreateParameter(); p8.ParameterName = "@cedula"; p8.Value = (object)record.Cedula ?? DBNull.Value; command.Parameters.Add(p8);
                    var p9 = command.CreateParameter(); p9.ParameterName = "@upload_id"; p9.Value = uploadHistory.Id; command.Parameters.Add(p9);

                    if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        _dbContext.Database.OpenConnection();
                    }
                    await command.ExecuteNonQueryAsync();
                }
            }

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

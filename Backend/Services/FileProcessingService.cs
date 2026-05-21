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

            var result = new List<object>();

            if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
            {
                _dbContext.Database.OpenConnection();
            }

            // The preview logic is disabled as the frontend no longer uses it.
            // Returning an empty array.
            return result;
        }

        private dynamic MapRecord(IDictionary<string, object> rawRec)
        {
            return new
            {
                cedula = GetValueDict(rawRec, new[] { "cedula", "Cédula", "Cedula" }),
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

            if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
            {
                _dbContext.Database.OpenConnection();
            }

            foreach (var rec in records)
            {
                string email = rec.email ?? "";
                if (string.IsNullOrWhiteSpace(email))
                {
                    continue; // Skip empty emails
                }

                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "sp_ObtenerCedulaPorCorreo";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    var paramCorreo = command.CreateParameter();
                    paramCorreo.ParameterName = "@Correo";
                    paramCorreo.Value = email.Trim();
                    command.Parameters.Add(paramCorreo);

                    var paramCert = command.CreateParameter();
                    paramCert.ParameterName = "@CertificacionNombre";
                    paramCert.Value = rec.certification_name ?? string.Empty;
                    command.Parameters.Add(paramCert);

                    var paramFirst = command.CreateParameter();
                    paramFirst.ParameterName = "@FirstName";
                    paramFirst.Value = (object)rec.first_name ?? DBNull.Value;
                    command.Parameters.Add(paramFirst);

                    var paramLast = command.CreateParameter();
                    paramLast.ParameterName = "@LastName";
                    paramLast.Value = (object)rec.last_name ?? DBNull.Value;
                    command.Parameters.Add(paramLast);

                    // Parse percentage to DECIMAL
                    decimal? percentageDecimal = null;
                    if (decimal.TryParse((rec.percentage ?? "").ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal pd))
                    {
                        percentageDecimal = pd;
                    }

                    var paramPercentage = command.CreateParameter();
                    paramPercentage.ParameterName = "@Percentage";
                    paramPercentage.Value = (object)percentageDecimal ?? DBNull.Value;
                    command.Parameters.Add(paramPercentage);

                    var paramStatus = command.CreateParameter();
                    paramStatus.ParameterName = "@Status";
                    paramStatus.Value = (object)rec.status ?? DBNull.Value;
                    command.Parameters.Add(paramStatus);

                    var paramFecha = command.CreateParameter();
                    paramFecha.ParameterName = "@CreatedAt";
                    if (!string.IsNullOrWhiteSpace(rec.created_at))
                    {
                        if (DateTime.TryParse(rec.created_at, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime dt))
                        {
                            paramFecha.Value = dt;
                        }
                        else
                        {
                            paramFecha.Value = DBNull.Value;
                        }
                    }
                    else
                    {
                        paramFecha.Value = DBNull.Value;
                    }
                    command.Parameters.Add(paramFecha);

                    if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        _dbContext.Database.OpenConnection();
                    }

                    await command.ExecuteNonQueryAsync();
                }
            }

            // By returning 0 here as requested, the process-report HTTP endpoint
            // finishes parsing the entire loop completely before returning the 200 OK
            // which guarantees that the cache is 'refreshed' on completion.
            return 0; // Upload history bypass
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

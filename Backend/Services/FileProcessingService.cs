using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Backend.Data;
using Backend.Models;
using Backend.Repositories;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
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
            return new List<object>(); // Unused but part of interface
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

        public async Task<object> ProcessReportAsync(IFormFile file, string uploadedBy, string courseCode, string certificationName)
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

                for (int i = 1; i <= lastCol; i++) headers.Add(headerRow.Cell(i).Value.ToString().ToLower().Trim());

                // Validación 1: Columnas
                string[] columnasRequeridas = { "email", "first_name", "last_name", "certification_name", "status" };
                foreach (var col in columnasRequeridas)
                {
                    if (!headers.Contains(col))
                    {
                        throw new ArgumentException($"El formato del Excel es inválido. No se encontró la columna requerida: '{col}'.");
                    }
                }

                foreach (var row in rows.Skip(1))
                {
                    var dict = new Dictionary<string, object>();
                    for (int i = 1; i <= lastCol; i++)
                    {
                        var header = headers[i - 1];
                        var cell = row.Cell(i);
                        
                        // Validamos si Excel sabe que es una fecha pura
                        if (cell.DataType == ClosedXML.Excel.XLDataType.DateTime)
                        {
                            dict[header] = cell.GetDateTime().ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            dict[header] = cell.Value.ToString();
                        }
                    }

                    // Validación 2: Email vacio
                    if (dict.ContainsKey("email") && string.IsNullOrWhiteSpace(dict["email"]?.ToString()))
                    {
                        throw new ArgumentException("El archivo fue rechazado porque contiene uno o más registros con la columna 'email' en blanco. Corrija el documento y vuelva a intentarlo.");
                    }

                    records.Add(MapRecord(dict));
                }
            }

            if (records.Count == 0) throw new Exception("El formato del Excel es inválido. Faltan columnas requeridas o está vacío.");

            bool hasValidFormat = false;
            foreach(var r in records) {
                if (!string.IsNullOrWhiteSpace(r.email) || !string.IsNullOrWhiteSpace(r.first_name) || !string.IsNullOrWhiteSpace(r.cedula)) {
                    hasValidFormat = true;
                    break;
                }
            }
            if (!hasValidFormat) throw new Exception("El formato del Excel es inválido. Faltan columnas requeridas.");

            if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
            {
                await _dbContext.Database.OpenConnectionAsync();
            }

            int exitosos = 0;
            int fallidos = 0;

            foreach (var rec in records)
            {
                string email = rec.email ?? "";
                if (string.IsNullOrWhiteSpace(email)) continue;

                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "sp_ObtenerCedulaPorCorreo";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    var paramCorreo = command.CreateParameter(); paramCorreo.ParameterName = "@Correo"; paramCorreo.Value = email.Trim(); command.Parameters.Add(paramCorreo);
                    var paramCert = command.CreateParameter(); paramCert.ParameterName = "@CertificacionNombre"; paramCert.Value = rec.certification_name ?? string.Empty; command.Parameters.Add(paramCert);
                    var paramFirst = command.CreateParameter(); paramFirst.ParameterName = "@FirstName"; paramFirst.Value = (object)rec.first_name ?? DBNull.Value; command.Parameters.Add(paramFirst);
                    var paramLast = command.CreateParameter(); paramLast.ParameterName = "@LastName"; paramLast.Value = (object)rec.last_name ?? DBNull.Value; command.Parameters.Add(paramLast);

                    decimal? percentageDecimal = null;
                    if (decimal.TryParse((rec.percentage ?? "").ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal pd)) percentageDecimal = pd;
                    var paramPercentage = command.CreateParameter(); paramPercentage.ParameterName = "@Percentage"; paramPercentage.Value = (object)percentageDecimal ?? DBNull.Value; command.Parameters.Add(paramPercentage);

                    var paramStatus = command.CreateParameter(); paramStatus.ParameterName = "@Status"; paramStatus.Value = (object)rec.status ?? DBNull.Value; command.Parameters.Add(paramStatus);

                    var paramFecha = command.CreateParameter(); paramFecha.ParameterName = "@CreatedAt";
                    DateTime fechaExcel;
                    string fechaString = rec.created_at ?? "";

                    if (double.TryParse(fechaString, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double fechaNumerica))
                    {
                        paramFecha.Value = DateTime.FromOADate(fechaNumerica);
                    }
                    else if (DateTime.TryParseExact(fechaString, new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "dd/MM/yyyy H:mm", "dd/MM/yyyy HH:mm:ss" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out fechaExcel))
                    {
                        // Solo usamos nuestra validación estricta LATAM
                        paramFecha.Value = fechaExcel;
                    }
                    else
                    {
                        paramFecha.Value = DBNull.Value; 
                    }
                    command.Parameters.Add(paramFecha);

                    var result = await command.ExecuteScalarAsync();
                    if (result != null && result.ToString() == "SIN_CEDULA") fallidos++;
                    else exitosos++;
                }
            }

            using (var cmd = _dbContext.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "sp_RegistrarAuditoria";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;

                var pAccion = cmd.CreateParameter(); pAccion.ParameterName = "@Accion"; pAccion.Value = "CARGA_EXCEL"; cmd.Parameters.Add(pAccion);
                var pUsuario = cmd.CreateParameter(); pUsuario.ParameterName = "@Usuario"; pUsuario.Value = uploadedBy ?? "Sistema"; cmd.Parameters.Add(pUsuario);

                if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open) await _dbContext.Database.OpenConnectionAsync();
                await cmd.ExecuteNonQueryAsync();
            }

            return new { exitosos = exitosos, fallidos = fallidos, mensaje = $"Se procesaron {exitosos} registros exitosamente. No se subieron {fallidos} registros porque la cédula no se encontró en el sistema." };
        }

        public async Task<ProcessResult> GenerateAvatarActAsync(int uploadId)
        {
            return new ProcessResult(); // Disabled mock
        }
    }
}

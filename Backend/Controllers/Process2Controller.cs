using System;
using System.Linq;
using System.Threading.Tasks;
using Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class Process2Controller : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public Process2Controller(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [Backend.Attributes.PermissionAuthorize("/process2")]
        [HttpGet]
        public async Task<IActionResult> GetProcess2Records()
        {
            var records = new System.Collections.Generic.List<object>();
            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    // Reusing the same history procedure for process 2 view as requested
                    command.CommandText = "sp_ConsultarHistorialCertificaciones";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        await _dbContext.Database.OpenConnectionAsync();
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            decimal? percentageVal = null;
                            if (!reader.IsDBNull(reader.GetOrdinal("percentage")))
                            {
                                var rawPct = reader.GetValue(reader.GetOrdinal("percentage"));
                                if (decimal.TryParse(rawPct.ToString(), out decimal parsedPct))
                                {
                                    percentageVal = parsedPct;
                                }
                            }

                            records.Add(new
                            {
                                id = reader.IsDBNull(reader.GetOrdinal("id")) ? 0 : reader.GetInt32(reader.GetOrdinal("id")),
                                cedula = reader.IsDBNull(reader.GetOrdinal("cedula")) ? null : reader.GetValue(reader.GetOrdinal("cedula")).ToString(),
                                email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetValue(reader.GetOrdinal("email")).ToString(),
                                certification_name = reader.IsDBNull(reader.GetOrdinal("certification_name")) ? null : reader.GetValue(reader.GetOrdinal("certification_name")).ToString(),
                                first_name = reader.IsDBNull(reader.GetOrdinal("first_name")) ? null : reader.GetValue(reader.GetOrdinal("first_name")).ToString(),
                                last_name = reader.IsDBNull(reader.GetOrdinal("last_name")) ? null : reader.GetValue(reader.GetOrdinal("last_name")).ToString(),
                                percentage = percentageVal,
                                status = reader.IsDBNull(reader.GetOrdinal("status")) ? null : reader.GetValue(reader.GetOrdinal("status")).ToString(),
                                created_at = reader.IsDBNull(reader.GetOrdinal("created_at")) ? (System.DateTime?)null : reader.GetDateTime(reader.GetOrdinal("created_at")),
                                updated_at = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? (System.DateTime?)null : reader.GetDateTime(reader.GetOrdinal("updated_at"))
                            });
                        }
                    }
                }
                return Ok(records);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        public class EditRecordRequestProcess2
        {
            [System.Text.Json.Serialization.JsonPropertyName("id")]
            public int Id { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("first_name")]
            public string? FirstName { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("last_name")]
            public string? LastName { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("percentage")]
            public decimal? Percentage { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("status")]
            public string? Status { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("certification_name")]
            public string? CertificationName { get; set; }
        }

        [Backend.Attributes.PermissionAuthorize("/process2")]
        [HttpPost("edit")]
        public async Task<IActionResult> EditProcess2Record([FromBody] EditRecordRequestProcess2 request)
        {
            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "sp_EditarRegistroCertificacion";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    var pId = command.CreateParameter(); pId.ParameterName = "@Id"; pId.Value = request.Id; command.Parameters.Add(pId);
                    var pFirst = command.CreateParameter(); pFirst.ParameterName = "@FirstName"; pFirst.Value = (object)request.FirstName ?? DBNull.Value; command.Parameters.Add(pFirst);
                    var pLast = command.CreateParameter(); pLast.ParameterName = "@LastName"; pLast.Value = (object)request.LastName ?? DBNull.Value; command.Parameters.Add(pLast);
                    var pPct = command.CreateParameter(); pPct.ParameterName = "@Percentage"; pPct.Value = (object)request.Percentage ?? DBNull.Value; command.Parameters.Add(pPct);
                    var pStatus = command.CreateParameter(); pStatus.ParameterName = "@Status"; pStatus.Value = (object)request.Status ?? DBNull.Value; command.Parameters.Add(pStatus);
                    var pCert = command.CreateParameter(); pCert.ParameterName = "@CertificationName"; pCert.Value = (object)request.CertificationName ?? DBNull.Value; command.Parameters.Add(pCert);

                    if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        await _dbContext.Database.OpenConnectionAsync();
                    }

                    await command.ExecuteNonQueryAsync();
                }

                return Ok(new { Message = "Registro actualizado exitosamente en Proceso 2." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SecurityController : ControllerBase
    {
        private readonly Data.AppDbContext _dbContext;

        public SecurityController(Data.AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

                public class CreateRoleRequest
        {
            public string NombreRol { get; set; } = string.Empty;
            public List<int> ModuleIds { get; set; } = new List<int>();
        }

        [HttpPost("roles")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NombreRol))
            {
                return BadRequest(new { Message = "El nombre del rol es requerido." });
            }

            try
            {
                if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                {
                    await _dbContext.Database.OpenConnectionAsync();
                }

                using (var transaction = await _dbContext.Database.GetDbConnection().BeginTransactionAsync())
                {
                    try
                    {
                        int newRoleId;

                        // Step A: Insert into cert_roles
                        using (var insertRoleCmd = _dbContext.Database.GetDbConnection().CreateCommand())
                        {
                            insertRoleCmd.Transaction = transaction;
                            insertRoleCmd.CommandText = "INSERT INTO cert_roles (nombre_rol, activo) VALUES (@NombreRol, 1); SELECT SCOPE_IDENTITY();";
                            insertRoleCmd.CommandType = System.Data.CommandType.Text;

                            var pName = insertRoleCmd.CreateParameter();
                            pName.ParameterName = "@NombreRol";
                            pName.Value = request.NombreRol;
                            insertRoleCmd.Parameters.Add(pName);

                            newRoleId = Convert.ToInt32(await insertRoleCmd.ExecuteScalarAsync());
                        }

                        // Step B: Insert permissions into cert_permisos
                        if (request.ModuleIds != null && request.ModuleIds.Count > 0)
                        {
                            foreach (var moduleId in request.ModuleIds)
                            {
                                using (var insertPermCmd = _dbContext.Database.GetDbConnection().CreateCommand())
                                {
                                    insertPermCmd.Transaction = transaction;
                                    insertPermCmd.CommandText = "INSERT INTO cert_permisos (rol_id, modulo_id) VALUES (@RolId, @ModuloId);";
                                    insertPermCmd.CommandType = System.Data.CommandType.Text;

                                    var pRolId = insertPermCmd.CreateParameter();
                                    pRolId.ParameterName = "@RolId";
                                    pRolId.Value = newRoleId;
                                    insertPermCmd.Parameters.Add(pRolId);

                                    var pModId = insertPermCmd.CreateParameter();
                                    pModId.ParameterName = "@ModuloId";
                                    pModId.Value = moduleId;
                                    insertPermCmd.Parameters.Add(pModId);

                                    await insertPermCmd.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        await transaction.CommitAsync();
                        return Ok(new { Message = "Rol y permisos creados exitosamente." });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { Message = "Error al crear el rol: " + ex.Message });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error interno del servidor: " + ex.Message });
            }
        }

        [HttpGet("modules")]
        public async Task<IActionResult> GetModules()
        {
            var records = new List<object>();
            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "sp_ListarTodosLosModulos";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        await _dbContext.Database.OpenConnectionAsync();
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            records.Add(new
                            {
                                id = reader.GetInt32(reader.GetOrdinal("Id")),
                                nombre = reader.GetString(reader.GetOrdinal("NombreModulo")),
                                ruta = reader.GetString(reader.GetOrdinal("RutaModulo"))
                            });
                        }
                    }
                }
                return Ok(records);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Could not find stored procedure"))
                {
                    return Ok(new[]
                    {
                        new { id = 1, nombre = "Cargar Archivo", ruta = "/upload" },
                        new { id = 2, nombre = "Historial", ruta = "/history" },
                        new { id = 3, nombre = "Seguridad", ruta = "/security" }
                    });
                }
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}

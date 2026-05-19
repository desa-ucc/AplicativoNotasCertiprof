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
                                name = reader.GetString(reader.GetOrdinal("NombreModulo")),
                                path = reader.GetString(reader.GetOrdinal("RutaModulo"))
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
                        new { id = 1, name = "Cargar Archivo", path = "/upload" },
                        new { id = 2, name = "Historial", path = "/history" },
                        new { id = 3, name = "Seguridad", path = "/security" }
                    });
                }
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("roles/{roleId}/modules")]
        public async Task<IActionResult> GetRoleModules(int roleId)
        {
            var records = new List<object>();
            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "sp_ObtenerMenuPorRol";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    var pRol = command.CreateParameter();
                    pRol.ParameterName = "@RolId";
                    pRol.Value = roleId;
                    command.Parameters.Add(pRol);

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
                                id = reader.GetInt32(reader.GetOrdinal("ModuloId")),
                                name = reader.GetString(reader.GetOrdinal("NombreModulo")),
                                path = reader.GetString(reader.GetOrdinal("RutaFrontEnd"))
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
                      return Ok(new List<object>()); // Return empty list to prevent frontend crash on missing SP
                 }
                 return BadRequest(new { Message = ex.Message });
            }
        }

        public class UpdatePermissionsRequest
        {
            public List<int> ModuleIds { get; set; } = new List<int>();
        }

        [HttpPut("roles/{roleId}/permissions")]
        public async Task<IActionResult> UpdateRolePermissions(int roleId, [FromBody] UpdatePermissionsRequest request)
        {
            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "sp_ActualizarPermisosRol";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    var pRol = command.CreateParameter();
                    pRol.ParameterName = "@RolId";
                    pRol.Value = roleId;
                    command.Parameters.Add(pRol);

                    var pModulos = command.CreateParameter();
                    pModulos.ParameterName = "@ModulosIds";
                    pModulos.Value = string.Join(",", request.ModuleIds);
                    command.Parameters.Add(pModulos);

                    if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        await _dbContext.Database.OpenConnectionAsync();
                    }

                    await command.ExecuteNonQueryAsync();
                }

                return Ok(new { Message = "Permisos actualizados correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}

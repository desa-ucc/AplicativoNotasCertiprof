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


        public class UpdatePermissionsRequest
        {
            public List<int> ModuleIds { get; set; } = new List<int>();
        }

        [HttpPut("roles/{id}/permissions")]
        public async Task<IActionResult> UpdatePermissions(int id, [FromBody] UpdatePermissionsRequest request)
        {
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
                        // Step 1: Delete existing permissions
                        using (var cmdDelete = _dbContext.Database.GetDbConnection().CreateCommand())
                        {
                            cmdDelete.Transaction = transaction;
                            cmdDelete.CommandText = "DELETE FROM cert_permisos WHERE rol_id = @RolId";
                            cmdDelete.CommandType = System.Data.CommandType.Text;

                            var pRolId = cmdDelete.CreateParameter();
                            pRolId.ParameterName = "@RolId";
                            pRolId.Value = id;
                            cmdDelete.Parameters.Add(pRolId);

                            await cmdDelete.ExecuteNonQueryAsync();
                        }

                        // Step 2: Insert new permissions
                        if (request.ModuleIds != null && request.ModuleIds.Count > 0)
                        {
                            foreach (int moduloId in request.ModuleIds)
                            {
                                using (var cmdInsert = _dbContext.Database.GetDbConnection().CreateCommand())
                                {
                                    cmdInsert.Transaction = transaction;
                                    cmdInsert.CommandText = "INSERT INTO cert_permisos (rol_id, modulo_id) VALUES (@RolId, @ModuloId)";
                                    cmdInsert.CommandType = System.Data.CommandType.Text;

                                    var pRolId = cmdInsert.CreateParameter();
                                    pRolId.ParameterName = "@RolId";
                                    pRolId.Value = id;
                                    cmdInsert.Parameters.Add(pRolId);

                                    var pModId = cmdInsert.CreateParameter();
                                    pModId.ParameterName = "@ModuloId";
                                    pModId.Value = moduloId;
                                    cmdInsert.Parameters.Add(pModId);

                                    await cmdInsert.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        await transaction.CommitAsync();
                        return Ok(new { message = "Permisos actualizados correctamente" });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return StatusCode(500, new { message = "Error interno", detail = ex.Message });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        [HttpGet("roles/{id}/modules")]
        public async Task<IActionResult> GetRoleModules(int id)
        {
            var records = new List<object>();
            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT m.id, m.nombre FROM cert_modulos m JOIN cert_permisos p ON m.id = p.modulo_id WHERE p.rol_id = @RolId";
                    command.CommandType = System.Data.CommandType.Text;

                    var pRolId = command.CreateParameter();
                    pRolId.ParameterName = "@RolId";
                    pRolId.Value = id;
                    command.Parameters.Add(pRolId);

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
                                id = reader.GetInt32(reader.GetOrdinal("id")),
                                nombre = reader.GetString(reader.GetOrdinal("nombre"))
                            });
                        }
                    }
                }
                return Ok(records);
            }
            catch (Exception ex)
            {
                // Simple mock fallback if tables don't exist in dev envs
                if (ex.Message.Contains("Invalid object name"))
                {
                    return Ok(new[] { new { id = 1, nombre = "Cargar Archivo" }, new { id = 2, nombre = "Historial" } });
                }
                return BadRequest(new { Message = ex.Message });
            }
        }


        public class EditRoleRequest
        {
            public string Nombre { get; set; } = string.Empty;
        }

        [HttpPut("roles/{id}")]
        public async Task<IActionResult> EditRole(int id, [FromBody] EditRoleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Nombre)) return BadRequest(new { Message = "El nombre no puede estar vacío." });
            try
            {
                if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                {
                    await _dbContext.Database.OpenConnectionAsync();
                }

                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "UPDATE cert_roles SET nombre_rol = @Nombre WHERE id = @Id";
                    command.CommandType = System.Data.CommandType.Text;

                    var pName = command.CreateParameter();
                    pName.ParameterName = "@Nombre";
                    pName.Value = request.Nombre;
                    command.Parameters.Add(pName);

                    var pId = command.CreateParameter();
                    pId.ParameterName = "@Id";
                    pId.Value = id;
                    command.Parameters.Add(pId);

                    await command.ExecuteNonQueryAsync();
                }
                return Ok(new { Message = "Rol actualizado" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("roles/{id}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
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
                        // 1. Delete from cert_permisos
                        using (var cmdPerms = _dbContext.Database.GetDbConnection().CreateCommand())
                        {
                            cmdPerms.Transaction = transaction;
                            cmdPerms.CommandText = "DELETE FROM cert_permisos WHERE rol_id = @Id";
                            cmdPerms.CommandType = System.Data.CommandType.Text;
                            var pId1 = cmdPerms.CreateParameter(); pId1.ParameterName = "@Id"; pId1.Value = id; cmdPerms.Parameters.Add(pId1);
                            await cmdPerms.ExecuteNonQueryAsync();
                        }

                        // 2. Delete from cert_roles
                        using (var cmdRoles = _dbContext.Database.GetDbConnection().CreateCommand())
                        {
                            cmdRoles.Transaction = transaction;
                            cmdRoles.CommandText = "DELETE FROM cert_roles WHERE id = @Id";
                            cmdRoles.CommandType = System.Data.CommandType.Text;
                            var pId2 = cmdRoles.CreateParameter(); pId2.ParameterName = "@Id"; pId2.Value = id; cmdRoles.Parameters.Add(pId2);
                            await cmdRoles.ExecuteNonQueryAsync();
                        }

                        await transaction.CommitAsync();
                        return Ok(new { Message = "Rol eliminado correctamente." });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { Message = "No se pudo eliminar el rol. " + ex.Message });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error interno del servidor", Detail = ex.Message });
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
                    command.CommandText = "sp_ObtenerModulos";
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
                                id = reader.GetInt32(reader.GetOrdinal("id")),
                                nombre = reader.GetString(reader.GetOrdinal("nombre")),
                                ruta = reader.GetString(reader.GetOrdinal("ruta")),
                                icono = reader.IsDBNull(reader.GetOrdinal("icono")) ? null : reader.GetString(reader.GetOrdinal("icono"))
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
                        new { id = 1, nombre = "Cargar Archivo", ruta = "/upload", icono = "upload_file" },
                        new { id = 2, nombre = "Historial", ruta = "/history", icono = "history" },
                        new { id = 3, nombre = "Seguridad", ruta = "/security", icono = "security" }
                    });
                }
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}

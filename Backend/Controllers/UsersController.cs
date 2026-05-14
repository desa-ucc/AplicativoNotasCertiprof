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
    [Authorize(Policy = "AdminPolicy")]
    public class UsersController : ControllerBase
    {
        private readonly Data.AppDbContext _dbContext;

        public UsersController(Data.AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var records = new List<object>();
            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT u.id, u.username, u.rol_id, r.nombre_rol as role_name FROM cert_usuarios u JOIN cert_roles r ON u.rol_id = r.id";

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
                                username = reader.GetString(reader.GetOrdinal("username")),
                                rol_id = reader.GetInt32(reader.GetOrdinal("rol_id")),
                                role_name = reader.GetString(reader.GetOrdinal("role_name"))
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

        public class CreateUserRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public int RolId { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                string hashString;
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(request.Password));
                    hashString = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
                }

                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "sp_CrearUsuario";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    var pUser = command.CreateParameter(); pUser.ParameterName = "@Username"; pUser.Value = request.Username; command.Parameters.Add(pUser);
                    var pHash = command.CreateParameter(); pHash.ParameterName = "@PasswordHash"; pHash.Value = hashString; command.Parameters.Add(pHash);
                    var pRol = command.CreateParameter(); pRol.ParameterName = "@RolId"; pRol.Value = request.RolId; command.Parameters.Add(pRol);

                    if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        await _dbContext.Database.OpenConnectionAsync();
                    }

                    await command.ExecuteNonQueryAsync();
                }

                return Ok(new { Message = "Usuario creado exitosamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }


        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var records = new List<object>();
            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT id, nombre_rol FROM cert_roles";

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
                                name = reader.GetString(reader.GetOrdinal("nombre_rol"))
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

        public class CreateRoleRequest { public string Name { get; set; } = string.Empty; }

        [HttpPost("roles")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
        {
            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "sp_CrearRol";
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    var pName = command.CreateParameter(); pName.ParameterName = "@NombreRol"; pName.Value = request.Name; command.Parameters.Add(pName);

                    if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        await _dbContext.Database.OpenConnectionAsync();
                    }
                    await command.ExecuteNonQueryAsync();
                }
                return Ok(new { Message = "Rol creado exitosamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("/api/security/modules")]
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
                                id = reader.GetInt32(reader.GetOrdinal("id")),
                                name = reader.GetString(reader.GetOrdinal("nombre_modulo")),
                                path = reader.GetString(reader.GetOrdinal("ruta"))
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

        [HttpGet("roles/{id}/modules")]
        public async Task<IActionResult> GetRoleModules(int id)
        {
            var records = new List<object>();
            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT modulo_id FROM cert_permisos WHERE rol_id = @RolId";
                    var pId = command.CreateParameter(); pId.ParameterName = "@RolId"; pId.Value = id; command.Parameters.Add(pId);

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
                                id = reader.GetInt32(reader.GetOrdinal("modulo_id"))
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

        public class UpdatePermissionsRequest
        {
            public List<int> ModuleIds { get; set; } = new List<int>();
        }

        [HttpPut("roles/{id}/permissions")]
        public async Task<IActionResult> UpdateRolePermissions(int id, [FromBody] UpdatePermissionsRequest request)
        {
            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "sp_ActualizarPermisosRol";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    var pRolId = command.CreateParameter(); pRolId.ParameterName = "@RolId"; pRolId.Value = id; command.Parameters.Add(pRolId);

                    var moduleIdsString = string.Join(",", request.ModuleIds);
                    var pModuleIds = command.CreateParameter(); pModuleIds.ParameterName = "@ModulosIds"; pModuleIds.Value = moduleIdsString; command.Parameters.Add(pModuleIds);

                    if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        await _dbContext.Database.OpenConnectionAsync();
                    }

                    await command.ExecuteNonQueryAsync();
                }

                return Ok(new { Message = "Permisos actualizados exitosamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        public class UpdateUserRequest
        {
            public int Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public int RolId { get; set; }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
        {
            try
            {
                string hashString = null;
                if (!string.IsNullOrEmpty(request.Password))
                {
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(request.Password));
                        hashString = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
                    }
                }

                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "sp_EditarUsuario";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    var pId = command.CreateParameter(); pId.ParameterName = "@Id"; pId.Value = request.Id; command.Parameters.Add(pId);
                    var pUser = command.CreateParameter(); pUser.ParameterName = "@Username"; pUser.Value = request.Username; command.Parameters.Add(pUser);
                    var pHash = command.CreateParameter(); pHash.ParameterName = "@PasswordHash"; pHash.Value = (object)hashString ?? DBNull.Value; command.Parameters.Add(pHash);
                    var pRol = command.CreateParameter(); pRol.ParameterName = "@RolId"; pRol.Value = request.RolId; command.Parameters.Add(pRol);

                    if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        await _dbContext.Database.OpenConnectionAsync();
                    }

                    await command.ExecuteNonQueryAsync();
                }

                return Ok(new { Message = "Usuario actualizado exitosamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "sp_EliminarUsuario";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    var pId = command.CreateParameter(); pId.ParameterName = "@Id"; pId.Value = id; command.Parameters.Add(pId);

                    if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        await _dbContext.Database.OpenConnectionAsync();
                    }

                    await command.ExecuteNonQueryAsync();
                }

                return Ok(new { Message = "Usuario eliminado exitosamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}

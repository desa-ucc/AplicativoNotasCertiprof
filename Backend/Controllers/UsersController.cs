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


        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var records = new List<object>();
            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT id, nombre_rol FROM cert_roles WHERE activo = 1";
                    command.CommandType = System.Data.CommandType.Text;

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
                                nombre_rol = reader.GetString(reader.GetOrdinal("nombre_rol"))
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


        public class UpdateUserRequest
        {
            public int Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public int RolId { get; set; }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                {
                    await _dbContext.Database.OpenConnectionAsync();
                }

                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    string query = "";
                    if (string.IsNullOrWhiteSpace(request.Password))
                    {
                        query = "UPDATE cert_usuarios SET username = @Username, rol_id = @RolId WHERE id = @Id";
                        command.CommandText = query;
                        command.CommandType = System.Data.CommandType.Text;
                    }
                    else
                    {
                        query = "UPDATE cert_usuarios SET username = @Username, password_hash = @PasswordHash, rol_id = @RolId WHERE id = @Id";
                        command.CommandText = query;
                        command.CommandType = System.Data.CommandType.Text;

                        string hashString;
                        using (var sha256 = System.Security.Cryptography.SHA256.Create())
                        {
                            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(request.Password));
                            hashString = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
                        }

                        var pHash = command.CreateParameter();
                        pHash.ParameterName = "@PasswordHash";
                        pHash.Value = hashString;
                        command.Parameters.Add(pHash);
                    }

                    var pId = command.CreateParameter();
                    pId.ParameterName = "@Id";
                    pId.Value = id; // use path id
                    command.Parameters.Add(pId);

                    var pUser = command.CreateParameter();
                    pUser.ParameterName = "@Username";
                    pUser.Value = request.Username;
                    command.Parameters.Add(pUser);

                    var pRol = command.CreateParameter();
                    pRol.ParameterName = "@RolId";
                    pRol.Value = request.RolId;
                    command.Parameters.Add(pRol);

                    await command.ExecuteNonQueryAsync();
                }

                return Ok(new { Message = "Usuario actualizado correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error en la base de datos: {ex.Message}" });
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

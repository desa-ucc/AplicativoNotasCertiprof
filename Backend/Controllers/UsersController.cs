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
                    command.CommandText = "SELECT u.id, u.username, u.rol_id, r.name as role_name FROM cert_usuarios u JOIN cert_roles r ON u.rol_id = r.id";

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
                byte[] hashBytes;
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(request.Password));
                }

                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "INSERT INTO cert_usuarios (username, password_hash, rol_id) VALUES (@Username, @Hash, @RolId)";

                    var pUser = command.CreateParameter(); pUser.ParameterName = "@Username"; pUser.Value = request.Username; command.Parameters.Add(pUser);
                    var pHash = command.CreateParameter(); pHash.ParameterName = "@Hash"; pHash.Value = hashBytes; command.Parameters.Add(pHash);
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "DELETE FROM cert_usuarios WHERE id = @Id";

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

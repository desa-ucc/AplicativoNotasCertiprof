using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Data;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly IAuthService _authService;

        public AuthController(AppDbContext dbContext, IAuthService authService)
        {
            _dbContext = dbContext;
            _authService = authService;
        }

        public class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class MenuItem
        {
            public string Path { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                {
                    await _dbContext.Database.OpenConnectionAsync();
                }

                int? roleId = null;
                string roleName = string.Empty;
                byte[]? storedHashBytes = null;
                bool isUserValid = false;

                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT u.id, u.username, u.password_hash, r.id as role_id, r.nombre_rol as role_name FROM cert_usuarios u JOIN cert_roles r ON u.rol_id = r.id WHERE u.username = @Username";
                    var pUser = command.CreateParameter();
                    pUser.ParameterName = "@Username";
                    pUser.Value = request.Username;
                    command.Parameters.Add(pUser);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var passwordHashValue = reader.GetValue(reader.GetOrdinal("password_hash"));
                            roleName = reader.GetString(reader.GetOrdinal("role_name"));
                            roleId = reader.GetInt32(reader.GetOrdinal("role_id"));

                            if (passwordHashValue is byte[] bytes)
                            {
                                storedHashBytes = bytes;
                            }
                            else if (passwordHashValue is string hashStringValue)
                            {
                                if (hashStringValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                                {
                                    hashStringValue = hashStringValue[2..];
                                }

                                if (hashStringValue.Length % 2 == 0)
                                {
                                    storedHashBytes = new byte[hashStringValue.Length / 2];
                                    for (int i = 0; i < storedHashBytes.Length; i++)
                                    {
                                        storedHashBytes[i] = Convert.ToByte(hashStringValue.Substring(i * 2, 2), 16);
                                    }
                                }
                            }

                            if (storedHashBytes != null)
                            {
                                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                                {
                                    var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(request.Password));
                                    if (hashedBytes.SequenceEqual(storedHashBytes))
                                    {
                                        isUserValid = true;
                                    }
                                }
                            }
                        }
                    }
                }

                if (isUserValid && roleId.HasValue)
                {
                    var token = _authService.GenerateJwtToken(request.Username, roleName);
                    var menu = new List<MenuItem>();

                    try
                    {
                        using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                        {
                            command.CommandText = "sp_ObtenerMenuPorRol";
                            command.CommandType = System.Data.CommandType.StoredProcedure;

                            var pRoleId = command.CreateParameter();
                            pRoleId.ParameterName = "@RolId";
                            pRoleId.Value = roleId.Value;
                            command.Parameters.Add(pRoleId);

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    menu.Add(new MenuItem
                                    {
                                        Name = reader.GetString(reader.GetOrdinal("Nombre")),
                                        Path = reader.GetString(reader.GetOrdinal("Ruta")),
                                        Icon = reader.GetString(reader.GetOrdinal("Icono"))
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log or ignore SP error
                    }

                    return Ok(new { Token = token, Role = roleName, Menu = menu });
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Invalid object name 'cert_usuarios'") || ex.Message.Contains("Invalid object name 'cert_roles'"))
                {
                    var adminUser = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin";
                    var adminPass = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "admin123";
                    var docenteUser = Environment.GetEnvironmentVariable("DOCENTE_USERNAME") ?? "docente";
                    var docentePass = Environment.GetEnvironmentVariable("DOCENTE_PASSWORD") ?? "docente123";

                    var defaultAdminMenu = new List<MenuItem> {
                        new MenuItem { Name = "Cargar Archivo", Path = "/upload", Icon = "upload_file" },
                        new MenuItem { Name = "Historial", Path = "/history", Icon = "history" },
                        new MenuItem { Name = "Seguridad", Path = "/security", Icon = "security" }
                    };

                    var defaultDocenteMenu = new List<MenuItem> {
                        new MenuItem { Name = "Historial", Path = "/history", Icon = "history" }
                    };

                    if (request.Username == adminUser && request.Password == adminPass)
                    {
                        var token = _authService.GenerateJwtToken("admin", "Administrador");
                        return Ok(new { Token = token, Role = "Administrador", Menu = defaultAdminMenu });
                    }
                    else if (request.Username == docenteUser && request.Password == docentePass)
                    {
                        var token = _authService.GenerateJwtToken("docente", "Consulta");
                        return Ok(new { Token = token, Role = "Consulta", Menu = defaultDocenteMenu });
                    }
                }

                return BadRequest(new { Message = ex.Message });
            }

            return Unauthorized(new { Message = "Credenciales inválidas" });
        }
    }
}

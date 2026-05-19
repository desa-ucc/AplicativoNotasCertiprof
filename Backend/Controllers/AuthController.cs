using System;
using System.Linq;
using System.Threading.Tasks;
using Backend.Data;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
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
            public string PasswordPlain { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "sp_ValidarLogin";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@Username", SqlDbType.VarChar, 100) { Value = request.Username ?? string.Empty });
                    command.Parameters.Add(new SqlParameter("@PasswordPlain", SqlDbType.VarChar, 100) { Value = request.PasswordPlain ?? string.Empty });

                    if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        await _dbContext.Database.OpenConnectionAsync();
                    }

                    int? rolId = null;
                    string username = null;
                    bool activo = false;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            rolId = reader.GetInt32(reader.GetOrdinal("rol_id"));
                            username = reader.GetString(reader.GetOrdinal("username"));
                            activo = reader.GetBoolean(reader.GetOrdinal("activo"));
                        }
                    }

                    if (rolId.HasValue && activo)
                    {
                        string roleName = "Consulta"; // Default fallback

                        // Try to fetch Role Name since it's needed for token
                        using (var roleCommand = _dbContext.Database.GetDbConnection().CreateCommand())
                        {
                            roleCommand.CommandText = "SELECT nombre_rol FROM cert_roles WHERE id = @RolId";
                            var pRolId = roleCommand.CreateParameter();
                            pRolId.ParameterName = "@RolId";
                            pRolId.Value = rolId.Value;
                            roleCommand.Parameters.Add(pRolId);

                            using (var roleReader = await roleCommand.ExecuteReaderAsync())
                            {
                                if (await roleReader.ReadAsync())
                                {
                                    roleName = roleReader.GetString(0);
                                }
                            }
                        }

                        var token = _authService.GenerateJwtToken(username, roleName);

                        var menuList = new System.Collections.Generic.List<object>();
                        using (var menuCommand = _dbContext.Database.GetDbConnection().CreateCommand())
                        {
                            menuCommand.CommandText = "sp_ObtenerMenuPorRol";
                            menuCommand.CommandType = System.Data.CommandType.StoredProcedure;

                            var pRol = menuCommand.CreateParameter();
                            pRol.ParameterName = "@RolNombre";
                            pRol.Value = roleName;
                            menuCommand.Parameters.Add(pRol);

                            using (var menuReader = await menuCommand.ExecuteReaderAsync())
                            {
                                while (await menuReader.ReadAsync())
                                {
                                    menuList.Add(new {
                                        name = menuReader.GetString(menuReader.GetOrdinal("Nombre")),
                                        path = menuReader.GetString(menuReader.GetOrdinal("Ruta")),
                                        icon = menuReader.IsDBNull(menuReader.GetOrdinal("Icono")) ? "" : menuReader.GetString(menuReader.GetOrdinal("Icono"))
                                    });
                                }
                            }
                        }

                        // Return what was asked
                        return Ok(new { Token = token, RoleId = rolId.Value, Role = roleName, Menu = menuList });
                    }

                }
            }
            catch (Exception ex)
            {
                // Fallback for testing if cert_usuarios/cert_roles don't exist yet in local testing
                if (ex.Message.Contains("Invalid object name 'cert_usuarios'") || ex.Message.Contains("Invalid object name 'cert_roles'"))
                {
                    var adminUser = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin";
                    var adminPass = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "admin123";
                    var docenteUser = Environment.GetEnvironmentVariable("DOCENTE_USERNAME") ?? "docente";
                    var docentePass = Environment.GetEnvironmentVariable("DOCENTE_PASSWORD") ?? "docente123";

                    if (request.Username == adminUser && request.PasswordPlain == adminPass)
                    {
                        var token = _authService.GenerateJwtToken("admin", "Administrador");
                        var fallbackMenuAdmin = new[] {
                            new { name = "Cargar Archivo", path = "/upload", icon = "upload_file" },
                            new { name = "Historial", path = "/history", icon = "history" },
                            new { name = "Seguridad", path = "/security", icon = "security" }
                        };
                        return Ok(new { Token = token, Role = "Administrador", Menu = fallbackMenuAdmin });
                    }
                    else if (request.Username == docenteUser && request.PasswordPlain == docentePass)
                    {
                        var token = _authService.GenerateJwtToken("docente", "Docente");
                        var fallbackMenuDocente = new[] {
                            new { name = "Historial", path = "/history", icon = "history" }
                        };
                        return Ok(new { Token = token, Role = "Docente", Menu = fallbackMenuDocente });
                    }
                }

                return BadRequest(new { Message = ex.Message });
            }

            return Unauthorized(new { Message = "Credenciales inválidas" });
        }
    }
}

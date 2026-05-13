using System;
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

                    var pUser = command.CreateParameter();
                    pUser.ParameterName = "@Username";
                    pUser.Value = request.Username;
                    command.Parameters.Add(pUser);

                    var pPass = command.CreateParameter();
                    pPass.ParameterName = "@PasswordPlain";
                    pPass.Value = request.PasswordPlain;
                    command.Parameters.Add(pPass);

                    if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        await _dbContext.Database.OpenConnectionAsync();
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var roleName = reader.GetString(reader.GetOrdinal("role_name"));
                            var rolId = reader.GetInt32(reader.GetOrdinal("rol_id"));

                            var token = _authService.GenerateJwtToken(request.Username, roleName);
                            var menuList = new System.Collections.Generic.List<object>();

                            // Close reader before executing another SP
                            await reader.CloseAsync();

                            using (var menuCmd = _dbContext.Database.GetDbConnection().CreateCommand())
                            {
                                menuCmd.CommandText = "sp_ObtenerMenuPorRol";
                                menuCmd.CommandType = System.Data.CommandType.StoredProcedure;
                                var pRolId = menuCmd.CreateParameter();
                                pRolId.ParameterName = "@RolId";
                                pRolId.Value = rolId;
                                menuCmd.Parameters.Add(pRolId);

                                using (var menuReader = await menuCmd.ExecuteReaderAsync())
                                {
                                    while (await menuReader.ReadAsync())
                                    {
                                        menuList.Add(new {
                                            name = menuReader.GetString(menuReader.GetOrdinal("NombreModulo")),
                                            path = menuReader.GetString(menuReader.GetOrdinal("Ruta")),
                                            icon = menuReader.IsDBNull(menuReader.GetOrdinal("Icono")) ? "pi-folder" : menuReader.GetString(menuReader.GetOrdinal("Icono"))
                                        });
                                    }
                                }
                            }

                            return Ok(new { Token = token, Role = roleName, Menu = menuList });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback for testing if cert_usuarios/cert_roles don't exist yet in local testing
                if (ex.Message.Contains("Could not find stored procedure 'sp_ValidarLogin'") || ex.Message.Contains("Invalid object name"))
                {
                    var adminUser = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin";
                    var adminPass = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "admin123";
                    var docenteUser = Environment.GetEnvironmentVariable("DOCENTE_USERNAME") ?? "docente";
                    var docentePass = Environment.GetEnvironmentVariable("DOCENTE_PASSWORD") ?? "docente123";

                    if (request.Username == adminUser && request.PasswordPlain == adminPass)
                    {
                        var token = _authService.GenerateJwtToken("admin", "Administrador");
                        var adminMenu = new[] {
                            new { name = "Cargar Archivo", path = "/upload", icon = "pi-upload" },
                            new { name = "Historial", path = "/history", icon = "pi-database" },
                            new { name = "Seguridad", path = "/security", icon = "pi-shield" }
                        };
                        return Ok(new { Token = token, Role = "Administrador", Menu = adminMenu });
                    }
                    else if (request.Username == docenteUser && request.PasswordPlain == docentePass)
                    {
                        var token = _authService.GenerateJwtToken("docente", "Docente");
                        var docenteMenu = new[] {
                            new { name = "Historial", path = "/history", icon = "pi-database" }
                        };
                        return Ok(new { Token = token, Role = "Docente", Menu = docenteMenu });
                    }
                }

                return BadRequest(new { Message = ex.Message });
            }

            return Unauthorized(new { Message = "Credenciales inválidas" });
        }
    }
}

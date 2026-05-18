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
            public string PasswordPlain { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    // 1. Verify credentials and get role details using sp_ValidarLogin
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

                    int? rolId = null;
                    string roleName = "";
                    bool credentialsValid = false;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            rolId = reader.GetInt32(reader.GetOrdinal("rol_id"));
                            roleName = reader.GetString(reader.GetOrdinal("role_name"));
                            credentialsValid = true;
                        }
                    }

                    // 2. If valid, fetch the allowed menu modules for this role via SP
                    if (credentialsValid && rolId.HasValue)
                    {
                        var menu = new List<object>();

                        using (var menuCmd = _dbContext.Database.GetDbConnection().CreateCommand())
                        {
                            menuCmd.CommandText = "sp_ObtenerMenuPorRol";
                            menuCmd.CommandType = System.Data.CommandType.StoredProcedure;

                            var pRol = menuCmd.CreateParameter();
                            pRol.ParameterName = "@RolId";
                            pRol.Value = rolId.Value;
                            menuCmd.Parameters.Add(pRol);

                            using (var menuReader = await menuCmd.ExecuteReaderAsync())
                            {
                                while (await menuReader.ReadAsync())
                                {
                                    menu.Add(new {
                                        id = menuReader.GetInt32(menuReader.GetOrdinal("ModuloId")),
                                        name = menuReader.GetString(menuReader.GetOrdinal("NombreModulo")),
                                        path = menuReader.GetString(menuReader.GetOrdinal("RutaFrontEnd"))
                                    });
                                }
                            }
                        }

                        var token = _authService.GenerateJwtToken(request.Username, roleName);
                        return Ok(new { Token = token, Role = roleName, Menu = menu });
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback for testing if cert_usuarios/cert_roles don't exist yet in local testing
                if (ex.Message.Contains("Could not find stored procedure") || ex.Message.Contains("Invalid object name"))
                {
                    var adminUser = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin";
                    var adminPass = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "admin123";
                    var docenteUser = Environment.GetEnvironmentVariable("DOCENTE_USERNAME") ?? "docente";
                    var docentePass = Environment.GetEnvironmentVariable("DOCENTE_PASSWORD") ?? "docente123";

                    if (request.Username == adminUser && request.PasswordPlain == adminPass)
                    {
                        var token = _authService.GenerateJwtToken("admin", "Administrador");
                        var fallbackMenu = new List<object> {
                            new { id = 1, name = "Cargar Archivo", path = "/upload" },
                            new { id = 2, name = "Historial", path = "/history" },
                            new { id = 3, name = "Seguridad", path = "/security" }
                        };
                        return Ok(new { Token = token, Role = "Administrador", Menu = fallbackMenu });
                    }
                    else if (request.Username == docenteUser && request.PasswordPlain == docentePass)
                    {
                        var token = _authService.GenerateJwtToken("docente", "Docente");
                        var fallbackMenu = new List<object> {
                            new { id = 1, name = "Cargar Archivo", path = "/upload" },
                            new { id = 2, name = "Historial", path = "/history" }
                        };
                        return Ok(new { Token = token, Role = "Docente", Menu = fallbackMenu });
                    }
                }

                return BadRequest(new { Message = ex.Message });
            }

            return Unauthorized(new { Message = "Credenciales inválidas" });
        }
    }
}

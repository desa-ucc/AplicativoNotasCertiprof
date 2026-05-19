using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Backend.Data;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        public class LoginRequest
        {
            [JsonPropertyName("username")]
            public string Username { get; set; } = string.Empty;

            [JsonPropertyName("passwordPlain")]
            public string PasswordPlain { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.PasswordPlain))
                return BadRequest(new { message = "Faltan credenciales en el payload" });

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";

                int? rolId = null;
                string roleName = "";
                bool credentialsValid = false;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_ValidarLogin", conn))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.Add(new SqlParameter("@Username", System.Data.SqlDbType.VarChar, 100) { Value = request.Username });
                        cmd.Parameters.Add(new SqlParameter("@PasswordPlain", System.Data.SqlDbType.VarChar, 100) { Value = request.PasswordPlain });

                        await conn.OpenAsync();
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                rolId = reader.GetInt32(reader.GetOrdinal("rol_id"));
                                roleName = reader.GetString(reader.GetOrdinal("role_name"));
                                credentialsValid = true;
                            }
                        }
                    }

                    if (credentialsValid && rolId.HasValue)
                    {
                        var menu = new List<object>();

                        using (SqlCommand menuCmd = new SqlCommand("sp_ObtenerMenuPorRol", conn))
                        {
                            menuCmd.CommandType = System.Data.CommandType.StoredProcedure;
                            menuCmd.Parameters.Add(new SqlParameter("@RolId", SqlDbType.Int) { Value = rolId.Value });

                            using (SqlDataReader menuReader = await menuCmd.ExecuteReaderAsync())
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
                        return Ok(new { Token = token, Role = roleName, RolId = rolId.Value, Menu = menu });
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback for testing if cert_usuarios/cert_roles don't exist yet in local testing
                if (ex.Message.Contains("Could not find stored procedure") || ex.Message.Contains("Invalid object name") || ex.Message.Contains("Login failed"))
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
                        return Ok(new { Token = token, Role = "Administrador", RolId = 1, Menu = fallbackMenu });
                    }
                    else if (request.Username == docenteUser && request.PasswordPlain == docentePass)
                    {
                        var token = _authService.GenerateJwtToken("docente", "Docente");
                        var fallbackMenu = new List<object> {
                            new { id = 1, name = "Cargar Archivo", path = "/upload" },
                            new { id = 2, name = "Historial", path = "/history" }
                        };
                        return Ok(new { Token = token, Role = "Docente", RolId = 2, Menu = fallbackMenu });
                    }
                }

                return BadRequest(new { Message = ex.Message });
            }

            return Unauthorized(new { Message = "Credenciales inválidas" });
        }
    }
}

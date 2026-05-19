using System;
<<<<<<< HEAD
=======
using System.Collections.Generic;
using System.Data;
>>>>>>> parent of 4b8b460 (fix: Direct ADO.NET implementation for Login to resolve 400/401 binding and hashing issues)
using System.Linq;
using System.Threading.Tasks;
using Backend.Data;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
<<<<<<< HEAD
=======
using Microsoft.Data.SqlClient;
>>>>>>> parent of 4b8b460 (fix: Direct ADO.NET implementation for Login to resolve 400/401 binding and hashing issues)
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
<<<<<<< HEAD
            public string Password { get; set; } = string.Empty;
=======
            public string PasswordPlain { get; set; } = string.Empty;
>>>>>>> parent of 4b8b460 (fix: Direct ADO.NET implementation for Login to resolve 400/401 binding and hashing issues)
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
<<<<<<< HEAD
                    // Query directly the cert_usuarios table. Using ADO.NET the House Way.
                    command.CommandText = "SELECT u.id, u.username, u.password_hash, r.nombre_rol as role_name FROM cert_usuarios u JOIN cert_roles r ON u.rol_id = r.id WHERE u.username = @Username";

                    var pUser = command.CreateParameter();
                    pUser.ParameterName = "@Username";
                    pUser.Value = request.Username;
                    command.Parameters.Add(pUser);

                    if (_dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        await _dbContext.Database.OpenConnectionAsync();
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var passwordHashValue = reader.GetValue(reader.GetOrdinal("password_hash"));
                            var roleName = reader.GetString(reader.GetOrdinal("role_name"));

                            byte[]? storedHashBytes = null;

                            if (passwordHashValue is byte[] bytes)
=======
                    // 1. Verify credentials and get role details using sp_ValidarLogin
                    command.CommandText = "sp_ValidarLogin";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    // Fix: Explicitly define SqlDbType.VarChar to prevent HASHBYTES mismatch
                    var pUser = command.CreateParameter();
                    pUser.ParameterName = "@Username";
                    ((SqlParameter)pUser).SqlDbType = SqlDbType.VarChar;
                    ((SqlParameter)pUser).Size = 100;
                    pUser.Value = request.Username;
                    command.Parameters.Add(pUser);

                    var pPass = command.CreateParameter();
                    pPass.ParameterName = "@PasswordPlain";
                    ((SqlParameter)pPass).SqlDbType = SqlDbType.VarChar;
                    ((SqlParameter)pPass).Size = 100;
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
>>>>>>> parent of 4b8b460 (fix: Direct ADO.NET implementation for Login to resolve 400/401 binding and hashing issues)
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
                                        var token = _authService.GenerateJwtToken(request.Username, roleName);
                                        return Ok(new { Token = token, Role = roleName });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback for testing if cert_usuarios/cert_roles don't exist yet in local testing
<<<<<<< HEAD
                if (ex.Message.Contains("Invalid object name 'cert_usuarios'") || ex.Message.Contains("Invalid object name 'cert_roles'"))
=======
                if (ex.Message.Contains("Could not find stored procedure") || ex.Message.Contains("Invalid object name"))
>>>>>>> parent of 4b8b460 (fix: Direct ADO.NET implementation for Login to resolve 400/401 binding and hashing issues)
                {
                    var adminUser = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin";
                    var adminPass = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "admin123";
                    var docenteUser = Environment.GetEnvironmentVariable("DOCENTE_USERNAME") ?? "docente";
                    var docentePass = Environment.GetEnvironmentVariable("DOCENTE_PASSWORD") ?? "docente123";

                    if (request.Username == adminUser && request.Password == adminPass)
                    {
                        var token = _authService.GenerateJwtToken("admin", "Administrador");
                        return Ok(new { Token = token, Role = "Administrador" });
                    }
                    else if (request.Username == docenteUser && request.Password == docentePass)
                    {
                        var token = _authService.GenerateJwtToken("docente", "Docente");
                        return Ok(new { Token = token, Role = "Docente" });
                    }
                }

                return BadRequest(new { Message = ex.Message });
            }

            return Unauthorized(new { Message = "Credenciales inválidas" });
        }
    }
}

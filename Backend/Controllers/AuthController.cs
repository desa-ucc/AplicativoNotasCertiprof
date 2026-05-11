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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Note: DB authentication has been bypassed as 'cert_users' is not available.
            // Using a simple fallback for testing, but in production this should be
            // verified against an Identity Provider, LDAP, or an environment-configured secret.
            var adminUser = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin";
            var adminPass = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "admin123";
            var docenteUser = Environment.GetEnvironmentVariable("DOCENTE_USERNAME") ?? "docente";
            var docentePass = Environment.GetEnvironmentVariable("DOCENTE_PASSWORD") ?? "docente123";

            if (request.Username == adminUser && request.Password == adminPass)
            {
                var token = _authService.GenerateJwtToken("admin", "Admin");
                return Ok(new { Token = token, Role = "Admin" });
            }
            else if (request.Username == docenteUser && request.Password == docentePass)
            {
                var token = _authService.GenerateJwtToken("docente", "Docente");
                return Ok(new { Token = token, Role = "Docente" });
            }

            return Unauthorized(new { Message = "Credenciales inválidas" });
        }
    }
}

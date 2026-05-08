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
            // Bypass DB authentication and use a hardcoded admin user
            if (request.Username == "admin" && request.Password == "admin123")
            {
                var token = _authService.GenerateJwtToken("admin", "Admin");
                return Ok(new { Token = token, Role = "Admin" });
            }
            else if (request.Username == "docente" && request.Password == "docente123")
            {
                var token = _authService.GenerateJwtToken("docente", "Docente");
                return Ok(new { Token = token, Role = "Docente" });
            }

            return Unauthorized(new { Message = "Credenciales inválidas" });
        }
    }
}

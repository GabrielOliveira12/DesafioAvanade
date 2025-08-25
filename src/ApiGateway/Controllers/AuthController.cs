using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            try
            {
                if (IsValidUser(request.Username, request.Password))
                {
                    var token = GenerateJwtToken(request.Username);
                    return Ok(new { token, expires = DateTime.UtcNow.AddHours(24) });
                }

                return Unauthorized("Credenciais inválidas");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante o login para usuário {Username}", request.Username);
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest("Username e password são obrigatórios");
                }

                if (request.Password.Length < 6)
                {
                    return BadRequest("Password deve ter pelo menos 6 caracteres");
                }

                var token = GenerateJwtToken(request.Username);
                return Ok(new { token, expires = DateTime.UtcNow.AddHours(24), message = "Usuário registrado com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante o registro para usuário {Username}", request.Username);
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        private bool IsValidUser(string username, string password)
        {
            var validUsers = new Dictionary<string, string>
            {
                { "admin", "admin123" },
                { "user1", "user123" },
                { "test", "test123" }
            };

            return validUsers.ContainsKey(username) && validUsers[username] == password;
        }

        private string GenerateJwtToken(string username)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "minha-chave-secreta-super-segura-de-256-bits";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, username),
                new Claim(ClaimTypes.Name, username),
                new Claim("sub", username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "ApiGateway",
                audience: _configuration["Jwt:Audience"] ?? "ApiGateway",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}

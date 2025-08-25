using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using System.IdentityModel.Tokens.Jwt;
using ApiGateway.Controllers;
using Xunit;

namespace ApiGateway.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly AuthController _controller;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<AuthController>> _mockLogger;

        public AuthControllerTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<AuthController>>();

            _mockConfiguration.Setup(x => x["Jwt:Key"]).Returns("minha-chave-secreta-super-segura-de-256-bits");
            _mockConfiguration.Setup(x => x["Jwt:Issuer"]).Returns("ApiGateway");
            _mockConfiguration.Setup(x => x["Jwt:Audience"]).Returns("ApiGateway");

            _controller = new AuthController(_mockConfiguration.Object, _mockLogger.Object);
        }

        [Fact]
        public void Login_CredenciaisValidas_DeveRetornarToken()
        {
            var request = new LoginRequest
            {
                Username = "admin",
                Password = "admin123"
            };

            var resultado = _controller.Login(request) as OkObjectResult;

            resultado.Should().NotBeNull();
            resultado!.StatusCode.Should().Be(200);

            var response = resultado.Value as dynamic;
            response.Should().NotBeNull();
        }

        [Fact]
        public void Login_CredenciaisInvalidas_DeveRetornarUnauthorized()
        {
            var request = new LoginRequest
            {
                Username = "usuario_inexistente",
                Password = "senha_errada"
            };

            var resultado = _controller.Login(request);

            resultado.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void Login_UsuarioVazio_DeveRetornarUnauthorized()
        {
            var request = new LoginRequest
            {
                Username = "",
                Password = "senha123"
            };

            var resultado = _controller.Login(request);

            resultado.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void Login_SenhaVazia_DeveRetornarUnauthorized()
        {
            var request = new LoginRequest
            {
                Username = "admin",
                Password = ""
            };

            var resultado = _controller.Login(request);

            resultado.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void Register_DadosValidos_DeveRetornarToken()
        {
            var request = new RegisterRequest
            {
                Username = "novousuario",
                Password = "senha123",
                Email = "novo@email.com"
            };

            var resultado = _controller.Register(request) as OkObjectResult;

            resultado.Should().NotBeNull();
            resultado!.StatusCode.Should().Be(200);

            var response = resultado.Value as dynamic;
            response.Should().NotBeNull();
        }

        [Fact]
        public void Register_UsuarioVazio_DeveRetornarBadRequest()
        {
            var request = new RegisterRequest
            {
                Username = "",
                Password = "senha123",
                Email = "email@test.com"
            };

            var resultado = _controller.Register(request);

            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void Register_SenhaVazia_DeveRetornarBadRequest()
        {
            var request = new RegisterRequest
            {
                Username = "usuario",
                Password = "",
                Email = "email@test.com"
            };

            var resultado = _controller.Register(request);

            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void Register_SenhaCurta_DeveRetornarBadRequest()
        {
            var request = new RegisterRequest
            {
                Username = "usuario",
                Password = "123",
                Email = "email@test.com"
            };

            var resultado = _controller.Register(request);

            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Theory]
        [InlineData("admin", "admin123")]
        [InlineData("user1", "user123")]
        [InlineData("test", "test123")]
        public void Login_UsuariosValidos_DeveGerarTokenValido(string username, string password)
        {
            var request = new LoginRequest
            {
                Username = username,
                Password = password
            };

            var resultado = _controller.Login(request) as OkObjectResult;
            resultado.Should().NotBeNull();

            var response = resultado!.Value;
            var tokenProperty = response!.GetType().GetProperty("token");
            tokenProperty.Should().NotBeNull();

            var token = tokenProperty!.GetValue(response) as string;
            token.Should().NotBeNullOrEmpty();

            var tokenHandler = new JwtSecurityTokenHandler();
            var isValidToken = tokenHandler.CanReadToken(token);
            isValidToken.Should().BeTrue();
        }
    }
}

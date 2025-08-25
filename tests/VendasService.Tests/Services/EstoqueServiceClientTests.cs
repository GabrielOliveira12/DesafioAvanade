using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using FluentAssertions;
using System.Net;
using System.Text.Json;
using VendasService.Services;
using Shared.DTOs;
using Xunit;

namespace VendasService.Tests.Services
{
    public class EstoqueServiceClientTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<ILogger<EstoqueServiceClient>> _mockLogger;
        private readonly HttpClient _httpClient;
        private readonly EstoqueServiceClient _estoqueServiceClient;

        public EstoqueServiceClientTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockLogger = new Mock<ILogger<EstoqueServiceClient>>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://localhost:7001")
            };
            _estoqueServiceClient = new EstoqueServiceClient(_httpClient, _mockLogger.Object);
        }

        [Fact]
        public async Task ValidarEstoqueAsync_EstoqueSuficiente_DeveRetornarTrue()
        {
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("true")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var resultado = await _estoqueServiceClient.ValidarEstoqueAsync(1, 5);

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task ValidarEstoqueAsync_EstoqueInsuficiente_DeveRetornarFalse()
        {
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("false")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var resultado = await _estoqueServiceClient.ValidarEstoqueAsync(1, 10);

            resultado.Should().BeFalse();
        }

        [Fact]
        public async Task ValidarEstoqueAsync_ErroRede_DeveRetornarFalse()
        {
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Erro de rede"));

            var resultado = await _estoqueServiceClient.ValidarEstoqueAsync(1, 5);

            resultado.Should().BeFalse();
        }

        [Fact]
        public async Task GetProdutoAsync_ProdutoExistente_DeveRetornarProduto()
        {
            var produto = new ProdutoDto
            {
                Id = 1,
                Nome = "Produto Teste",
                Descricao = "Descrição do produto",
                Preco = 100.00m,
                QuantidadeEstoque = 10
            };

            var json = JsonSerializer.Serialize(produto);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var resultado = await _estoqueServiceClient.GetProdutoAsync(1);

            resultado.Should().NotBeNull();
            resultado!.Id.Should().Be(1);
            resultado.Nome.Should().Be("Produto Teste");
        }

        [Fact]
        public async Task GetProdutoAsync_ProdutoInexistente_DeveRetornarNull()
        {
            var responseMessage = new HttpResponseMessage(HttpStatusCode.NotFound);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var resultado = await _estoqueServiceClient.GetProdutoAsync(999);

            resultado.Should().BeNull();
        }

        [Fact]
        public async Task AtualizarEstoqueAsync_Sucesso_DeveRetornarTrue()
        {
            var responseMessage = new HttpResponseMessage(HttpStatusCode.NoContent);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var resultado = await _estoqueServiceClient.AtualizarEstoqueAsync(1, 5);

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task AtualizarEstoqueAsync_BadRequest_DeveRetornarFalse()
        {
            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var resultado = await _estoqueServiceClient.AtualizarEstoqueAsync(1, 100);

            resultado.Should().BeFalse();
        }
    }
}

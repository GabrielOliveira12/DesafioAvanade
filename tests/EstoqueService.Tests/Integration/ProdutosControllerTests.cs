using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using EstoqueService.Data;
using Shared.DTOs;
using Xunit;

namespace EstoqueService.Tests.Integration
{
    public class ProdutosControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ProdutosControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            
            SetupDatabase();
            SetupAuthentication();
        }

        private void SetupDatabase()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EstoqueDbContext>();
            context.Database.EnsureCreated();
        }

        private void SetupAuthentication()
        {
            var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0dXNlciIsIm5hbWUiOiJ0ZXN0dXNlciIsImlhdCI6MTUxNjIzOTAyMn0.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        [Fact]
        public async Task GetProdutos_DeveRetornarListaDeProdutos()
        {
            var response = await _client.GetAsync("/api/produtos");

            response.IsSuccessStatusCode.Should().BeTrue();
            
            var content = await response.Content.ReadAsStringAsync();
            var produtos = JsonSerializer.Deserialize<List<ProdutoDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            produtos.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetProduto_ProdutoExistente_DeveRetornarProduto()
        {
            var response = await _client.GetAsync("/api/produtos/1");

            response.IsSuccessStatusCode.Should().BeTrue();
            
            var content = await response.Content.ReadAsStringAsync();
            var produto = JsonSerializer.Deserialize<ProdutoDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            produto.Should().NotBeNull();
            produto!.Id.Should().Be(1);
        }

        [Fact]
        public async Task GetProduto_ProdutoInexistente_DeveRetornarNotFound()
        {
            var response = await _client.GetAsync("/api/produtos/999");

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CriarProduto_DadosValidos_DeveRetornarCreated()
        {
            var novoProduto = new CriarProdutoDto
            {
                Nome = "Produto Teste",
                Descricao = "Descrição do produto teste",
                Preco = 99.99m,
                QuantidadeEstoque = 10
            };

            var json = JsonSerializer.Serialize(novoProduto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/produtos", content);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var produtoCriado = JsonSerializer.Deserialize<ProdutoDto>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            produtoCriado.Should().NotBeNull();
            produtoCriado!.Nome.Should().Be("Produto Teste");
        }

        [Fact]
        public async Task ValidarEstoque_EstoqueSuficiente_DeveRetornarTrue()
        {
            var quantidade = 5;
            var json = JsonSerializer.Serialize(quantidade);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/produtos/1/validar-estoque", content);

            response.IsSuccessStatusCode.Should().BeTrue();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var resultado = JsonSerializer.Deserialize<bool>(responseContent);

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task GetProdutos_SemAutenticacao_DeveRetornarUnauthorized()
        {
            var clientSemAuth = _factory.CreateClient();
            
            var response = await clientSemAuth.GetAsync("/api/produtos");

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }
    }
}

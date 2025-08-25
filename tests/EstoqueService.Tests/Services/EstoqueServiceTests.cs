using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using EstoqueService.Data;
using EstoqueService.Services;
using Shared.Models;
using Shared.DTOs;
using Shared.Messaging;
using Xunit;

namespace EstoqueService.Tests.Services
{
    public class EstoqueServiceTests : IDisposable
    {
        private readonly EstoqueDbContext _context;
        private readonly Mock<IMessagePublisher> _mockMessagePublisher;
        private readonly Mock<ILogger<EstoqueService.Services.EstoqueService>> _mockLogger;
        private readonly EstoqueService.Services.EstoqueService _estoqueService;

        public EstoqueServiceTests()
        {
            var options = new DbContextOptionsBuilder<EstoqueDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new EstoqueDbContext(options);
            _mockMessagePublisher = new Mock<IMessagePublisher>();
            _mockLogger = new Mock<ILogger<EstoqueService.Services.EstoqueService>>();

            _estoqueService = new EstoqueService.Services.EstoqueService(
                _context,
                _mockMessagePublisher.Object,
                _mockLogger.Object);

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            var produtos = new List<Produto>
            {
                new Produto { Id = 1, Nome = "Notebook Gamer", Descricao = "Notebook para jogos", Preco = 2500.00m, QuantidadeEstoque = 10 },
                new Produto { Id = 2, Nome = "Mouse Gamer", Descricao = "Mouse óptico", Preco = 150.00m, QuantidadeEstoque = 50 },
                new Produto { Id = 3, Nome = "Teclado Mecânico", Descricao = "Teclado RGB", Preco = 300.00m, QuantidadeEstoque = 25 }
            };

            _context.Produtos.AddRange(produtos);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetProdutosAsync_DeveRetornarApenasProdutosComEstoque()
        {
            _context.Produtos.Add(new Produto { Id = 4, Nome = "Produto Sem Estoque", Preco = 100.00m, QuantidadeEstoque = 0 });
            await _context.SaveChangesAsync();

            var resultado = await _estoqueService.GetProdutosAsync();

            resultado.Should().HaveCount(3);
            resultado.Should().OnlyContain(p => p.QuantidadeEstoque > 0);
        }

        [Fact]
        public async Task GetProdutoByIdAsync_ProdutoExistente_DeveRetornarProduto()
        {
            var resultado = await _estoqueService.GetProdutoByIdAsync(1);

            resultado.Should().NotBeNull();
            resultado!.Id.Should().Be(1);
            resultado.Nome.Should().Be("Notebook Gamer");
        }

        [Fact]
        public async Task GetProdutoByIdAsync_ProdutoInexistente_DeveRetornarNull()
        {
            var resultado = await _estoqueService.GetProdutoByIdAsync(999);

            resultado.Should().BeNull();
        }

        [Fact]
        public async Task CriarProdutoAsync_DadosValidos_DeveCriarProduto()
        {
            var criarProdutoDto = new CriarProdutoDto
            {
                Nome = "Novo Produto",
                Descricao = "Descrição do novo produto",
                Preco = 199.99m,
                QuantidadeEstoque = 15
            };

            var resultado = await _estoqueService.CriarProdutoAsync(criarProdutoDto);

            resultado.Should().NotBeNull();
            resultado.Nome.Should().Be("Novo Produto");
            resultado.Preco.Should().Be(199.99m);

            var produtoNoBanco = await _context.Produtos.FindAsync(resultado.Id);
            produtoNoBanco.Should().NotBeNull();
        }

        [Fact]
        public async Task AtualizarEstoqueAsync_QuantidadeValida_DeveAtualizarEstoque()
        {
            var produtoId = 1;
            var quantidadeParaReduzir = 3;

            var resultado = await _estoqueService.AtualizarEstoqueAsync(produtoId, quantidadeParaReduzir);

            resultado.Should().BeTrue();

            var produto = await _context.Produtos.FindAsync(produtoId);
            produto!.QuantidadeEstoque.Should().Be(7);

            _mockMessagePublisher.Verify(
                x => x.PublishAsync("estoque.exchange", "estoque.atualizado", It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task AtualizarEstoqueAsync_QuantidadeExcedente_NaoDeveAtualizar()
        {
            var produtoId = 1;
            var quantidadeExcedente = 15;

            var resultado = await _estoqueService.AtualizarEstoqueAsync(produtoId, quantidadeExcedente);

            resultado.Should().BeFalse();

            var produto = await _context.Produtos.FindAsync(produtoId);
            produto!.QuantidadeEstoque.Should().Be(10);
        }

        [Fact]
        public async Task AtualizarEstoqueAsync_ProdutoInexistente_DeveRetornarFalse()
        {
            var resultado = await _estoqueService.AtualizarEstoqueAsync(999, 1);

            resultado.Should().BeFalse();
        }

        [Fact]
        public async Task ValidarEstoqueAsync_EstoqueSuficiente_DeveRetornarTrue()
        {
            var resultado = await _estoqueService.ValidarEstoqueAsync(1, 5);

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task ValidarEstoqueAsync_EstoqueInsuficiente_DeveRetornarFalse()
        {
            var resultado = await _estoqueService.ValidarEstoqueAsync(1, 15);

            resultado.Should().BeFalse();
        }

        [Fact]
        public async Task ValidarEstoqueAsync_ProdutoInexistente_DeveRetornarFalse()
        {
            var resultado = await _estoqueService.ValidarEstoqueAsync(999, 1);

            resultado.Should().BeFalse();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

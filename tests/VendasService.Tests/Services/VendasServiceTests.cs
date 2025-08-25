using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using VendasService.Data;
using VendasService.Services;
using Shared.Models;
using Shared.DTOs;
using Shared.Messaging;
using Xunit;

namespace VendasService.Tests.Services
{
    public class VendasServiceTests : IDisposable
    {
        private readonly VendasDbContext _context;
        private readonly Mock<IEstoqueServiceClient> _mockEstoqueClient;
        private readonly Mock<IMessagePublisher> _mockMessagePublisher;
        private readonly Mock<ILogger<VendasService.Services.VendasService>> _mockLogger;
        private readonly VendasService.Services.VendasService _vendasService;

        public VendasServiceTests()
        {
            var options = new DbContextOptionsBuilder<VendasDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new VendasDbContext(options);
            _mockEstoqueClient = new Mock<IEstoqueServiceClient>();
            _mockMessagePublisher = new Mock<IMessagePublisher>();
            _mockLogger = new Mock<ILogger<VendasService.Services.VendasService>>();

            _vendasService = new VendasService.Services.VendasService(
                _context,
                _mockEstoqueClient.Object,
                _mockMessagePublisher.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task CriarPedidoAsync_PedidoVazio_DeveRetornarNull()
        {
            var criarPedidoDto = new CriarPedidoDto { Itens = new List<ItemPedidoDto>() };

            var resultado = await _vendasService.CriarPedidoAsync(criarPedidoDto, "cliente1");

            resultado.Should().BeNull();
        }

        [Fact]
        public async Task CriarPedidoAsync_ProdutoInexistente_DeveRetornarNull()
        {
            var criarPedidoDto = new CriarPedidoDto
            {
                Itens = new List<ItemPedidoDto>
                {
                    new ItemPedidoDto { ProdutoId = 1, Quantidade = 2 }
                }
            };

            _mockEstoqueClient.Setup(x => x.GetProdutoAsync(1))
                .ReturnsAsync((ProdutoDto?)null);

            var resultado = await _vendasService.CriarPedidoAsync(criarPedidoDto, "cliente1");

            resultado.Should().BeNull();
        }

        [Fact]
        public async Task CriarPedidoAsync_EstoqueInsuficiente_DeveRetornarNull()
        {
            var criarPedidoDto = new CriarPedidoDto
            {
                Itens = new List<ItemPedidoDto>
                {
                    new ItemPedidoDto { ProdutoId = 1, Quantidade = 10 }
                }
            };

            var produto = new ProdutoDto { Id = 1, Nome = "Produto 1", Preco = 100.00m, QuantidadeEstoque = 5 };

            _mockEstoqueClient.Setup(x => x.GetProdutoAsync(1))
                .ReturnsAsync(produto);
            _mockEstoqueClient.Setup(x => x.ValidarEstoqueAsync(1, 10))
                .ReturnsAsync(false);

            var resultado = await _vendasService.CriarPedidoAsync(criarPedidoDto, "cliente1");

            resultado.Should().BeNull();
        }

        [Fact]
        public async Task CriarPedidoAsync_DadosValidos_DeveCriarPedidoComSucesso()
        {
            var criarPedidoDto = new CriarPedidoDto
            {
                Itens = new List<ItemPedidoDto>
                {
                    new ItemPedidoDto { ProdutoId = 1, Quantidade = 2 }
                }
            };

            var produto = new ProdutoDto { Id = 1, Nome = "Produto 1", Preco = 100.00m, QuantidadeEstoque = 10 };

            _mockEstoqueClient.Setup(x => x.GetProdutoAsync(1))
                .ReturnsAsync(produto);
            _mockEstoqueClient.Setup(x => x.ValidarEstoqueAsync(1, 2))
                .ReturnsAsync(true);
            _mockEstoqueClient.Setup(x => x.AtualizarEstoqueAsync(1, 2))
                .ReturnsAsync(true);

            var resultado = await _vendasService.CriarPedidoAsync(criarPedidoDto, "cliente1");

            resultado.Should().NotBeNull();
            resultado!.ClienteId.Should().Be("cliente1");
            resultado.ValorTotal.Should().Be(200.00m);
            resultado.Status.Should().Be("Confirmado");
            resultado.Itens.Should().HaveCount(1);

            _mockMessagePublisher.Verify(
                x => x.PublishAsync("vendas.exchange", "venda.realizada", It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task CriarPedidoAsync_FalhaAtualizacaoEstoque_DeveCancelarPedido()
        {
            var criarPedidoDto = new CriarPedidoDto
            {
                Itens = new List<ItemPedidoDto>
                {
                    new ItemPedidoDto { ProdutoId = 1, Quantidade = 2 }
                }
            };

            var produto = new ProdutoDto { Id = 1, Nome = "Produto 1", Preco = 100.00m, QuantidadeEstoque = 10 };

            _mockEstoqueClient.Setup(x => x.GetProdutoAsync(1))
                .ReturnsAsync(produto);
            _mockEstoqueClient.Setup(x => x.ValidarEstoqueAsync(1, 2))
                .ReturnsAsync(true);
            _mockEstoqueClient.Setup(x => x.AtualizarEstoqueAsync(1, 2))
                .ReturnsAsync(false);

            var resultado = await _vendasService.CriarPedidoAsync(criarPedidoDto, "cliente1");

            resultado.Should().NotBeNull();
            resultado!.Status.Should().Be("Cancelado");
        }

        [Fact]
        public async Task GetPedidosAsync_DeveRetornarPedidosDoCliente()
        {
            var pedido1 = new Pedido
            {
                Id = 1,
                ClienteId = "cliente1",
                ValorTotal = 200.00m,
                Status = StatusPedido.Confirmado,
                Itens = new List<ItemPedido>()
            };

            var pedido2 = new Pedido
            {
                Id = 2,
                ClienteId = "cliente2",
                ValorTotal = 300.00m,
                Status = StatusPedido.Confirmado,
                Itens = new List<ItemPedido>()
            };

            _context.Pedidos.AddRange(pedido1, pedido2);
            await _context.SaveChangesAsync();

            var resultado = await _vendasService.GetPedidosAsync("cliente1");

            resultado.Should().HaveCount(1);
            resultado.First().ClienteId.Should().Be("cliente1");
        }

        [Fact]
        public async Task GetPedidoByIdAsync_PedidoExistente_DeveRetornarPedido()
        {
            var pedido = new Pedido
            {
                Id = 1,
                ClienteId = "cliente1",
                ValorTotal = 200.00m,
                Status = StatusPedido.Confirmado,
                Itens = new List<ItemPedido>()
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            var resultado = await _vendasService.GetPedidoByIdAsync(1, "cliente1");

            resultado.Should().NotBeNull();
            resultado!.Id.Should().Be(1);
        }

        [Fact]
        public async Task GetPedidoByIdAsync_PedidoDeOutroCliente_DeveRetornarNull()
        {
            var pedido = new Pedido
            {
                Id = 1,
                ClienteId = "cliente1",
                ValorTotal = 200.00m,
                Status = StatusPedido.Confirmado,
                Itens = new List<ItemPedido>()
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            var resultado = await _vendasService.GetPedidoByIdAsync(1, "cliente2");

            resultado.Should().BeNull();
        }

        [Fact]
        public async Task CancelarPedidoAsync_PedidoPendente_DeveCancelarComSucesso()
        {
            var pedido = new Pedido
            {
                Id = 1,
                ClienteId = "cliente1",
                ValorTotal = 200.00m,
                Status = StatusPedido.Pendente,
                Itens = new List<ItemPedido>()
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            var resultado = await _vendasService.CancelarPedidoAsync(1, "cliente1");

            resultado.Should().BeTrue();

            var pedidoCancelado = await _context.Pedidos.FindAsync(1);
            pedidoCancelado!.Status.Should().Be(StatusPedido.Cancelado);
        }

        [Fact]
        public async Task CancelarPedidoAsync_PedidoConfirmado_NaoDeveCancelar()
        {
            var pedido = new Pedido
            {
                Id = 1,
                ClienteId = "cliente1",
                ValorTotal = 200.00m,
                Status = StatusPedido.Confirmado,
                Itens = new List<ItemPedido>()
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            var resultado = await _vendasService.CancelarPedidoAsync(1, "cliente1");

            resultado.Should().BeFalse();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

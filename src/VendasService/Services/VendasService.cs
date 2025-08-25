using Microsoft.EntityFrameworkCore;
using VendasService.Data;
using Shared.Models;
using Shared.DTOs;
using Shared.Messaging;
using Shared.Events;

namespace VendasService.Services
{
    public class VendasService : IVendasService
    {
        private readonly VendasDbContext _context;
        private readonly IEstoqueServiceClient _estoqueClient;
        private readonly IMessagePublisher _messagePublisher;
        private readonly ILogger<VendasService> _logger;

        public VendasService(
            VendasDbContext context, 
            IEstoqueServiceClient estoqueClient,
            IMessagePublisher messagePublisher,
            ILogger<VendasService> logger)
        {
            _context = context;
            _estoqueClient = estoqueClient;
            _messagePublisher = messagePublisher;
            _logger = logger;
        }

        public async Task<PedidoDto?> CriarPedidoAsync(CriarPedidoDto criarPedidoDto, string clienteId)
        {
            if (!criarPedidoDto.Itens.Any())
            {
                _logger.LogWarning("Tentativa de criar pedido sem itens para cliente {ClienteId}", clienteId);
                return null;
            }

            var itensValidados = new List<ItemPedidoDto>();
            
            foreach (var item in criarPedidoDto.Itens)
            {
                var produto = await _estoqueClient.GetProdutoAsync(item.ProdutoId);
                if (produto == null)
                {
                    _logger.LogWarning("Produto {ProdutoId} não encontrado", item.ProdutoId);
                    return null;
                }

                var estoqueValido = await _estoqueClient.ValidarEstoqueAsync(item.ProdutoId, item.Quantidade);
                if (!estoqueValido)
                {
                    _logger.LogWarning("Estoque insuficiente para produto {ProdutoId}", item.ProdutoId);
                    return null;
                }

                itensValidados.Add(new ItemPedidoDto
                {
                    ProdutoId = item.ProdutoId,
                    NomeProduto = produto.Nome,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = produto.Preco,
                    SubTotal = item.Quantidade * produto.Preco
                });
            }

            var pedido = new Pedido
            {
                ClienteId = clienteId,
                Status = StatusPedido.Pendente,
                DataCriacao = DateTime.UtcNow,
                Itens = itensValidados.Select(i => new ItemPedido
                {
                    ProdutoId = i.ProdutoId,
                    NomeProduto = i.NomeProduto,
                    Quantidade = i.Quantidade,
                    PrecoUnitario = i.PrecoUnitario
                }).ToList()
            };

            pedido.ValorTotal = pedido.Itens.Sum(i => i.SubTotal);

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            var estoqueAtualizado = true;
            foreach (var item in pedido.Itens)
            {
                var sucesso = await _estoqueClient.AtualizarEstoqueAsync(item.ProdutoId, item.Quantidade);
                if (!sucesso)
                {
                    estoqueAtualizado = false;
                    break;
                }
            }

            if (estoqueAtualizado)
            {
                pedido.Status = StatusPedido.Confirmado;
                await _context.SaveChangesAsync();

                var eventoVenda = new VendaRealizadaEvent
                {
                    PedidoId = pedido.Id,
                    Produtos = pedido.Itens.Select(i => new ProdutoVendido
                    {
                        ProdutoId = i.ProdutoId,
                        QuantidadeVendida = i.Quantidade
                    }).ToList()
                };

                await _messagePublisher.PublishAsync("vendas.exchange", "venda.realizada", eventoVenda);

                _logger.LogInformation("Pedido criado e confirmado: {PedidoId} para cliente {ClienteId}", 
                    pedido.Id, clienteId);
            }
            else
            {
                pedido.Status = StatusPedido.Cancelado;
                await _context.SaveChangesAsync();
                
                _logger.LogWarning("Pedido cancelado devido a falha na atualização do estoque: {PedidoId}", pedido.Id);
            }

            return new PedidoDto
            {
                Id = pedido.Id,
                ClienteId = pedido.ClienteId,
                ValorTotal = pedido.ValorTotal,
                Status = pedido.Status.ToString(),
                DataCriacao = pedido.DataCriacao,
                Itens = pedido.Itens.Select(i => new ItemPedidoDto
                {
                    ProdutoId = i.ProdutoId,
                    NomeProduto = i.NomeProduto,
                    Quantidade = i.Quantidade,
                    PrecoUnitario = i.PrecoUnitario,
                    SubTotal = i.SubTotal
                }).ToList()
            };
        }

        public async Task<IEnumerable<PedidoDto>> GetPedidosAsync(string clienteId)
        {
            var pedidos = await _context.Pedidos
                .Include(p => p.Itens)
                .Where(p => p.ClienteId == clienteId)
                .OrderByDescending(p => p.DataCriacao)
                .Select(p => new PedidoDto
                {
                    Id = p.Id,
                    ClienteId = p.ClienteId,
                    ValorTotal = p.ValorTotal,
                    Status = p.Status.ToString(),
                    DataCriacao = p.DataCriacao,
                    Itens = p.Itens.Select(i => new ItemPedidoDto
                    {
                        ProdutoId = i.ProdutoId,
                        NomeProduto = i.NomeProduto,
                        Quantidade = i.Quantidade,
                        PrecoUnitario = i.PrecoUnitario,
                        SubTotal = i.SubTotal
                    }).ToList()
                })
                .ToListAsync();

            return pedidos;
        }

        public async Task<PedidoDto?> GetPedidoByIdAsync(int id, string clienteId)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == id && p.ClienteId == clienteId);

            if (pedido == null) return null;

            return new PedidoDto
            {
                Id = pedido.Id,
                ClienteId = pedido.ClienteId,
                ValorTotal = pedido.ValorTotal,
                Status = pedido.Status.ToString(),
                DataCriacao = pedido.DataCriacao,
                Itens = pedido.Itens.Select(i => new ItemPedidoDto
                {
                    ProdutoId = i.ProdutoId,
                    NomeProduto = i.NomeProduto,
                    Quantidade = i.Quantidade,
                    PrecoUnitario = i.PrecoUnitario,
                    SubTotal = i.SubTotal
                }).ToList()
            };
        }

        public async Task<bool> CancelarPedidoAsync(int id, string clienteId)
        {
            var pedido = await _context.Pedidos
                .FirstOrDefaultAsync(p => p.Id == id && p.ClienteId == clienteId);

            if (pedido == null || pedido.Status != StatusPedido.Pendente)
            {
                return false;
            }

            pedido.Status = StatusPedido.Cancelado;
            pedido.DataAtualizacao = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            _logger.LogInformation("Pedido cancelado: {PedidoId} para cliente {ClienteId}", id, clienteId);
            
            return true;
        }
    }
}

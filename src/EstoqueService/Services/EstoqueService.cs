using Microsoft.EntityFrameworkCore;
using EstoqueService.Data;
using Shared.Models;
using Shared.DTOs;
using Shared.Messaging;
using Shared.Events;

namespace EstoqueService.Services
{
    public class EstoqueService : IEstoqueService
    {
        private readonly EstoqueDbContext _context;
        private readonly IMessagePublisher _messagePublisher;
        private readonly ILogger<EstoqueService> _logger;

        public EstoqueService(EstoqueDbContext context, IMessagePublisher messagePublisher, ILogger<EstoqueService> logger)
        {
            _context = context;
            _messagePublisher = messagePublisher;
            _logger = logger;
        }

        public async Task<IEnumerable<ProdutoDto>> GetProdutosAsync()
        {
            var produtos = await _context.Produtos
                .Where(p => p.QuantidadeEstoque > 0)
                .Select(p => new ProdutoDto
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    Descricao = p.Descricao,
                    Preco = p.Preco,
                    QuantidadeEstoque = p.QuantidadeEstoque
                })
                .ToListAsync();

            return produtos;
        }

        public async Task<ProdutoDto?> GetProdutoByIdAsync(int id)
        {
            var produto = await _context.Produtos.FindAsync(id);
            if (produto == null) return null;

            return new ProdutoDto
            {
                Id = produto.Id,
                Nome = produto.Nome,
                Descricao = produto.Descricao,
                Preco = produto.Preco,
                QuantidadeEstoque = produto.QuantidadeEstoque
            };
        }

        public async Task<ProdutoDto> CriarProdutoAsync(CriarProdutoDto criarProdutoDto)
        {
            var produto = new Produto
            {
                Nome = criarProdutoDto.Nome,
                Descricao = criarProdutoDto.Descricao,
                Preco = criarProdutoDto.Preco,
                QuantidadeEstoque = criarProdutoDto.QuantidadeEstoque,
                DataCriacao = DateTime.UtcNow
            };

            _context.Produtos.Add(produto);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Produto criado: {ProdutoId} - {Nome}", produto.Id, produto.Nome);

            return new ProdutoDto
            {
                Id = produto.Id,
                Nome = produto.Nome,
                Descricao = produto.Descricao,
                Preco = produto.Preco,
                QuantidadeEstoque = produto.QuantidadeEstoque
            };
        }

        public async Task<bool> AtualizarEstoqueAsync(int produtoId, int quantidade)
        {
            var produto = await _context.Produtos.FindAsync(produtoId);
            if (produto == null) return false;

            produto.QuantidadeEstoque -= quantidade;
            produto.DataAtualizacao = DateTime.UtcNow;

            if (produto.QuantidadeEstoque < 0)
            {
                _logger.LogWarning("Tentativa de reduzir estoque abaixo de zero para produto: {ProdutoId}", produtoId);
                return false;
            }

            await _context.SaveChangesAsync();

            var eventoEstoque = new EstoqueAtualizadoEvent
            {
                ProdutoId = produto.Id,
                NovaQuantidade = produto.QuantidadeEstoque
            };

            await _messagePublisher.PublishAsync("estoque.exchange", "estoque.atualizado", eventoEstoque);

            _logger.LogInformation("Estoque atualizado para produto: {ProdutoId}, Nova quantidade: {Quantidade}", 
                produtoId, produto.QuantidadeEstoque);

            return true;
        }

        public async Task<bool> ValidarEstoqueAsync(int produtoId, int quantidadeRequerida)
        {
            var produto = await _context.Produtos.FindAsync(produtoId);
            return produto != null && produto.QuantidadeEstoque >= quantidadeRequerida;
        }
    }
}

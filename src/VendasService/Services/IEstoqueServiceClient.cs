using Shared.DTOs;

namespace VendasService.Services
{
    public interface IEstoqueServiceClient
    {
        Task<bool> ValidarEstoqueAsync(int produtoId, int quantidade);
        Task<ProdutoDto?> GetProdutoAsync(int produtoId);
        Task<bool> AtualizarEstoqueAsync(int produtoId, int quantidade);
    }
}

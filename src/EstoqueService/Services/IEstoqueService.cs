using Shared.Models;
using Shared.DTOs;

namespace EstoqueService.Services
{
    public interface IEstoqueService
    {
        Task<IEnumerable<ProdutoDto>> GetProdutosAsync();
        Task<ProdutoDto?> GetProdutoByIdAsync(int id);
        Task<ProdutoDto> CriarProdutoAsync(CriarProdutoDto criarProdutoDto);
        Task<bool> AtualizarEstoqueAsync(int produtoId, int quantidade);
        Task<bool> ValidarEstoqueAsync(int produtoId, int quantidadeRequerida);
    }
}

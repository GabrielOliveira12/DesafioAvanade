using Shared.DTOs;

namespace VendasService.Services
{
    public interface IVendasService
    {
        Task<PedidoDto?> CriarPedidoAsync(CriarPedidoDto criarPedidoDto, string clienteId);
        Task<IEnumerable<PedidoDto>> GetPedidosAsync(string clienteId);
        Task<PedidoDto?> GetPedidoByIdAsync(int id, string clienteId);
        Task<bool> CancelarPedidoAsync(int id, string clienteId);
    }
}

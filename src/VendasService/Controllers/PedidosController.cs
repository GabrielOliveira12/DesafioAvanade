using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Shared.DTOs;
using VendasService.Services;

namespace VendasService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PedidosController : ControllerBase
    {
        private readonly IVendasService _vendasService;
        private readonly ILogger<PedidosController> _logger;

        public PedidosController(IVendasService vendasService, ILogger<PedidosController> logger)
        {
            _vendasService = vendasService;
            _logger = logger;
        }

        private string GetClienteId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                   User.FindFirst("sub")?.Value ?? 
                   "usuario-anonimo";
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<PedidoDto>>> GetPedidos()
        {
            try
            {
                var clienteId = GetClienteId();
                var pedidos = await _vendasService.GetPedidosAsync(clienteId);
                return Ok(pedidos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar pedidos");
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<PedidoDto>> GetPedido(int id)
        {
            try
            {
                var clienteId = GetClienteId();
                var pedido = await _vendasService.GetPedidoByIdAsync(id, clienteId);
                
                if (pedido == null)
                {
                    return NotFound();
                }
                
                return Ok(pedido);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar pedido {PedidoId}", id);
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<PedidoDto>> CriarPedido(CriarPedidoDto criarPedidoDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var clienteId = GetClienteId();
                var pedido = await _vendasService.CriarPedidoAsync(criarPedidoDto, clienteId);
                
                if (pedido == null)
                {
                    return BadRequest("Não foi possível criar o pedido. Verifique os produtos e estoque.");
                }
                
                return CreatedAtAction(nameof(GetPedido), new { id = pedido.Id }, pedido);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar pedido");
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        [HttpPut("{id}/cancelar")]
        [Authorize]
        public async Task<ActionResult> CancelarPedido(int id)
        {
            try
            {
                var clienteId = GetClienteId();
                var sucesso = await _vendasService.CancelarPedidoAsync(id, clienteId);
                
                if (!sucesso)
                {
                    return BadRequest("Não foi possível cancelar o pedido");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cancelar pedido {PedidoId}", id);
                return StatusCode(500, "Erro interno do servidor");
            }
        }
    }
}

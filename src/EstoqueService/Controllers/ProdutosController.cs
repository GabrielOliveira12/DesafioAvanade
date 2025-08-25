using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;
using EstoqueService.Services;

namespace EstoqueService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProdutosController : ControllerBase
    {
        private readonly IEstoqueService _estoqueService;
        private readonly ILogger<ProdutosController> _logger;

        public ProdutosController(IEstoqueService estoqueService, ILogger<ProdutosController> logger)
        {
            _estoqueService = estoqueService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ProdutoDto>>> GetProdutos()
        {
            try
            {
                var produtos = await _estoqueService.GetProdutosAsync();
                return Ok(produtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar produtos");
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ProdutoDto>> GetProduto(int id)
        {
            try
            {
                var produto = await _estoqueService.GetProdutoByIdAsync(id);
                if (produto == null)
                {
                    return NotFound();
                }
                return Ok(produto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar produto {ProdutoId}", id);
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ProdutoDto>> CriarProduto(CriarProdutoDto criarProdutoDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var produto = await _estoqueService.CriarProdutoAsync(criarProdutoDto);
                return CreatedAtAction(nameof(GetProduto), new { id = produto.Id }, produto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar produto");
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        [HttpPost("{id}/validar-estoque")]
        [Authorize]
        public async Task<ActionResult<bool>> ValidarEstoque(int id, [FromBody] int quantidade)
        {
            try
            {
                var valido = await _estoqueService.ValidarEstoqueAsync(id, quantidade);
                return Ok(valido);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar estoque para produto {ProdutoId}", id);
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        [HttpPut("{id}/atualizar-estoque")]
        [Authorize]
        public async Task<ActionResult> AtualizarEstoque(int id, [FromBody] int quantidade)
        {
            try
            {
                var sucesso = await _estoqueService.AtualizarEstoqueAsync(id, quantidade);
                if (!sucesso)
                {
                    return BadRequest("Não foi possível atualizar o estoque");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar estoque para produto {ProdutoId}", id);
                return StatusCode(500, "Erro interno do servidor");
            }
        }
    }
}

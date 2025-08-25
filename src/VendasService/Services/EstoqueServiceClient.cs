using System.Text;
using System.Text.Json;
using Shared.DTOs;

namespace VendasService.Services
{
    public class EstoqueServiceClient : IEstoqueServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EstoqueServiceClient> _logger;

        public EstoqueServiceClient(HttpClient httpClient, ILogger<EstoqueServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> ValidarEstoqueAsync(int produtoId, int quantidade)
        {
            try
            {
                var json = JsonSerializer.Serialize(quantidade);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"/api/produtos/{produtoId}/validar-estoque", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<bool>(responseContent);
                }
                
                _logger.LogWarning("Falha ao validar estoque para produto {ProdutoId}. Status: {StatusCode}", 
                    produtoId, response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar estoque para produto {ProdutoId}", produtoId);
                return false;
            }
        }

        public async Task<ProdutoDto?> GetProdutoAsync(int produtoId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/produtos/{produtoId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ProdutoDto>(json, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                
                _logger.LogWarning("Falha ao buscar produto {ProdutoId}. Status: {StatusCode}", 
                    produtoId, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar produto {ProdutoId}", produtoId);
                return null;
            }
        }

        public async Task<bool> AtualizarEstoqueAsync(int produtoId, int quantidade)
        {
            try
            {
                var json = JsonSerializer.Serialize(quantidade);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync($"/api/produtos/{produtoId}/atualizar-estoque", content);
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                
                _logger.LogWarning("Falha ao atualizar estoque para produto {ProdutoId}. Status: {StatusCode}", 
                    produtoId, response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar estoque para produto {ProdutoId}", produtoId);
                return false;
            }
        }
    }
}

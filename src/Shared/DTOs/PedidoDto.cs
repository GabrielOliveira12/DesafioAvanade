namespace Shared.DTOs
{
    public class PedidoDto
    {
        public int Id { get; set; }
        public string ClienteId { get; set; } = string.Empty;
        public List<ItemPedidoDto> Itens { get; set; } = new();
        public decimal ValorTotal { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
    }

    public class CriarPedidoDto
    {
        public List<ItemPedidoDto> Itens { get; set; } = new();
    }

    public class ItemPedidoDto
    {
        public int ProdutoId { get; set; }
        public string NomeProduto { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal SubTotal { get; set; }
    }
}

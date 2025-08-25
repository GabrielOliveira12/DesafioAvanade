namespace Shared.Models
{
    public enum StatusPedido
    {
        Pendente = 1,
        Confirmado = 2,
        Cancelado = 3,
        Entregue = 4
    }

    public class Pedido
    {
        public int Id { get; set; }
        public string ClienteId { get; set; } = string.Empty;
        public List<ItemPedido> Itens { get; set; } = new();
        public decimal ValorTotal { get; set; }
        public StatusPedido Status { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public DateTime? DataAtualizacao { get; set; }
    }

    public class ItemPedido
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public int ProdutoId { get; set; }
        public string NomeProduto { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal SubTotal => Quantidade * PrecoUnitario;
    }
}

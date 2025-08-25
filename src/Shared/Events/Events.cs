namespace Shared.Events
{
    public class VendaRealizadaEvent
    {
        public int PedidoId { get; set; }
        public List<ProdutoVendido> Produtos { get; set; } = new();
        public DateTime DataVenda { get; set; } = DateTime.UtcNow;
    }

    public class ProdutoVendido
    {
        public int ProdutoId { get; set; }
        public int QuantidadeVendida { get; set; }
    }

    public class EstoqueAtualizadoEvent
    {
        public int ProdutoId { get; set; }
        public int NovaQuantidade { get; set; }
        public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;
    }
}

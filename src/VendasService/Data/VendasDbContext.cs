using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace VendasService.Data
{
    public class VendasDbContext : DbContext
    {
        public VendasDbContext(DbContextOptions<VendasDbContext> options) : base(options)
        {
        }

        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<ItemPedido> ItensPedido { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Pedido>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ClienteId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ValorTotal).HasPrecision(18, 2);
                entity.Property(e => e.Status).HasConversion<int>();
                
                entity.HasMany(p => p.Itens)
                      .WithOne()
                      .HasForeignKey(i => i.PedidoId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ItemPedido>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NomeProduto).IsRequired().HasMaxLength(200);
                entity.Property(e => e.PrecoUnitario).HasPrecision(18, 2);
                entity.Ignore(e => e.SubTotal);
            });
        }
    }
}

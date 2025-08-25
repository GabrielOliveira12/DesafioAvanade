using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace EstoqueService.Data
{
    public class EstoqueDbContext : DbContext
    {
        public EstoqueDbContext(DbContextOptions<EstoqueDbContext> options) : base(options)
        {
        }

        public DbSet<Produto> Produtos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Produto>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Descricao).HasMaxLength(1000);
                entity.Property(e => e.Preco).HasPrecision(18, 2);
                entity.Property(e => e.QuantidadeEstoque).IsRequired();
            });

            modelBuilder.Entity<Produto>().HasData(
                new Produto { Id = 1, Nome = "Notebook Gamer", Descricao = "Notebook para jogos de alta performance", Preco = 2500.00m, QuantidadeEstoque = 10 },
                new Produto { Id = 2, Nome = "Mouse Gamer", Descricao = "Mouse óptico para jogos", Preco = 150.00m, QuantidadeEstoque = 50 },
                new Produto { Id = 3, Nome = "Teclado Mecânico", Descricao = "Teclado mecânico RGB", Preco = 300.00m, QuantidadeEstoque = 25 }
            );
        }
    }
}

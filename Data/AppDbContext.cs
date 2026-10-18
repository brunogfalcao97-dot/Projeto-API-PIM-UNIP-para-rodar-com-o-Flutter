using Microsoft.EntityFrameworkCore;
using PimTintas.Api.Models;

namespace PimTintas.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<Cor> Cores => Set<Cor>();
    public DbSet<TipoTinta> TiposTinta => Set<TipoTinta>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Cupom> Cupons => Set<Cupom>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoItem> PedidoItens => Set<PedidoItem>();
    public DbSet<Favorito> Favoritos => Set<Favorito>();
    public DbSet<Simulacao> Simulacoes => Set<Simulacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.Property(u => u.Nome).HasMaxLength(100).IsRequired();
            e.Property(u => u.Email).HasMaxLength(150).IsRequired();
            e.Property(u => u.SenhaHash).HasMaxLength(100).IsRequired();
            e.Property(u => u.Telefone).HasMaxLength(20);
            // O valor padrão do banco faz os usuários que já existiam virarem "Cliente" na migration.
            e.Property(u => u.Perfil).HasMaxLength(20).IsRequired().HasDefaultValue(Perfis.Cliente);
            e.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Marca>(e =>
        {
            e.ToTable("Marcas");
            e.Property(m => m.Nome).HasMaxLength(80).IsRequired();
            e.Property(m => m.Descricao).HasMaxLength(300);
            e.HasIndex(m => m.Nome).IsUnique();
        });

        modelBuilder.Entity<Cor>(e =>
        {
            e.ToTable("Cores");
            e.Property(c => c.Nome).HasMaxLength(80).IsRequired();
            e.Property(c => c.CodigoHex).HasMaxLength(7).IsRequired();
            e.Property(c => c.Familia).HasMaxLength(40);
            e.Property(c => c.CodigoPantone).HasMaxLength(40);
        });

        modelBuilder.Entity<TipoTinta>(e =>
        {
            e.ToTable("TiposTinta");
            e.Property(t => t.Nome).HasMaxLength(80).IsRequired();
            e.Property(t => t.Acabamento).HasMaxLength(30).IsRequired();
            e.Property(t => t.Descricao).HasMaxLength(300);
            e.Property(t => t.RendimentoM2PorLitro).HasPrecision(6, 2);
            e.Property(t => t.FatorContraste).HasPrecision(4, 2);
        });

        modelBuilder.Entity<Produto>(e =>
        {
            e.ToTable("Produtos");
            e.Property(p => p.Sku).HasMaxLength(40).IsRequired();
            e.Property(p => p.VolumeLitros).HasPrecision(6, 2);
            e.Property(p => p.Preco).HasPrecision(10, 2);
            e.Property(p => p.RowVersion).IsRowVersion();
            e.HasIndex(p => p.Sku).IsUnique();

            e.HasOne(p => p.Marca).WithMany().HasForeignKey(p => p.MarcaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.TipoTinta).WithMany().HasForeignKey(p => p.TipoTintaId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Cupom>(e =>
        {
            e.ToTable("Cupons");
            e.Property(c => c.Codigo).HasMaxLength(40).IsRequired();
            e.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.Valor).HasPrecision(10, 2);
            e.Property(c => c.ValorMinimoPedido).HasPrecision(10, 2);
            e.Property(c => c.RowVersion).IsRowVersion();
            e.HasIndex(c => c.Codigo).IsUnique();
        });

        modelBuilder.Entity<Pedido>(e =>
        {
            e.ToTable("Pedidos");
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.Subtotal).HasPrecision(10, 2);
            e.Property(p => p.DescontoVolume).HasPrecision(10, 2);
            e.Property(p => p.DescontoCupom).HasPrecision(10, 2);
            e.Property(p => p.Total).HasPrecision(10, 2);
            e.Property(p => p.CupomCodigo).HasMaxLength(40);

            // O histórico de compras não some se alguém apagar o usuário ou o cupom.
            e.HasOne(p => p.Usuario).WithMany(u => u.Pedidos).HasForeignKey(p => p.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Cupom).WithMany().HasForeignKey(p => p.CupomId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(p => new { p.UsuarioId, p.CriadoEm });
        });

        modelBuilder.Entity<PedidoItem>(e =>
        {
            e.ToTable("PedidoItens");
            e.Property(i => i.PrecoUnitario).HasPrecision(10, 2);
            e.Property(i => i.VolumeLitros).HasPrecision(6, 2);
            e.Property(i => i.ProdutoDescricao).HasMaxLength(200).IsRequired();
            e.Property(i => i.CorNome).HasMaxLength(80).IsRequired();
            e.Property(i => i.CorHex).HasMaxLength(7).IsRequired();

            e.HasOne(i => i.Pedido).WithMany(p => p.Itens).HasForeignKey(i => i.PedidoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Produto).WithMany().HasForeignKey(i => i.ProdutoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.Cor).WithMany().HasForeignKey(i => i.CorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Favorito>(e =>
        {
            e.ToTable("Favoritos");
            e.HasKey(f => new { f.UsuarioId, f.CorId });
            e.HasOne(f => f.Usuario).WithMany(u => u.Favoritos).HasForeignKey(f => f.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(f => f.Cor).WithMany().HasForeignKey(f => f.CorId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Simulacao>(e =>
        {
            e.ToTable("Simulacoes");
            e.Property(s => s.CaminhoImagem).HasMaxLength(300).IsRequired();
            e.Property(s => s.Nome).HasMaxLength(100);
            e.Property(s => s.AreaM2).HasPrecision(8, 2);

            e.HasOne(s => s.Usuario).WithMany(u => u.Simulacoes).HasForeignKey(s => s.UsuarioId).OnDelete(DeleteBehavior.Cascade);

            // Não deixa apagar uma cor/tipo que já foi usado em simulações.
            e.HasOne(s => s.Cor).WithMany().HasForeignKey(s => s.CorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.TipoTinta).WithMany().HasForeignKey(s => s.TipoTintaId).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(s => new { s.UsuarioId, s.CriadoEm });
        });
    }
}

namespace PimTintas.Api.Models;

/// <summary>Cliente (ou administrador) do site/app. A senha nunca é guardada em texto: só o hash BCrypt.</summary>
public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;

    /// <summary>"Cliente" ou "Admin" (veja <see cref="Perfis"/>).</summary>
    public string Perfil { get; set; } = Perfis.Cliente;

    public string? Telefone { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public ICollection<Simulacao> Simulacoes { get; set; } = new List<Simulacao>();
    public ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();
    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
}

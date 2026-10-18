namespace PimTintas.Api.Models;

/// <summary>Cor favoritada por um cliente.</summary>
public class Favorito
{
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int CorId { get; set; }
    public Cor Cor { get; set; } = null!;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

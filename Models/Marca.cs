namespace PimTintas.Api.Models;

/// <summary>Marca de tinta vendida na loja.</summary>
public class Marca
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }

    /// <summary>Marca desativada some do catálogo, mas o histórico de pedidos continua válido.</summary>
    public bool Inativa { get; set; }
}

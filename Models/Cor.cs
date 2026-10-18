namespace PimTintas.Api.Models;

/// <summary>Cor do catálogo. O RGB é calculado a partir do HEX (não é guardado duas vezes).</summary>
public class Cor
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;

    /// <summary>Código no formato #RRGGBB.</summary>
    public string CodigoHex { get; set; } = string.Empty;

    public string? Familia { get; set; }

    /// <summary>Código Pantone equivalente, quando houver (ex.: "PANTONE 19-4052").</summary>
    public string? CodigoPantone { get; set; }

    public bool Inativa { get; set; }
}

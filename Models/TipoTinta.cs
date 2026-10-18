namespace PimTintas.Api.Models;

/// <summary>
/// Tipo de tinta com seu acabamento (ex.: Acrílica Fosco). Concentra os parâmetros
/// técnicos que o app usa, para a regra ficar só na API.
/// </summary>
public class TipoTinta
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;

    /// <summary>Fosco, Acetinado ou Brilhante.</summary>
    public string Acabamento { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    /// <summary>Quantos m² um litro cobre em uma demão. Base do cálculo de litros.</summary>
    public decimal RendimentoM2PorLitro { get; set; } = 10m;

    /// <summary>Quantidade de demãos que o fabricante recomenda.</summary>
    public int DemaosRecomendadas { get; set; } = 2;

    /// <summary>
    /// Parâmetro visual da simulação: quanto do sombreamento original da foto é mantido
    /// (fosco &lt; acetinado &lt; brilhante). O app só aplica o número que vem daqui.
    /// </summary>
    public decimal FatorContraste { get; set; } = 0.95m;

    public bool Inativo { get; set; }
}

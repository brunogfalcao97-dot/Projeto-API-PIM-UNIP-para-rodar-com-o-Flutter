using System.ComponentModel.DataAnnotations;

namespace PimTintas.Api.Dtos;

/// <summary>
/// Informe a altura e a largura da parede OU a área total em m².
/// "AreaDescontarM2" é a soma de portas, janelas e outras partes que não serão pintadas.
/// </summary>
public class CalculoTintaRequest
{
    [Range(0.1, 1000, ErrorMessage = "A altura deve estar entre 0,1 e 1000 metros.")]
    public decimal? AlturaM { get; set; }

    [Range(0.1, 1000, ErrorMessage = "A largura deve estar entre 0,1 e 1000 metros.")]
    public decimal? LarguraM { get; set; }

    [Range(0.1, 100000, ErrorMessage = "A área deve ser maior que zero.")]
    public decimal? AreaM2 { get; set; }

    [Range(0, 100000, ErrorMessage = "A área a descontar não pode ser negativa.")]
    public decimal AreaDescontarM2 { get; set; }

    /// <summary>Se não informado, usa as demãos recomendadas do tipo de tinta.</summary>
    [Range(1, 6, ErrorMessage = "Informe de 1 a 6 demãos.")]
    public int? Demaos { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Escolha um tipo de tinta.")]
    public int TipoTintaId { get; set; }

    /// <summary>Opcional: limita as sugestões a uma marca.</summary>
    public int? MarcaId { get; set; }
}

public record ItemOpcaoDto(
    int ProdutoId,
    string Sku,
    string Descricao,
    decimal VolumeLitros,
    int Quantidade,
    decimal PrecoUnitario,
    decimal Subtotal);

/// <summary>Uma forma de comprar a quantidade necessária (a mais barata de cada marca).</summary>
public record OpcaoCompraDto(
    int MarcaId,
    string MarcaNome,
    List<ItemOpcaoDto> Itens,
    decimal LitrosComprados,
    decimal SobraLitros,
    decimal Total);

public record CalculoTintaDto(
    decimal AreaBrutaM2,
    decimal AreaDescontadaM2,
    decimal AreaLiquidaM2,
    int Demaos,
    decimal RendimentoM2PorLitro,
    decimal MargemPerdaPercentual,
    decimal LitrosNecessarios,
    int TipoTintaId,
    string TipoTintaNome,
    string? Aviso,
    List<OpcaoCompraDto> Opcoes);

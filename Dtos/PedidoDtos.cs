using System.ComponentModel.DataAnnotations;

namespace PimTintas.Api.Dtos;

public class ItemCarrinhoRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Produto inválido.")]
    public int ProdutoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Escolha a cor da tinta.")]
    public int CorId { get; set; }

    [Range(1, 1000, ErrorMessage = "A quantidade deve ser de 1 a 1000.")]
    public int Quantidade { get; set; }
}

/// <summary>Usado tanto para o orçamento (só calcula) quanto para criar o pedido.</summary>
public class CarrinhoRequest
{
    [Required(ErrorMessage = "O carrinho está vazio.")]
    [MinLength(1, ErrorMessage = "O carrinho está vazio.")]
    [MaxLength(50, ErrorMessage = "O carrinho aceita até 50 itens diferentes.")]
    public List<ItemCarrinhoRequest> Itens { get; set; } = new();

    [StringLength(40, ErrorMessage = "Cupom inválido.")]
    public string? Cupom { get; set; }
}

public record LinhaPedidoDto(
    int ProdutoId,
    string ProdutoDescricao,
    decimal VolumeLitros,
    int CorId,
    string CorNome,
    string CorHex,
    int Quantidade,
    decimal PrecoUnitario,
    decimal Total);

/// <summary>Resultado de "quanto custa": preços, descontos e total calculados pela API.</summary>
public record OrcamentoDto(
    List<LinhaPedidoDto> Itens,
    decimal LitrosTotais,
    decimal Subtotal,
    decimal DescontoVolume,
    decimal DescontoCupom,
    decimal Total,
    string? Cupom);

public record PedidoResumoDto(int Id, DateTime CriadoEm, string Status, decimal Total, int QuantidadeItens);

public record PedidoDto(
    int Id,
    DateTime CriadoEm,
    string Status,
    List<LinhaPedidoDto> Itens,
    decimal Subtotal,
    decimal DescontoVolume,
    decimal DescontoCupom,
    decimal Total,
    string? Cupom,
    bool PodeCancelar);

namespace PimTintas.Api.Models;

/// <summary>Cupom de desconto. O código é guardado em maiúsculas.</summary>
public class Cupom
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public TipoDesconto Tipo { get; set; } = TipoDesconto.Percentual;

    /// <summary>Porcentagem (Tipo = Percentual) ou reais (Tipo = ValorFixo).</summary>
    public decimal Valor { get; set; }

    /// <summary>Valor mínimo do pedido para o cupom valer (0 = sem mínimo).</summary>
    public decimal ValorMinimoPedido { get; set; }

    public DateTime? ValidoAte { get; set; }

    /// <summary>Quantas vezes o cupom pode ser usado no total (null = ilimitado).</summary>
    public int? UsosMaximos { get; set; }

    public int UsosRealizados { get; set; }
    public bool Inativo { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

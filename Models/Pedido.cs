namespace PimTintas.Api.Models;

/// <summary>Pedido de compra. Os valores ficam gravados no momento da compra (não mudam se o preço mudar depois).</summary>
public class Pedido
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public StatusPedido Status { get; set; } = StatusPedido.Pendente;

    public decimal Subtotal { get; set; }
    public decimal DescontoVolume { get; set; }
    public decimal DescontoCupom { get; set; }
    public decimal Total { get; set; }

    public int? CupomId { get; set; }
    public Cupom? Cupom { get; set; }

    /// <summary>Código do cupom usado, guardado como texto para o histórico.</summary>
    public string? CupomCodigo { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }

    public ICollection<PedidoItem> Itens { get; set; } = new List<PedidoItem>();
}

public class PedidoItem
{
    public int Id { get; set; }

    public int PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;

    public int ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;

    public int CorId { get; set; }
    public Cor Cor { get; set; } = null!;

    public int Quantidade { get; set; }

    // Dados congelados na hora da compra.
    public decimal PrecoUnitario { get; set; }
    public string ProdutoDescricao { get; set; } = string.Empty;
    public decimal VolumeLitros { get; set; }
    public string CorNome { get; set; } = string.Empty;
    public string CorHex { get; set; } = string.Empty;
}

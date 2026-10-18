namespace PimTintas.Api.Models;

/// <summary>
/// Item vendável: uma lata de uma marca, de um tipo de tinta, em um volume.
/// A cor é escolhida no pedido (a tinta é preparada na cor, como nas lojas).
/// </summary>
public class Produto
{
    public int Id { get; set; }

    /// <summary>Código único do produto no estoque.</summary>
    public string Sku { get; set; } = string.Empty;

    public int MarcaId { get; set; }
    public Marca Marca { get; set; } = null!;

    public int TipoTintaId { get; set; }
    public TipoTinta TipoTinta { get; set; } = null!;

    /// <summary>Volume da lata em litros (múltiplo de 0,1).</summary>
    public decimal VolumeLitros { get; set; }

    public decimal Preco { get; set; }
    public int Estoque { get; set; }
    public bool Inativo { get; set; }

    /// <summary>Controle de concorrência: impede dois pedidos de baixarem o mesmo estoque ao mesmo tempo.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

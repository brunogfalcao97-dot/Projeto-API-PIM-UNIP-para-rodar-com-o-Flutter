using System.Globalization;
using PimTintas.Api.Dtos;
using PimTintas.Api.Models;
using PimTintas.Api.Services;

namespace PimTintas.Api.Extensions;

/// <summary>Conversões de entidade para DTO e pequenos formatadores de texto.</summary>
public static class Mapeamento
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>"#F8F8F5" vira (248, 248, 245). HEX inválido vira preto, sem derrubar a API.</summary>
    public static RgbDto ParaRgb(string hex)
    {
        var limpo = hex.TrimStart('#');
        if (limpo.Length != 6
            || !int.TryParse(limpo[..2], NumberStyles.HexNumber, Inv, out var r)
            || !int.TryParse(limpo.Substring(2, 2), NumberStyles.HexNumber, Inv, out var g)
            || !int.TryParse(limpo.Substring(4, 2), NumberStyles.HexNumber, Inv, out var b))
            return new RgbDto(0, 0, 0);

        return new RgbDto(r, g, b);
    }

    /// <summary>0.9 vira "0,9"; 18 vira "18"; 3.6 vira "3,6".</summary>
    public static string Volume(decimal litros)
    {
        var texto = litros.ToString("0.0#", Inv);
        if (texto.EndsWith(".0", StringComparison.Ordinal)) texto = texto[..^2];
        return texto.Replace('.', ',');
    }

    /// <summary>Valor em reais no formato "R$ 1234,50" (sem depender da cultura do servidor).</summary>
    public static string Moeda(decimal valor) =>
        "R$ " + valor.ToString("0.00", Inv).Replace('.', ',');

    public static string DescricaoProduto(string marca, string tipo, string acabamento, decimal volume) =>
        $"{marca} {tipo} {acabamento} {Volume(volume)}L";

    /// <summary>Exige Marca e TipoTinta carregados (Include).</summary>
    public static string Descricao(this Produto p) =>
        DescricaoProduto(p.Marca.Nome, p.TipoTinta.Nome, p.TipoTinta.Acabamento, p.VolumeLitros);

    public static CorDto ParaDto(this Cor c) =>
        new(c.Id, c.Nome, c.CodigoHex, ParaRgb(c.CodigoHex), c.Familia, c.CodigoPantone);

    public static TipoTintaDto ParaDto(this TipoTinta t) =>
        new(t.Id, t.Nome, t.Acabamento, t.Descricao, t.RendimentoM2PorLitro, t.DemaosRecomendadas, t.FatorContraste);

    public static MarcaDto ParaDto(this Marca m) => new(m.Id, m.Nome, m.Descricao);

    /// <summary>Exige Marca e TipoTinta carregados (Include).</summary>
    public static ProdutoDto ParaDto(this Produto p) =>
        new(p.Id, p.Sku, p.Descricao(), p.MarcaId, p.Marca.Nome, p.TipoTintaId, p.TipoTinta.Nome,
            p.TipoTinta.Acabamento, p.VolumeLitros, p.Preco, p.Estoque, p.Estoque > 0);

    public static LinhaPedidoDto ParaDto(this PedidoItem i) =>
        new(i.ProdutoId, i.ProdutoDescricao, i.VolumeLitros, i.CorId, i.CorNome, i.CorHex, i.Quantidade,
            i.PrecoUnitario, i.PrecoUnitario * i.Quantidade);

    /// <summary>Exige Itens carregados (Include).</summary>
    public static PedidoDto ParaDto(this Pedido p) =>
        new(p.Id, p.CriadoEm, p.Status.ToString(), p.Itens.Select(i => i.ParaDto()).ToList(),
            p.Subtotal, p.DescontoVolume, p.DescontoCupom, p.Total, p.CupomCodigo,
            RegrasPedido.ClientePodeCancelar(p.Status));

    public static CupomAdminDto ParaAdminDto(this Cupom c) =>
        new(c.Id, c.Codigo, c.Tipo.ToString(), c.Valor, c.ValorMinimoPedido, c.ValidoAte, c.UsosMaximos,
            c.UsosRealizados, c.Inativo);
}

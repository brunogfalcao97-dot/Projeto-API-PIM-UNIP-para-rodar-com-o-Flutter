using PimTintas.Api.Extensions;
using PimTintas.Api.Models;

namespace PimTintas.Api.Services;

public record ResultadoPreco(decimal Subtotal, decimal DescontoVolume, decimal DescontoCupom, decimal Total);

/// <summary>
/// Regras de preço, desconto e cupom. Sem banco e sem HTTP: só contas.
///
/// Ordem: subtotal → desconto por volume → cupom sobre o que sobrou → total.
/// O desconto por volume e o cupom acumulam.
/// </summary>
public static class Precificador
{
    public static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    public static decimal DescontoVolume(decimal subtotal, LojaOptions loja)
    {
        if (loja.DescontoVolumePercentual <= 0 || subtotal < loja.DescontoVolumeAcimaDe) return 0m;
        return Arredondar(subtotal * loja.DescontoVolumePercentual / 100m);
    }

    /// <summary>Devolve a mensagem de erro se o cupom não puder ser usado, ou null se estiver tudo certo.</summary>
    public static string? ValidarCupom(Cupom cupom, decimal baseCalculo, DateTime agoraUtc)
    {
        if (cupom.Inativo) return "Este cupom não está mais ativo.";
        if (cupom.ValidoAte is { } validade && agoraUtc > validade) return "Este cupom expirou.";
        if (cupom.UsosMaximos is { } maximo && cupom.UsosRealizados >= maximo) return "Este cupom esgotou.";
        if (baseCalculo < cupom.ValorMinimoPedido)
            return $"Este cupom vale para pedidos a partir de {Mapeamento.Moeda(cupom.ValorMinimoPedido)}.";
        return null;
    }

    public static decimal DescontoCupom(Cupom cupom, decimal baseCalculo) => cupom.Tipo switch
    {
        TipoDesconto.Percentual => Arredondar(baseCalculo * Math.Min(cupom.Valor, 100m) / 100m),
        TipoDesconto.ValorFixo => Math.Min(cupom.Valor, baseCalculo),
        _ => 0m,
    };

    /// <summary>O cupom precisa ter sido validado antes (veja <see cref="ValidarCupom"/>).</summary>
    public static ResultadoPreco Calcular(decimal subtotal, LojaOptions loja, Cupom? cupom)
    {
        subtotal = Arredondar(subtotal);
        var volume = DescontoVolume(subtotal, loja);
        var baseCupom = subtotal - volume;
        var desconto = cupom is null ? 0m : DescontoCupom(cupom, baseCupom);
        var total = Math.Max(0m, baseCupom - desconto);
        return new ResultadoPreco(subtotal, volume, desconto, Arredondar(total));
    }
}

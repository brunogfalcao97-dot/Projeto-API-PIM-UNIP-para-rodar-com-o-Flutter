namespace PimTintas.Api.Services;

/// <summary>
/// Contas de quanta tinta comprar. Sem banco e sem HTTP: só números, para ser fácil de testar.
/// </summary>
public static class CalculadoraTinta
{
    public record Lata(int ProdutoId, decimal VolumeLitros, decimal Preco, int Estoque);

    /// <summary>
    /// litros = área × demãos ÷ rendimento, mais a margem de perda, arredondado PARA CIMA em décimos de litro.
    /// </summary>
    public static decimal CalcularLitros(
        decimal areaLiquidaM2, int demaos, decimal rendimentoM2PorLitro, decimal margemPerdaPercentual)
    {
        var litros = areaLiquidaM2 * demaos / rendimentoM2PorLitro * (1m + margemPerdaPercentual / 100m);
        return Math.Ceiling(litros * 10m) / 10m;
    }

    /// <summary>
    /// Acha a combinação de latas MAIS BARATA que cobre pelo menos <paramref name="litrosNecessarios"/>,
    /// respeitando o estoque de cada lata. Em caso de empate no preço, escolhe a que sobra menos tinta.
    /// Devolve null se o estoque total não chega.
    ///
    /// Programação dinâmica (problema da mochila): trabalha em décimos de litro e centavos, então não
    /// há erro de arredondamento. Os volumes das latas precisam ser múltiplos de 0,1 L.
    /// </summary>
    public static IReadOnlyList<(Lata Lata, int Quantidade)>? MelhorCombinacao(
        IReadOnlyList<Lata> latas, decimal litrosNecessarios)
    {
        if (latas.Count == 0 || litrosNecessarios <= 0) return null;

        var alvo = (int)Math.Ceiling(litrosNecessarios * 10m);
        var volumes = latas.Select(l => (int)Math.Round(l.VolumeLitros * 10m)).ToArray();
        var custos = latas.Select(l => (long)Math.Round(l.Preco * 100m, MidpointRounding.AwayFromZero)).ToArray();

        // Pode passar do alvo em até uma lata grande.
        var capacidade = alvo + volumes.Max();

        const long Infinito = long.MaxValue / 4;
        var custo = new long[capacidade + 1];
        Array.Fill(custo, Infinito);
        custo[0] = 0;

        var escolhas = new int[latas.Count][];

        for (var i = 0; i < latas.Count; i++)
        {
            var escolha = new int[capacidade + 1];
            escolhas[i] = escolha;
            if (volumes[i] <= 0) continue; // lata inválida: ignorada

            var maximo = Math.Min(latas[i].Estoque, capacidade / volumes[i]);
            var novo = new long[capacidade + 1];
            Array.Fill(novo, Infinito);

            for (var j = 0; j <= capacidade; j++)
            {
                if (custo[j] >= Infinito) continue;

                for (var k = 0; k <= maximo; k++)
                {
                    var destino = j + k * volumes[i];
                    if (destino > capacidade) break;

                    var total = custo[j] + k * custos[i];
                    if (total < novo[destino])
                    {
                        novo[destino] = total;
                        escolha[destino] = k;
                    }
                }
            }
            custo = novo;
        }

        // Entre as quantidades que cobrem o alvo, a mais barata (a menor em caso de empate).
        var melhor = -1;
        var melhorCusto = Infinito;
        for (var j = alvo; j <= capacidade; j++)
        {
            if (custo[j] < melhorCusto)
            {
                melhorCusto = custo[j];
                melhor = j;
            }
        }
        if (melhor < 0) return null;

        // Volta pelas escolhas para descobrir quantas latas de cada tipo entram.
        var resultado = new List<(Lata, int)>();
        var restante = melhor;
        for (var i = latas.Count - 1; i >= 0; i--)
        {
            var k = escolhas[i][restante];
            if (k > 0) resultado.Add((latas[i], k));
            restante -= k * volumes[i];
        }

        resultado.Reverse();
        return resultado;
    }
}

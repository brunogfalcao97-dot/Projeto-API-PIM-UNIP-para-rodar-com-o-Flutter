using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PimTintas.Api.Data;
using PimTintas.Api.Dtos;
using PimTintas.Api.Extensions;
using PimTintas.Api.Services;

namespace PimTintas.Api.Controllers;

[ApiController]
[Route("api/calculos")]
[AllowAnonymous]
public class CalculosController(AppDbContext db, IOptions<LojaOptions> opcoes) : ControllerBase
{
    /// <summary>
    /// Quanta tinta comprar. Recebe as medidas da parede (m²), calcula os litros pelo rendimento do tipo de
    /// tinta e sugere, para cada marca, a combinação de latas mais barata que cobre o necessário.
    /// </summary>
    [HttpPost("tinta")]
    [ProducesResponseType<CalculoTintaDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CalcularTinta(CalculoTintaRequest requisicao, CancellationToken ct)
    {
        var loja = opcoes.Value;

        decimal areaBruta;
        if (requisicao.AlturaM is { } altura && requisicao.LarguraM is { } largura)
            areaBruta = altura * largura;
        else if (requisicao.AreaM2 is { } area)
            areaBruta = area;
        else
            return Problem(
                detail: "Informe a altura e a largura da parede, ou a área em m².",
                statusCode: StatusCodes.Status400BadRequest);

        areaBruta = Math.Round(areaBruta, 2, MidpointRounding.AwayFromZero);
        var descontada = Math.Round(requisicao.AreaDescontarM2, 2, MidpointRounding.AwayFromZero);

        if (descontada >= areaBruta)
            return Problem(
                detail: "A área a descontar (portas e janelas) precisa ser menor que a área da parede.",
                statusCode: StatusCodes.Status400BadRequest);

        var areaLiquida = areaBruta - descontada;
        if (areaLiquida > loja.AreaMaximaM2)
            return Problem(
                detail: $"A área máxima aceita no cálculo é {loja.AreaMaximaM2:0} m².",
                statusCode: StatusCodes.Status400BadRequest);

        var tipo = await db.TiposTinta.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == requisicao.TipoTintaId && !t.Inativo, ct);
        if (tipo is null)
            return Problem(detail: "Tipo de tinta não encontrado.", statusCode: StatusCodes.Status404NotFound);

        if (tipo.RendimentoM2PorLitro <= 0)
            return Problem(
                detail: "Este tipo de tinta ainda não tem o rendimento cadastrado.",
                statusCode: StatusCodes.Status422UnprocessableEntity);

        var demaos = requisicao.Demaos ?? tipo.DemaosRecomendadas;
        var litros = CalculadoraTinta.CalcularLitros(areaLiquida, demaos, tipo.RendimentoM2PorLitro, loja.MargemPerdaPercentual);

        // Latas em estoque deste tipo de tinta (e da marca escolhida, se houver).
        var consulta = db.Produtos
            .AsNoTracking()
            .Include(p => p.Marca)
            .Where(p => p.TipoTintaId == tipo.Id && !p.Inativo && !p.Marca.Inativa && p.Estoque > 0);

        if (requisicao.MarcaId is { } marcaId) consulta = consulta.Where(p => p.MarcaId == marcaId);

        var produtos = await consulta.ToListAsync(ct);

        var listaOpcoes = new List<OpcaoCompraDto>();
        foreach (var grupo in produtos.GroupBy(p => p.MarcaId))
        {
            var latas = grupo
                .Select(p => new CalculadoraTinta.Lata(p.Id, p.VolumeLitros, p.Preco, p.Estoque))
                .ToList();

            var combinacao = CalculadoraTinta.MelhorCombinacao(latas, litros);
            if (combinacao is null) continue; // o estoque desta marca não chega

            var porId = grupo.ToDictionary(p => p.Id);
            var itens = combinacao
                .Select(c =>
                {
                    var p = porId[c.Lata.ProdutoId];
                    return new ItemOpcaoDto(
                        p.Id, p.Sku,
                        Mapeamento.DescricaoProduto(p.Marca.Nome, tipo.Nome, tipo.Acabamento, p.VolumeLitros),
                        p.VolumeLitros, c.Quantidade, p.Preco, p.Preco * c.Quantidade);
                })
                .ToList();

            var comprados = itens.Sum(i => i.VolumeLitros * i.Quantidade);
            var marca = grupo.First().Marca;
            listaOpcoes.Add(new OpcaoCompraDto(
                marca.Id, marca.Nome, itens, comprados, comprados - litros, itens.Sum(i => i.Subtotal)));
        }

        listaOpcoes = listaOpcoes.OrderBy(o => o.Total).ToList();

        var aviso = listaOpcoes.Count == 0
            ? "Nenhuma marca tem estoque suficiente deste tipo de tinta no momento."
            : null;

        return Ok(new CalculoTintaDto(
            areaBruta, descontada, areaLiquida, demaos, tipo.RendimentoM2PorLitro, loja.MargemPerdaPercentual,
            litros, tipo.Id, $"{tipo.Nome} {tipo.Acabamento}", aviso, listaOpcoes));
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PimTintas.Api.Data;
using PimTintas.Api.Dtos;
using PimTintas.Api.Extensions;
using PimTintas.Api.Models;

namespace PimTintas.Api.Services;

public record LinhaCotada(Produto Produto, Cor Cor, int Quantidade);

public class Cotacao
{
    public List<LinhaCotada> Linhas { get; init; } = new();
    public ResultadoPreco Preco { get; init; } = null!;
    public Cupom? Cupom { get; init; }
}

/// <summary>Sucesso (Cotacao preenchida) ou erro com a mensagem pronta para mostrar ao cliente.</summary>
public record ResultadoCotacao(Cotacao? Cotacao, string? Erro, int StatusErro = StatusCodes.Status400BadRequest)
{
    public bool Ok => Cotacao is not null;
}

/// <summary>
/// Tudo o que envolve fechar uma compra: validar o carrinho, conferir estoque, aplicar descontos e
/// cupom, gravar o pedido e devolver o estoque se ele for cancelado.
/// </summary>
public class PedidoService(AppDbContext db, IOptions<LojaOptions> opcoes)
{
    private readonly LojaOptions _loja = opcoes.Value;

    private static ResultadoCotacao Falha(string mensagem, int status = StatusCodes.Status400BadRequest) =>
        new(null, mensagem, status);

    /// <summary>Confere o carrinho e calcula preços, descontos e total. Não grava nada.</summary>
    public async Task<ResultadoCotacao> CotarAsync(CarrinhoRequest requisicao, CancellationToken ct)
    {
        // O mesmo produto na mesma cor, repetido, vira uma linha só.
        var agrupados = requisicao.Itens
            .GroupBy(i => (i.ProdutoId, i.CorId))
            .Select(g => (g.Key.ProdutoId, g.Key.CorId, Quantidade: g.Sum(x => x.Quantidade)))
            .ToList();

        if (agrupados.Any(i => i.Quantidade > _loja.QuantidadeMaximaPorItem))
            return Falha($"A quantidade máxima por item é {_loja.QuantidadeMaximaPorItem}.");

        var idsProduto = agrupados.Select(i => i.ProdutoId).Distinct().ToList();
        var idsCor = agrupados.Select(i => i.CorId).Distinct().ToList();

        var produtos = await db.Produtos
            .Include(p => p.Marca)
            .Include(p => p.TipoTinta)
            .Where(p => idsProduto.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var cores = await db.Cores
            .Where(c => idsCor.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);

        var linhas = new List<LinhaCotada>();
        foreach (var item in agrupados)
        {
            if (!produtos.TryGetValue(item.ProdutoId, out var produto)
                || produto.Inativo || produto.Marca.Inativa || produto.TipoTinta.Inativo)
                return Falha("Um dos produtos do carrinho não está mais disponível.");

            if (!cores.TryGetValue(item.CorId, out var cor) || cor.Inativa)
                return Falha("Uma das cores do carrinho não está mais disponível.");

            linhas.Add(new LinhaCotada(produto, cor, item.Quantidade));
        }

        // O estoque é do produto: soma as quantidades de todas as cores do mesmo produto.
        foreach (var grupo in linhas.GroupBy(l => l.Produto))
        {
            var pedida = grupo.Sum(l => l.Quantidade);
            if (pedida > grupo.Key.Estoque)
            {
                var mensagem = grupo.Key.Estoque == 0
                    ? $"{grupo.Key.Descricao()} está sem estoque."
                    : $"Estoque insuficiente de {grupo.Key.Descricao()}: restam {grupo.Key.Estoque}.";
                return Falha(mensagem, StatusCodes.Status409Conflict);
            }
        }

        var subtotal = Precificador.Arredondar(linhas.Sum(l => l.Produto.Preco * l.Quantidade));

        Cupom? cupom = null;
        if (!string.IsNullOrWhiteSpace(requisicao.Cupom))
        {
            var codigo = requisicao.Cupom.Trim().ToUpperInvariant();
            cupom = await db.Cupons.FirstOrDefaultAsync(c => c.Codigo == codigo, ct);
            if (cupom is null) return Falha("Cupom não encontrado.");

            var baseCupom = subtotal - Precificador.DescontoVolume(subtotal, _loja);
            var erro = Precificador.ValidarCupom(cupom, baseCupom, DateTime.UtcNow);
            if (erro is not null) return Falha(erro);
        }

        var cotacao = new Cotacao
        {
            Linhas = linhas,
            Preco = Precificador.Calcular(subtotal, _loja, cupom),
            Cupom = cupom,
        };
        return new ResultadoCotacao(cotacao, null);
    }

    public static OrcamentoDto ParaOrcamento(Cotacao cotacao)
    {
        var itens = cotacao.Linhas
            .Select(l => new LinhaPedidoDto(
                l.Produto.Id, l.Produto.Descricao(), l.Produto.VolumeLitros,
                l.Cor.Id, l.Cor.Nome, l.Cor.CodigoHex,
                l.Quantidade, l.Produto.Preco, l.Produto.Preco * l.Quantidade))
            .ToList();

        var litros = cotacao.Linhas.Sum(l => l.Produto.VolumeLitros * l.Quantidade);

        return new OrcamentoDto(
            itens, litros,
            cotacao.Preco.Subtotal, cotacao.Preco.DescontoVolume, cotacao.Preco.DescontoCupom, cotacao.Preco.Total,
            cotacao.Cupom?.Codigo);
    }

    /// <summary>Cria o pedido, baixa o estoque e conta o uso do cupom, tudo em uma única gravação.</summary>
    public async Task<(Pedido? Pedido, string? Erro, int StatusErro)> CriarAsync(
        int usuarioId, CarrinhoRequest requisicao, CancellationToken ct)
    {
        var resultado = await CotarAsync(requisicao, ct);
        if (!resultado.Ok) return (null, resultado.Erro, resultado.StatusErro);
        var cotacao = resultado.Cotacao!;

        var pedido = new Pedido
        {
            UsuarioId = usuarioId,
            Status = StatusPedido.Pendente,
            Subtotal = cotacao.Preco.Subtotal,
            DescontoVolume = cotacao.Preco.DescontoVolume,
            DescontoCupom = cotacao.Preco.DescontoCupom,
            Total = cotacao.Preco.Total,
            CupomId = cotacao.Cupom?.Id,
            CupomCodigo = cotacao.Cupom?.Codigo,
            Itens = cotacao.Linhas
                .Select(l => new PedidoItem
                {
                    ProdutoId = l.Produto.Id,
                    CorId = l.Cor.Id,
                    Quantidade = l.Quantidade,
                    PrecoUnitario = l.Produto.Preco,
                    ProdutoDescricao = l.Produto.Descricao(),
                    VolumeLitros = l.Produto.VolumeLitros,
                    CorNome = l.Cor.Nome,
                    CorHex = l.Cor.CodigoHex,
                })
                .ToList(),
        };

        foreach (var grupo in cotacao.Linhas.GroupBy(l => l.Produto))
            grupo.Key.Estoque -= grupo.Sum(l => l.Quantidade);

        if (cotacao.Cupom is not null) cotacao.Cupom.UsosRealizados++;

        db.Pedidos.Add(pedido);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Outro pedido levou o estoque (ou o último uso do cupom) no mesmo instante.
            return (null,
                "O estoque ou o cupom mudou enquanto o pedido era feito. Confira o carrinho e tente de novo.",
                StatusCodes.Status409Conflict);
        }

        return (pedido, null, StatusCodes.Status201Created);
    }

    /// <summary>
    /// Cancela o pedido, devolve o estoque e libera o uso do cupom. O pedido precisa vir com os Itens carregados.
    /// Devolve false se algum dado mudou ao mesmo tempo (o chamador pode pedir para tentar de novo).
    /// </summary>
    public async Task<bool> CancelarAsync(Pedido pedido, CancellationToken ct)
    {
        var ids = pedido.Itens.Select(i => i.ProdutoId).Distinct().ToList();
        var produtos = await db.Produtos.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        foreach (var item in pedido.Itens)
        {
            if (produtos.TryGetValue(item.ProdutoId, out var produto))
                produto.Estoque += item.Quantidade;
        }

        if (pedido.CupomId is { } cupomId)
        {
            var cupom = await db.Cupons.FirstOrDefaultAsync(c => c.Id == cupomId, ct);
            if (cupom is { UsosRealizados: > 0 }) cupom.UsosRealizados--;
        }

        pedido.Status = StatusPedido.Cancelado;
        pedido.AtualizadoEm = DateTime.UtcNow;

        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}

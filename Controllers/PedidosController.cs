using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PimTintas.Api.Data;
using PimTintas.Api.Dtos;
using PimTintas.Api.Extensions;
using PimTintas.Api.Services;

namespace PimTintas.Api.Controllers;

[ApiController]
[Route("api/pedidos")]
[Authorize]
public class PedidosController(AppDbContext db, PedidoService pedidos) : ControllerBase
{
    /// <summary>
    /// "Quanto custa?": confere o carrinho (produtos, cores, estoque, cupom) e devolve preços e descontos.
    /// Não grava nada e não exige login, então o visitante também vê o total.
    /// </summary>
    [HttpPost("orcamento")]
    [AllowAnonymous]
    [ProducesResponseType<OrcamentoDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Orcamento(CarrinhoRequest requisicao, CancellationToken ct)
    {
        var resultado = await pedidos.CotarAsync(requisicao, ct);
        if (!resultado.Ok)
            return Problem(detail: resultado.Erro, statusCode: resultado.StatusErro);

        return Ok(PedidoService.ParaOrcamento(resultado.Cotacao!));
    }

    /// <summary>Fecha o pedido: baixa o estoque, aplica descontos e cupom e grava o histórico.</summary>
    [HttpPost]
    [ProducesResponseType<PedidoDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CarrinhoRequest requisicao, CancellationToken ct)
    {
        if (User.ObterUsuarioId() is not int usuarioId) return Unauthorized();

        var (pedido, erro, status) = await pedidos.CriarAsync(usuarioId, requisicao, ct);
        if (pedido is null)
            return Problem(detail: erro, statusCode: status);

        return CreatedAtAction(nameof(Obter), new { id = pedido.Id }, pedido.ParaDto());
    }

    /// <summary>Histórico de compras do cliente logado (mais recentes primeiro).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PedidoResumoDto>>> Listar(CancellationToken ct)
    {
        if (User.ObterUsuarioId() is not int usuarioId) return Unauthorized();

        var lista = await db.Pedidos
            .AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId)
            .OrderByDescending(p => p.CriadoEm)
            .Select(p => new { p.Id, p.CriadoEm, p.Status, p.Total, Quantidade = p.Itens.Sum(i => i.Quantidade) })
            .ToListAsync(ct);

        return Ok(lista.Select(p => new PedidoResumoDto(p.Id, p.CriadoEm, p.Status.ToString(), p.Total, p.Quantidade)));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PedidoDto>> Obter(int id, CancellationToken ct)
    {
        if (User.ObterUsuarioId() is not int usuarioId) return Unauthorized();

        var pedido = await db.Pedidos
            .AsNoTracking()
            .Include(p => p.Itens)
            .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == usuarioId, ct);

        return pedido is null ? NotFound() : Ok(pedido.ParaDto());
    }

    /// <summary>O cliente cancela o próprio pedido enquanto ele ainda está pendente. O estoque volta.</summary>
    [HttpPost("{id:int}/cancelar")]
    public async Task<ActionResult<PedidoDto>> Cancelar(int id, CancellationToken ct)
    {
        if (User.ObterUsuarioId() is not int usuarioId) return Unauthorized();

        var pedido = await db.Pedidos
            .Include(p => p.Itens)
            .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == usuarioId, ct);
        if (pedido is null) return NotFound();

        if (!RegrasPedido.ClientePodeCancelar(pedido.Status))
            return Problem(
                detail: "Este pedido não pode mais ser cancelado pelo app. Fale com a loja.",
                statusCode: StatusCodes.Status409Conflict);

        if (!await pedidos.CancelarAsync(pedido, ct))
            return Problem(
                detail: "O pedido mudou enquanto era cancelado. Tente de novo.",
                statusCode: StatusCodes.Status409Conflict);

        return Ok(pedido.ParaDto());
    }
}

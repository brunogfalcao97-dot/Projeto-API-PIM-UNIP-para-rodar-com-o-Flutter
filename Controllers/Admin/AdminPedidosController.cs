using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PimTintas.Api.Data;
using PimTintas.Api.Dtos;
using PimTintas.Api.Extensions;
using PimTintas.Api.Models;
using PimTintas.Api.Services;

namespace PimTintas.Api.Controllers.Admin;

/// <summary>Pedidos de todos os clientes. Só administradores.</summary>
[ApiController]
[Route("api/admin/pedidos")]
[Authorize(Roles = Perfis.Admin)]
public class AdminPedidosController(AppDbContext db, PedidoService pedidos) : ControllerBase
{
    /// <summary>Últimos 200 pedidos. Filtro opcional: status (Pendente, Pago, Enviado, Entregue ou Cancelado).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PedidoAdminResumoDto>>> Listar([FromQuery] string? status, CancellationToken ct)
    {
        var consulta = db.Pedidos.AsNoTracking().Include(p => p.Usuario).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<StatusPedido>(status, ignoreCase: true, out var filtro))
                return Problem(detail: "Status inválido.", statusCode: StatusCodes.Status400BadRequest);

            consulta = consulta.Where(p => p.Status == filtro);
        }

        var lista = await consulta.OrderByDescending(p => p.CriadoEm).Take(200).ToListAsync(ct);

        return Ok(lista.Select(p => new PedidoAdminResumoDto(
            p.Id, p.CriadoEm, p.Status.ToString(), p.Total, p.UsuarioId, p.Usuario.Nome, p.Usuario.Email)));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PedidoDto>> Obter(int id, CancellationToken ct)
    {
        var pedido = await db.Pedidos.AsNoTracking().Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == id, ct);
        return pedido is null ? NotFound() : Ok(pedido.ParaDto());
    }

    /// <summary>Muda o status (Pendente → Pago → Enviado → Entregue). Cancelar devolve o estoque e o uso do cupom.</summary>
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<PedidoDto>> AlterarStatus(int id, AlterarStatusPedidoRequest requisicao, CancellationToken ct)
    {
        if (!Enum.TryParse<StatusPedido>(requisicao.Status, ignoreCase: true, out var novo) || !Enum.IsDefined(novo))
            return Problem(detail: "Status inválido.", statusCode: StatusCodes.Status400BadRequest);

        var pedido = await db.Pedidos.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pedido is null) return NotFound();

        if (!RegrasPedido.TransicaoValida(pedido.Status, novo))
            return Problem(
                detail: $"Não é possível mudar o pedido de {pedido.Status} para {novo}.",
                statusCode: StatusCodes.Status409Conflict);

        if (novo == StatusPedido.Cancelado)
        {
            if (!await pedidos.CancelarAsync(pedido, ct))
                return Problem(
                    detail: "Os dados mudaram enquanto o pedido era cancelado. Tente de novo.",
                    statusCode: StatusCodes.Status409Conflict);
        }
        else
        {
            pedido.Status = novo;
            pedido.AtualizadoEm = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return Ok(pedido.ParaDto());
    }
}

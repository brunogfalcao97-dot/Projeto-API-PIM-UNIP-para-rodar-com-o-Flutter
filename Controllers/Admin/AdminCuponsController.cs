using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PimTintas.Api.Data;
using PimTintas.Api.Dtos;
using PimTintas.Api.Extensions;
using PimTintas.Api.Models;

namespace PimTintas.Api.Controllers.Admin;

/// <summary>Cupons de desconto. Só administradores. "Excluir" apenas desativa.</summary>
[ApiController]
[Route("api/admin/cupons")]
[Authorize(Roles = Perfis.Admin)]
public class AdminCuponsController(AppDbContext db) : ControllerBase
{
    /// <summary>Converte e confere o tipo e o valor. Devolve a mensagem de erro, se houver.</summary>
    private static string? Validar(CupomRequest r, out TipoDesconto tipo)
    {
        if (!Enum.TryParse(r.Tipo, ignoreCase: true, out tipo) || !Enum.IsDefined(tipo))
            return "O tipo do cupom deve ser \"Percentual\" ou \"ValorFixo\".";

        if (tipo == TipoDesconto.Percentual && r.Valor > 100)
            return "Um cupom percentual não pode passar de 100%.";

        return null;
    }

    private static void Aplicar(Cupom cupom, CupomRequest r, TipoDesconto tipo)
    {
        cupom.Codigo = r.Codigo.Trim().ToUpperInvariant();
        cupom.Tipo = tipo;
        cupom.Valor = r.Valor;
        cupom.ValorMinimoPedido = r.ValorMinimoPedido;
        cupom.ValidoAte = r.ValidoAte?.ToUniversalTime();
        cupom.UsosMaximos = r.UsosMaximos;
        cupom.Inativo = r.Inativo;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CupomAdminDto>>> Listar(CancellationToken ct)
    {
        var cupons = await db.Cupons.AsNoTracking().OrderBy(c => c.Id).ToListAsync(ct);
        return Ok(cupons.Select(c => c.ParaAdminDto()));
    }

    [HttpPost]
    public async Task<IActionResult> Criar(CupomRequest requisicao, CancellationToken ct)
    {
        var erro = Validar(requisicao, out var tipo);
        if (erro is not null) return Problem(detail: erro, statusCode: StatusCodes.Status400BadRequest);

        var codigo = requisicao.Codigo.Trim().ToUpperInvariant();
        if (await db.Cupons.AnyAsync(c => c.Codigo == codigo, ct))
            return Problem(detail: "Já existe um cupom com esse código.", statusCode: StatusCodes.Status409Conflict);

        var cupom = new Cupom();
        Aplicar(cupom, requisicao, tipo);
        db.Cupons.Add(cupom);
        await db.SaveChangesAsync(ct);

        return Created($"/api/admin/cupons/{cupom.Id}", cupom.ParaAdminDto());
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, CupomRequest requisicao, CancellationToken ct)
    {
        var cupom = await db.Cupons.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cupom is null) return NotFound();

        var erro = Validar(requisicao, out var tipo);
        if (erro is not null) return Problem(detail: erro, statusCode: StatusCodes.Status400BadRequest);

        var codigo = requisicao.Codigo.Trim().ToUpperInvariant();
        if (await db.Cupons.AnyAsync(c => c.Id != id && c.Codigo == codigo, ct))
            return Problem(detail: "Já existe um cupom com esse código.", statusCode: StatusCodes.Status409Conflict);

        Aplicar(cupom, requisicao, tipo);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Problem(
                detail: "O cupom foi usado enquanto você editava. Recarregue e tente de novo.",
                statusCode: StatusCodes.Status409Conflict);
        }

        return Ok(cupom.ParaAdminDto());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Desativar(int id, CancellationToken ct)
    {
        var cupom = await db.Cupons.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cupom is null) return NotFound();

        cupom.Inativo = true;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

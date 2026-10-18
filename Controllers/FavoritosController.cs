using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PimTintas.Api.Data;
using PimTintas.Api.Dtos;
using PimTintas.Api.Extensions;
using PimTintas.Api.Models;

namespace PimTintas.Api.Controllers;

[ApiController]
[Route("api/favoritos")]
[Authorize]
public class FavoritosController(AppDbContext db) : ControllerBase
{
    /// <summary>Cores favoritas do cliente logado (as mais recentes primeiro).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CorDto>>> Listar(CancellationToken ct)
    {
        if (User.ObterUsuarioId() is not int usuarioId) return Unauthorized();

        var cores = await db.Favoritos
            .AsNoTracking()
            .Where(f => f.UsuarioId == usuarioId && !f.Cor.Inativa)
            .OrderByDescending(f => f.CriadoEm)
            .Select(f => f.Cor)
            .ToListAsync(ct);

        return Ok(cores.Select(c => c.ParaDto()));
    }

    /// <summary>Favorita uma cor. Pode repetir a chamada sem problema.</summary>
    [HttpPut("{corId:int}")]
    public async Task<IActionResult> Adicionar(int corId, CancellationToken ct)
    {
        if (User.ObterUsuarioId() is not int usuarioId) return Unauthorized();

        if (!await db.Cores.AnyAsync(c => c.Id == corId && !c.Inativa, ct))
            return Problem(detail: "Cor não encontrada.", statusCode: StatusCodes.Status404NotFound);

        if (await db.Favoritos.AnyAsync(f => f.UsuarioId == usuarioId && f.CorId == corId, ct))
            return NoContent();

        db.Favoritos.Add(new Favorito { UsuarioId = usuarioId, CorId = corId });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Dois toques seguidos no coração: o segundo cai na chave duplicada e o resultado é o mesmo.
        }

        return NoContent();
    }

    [HttpDelete("{corId:int}")]
    public async Task<IActionResult> Remover(int corId, CancellationToken ct)
    {
        if (User.ObterUsuarioId() is not int usuarioId) return Unauthorized();

        await db.Favoritos
            .Where(f => f.UsuarioId == usuarioId && f.CorId == corId)
            .ExecuteDeleteAsync(ct);

        return NoContent();
    }
}

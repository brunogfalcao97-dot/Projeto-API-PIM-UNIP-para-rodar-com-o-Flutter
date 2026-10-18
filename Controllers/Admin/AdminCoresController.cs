using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PimTintas.Api.Data;
using PimTintas.Api.Dtos;
using PimTintas.Api.Models;

namespace PimTintas.Api.Controllers.Admin;

/// <summary>Cadastro de cores (HEX e Pantone). Só administradores. "Excluir" apenas desativa.</summary>
[ApiController]
[Route("api/admin/cores")]
[Authorize(Roles = Perfis.Admin)]
public class AdminCoresController(AppDbContext db) : ControllerBase
{
    private static CorAdminDto Mapear(Cor c) => new(c.Id, c.Nome, c.CodigoHex, c.Familia, c.CodigoPantone, c.Inativa);

    private static void Aplicar(Cor cor, CorRequest r)
    {
        cor.Nome = r.Nome.Trim();
        cor.CodigoHex = r.CodigoHex.ToUpperInvariant();
        cor.Familia = string.IsNullOrWhiteSpace(r.Familia) ? null : r.Familia.Trim();
        cor.CodigoPantone = string.IsNullOrWhiteSpace(r.CodigoPantone) ? null : r.CodigoPantone.Trim();
        cor.Inativa = r.Inativa;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CorAdminDto>>> Listar(CancellationToken ct)
    {
        var cores = await db.Cores.AsNoTracking().OrderBy(c => c.Id).ToListAsync(ct);
        return Ok(cores.Select(Mapear));
    }

    [HttpPost]
    public async Task<IActionResult> Criar(CorRequest requisicao, CancellationToken ct)
    {
        var cor = new Cor();
        Aplicar(cor, requisicao);
        db.Cores.Add(cor);
        await db.SaveChangesAsync(ct);

        return Created($"/api/admin/cores/{cor.Id}", Mapear(cor));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, CorRequest requisicao, CancellationToken ct)
    {
        var cor = await db.Cores.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cor is null) return NotFound();

        Aplicar(cor, requisicao);
        await db.SaveChangesAsync(ct);
        return Ok(Mapear(cor));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Desativar(int id, CancellationToken ct)
    {
        var cor = await db.Cores.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cor is null) return NotFound();

        cor.Inativa = true;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

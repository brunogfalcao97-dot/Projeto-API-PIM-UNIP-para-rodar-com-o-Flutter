using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PimTintas.Api.Data;
using PimTintas.Api.Dtos;
using PimTintas.Api.Extensions;

namespace PimTintas.Api.Controllers;

[ApiController]
[Route("api/cores")]
[AllowAnonymous]
public class CoresController(AppDbContext db) : ControllerBase
{
    /// <summary>Catálogo de cores (público). Filtros opcionais: familia (ex.: Azul) e busca (parte do nome).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CorDto>>> Listar(
        [FromQuery] string? familia, [FromQuery] string? busca, CancellationToken ct)
    {
        var consulta = db.Cores.AsNoTracking().Where(c => !c.Inativa);

        if (!string.IsNullOrWhiteSpace(familia))
        {
            var f = familia.Trim();
            consulta = consulta.Where(c => c.Familia == f);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var b = busca.Trim();
            consulta = consulta.Where(c => c.Nome.Contains(b));
        }

        var cores = await consulta.OrderBy(c => c.Id).ToListAsync(ct);
        return Ok(cores.Select(c => c.ParaDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CorDto>> Obter(int id, CancellationToken ct)
    {
        var cor = await db.Cores.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && !c.Inativa, ct);
        return cor is null ? NotFound() : Ok(cor.ParaDto());
    }
}

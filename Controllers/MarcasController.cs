using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PimTintas.Api.Data;
using PimTintas.Api.Dtos;
using PimTintas.Api.Extensions;

namespace PimTintas.Api.Controllers;

[ApiController]
[Route("api/marcas")]
[AllowAnonymous]
public class MarcasController(AppDbContext db) : ControllerBase
{
    /// <summary>Marcas disponíveis (público).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MarcaDto>>> Listar(CancellationToken ct)
    {
        var marcas = await db.Marcas
            .AsNoTracking()
            .Where(m => !m.Inativa)
            .OrderBy(m => m.Nome)
            .ToListAsync(ct);

        return Ok(marcas.Select(m => m.ParaDto()));
    }
}

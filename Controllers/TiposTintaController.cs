using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PimTintas.Api.Data;
using PimTintas.Api.Dtos;
using PimTintas.Api.Extensions;

namespace PimTintas.Api.Controllers;

[ApiController]
[Route("api/tipos-tinta")]
[AllowAnonymous]
public class TiposTintaController(AppDbContext db) : ControllerBase
{
    /// <summary>Tipos de tinta, acabamentos e parâmetros técnicos (rendimento, demãos, fator visual). Público.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TipoTintaDto>>> Listar(CancellationToken ct)
    {
        var tipos = await db.TiposTinta
            .AsNoTracking()
            .Where(t => !t.Inativo)
            .OrderBy(t => t.Id)
            .ToListAsync(ct);

        return Ok(tipos.Select(t => t.ParaDto()));
    }
}

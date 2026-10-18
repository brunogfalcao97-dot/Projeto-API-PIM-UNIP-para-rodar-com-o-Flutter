using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PimTintas.Api.Data;
using PimTintas.Api.Dtos;
using PimTintas.Api.Extensions;

namespace PimTintas.Api.Controllers;

[ApiController]
[Route("api/produtos")]
[AllowAnonymous]
public class ProdutosController(AppDbContext db) : ControllerBase
{
    /// <summary>Produtos à venda (latas) com preço e estoque. Filtros opcionais: marcaId e tipoTintaId.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProdutoDto>>> Listar(
        [FromQuery] int? marcaId, [FromQuery] int? tipoTintaId, CancellationToken ct)
    {
        var consulta = db.Produtos
            .AsNoTracking()
            .Include(p => p.Marca)
            .Include(p => p.TipoTinta)
            .Where(p => !p.Inativo && !p.Marca.Inativa && !p.TipoTinta.Inativo);

        if (marcaId is { } m) consulta = consulta.Where(p => p.MarcaId == m);
        if (tipoTintaId is { } t) consulta = consulta.Where(p => p.TipoTintaId == t);

        var produtos = await consulta
            .OrderBy(p => p.Marca.Nome)
            .ThenBy(p => p.TipoTinta.Nome)
            .ThenBy(p => p.TipoTinta.Acabamento)
            .ThenBy(p => p.VolumeLitros)
            .ToListAsync(ct);

        return Ok(produtos.Select(p => p.ParaDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProdutoDto>> Obter(int id, CancellationToken ct)
    {
        var produto = await db.Produtos
            .AsNoTracking()
            .Include(p => p.Marca)
            .Include(p => p.TipoTinta)
            .FirstOrDefaultAsync(p => p.Id == id && !p.Inativo && !p.Marca.Inativa && !p.TipoTinta.Inativo, ct);

        return produto is null ? NotFound() : Ok(produto.ParaDto());
    }
}

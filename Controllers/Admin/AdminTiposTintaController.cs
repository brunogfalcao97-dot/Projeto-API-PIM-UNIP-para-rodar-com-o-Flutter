using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PimTintas.Api.Data;
using PimTintas.Api.Dtos;
using PimTintas.Api.Models;

namespace PimTintas.Api.Controllers.Admin;

/// <summary>Tipos de tinta e seus parâmetros técnicos (rendimento, demãos, fator visual). Só administradores.</summary>
[ApiController]
[Route("api/admin/tipos-tinta")]
[Authorize(Roles = Perfis.Admin)]
public class AdminTiposTintaController(AppDbContext db) : ControllerBase
{
    private static TipoTintaAdminDto Mapear(TipoTinta t) =>
        new(t.Id, t.Nome, t.Acabamento, t.Descricao, t.RendimentoM2PorLitro, t.DemaosRecomendadas, t.FatorContraste, t.Inativo);

    private static void Aplicar(TipoTinta tipo, TipoTintaRequest r)
    {
        tipo.Nome = r.Nome.Trim();
        tipo.Acabamento = r.Acabamento.Trim();
        tipo.Descricao = string.IsNullOrWhiteSpace(r.Descricao) ? null : r.Descricao.Trim();
        tipo.RendimentoM2PorLitro = r.RendimentoM2PorLitro;
        tipo.DemaosRecomendadas = r.DemaosRecomendadas;
        tipo.FatorContraste = r.FatorContraste;
        tipo.Inativo = r.Inativo;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TipoTintaAdminDto>>> Listar(CancellationToken ct)
    {
        var tipos = await db.TiposTinta.AsNoTracking().OrderBy(t => t.Id).ToListAsync(ct);
        return Ok(tipos.Select(Mapear));
    }

    [HttpPost]
    public async Task<IActionResult> Criar(TipoTintaRequest requisicao, CancellationToken ct)
    {
        var tipo = new TipoTinta();
        Aplicar(tipo, requisicao);
        db.TiposTinta.Add(tipo);
        await db.SaveChangesAsync(ct);

        return Created($"/api/admin/tipos-tinta/{tipo.Id}", Mapear(tipo));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, TipoTintaRequest requisicao, CancellationToken ct)
    {
        var tipo = await db.TiposTinta.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tipo is null) return NotFound();

        Aplicar(tipo, requisicao);
        await db.SaveChangesAsync(ct);
        return Ok(Mapear(tipo));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Desativar(int id, CancellationToken ct)
    {
        var tipo = await db.TiposTinta.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tipo is null) return NotFound();

        tipo.Inativo = true;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

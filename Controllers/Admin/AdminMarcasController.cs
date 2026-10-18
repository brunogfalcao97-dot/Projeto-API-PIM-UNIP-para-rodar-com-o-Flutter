using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PimTintas.Api.Data;
using PimTintas.Api.Dtos;
using PimTintas.Api.Models;

namespace PimTintas.Api.Controllers.Admin;

/// <summary>Cadastro de marcas. Só administradores. "Excluir" apenas desativa (o histórico de pedidos continua válido).</summary>
[ApiController]
[Route("api/admin/marcas")]
[Authorize(Roles = Perfis.Admin)]
public class AdminMarcasController(AppDbContext db) : ControllerBase
{
    private static MarcaAdminDto Mapear(Marca m) => new(m.Id, m.Nome, m.Descricao, m.Inativa);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MarcaAdminDto>>> Listar(CancellationToken ct)
    {
        var marcas = await db.Marcas.AsNoTracking().OrderBy(m => m.Nome).ToListAsync(ct);
        return Ok(marcas.Select(Mapear));
    }

    [HttpPost]
    public async Task<IActionResult> Criar(MarcaRequest requisicao, CancellationToken ct)
    {
        var nome = requisicao.Nome.Trim();
        if (await db.Marcas.AnyAsync(m => m.Nome == nome, ct))
            return Problem(detail: "Já existe uma marca com esse nome.", statusCode: StatusCodes.Status409Conflict);

        var marca = new Marca { Nome = nome, Descricao = requisicao.Descricao?.Trim(), Inativa = requisicao.Inativa };
        db.Marcas.Add(marca);
        await db.SaveChangesAsync(ct);

        return Created($"/api/admin/marcas/{marca.Id}", Mapear(marca));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, MarcaRequest requisicao, CancellationToken ct)
    {
        var marca = await db.Marcas.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (marca is null) return NotFound();

        var nome = requisicao.Nome.Trim();
        if (await db.Marcas.AnyAsync(m => m.Id != id && m.Nome == nome, ct))
            return Problem(detail: "Já existe uma marca com esse nome.", statusCode: StatusCodes.Status409Conflict);

        marca.Nome = nome;
        marca.Descricao = requisicao.Descricao?.Trim();
        marca.Inativa = requisicao.Inativa;
        await db.SaveChangesAsync(ct);

        return Ok(Mapear(marca));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Desativar(int id, CancellationToken ct)
    {
        var marca = await db.Marcas.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (marca is null) return NotFound();

        marca.Inativa = true;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

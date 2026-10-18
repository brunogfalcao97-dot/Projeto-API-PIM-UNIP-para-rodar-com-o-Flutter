using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PimTintas.Api.Data;
using PimTintas.Api.Dtos;
using PimTintas.Api.Extensions;

namespace PimTintas.Api.Controllers;

[ApiController]
[Route("api/perfil")]
[Authorize]
public class PerfilController(AppDbContext db) : ControllerBase
{
    private static PerfilDto Mapear(Models.Usuario u) =>
        new(u.Id, u.Nome, u.Email, u.Telefone, string.IsNullOrWhiteSpace(u.Perfil) ? Models.Perfis.Cliente : u.Perfil, u.CriadoEm);

    [HttpGet]
    public async Task<ActionResult<PerfilDto>> Obter(CancellationToken ct)
    {
        if (User.ObterUsuarioId() is not int usuarioId) return Unauthorized();

        var usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == usuarioId, ct);
        return usuario is null ? Unauthorized() : Ok(Mapear(usuario));
    }

    [HttpPut]
    public async Task<ActionResult<PerfilDto>> Atualizar(AtualizarPerfilRequest requisicao, CancellationToken ct)
    {
        if (User.ObterUsuarioId() is not int usuarioId) return Unauthorized();

        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId, ct);
        if (usuario is null) return Unauthorized();

        usuario.Nome = requisicao.Nome.Trim();
        usuario.Telefone = string.IsNullOrWhiteSpace(requisicao.Telefone) ? null : requisicao.Telefone.Trim();
        await db.SaveChangesAsync(ct);

        return Ok(Mapear(usuario));
    }

    [HttpPost("alterar-senha")]
    public async Task<IActionResult> AlterarSenha(AlterarSenhaRequest requisicao, CancellationToken ct)
    {
        if (User.ObterUsuarioId() is not int usuarioId) return Unauthorized();

        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId, ct);
        if (usuario is null) return Unauthorized();

        if (!BCrypt.Net.BCrypt.Verify(requisicao.SenhaAtual, usuario.SenhaHash))
            return Problem(detail: "A senha atual está incorreta.", statusCode: StatusCodes.Status400BadRequest);

        usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(requisicao.NovaSenha);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }
}

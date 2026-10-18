using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PimTintas.Api.Data;
using PimTintas.Api.Dtos;
using PimTintas.Api.Models;
using PimTintas.Api.Services;

namespace PimTintas.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
[EnableRateLimiting("auth")] // limite de tentativas por minuto, contra quem testa senhas em sequência
public class AuthController(AppDbContext db, TokenService tokens) : ControllerBase
{
    /// <summary>Cria a conta de um cliente. Contas novas são sempre "Cliente".</summary>
    [HttpPost("registrar")]
    [ProducesResponseType<UsuarioResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Registrar(RegistrarRequest requisicao, CancellationToken ct)
    {
        var email = requisicao.Email.Trim().ToLowerInvariant();

        if (await db.Usuarios.AnyAsync(u => u.Email == email, ct))
            return Problem(detail: "Este e-mail já está cadastrado.", statusCode: StatusCodes.Status409Conflict);

        var usuario = new Usuario
        {
            Nome = requisicao.Nome.Trim(),
            Email = email,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(requisicao.Senha),
            Perfil = Perfis.Cliente,
        };
        db.Usuarios.Add(usuario);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Dois cadastros simultâneos com o mesmo e-mail: o índice único barra o segundo.
            return Problem(detail: "Este e-mail já está cadastrado.", statusCode: StatusCodes.Status409Conflict);
        }

        return StatusCode(StatusCodes.Status201Created, new UsuarioResponse(usuario.Id, usuario.Nome, usuario.Email));
    }

    /// <summary>Valida e-mail e senha e devolve o token JWT (com o perfil do usuário).</summary>
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(LoginRequest requisicao, CancellationToken ct)
    {
        var email = requisicao.Email.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(requisicao.Senha, usuario.SenhaHash))
            return Problem(detail: "E-mail ou senha inválidos.", statusCode: StatusCodes.Status401Unauthorized);

        var (token, expiraEm) = tokens.Gerar(usuario);
        var perfil = string.IsNullOrWhiteSpace(usuario.Perfil) ? Perfis.Cliente : usuario.Perfil;
        return Ok(new LoginResponse(token, usuario.Nome, usuario.Email, perfil, expiraEm));
    }
}

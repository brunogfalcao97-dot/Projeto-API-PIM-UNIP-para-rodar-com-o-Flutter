using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PimTintas.Api.Models;

namespace PimTintas.Api.Services;

/// <summary>Gera o token JWT que o app Flutter envia em "Authorization: Bearer ...".</summary>
public class TokenService(IOptions<JwtOptions> opcoes)
{
    private readonly JwtOptions _jwt = opcoes.Value;

    public (string Token, DateTime ExpiraEm) Gerar(Usuario usuario)
    {
        var expiraEm = DateTime.UtcNow.AddMinutes(_jwt.ExpiraEmMinutos);

        var descritor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", usuario.Id.ToString()),
                new Claim("email", usuario.Email),
                new Claim("name", usuario.Nome),
                // O perfil vai no token: é ele que libera (ou bloqueia) as rotas /api/admin.
                new Claim("role", string.IsNullOrWhiteSpace(usuario.Perfil) ? Perfis.Cliente : usuario.Perfil),
            }),
            Expires = expiraEm,
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
                SecurityAlgorithms.HmacSha256),
        };

        var token = new JsonWebTokenHandler().CreateToken(descritor);
        return (token, expiraEm);
    }
}

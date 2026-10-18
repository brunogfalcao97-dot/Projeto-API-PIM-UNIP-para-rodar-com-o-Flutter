using System.Security.Claims;

namespace PimTintas.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id do usuário logado (claim "sub" do token) ou null se não houver.</summary>
    public static int? ObterUsuarioId(this ClaimsPrincipal usuario)
    {
        var valor = usuario.FindFirst("sub")?.Value
                    ?? usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(valor, out var id) ? id : null;
    }
}

namespace PimTintas.Api.Services;

/// <summary>Seção "Jwt" do appsettings.json.</summary>
public class JwtOptions
{
    /// <summary>Chave secreta usada para assinar o token (mínimo 32 caracteres).</summary>
    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = "PimTintas.Api";
    public string Audience { get; set; } = "PimTintas.App";
    public int ExpiraEmMinutos { get; set; } = 480;
}

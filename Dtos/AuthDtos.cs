using System.ComponentModel.DataAnnotations;

namespace PimTintas.Api.Dtos;

public class RegistrarRequest
{
    [Required(ErrorMessage = "Informe seu nome.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe seu e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe uma senha.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter ao menos 6 caracteres.")]
    public string Senha { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required(ErrorMessage = "Informe seu e-mail.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe sua senha.")]
    public string Senha { get; set; } = string.Empty;
}

public record UsuarioResponse(int Id, string Nome, string Email);

public record LoginResponse(string Token, string Nome, string Email, string Perfil, DateTime ExpiraEm);

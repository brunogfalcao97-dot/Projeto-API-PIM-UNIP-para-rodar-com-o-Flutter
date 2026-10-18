using System.ComponentModel.DataAnnotations;

namespace PimTintas.Api.Dtos;

public record PerfilDto(int Id, string Nome, string Email, string? Telefone, string Perfil, DateTime CriadoEm);

public class AtualizarPerfilRequest
{
    [Required(ErrorMessage = "Informe seu nome.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "O telefone deve ter até 20 caracteres.")]
    public string? Telefone { get; set; }
}

public class AlterarSenhaRequest
{
    [Required(ErrorMessage = "Informe a senha atual.")]
    public string SenhaAtual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a nova senha.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "A nova senha deve ter ao menos 6 caracteres.")]
    public string NovaSenha { get; set; } = string.Empty;
}

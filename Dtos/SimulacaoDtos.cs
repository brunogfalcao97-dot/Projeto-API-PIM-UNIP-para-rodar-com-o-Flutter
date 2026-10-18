using System.ComponentModel.DataAnnotations;

namespace PimTintas.Api.Dtos;

/// <summary>Campos do multipart/form-data enviado pelo app: corId, tipoTintaId, imagem e, opcionais, nome e areaM2.</summary>
public class CriarSimulacaoRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Escolha uma cor.")]
    public int CorId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Escolha um tipo de tinta.")]
    public int TipoTintaId { get; set; }

    [StringLength(100, ErrorMessage = "O nome do projeto deve ter até 100 caracteres.")]
    public string? Nome { get; set; }

    [Range(0.1, 100000, ErrorMessage = "A área deve ser maior que zero.")]
    public decimal? AreaM2 { get; set; }

    [Required(ErrorMessage = "Envie a imagem da simulação.")]
    public IFormFile Imagem { get; set; } = null!;
}

/// <summary>A imagem é privada: baixe com o token em GET {imagemUrl}.</summary>
public record SimulacaoDto(
    int Id,
    DateTime CriadoEm,
    string? Nome,
    decimal? AreaM2,
    int CorId,
    string CorNome,
    string CorHex,
    int TipoTintaId,
    string TipoTintaNome,
    string ImagemUrl);

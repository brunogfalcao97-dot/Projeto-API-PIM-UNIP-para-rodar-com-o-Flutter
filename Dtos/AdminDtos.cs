using System.ComponentModel.DataAnnotations;

namespace PimTintas.Api.Dtos;

// ----------------------------------------------------------------- entrada (cadastro e edição)

public class MarcaRequest
{
    [Required(ErrorMessage = "Informe o nome da marca.")]
    [StringLength(80, ErrorMessage = "O nome deve ter até 80 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Descricao { get; set; }

    public bool Inativa { get; set; }
}

public class CorRequest
{
    [Required(ErrorMessage = "Informe o nome da cor.")]
    [StringLength(80, ErrorMessage = "O nome deve ter até 80 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o código HEX.")]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "O código HEX deve estar no formato #RRGGBB.")]
    public string CodigoHex { get; set; } = string.Empty;

    [StringLength(40)]
    public string? Familia { get; set; }

    [StringLength(40)]
    public string? CodigoPantone { get; set; }

    public bool Inativa { get; set; }
}

public class TipoTintaRequest
{
    [Required(ErrorMessage = "Informe o nome do tipo de tinta.")]
    [StringLength(80, ErrorMessage = "O nome deve ter até 80 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o acabamento (Fosco, Acetinado ou Brilhante).")]
    [StringLength(30)]
    public string Acabamento { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Descricao { get; set; }

    [Range(1, 50, ErrorMessage = "O rendimento deve estar entre 1 e 50 m² por litro.")]
    public decimal RendimentoM2PorLitro { get; set; }

    [Range(1, 6, ErrorMessage = "As demãos recomendadas devem ser de 1 a 6.")]
    public int DemaosRecomendadas { get; set; }

    [Range(0.5, 1.5, ErrorMessage = "O fator de contraste deve estar entre 0,5 e 1,5.")]
    public decimal FatorContraste { get; set; }

    public bool Inativo { get; set; }
}

public class ProdutoRequest
{
    [Required(ErrorMessage = "Informe o SKU.")]
    [StringLength(40, ErrorMessage = "O SKU deve ter até 40 caracteres.")]
    public string Sku { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Escolha a marca.")]
    public int MarcaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Escolha o tipo de tinta.")]
    public int TipoTintaId { get; set; }

    [Range(0.1, 50, ErrorMessage = "O volume deve estar entre 0,1 e 50 litros.")]
    public decimal VolumeLitros { get; set; }

    [Range(0.01, 100000, ErrorMessage = "Informe um preço válido.")]
    public decimal Preco { get; set; }

    [Range(0, 1000000, ErrorMessage = "O estoque não pode ser negativo.")]
    public int Estoque { get; set; }

    public bool Inativo { get; set; }
}

public class CupomRequest
{
    [Required(ErrorMessage = "Informe o código do cupom.")]
    [StringLength(40)]
    [RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "Use só letras, números, hífen e sublinhado no código.")]
    public string Codigo { get; set; } = string.Empty;

    /// <summary>"Percentual" ou "ValorFixo".</summary>
    [Required(ErrorMessage = "Informe o tipo do desconto.")]
    public string Tipo { get; set; } = "Percentual";

    [Range(0.01, 100000, ErrorMessage = "Informe um valor válido.")]
    public decimal Valor { get; set; }

    [Range(0, 1000000)]
    public decimal ValorMinimoPedido { get; set; }

    public DateTime? ValidoAte { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "O limite de usos deve ser de pelo menos 1.")]
    public int? UsosMaximos { get; set; }

    public bool Inativo { get; set; }
}

public class AlterarStatusPedidoRequest
{
    /// <summary>Pendente, Pago, Enviado, Entregue ou Cancelado.</summary>
    [Required(ErrorMessage = "Informe o novo status.")]
    public string Status { get; set; } = string.Empty;
}

// ----------------------------------------------------------------- saída (inclui itens desativados)

public record MarcaAdminDto(int Id, string Nome, string? Descricao, bool Inativa);

public record CorAdminDto(int Id, string Nome, string CodigoHex, string? Familia, string? CodigoPantone, bool Inativa);

public record TipoTintaAdminDto(
    int Id,
    string Nome,
    string Acabamento,
    string? Descricao,
    decimal RendimentoM2PorLitro,
    int DemaosRecomendadas,
    decimal FatorContraste,
    bool Inativo);

public record ProdutoAdminDto(
    int Id,
    string Sku,
    string Nome,
    int MarcaId,
    int TipoTintaId,
    decimal VolumeLitros,
    decimal Preco,
    int Estoque,
    bool Inativo);

public record CupomAdminDto(
    int Id,
    string Codigo,
    string Tipo,
    decimal Valor,
    decimal ValorMinimoPedido,
    DateTime? ValidoAte,
    int? UsosMaximos,
    int UsosRealizados,
    bool Inativo);

public record PedidoAdminResumoDto(
    int Id,
    DateTime CriadoEm,
    string Status,
    decimal Total,
    int UsuarioId,
    string UsuarioNome,
    string UsuarioEmail);

namespace PimTintas.Api.Models;

/// <summary>Projeto salvo pelo cliente: foto já pintada + cor e tipo de tinta escolhidos.</summary>
public class Simulacao
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int CorId { get; set; }
    public Cor Cor { get; set; } = null!;

    public int TipoTintaId { get; set; }
    public TipoTinta TipoTinta { get; set; } = null!;

    /// <summary>Nome dado pelo cliente ao projeto (ex.: "Sala de estar"). Opcional.</summary>
    public string? Nome { get; set; }

    /// <summary>Área da parede em m², se o cliente tiver informado. Opcional.</summary>
    public decimal? AreaM2 { get; set; }

    /// <summary>
    /// Caminho do arquivo no armazenamento privado da API (App_Data). Imagens antigas, salvas em
    /// "uploads/...", continuam sendo lidas de wwwroot, mas agora só são entregues ao dono.
    /// </summary>
    public string CaminhoImagem { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

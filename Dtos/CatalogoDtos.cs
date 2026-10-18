namespace PimTintas.Api.Dtos;

public record RgbDto(int R, int G, int B);

public record CorDto(int Id, string Nome, string CodigoHex, RgbDto Rgb, string? Familia, string? CodigoPantone);

public record TipoTintaDto(
    int Id,
    string Nome,
    string Acabamento,
    string? Descricao,
    decimal RendimentoM2PorLitro,
    int DemaosRecomendadas,
    decimal FatorContraste);

public record MarcaDto(int Id, string Nome, string? Descricao);

public record ProdutoDto(
    int Id,
    string Sku,
    string Nome,
    int MarcaId,
    string MarcaNome,
    int TipoTintaId,
    string TipoTintaNome,
    string Acabamento,
    decimal VolumeLitros,
    decimal Preco,
    int Estoque,
    bool EmEstoque);

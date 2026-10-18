namespace PimTintas.Api.Services;

/// <summary>Regras da loja, na seção "Loja" do appsettings.json. Mudou a regra? Mude aqui, sem recompilar o app.</summary>
public class LojaOptions
{
    /// <summary>Pedidos com subtotal a partir deste valor ganham o desconto por volume.</summary>
    public decimal DescontoVolumeAcimaDe { get; set; } = 500m;

    /// <summary>Porcentagem do desconto por volume (5 = 5%). Use 0 para desligar.</summary>
    public decimal DescontoVolumePercentual { get; set; } = 5m;

    /// <summary>Folga somada ao cálculo de litros para cobrir perdas e retoques (10 = 10%).</summary>
    public decimal MargemPerdaPercentual { get; set; } = 10m;

    /// <summary>Maior área aceita no cálculo, em m².</summary>
    public decimal AreaMaximaM2 { get; set; } = 1000m;

    /// <summary>Maior quantidade de um mesmo item em um pedido.</summary>
    public int QuantidadeMaximaPorItem { get; set; } = 100;
}

/// <summary>Administrador criado automaticamente na primeira execução (seção "Admin" do appsettings.json).</summary>
public class AdminSeedOptions
{
    public string? Nome { get; set; }
    public string? Email { get; set; }
    public string? Senha { get; set; }
}

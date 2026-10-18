namespace PimTintas.Api.Models;

/// <summary>Perfis de acesso (guardados no usuário e enviados no token JWT).</summary>
public static class Perfis
{
    public const string Cliente = "Cliente";
    public const string Admin = "Admin";
}

public enum StatusPedido
{
    Pendente = 1,
    Pago = 2,
    Enviado = 3,
    Entregue = 4,
    Cancelado = 5,
}

public enum TipoDesconto
{
    /// <summary>Valor é uma porcentagem (ex.: 10 = 10%).</summary>
    Percentual = 1,

    /// <summary>Valor é em reais (ex.: 50 = R$ 50,00).</summary>
    ValorFixo = 2,
}

using PimTintas.Api.Models;

namespace PimTintas.Api.Services;

/// <summary>Quem pode mudar o status de um pedido, e para qual status. Fica só aqui.</summary>
public static class RegrasPedido
{
    private static readonly Dictionary<StatusPedido, StatusPedido[]> Transicoes = new()
    {
        [StatusPedido.Pendente] = [StatusPedido.Pago, StatusPedido.Cancelado],
        [StatusPedido.Pago] = [StatusPedido.Enviado, StatusPedido.Cancelado],
        [StatusPedido.Enviado] = [StatusPedido.Entregue, StatusPedido.Cancelado],
        [StatusPedido.Entregue] = [],
        [StatusPedido.Cancelado] = [],
    };

    /// <summary>O cliente só cancela enquanto o pedido não foi pago.</summary>
    public static bool ClientePodeCancelar(StatusPedido status) => status == StatusPedido.Pendente;

    public static bool TransicaoValida(StatusPedido de, StatusPedido para) =>
        Transicoes.TryGetValue(de, out var permitidos) && permitidos.Contains(para);
}

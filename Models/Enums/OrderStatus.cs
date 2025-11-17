namespace censudex_orders.Models.Enums
{
    /// <summary>
    /// Estados posibles de un pedido
    /// </summary>
    public enum OrderStatus
    {
        Pendiente,
        EnProcesamiento,
        Enviado,
        Entregado,
        Cancelado
    }
}
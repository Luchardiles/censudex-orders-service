namespace censudex_orders.Models.DTOs
{
    /// <summary>
    /// DTO para crear un nuevo pedido
    /// </summary>
    public class CreateOrderDto
    {
        public Guid ClientId { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public List<OrderItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// DTO para los items del pedido
    /// </summary>
    public class OrderItemDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// DTO para respuesta de pedido
    /// </summary>
    public class OrderResponseDto
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<OrderItemResponseDto> Items { get; set; } = new();
    }

    /// <summary>
    /// DTO para respuesta de items del pedido
    /// </summary>
    public class OrderItemResponseDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }

    /// <summary>
    /// DTO para actualizar estado de pedido
    /// </summary>
    public class UpdateOrderStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
    }

    /// <summary>
    /// DTO para cancelar pedido
    /// </summary>
    public class CancelOrderDto
    {
        public string CancellationReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para filtros de búsqueda
    /// </summary>
    public class OrderFilterDto
    {
        public Guid? OrderId { get; set; }
        public Guid? ClientId { get; set; }
        public string? ClientName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
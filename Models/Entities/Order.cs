using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace censudex_orders.Models.Entities
{
    /// <summary>
    /// Entidad que representa un pedido en el sistema
    /// </summary>
    public class Order
    {
        /// <summary>
        /// Identificador único del pedido (UUID V4)
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// ID del cliente que realizó el pedido
        /// </summary>
        [Required]
        public Guid ClientId { get; set; }

        /// <summary>
        /// Estado actual del pedido
        /// Valores: Pendiente, EnProcesamiento, Enviado, Entregado, Cancelado
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pendiente";

        /// <summary>
        /// Monto total del pedido
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Dirección de envío del pedido
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string ShippingAddress { get; set; } = string.Empty;

        /// <summary>
        /// Número de seguimiento del envío (opcional)
        /// </summary>
        [MaxLength(100)]
        public string? TrackingNumber { get; set; }

        /// <summary>
        /// Motivo de cancelación (si aplica)
        /// </summary>
        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        /// <summary>
        /// Fecha de creación del pedido
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Fecha de última actualización
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Colección de items del pedido
        /// </summary>
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    /// <summary>
    /// Entidad que representa un item dentro de un pedido
    /// </summary>
    public class OrderItem
    {
        /// <summary>
        /// Identificador único del item
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// ID del pedido al que pertenece
        /// </summary>
        [Required]
        public Guid OrderId { get; set; }

        /// <summary>
        /// ID del producto
        /// </summary>
        [Required]
        public Guid ProductId { get; set; }

        /// <summary>
        /// Nombre del producto (snapshot para historial)
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Cantidad de unidades del producto
        /// </summary>
        [Required]
        public int Quantity { get; set; }

        /// <summary>
        /// Precio unitario del producto al momento de la compra
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Subtotal del item (Quantity * UnitPrice)
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Subtotal { get; set; }

        /// <summary>
        /// Navegación al pedido padre
        /// </summary>
        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; } = null!;
    }
}
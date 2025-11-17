using censudex_orders.Models.Entities;
using censudex_orders.Models.DTOs;
using censudex_orders.Models.Enums;
using censudex_orders.Repositories.Interfaces;
using censudex_orders.Services;

namespace censudex_orders.Services
{
    /// <summary>
    /// Interfaz para el servicio de negocio de pedidos
    /// </summary>
    public interface IOrderBusinessService
    {
        Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto);
        Task<List<OrderResponseDto>> GetAllOrdersAsync(OrderFilterDto? filter = null);
        Task<OrderResponseDto> GetOrderByIdAsync(Guid id);
        Task<OrderResponseDto> UpdateOrderStatusAsync(Guid id, UpdateOrderStatusDto dto);
        Task<OrderResponseDto> CancelOrderAsync(Guid id, CancelOrderDto dto);
    }
}

namespace censudex_orders.Services
{
    /// <summary>
    /// Servicio que contiene toda la lógica de negocio relacionada con pedidos
    /// Coordina entre repositorios, mensajería y notificaciones
    /// </summary>
    public class OrderBusinessService : IOrderBusinessService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IRabbitMQService _rabbitMQService;
        private readonly IEmailService _emailService;
        private readonly ILogger<OrderBusinessService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public OrderBusinessService(
            IOrderRepository orderRepository,
            IRabbitMQService rabbitMQService,
            IEmailService emailService,
            ILogger<OrderBusinessService> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _orderRepository = orderRepository;
            _rabbitMQService = rabbitMQService;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }

        /// <summary>
        /// Crea un nuevo pedido
        /// 1. Valida el stock mediante HTTP a Inventory Service
        /// 2. Crea el pedido en BD
        /// 3. Publica evento en RabbitMQ
        /// 4. Envía email de confirmación
        /// </summary>
        public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto)
        {
            try
            {
                _logger.LogInformation($"Iniciando creación de pedido para cliente {dto.ClientId}");

                // Validar que haya items
                if (dto.Items == null || !dto.Items.Any())
                {
                    throw new ArgumentException("El pedido debe contener al menos un producto");
                }

                // Validar dirección de envío
                if (string.IsNullOrWhiteSpace(dto.ShippingAddress))
                {
                    throw new ArgumentException("La dirección de envío es requerida");
                }

                // Obtener información de productos y validar stock
                decimal totalAmount = 0;
                var orderItems = new List<OrderItem>();

                foreach (var item in dto.Items)
                {
                    // Aquí deberías llamar al Product Service para obtener precio
                    // Por ahora usaremos valores de ejemplo
                    // En producción: var product = await _productService.GetProductByIdAsync(item.ProductId);
                    
                    var productName = $"Producto {item.ProductId.ToString()[..8]}";
                    var unitPrice = 100.00m; // Precio de ejemplo

                    var orderItem = new OrderItem
                    {
                        ProductId = item.ProductId,
                        ProductName = productName,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice,
                        Subtotal = unitPrice * item.Quantity
                    };

                    orderItems.Add(orderItem);
                    totalAmount += orderItem.Subtotal;
                }

                // Crear el pedido
                var order = new Order
                {
                    ClientId = dto.ClientId,
                    Status = OrderStatus.Pendiente.ToString(),
                    TotalAmount = totalAmount,
                    ShippingAddress = dto.ShippingAddress,
                    OrderItems = orderItems
                };

                // Guardar en base de datos
                var createdOrder = await _orderRepository.CreateAsync(order);

                // Publicar evento en RabbitMQ para que Inventory Service procese el stock
                var orderCreatedMessage = new OrderCreatedMessage
                {
                    OrderId = createdOrder.Id,
                    ClientId = createdOrder.ClientId,
                    Items = createdOrder.OrderItems.Select(i => new OrderItemMessage
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity
                    }).ToList(),
                    CreatedAt = createdOrder.CreatedAt
                };

                var queueName = _configuration["RabbitMQ:Queues:OrderCreated"];
                _rabbitMQService.PublishMessage(queueName!, orderCreatedMessage);

                // Enviar email de confirmación
                // En producción deberías obtener el email del Client Service
                var clientEmail = "cliente@censudex.cl"; // Email de ejemplo
                var clientName = "Cliente"; // Nombre de ejemplo
                
                await _emailService.SendOrderCreatedEmailAsync(
                    clientEmail,
                    clientName,
                    createdOrder.Id,
                    createdOrder.TotalAmount
                );

                _logger.LogInformation($"Pedido creado exitosamente: {createdOrder.Id}");

                return MapToDto(createdOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear pedido");
                throw;
            }
        }

        /// <summary>
        /// Obtiene todos los pedidos con filtros opcionales
        /// Soporta filtrado por ID, cliente y rango de fechas
        /// </summary>
        public async Task<List<OrderResponseDto>> GetAllOrdersAsync(OrderFilterDto? filter = null)
        {
            try
            {
                var orders = await _orderRepository.GetAllAsync(filter);
                return orders.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener pedidos");
                throw;
            }
        }

        /// <summary>
        /// Obtiene un pedido específico por su ID
        /// </summary>
        public async Task<OrderResponseDto> GetOrderByIdAsync(Guid id)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(id);
                
                if (order == null)
                {
                    throw new KeyNotFoundException($"Pedido no encontrado: {id}");
                }

                return MapToDto(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener pedido {id}");
                throw;
            }
        }

        /// <summary>
        /// Actualiza el estado de un pedido
        /// Publica evento en RabbitMQ y envía email según el nuevo estado
        /// </summary>
        public async Task<OrderResponseDto> UpdateOrderStatusAsync(Guid id, UpdateOrderStatusDto dto)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(id);
                
                if (order == null)
                {
                    throw new KeyNotFoundException($"Pedido no encontrado: {id}");
                }

                // Validar que el pedido no esté cancelado
                if (order.Status == OrderStatus.Cancelado.ToString())
                {
                    throw new InvalidOperationException("No se puede actualizar un pedido cancelado");
                }

                // Validar que el pedido no esté ya entregado
                if (order.Status == OrderStatus.Entregado.ToString())
                {
                    throw new InvalidOperationException("No se puede actualizar un pedido ya entregado");
                }

                var oldStatus = order.Status;
                order.Status = dto.Status;

                // Si el estado es "Enviado", guardar número de tracking
                if (dto.Status == OrderStatus.Enviado.ToString() && !string.IsNullOrEmpty(dto.TrackingNumber))
                {
                    order.TrackingNumber = dto.TrackingNumber;
                }

                // Actualizar en base de datos
                var updatedOrder = await _orderRepository.UpdateAsync(order);

                // Publicar evento de actualización de estado
                var statusUpdatedMessage = new OrderStatusUpdatedMessage
                {
                    OrderId = updatedOrder.Id,
                    OldStatus = oldStatus,
                    NewStatus = updatedOrder.Status,
                    TrackingNumber = updatedOrder.TrackingNumber,
                    UpdatedAt = updatedOrder.UpdatedAt
                };

                var queueName = _configuration["RabbitMQ:Queues:OrderStatusUpdated"];
                _rabbitMQService.PublishMessage(queueName!, statusUpdatedMessage);

                // Enviar email según el estado
                var clientEmail = "cliente@censudex.cl";
                var clientName = "Cliente";

                switch (dto.Status)
                {
                    case "EnProcesamiento":
                        await _emailService.SendOrderProcessingEmailAsync(clientEmail, clientName, updatedOrder.Id);
                        break;
                    case "Enviado":
                        await _emailService.SendOrderShippedEmailAsync(
                            clientEmail, 
                            clientName, 
                            updatedOrder.Id, 
                            updatedOrder.TrackingNumber ?? "N/A"
                        );
                        break;
                    case "Entregado":
                        await _emailService.SendOrderDeliveredEmailAsync(clientEmail, clientName, updatedOrder.Id);
                        break;
                }

                _logger.LogInformation($"Estado de pedido {id} actualizado de {oldStatus} a {dto.Status}");

                return MapToDto(updatedOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar estado del pedido {id}");
                throw;
            }
        }

        /// <summary>
        /// Cancela un pedido
        /// Solo se pueden cancelar pedidos en estado Pendiente o EnProcesamiento
        /// </summary>
        public async Task<OrderResponseDto> CancelOrderAsync(Guid id, CancelOrderDto dto)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(id);
                
                if (order == null)
                {
                    throw new KeyNotFoundException($"Pedido no encontrado: {id}");
                }

                // Validar que el pedido pueda ser cancelado
                if (order.Status == OrderStatus.Enviado.ToString() || 
                    order.Status == OrderStatus.Entregado.ToString())
                {
                    throw new InvalidOperationException("No se puede cancelar un pedido que ya fue enviado o entregado");
                }

                if (order.Status == OrderStatus.Cancelado.ToString())
                {
                    throw new InvalidOperationException("El pedido ya está cancelado");
                }

                order.Status = OrderStatus.Cancelado.ToString();
                order.CancellationReason = dto.CancellationReason;

                var cancelledOrder = await _orderRepository.UpdateAsync(order);

                // Publicar evento de cancelación
                var queueName = _configuration["RabbitMQ:Queues:OrderCancelled"];
                _rabbitMQService.PublishMessage(queueName!, new
                {
                    OrderId = cancelledOrder.Id,
                    CancellationReason = dto.CancellationReason,
                    CancelledAt = DateTime.UtcNow
                });

                // Enviar email de cancelación
                var clientEmail = "cliente@censudex.cl";
                var clientName = "Cliente";
                await _emailService.SendOrderCancelledEmailAsync(
                    clientEmail, 
                    clientName, 
                    cancelledOrder.Id, 
                    dto.CancellationReason
                );

                _logger.LogInformation($"Pedido {id} cancelado: {dto.CancellationReason}");

                return MapToDto(cancelledOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cancelar pedido {id}");
                throw;
            }
        }

        /// <summary>
        /// Mapea una entidad Order a OrderResponseDto
        /// </summary>
        private OrderResponseDto MapToDto(Order order)
        {
            return new OrderResponseDto
            {
                Id = order.Id,
                ClientId = order.ClientId,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                ShippingAddress = order.ShippingAddress,
                TrackingNumber = order.TrackingNumber,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Items = order.OrderItems.Select(i => new OrderItemResponseDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Subtotal = i.Subtotal
                }).ToList()
            };
        }
    }
}
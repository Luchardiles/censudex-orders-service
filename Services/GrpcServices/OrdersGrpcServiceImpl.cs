using Grpc.Core;
using censudex_orders.Protos;
using censudex_orders.Services;
using censudex_orders.Models.DTOs;

namespace censudex_orders.Services.GrpcServices
{
    /// <summary>
    /// Implementación del servicio gRPC para Orders
    /// Este servicio es consumido por la API Gateway
    /// </summary>
    public class OrderGrpcService : Protos.OrderService.OrderServiceBase
    {
        private readonly IOrderBusinessService _orderBusinessService;
        private readonly ILogger<OrderGrpcService> _logger;

        public OrderGrpcService(
            IOrderBusinessService orderBusinessService,
            ILogger<OrderGrpcService> logger)
        {
            _orderBusinessService = orderBusinessService;
            _logger = logger;
        }

        /// <summary>
        /// Crea un nuevo pedido a través de gRPC
        /// Endpoint consumido por: POST /orders en API Gateway
        /// </summary>
        public override async Task<OrderResponse> CreateOrder(
            CreateOrderRequest request, 
            ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"gRPC CreateOrder llamado para cliente {request.ClientId}");

                // Validar que el client_id sea un GUID válido
                if (!Guid.TryParse(request.ClientId, out var clientId))
                {
                    throw new RpcException(new Status(
                        StatusCode.InvalidArgument, 
                        "El ID del cliente no es válido"
                    ));
                }

                // Validar items
                if (request.Items == null || request.Items.Count == 0)
                {
                    throw new RpcException(new Status(
                        StatusCode.InvalidArgument,
                        "El pedido debe contener al menos un producto"
                    ));
                }

                // Mapear request a DTO
                var createDto = new CreateOrderDto
                {
                    ClientId = clientId,
                    ShippingAddress = request.ShippingAddress,
                    Items = request.Items.Select(i => new OrderItemDto
                    {
                        ProductId = Guid.Parse(i.ProductId),
                        Quantity = i.Quantity
                    }).ToList()
                };

                // Crear el pedido
                var result = await _orderBusinessService.CreateOrderAsync(createDto);

                // Retornar respuesta gRPC
                return MapToGrpcResponse(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argumento inválido en CreateOrder");
                throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear pedido vía gRPC");
                throw new RpcException(new Status(StatusCode.Internal, "Error interno al crear pedido"));
            }
        }

        /// <summary>
        /// Obtiene todos los pedidos con filtros opcionales
        /// Endpoint consumido por: GET /orders en API Gateway
        /// </summary>
        public override async Task<OrderListResponse> GetOrders(
            GetOrdersRequest request, 
            ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("gRPC GetOrders llamado");

                // Construir filtros
                OrderFilterDto? filter = null;

                if (!string.IsNullOrEmpty(request.OrderId) ||
                    !string.IsNullOrEmpty(request.ClientId) ||
                    !string.IsNullOrEmpty(request.StartDate) ||
                    !string.IsNullOrEmpty(request.EndDate))
                {
                    filter = new OrderFilterDto();

                    if (!string.IsNullOrEmpty(request.OrderId) && Guid.TryParse(request.OrderId, out var orderId))
                    {
                        filter.OrderId = orderId;
                    }

                    if (!string.IsNullOrEmpty(request.ClientId) && Guid.TryParse(request.ClientId, out var clientId))
                    {
                        filter.ClientId = clientId;
                    }

                    if (!string.IsNullOrEmpty(request.ClientName))
                    {
                        filter.ClientName = request.ClientName;
                    }

                    if (!string.IsNullOrEmpty(request.StartDate) && DateTime.TryParse(request.StartDate, out var startDate))
                    {
                        filter.StartDate = startDate;
                    }

                    if (!string.IsNullOrEmpty(request.EndDate) && DateTime.TryParse(request.EndDate, out var endDate))
                    {
                        filter.EndDate = endDate;
                    }
                }

                // Obtener pedidos
                var orders = await _orderBusinessService.GetAllOrdersAsync(filter);

                // Mapear a respuesta gRPC
                var response = new OrderListResponse
                {
                    TotalCount = orders.Count
                };

                response.Orders.AddRange(orders.Select(MapToGrpcResponse));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener pedidos vía gRPC");
                throw new RpcException(new Status(StatusCode.Internal, "Error interno al obtener pedidos"));
            }
        }

        /// <summary>
        /// Obtiene un pedido específico por su ID
        /// Endpoint consumido por: GET /orders/{id} en API Gateway
        /// </summary>
        public override async Task<OrderResponse> GetOrderById(
            GetOrderByIdRequest request, 
            ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"gRPC GetOrderById llamado para pedido {request.OrderId}");

                if (!Guid.TryParse(request.OrderId, out var orderId))
                {
                    throw new RpcException(new Status(
                        StatusCode.InvalidArgument,
                        "El ID del pedido no es válido"
                    ));
                }

                var order = await _orderBusinessService.GetOrderByIdAsync(orderId);
                return MapToGrpcResponse(order);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Pedido no encontrado: {request.OrderId}");
                throw new RpcException(new Status(StatusCode.NotFound, "Pedido no encontrado"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener pedido por ID vía gRPC");
                throw new RpcException(new Status(StatusCode.Internal, "Error interno al obtener pedido"));
            }
        }

        /// <summary>
        /// Actualiza el estado de un pedido
        /// Endpoint consumido por: PUT /orders/{id}/status en API Gateway
        /// </summary>
        public override async Task<OrderResponse> UpdateOrderStatus(
            UpdateOrderStatusRequest request, 
            ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"gRPC UpdateOrderStatus llamado para pedido {request.OrderId}");

                if (!Guid.TryParse(request.OrderId, out var orderId))
                {
                    throw new RpcException(new Status(
                        StatusCode.InvalidArgument,
                        "El ID del pedido no es válido"
                    ));
                }

                var updateDto = new UpdateOrderStatusDto
                {
                    Status = request.Status,
                    TrackingNumber = request.TrackingNumber
                };

                var result = await _orderBusinessService.UpdateOrderStatusAsync(orderId, updateDto);
                return MapToGrpcResponse(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Pedido no encontrado: {request.OrderId}");
                throw new RpcException(new Status(StatusCode.NotFound, "Pedido no encontrado"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"Operación inválida en pedido {request.OrderId}");
                throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar estado del pedido vía gRPC");
                throw new RpcException(new Status(StatusCode.Internal, "Error interno al actualizar pedido"));
            }
        }

        /// <summary>
        /// Cancela un pedido
        /// Endpoint consumido por: PATCH /orders/{id} en API Gateway
        /// </summary>
        public override async Task<OrderResponse> CancelOrder(
            CancelOrderRequest request, 
            ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"gRPC CancelOrder llamado para pedido {request.OrderId}");

                if (!Guid.TryParse(request.OrderId, out var orderId))
                {
                    throw new RpcException(new Status(
                        StatusCode.InvalidArgument,
                        "El ID del pedido no es válido"
                    ));
                }

                var cancelDto = new CancelOrderDto
                {
                    CancellationReason = request.CancellationReason
                };

                var result = await _orderBusinessService.CancelOrderAsync(orderId, cancelDto);
                return MapToGrpcResponse(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Pedido no encontrado: {request.OrderId}");
                throw new RpcException(new Status(StatusCode.NotFound, "Pedido no encontrado"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"No se puede cancelar el pedido {request.OrderId}");
                throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar pedido vía gRPC");
                throw new RpcException(new Status(StatusCode.Internal, "Error interno al cancelar pedido"));
            }
        }

        /// <summary>
        /// Mapea un OrderResponseDto a OrderResponse de gRPC
        /// </summary>
        private OrderResponse MapToGrpcResponse(OrderResponseDto dto)
        {
            var response = new OrderResponse
            {
                Id = dto.Id.ToString(),
                ClientId = dto.ClientId.ToString(),
                Status = dto.Status,
                TotalAmount = (double)dto.TotalAmount,
                ShippingAddress = dto.ShippingAddress,
                TrackingNumber = dto.TrackingNumber ?? "",
                CancellationReason = "",
                CreatedAt = dto.CreatedAt.ToString("O"),
                UpdatedAt = dto.UpdatedAt.ToString("O")
            };

            response.Items.AddRange(dto.Items.Select(i => new OrderItemResponse
            {
                Id = i.Id.ToString(),
                ProductId = i.ProductId.ToString(),
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = (double)i.UnitPrice,
                Subtotal = (double)i.Subtotal
            }));

            return response;
        }
    }
}
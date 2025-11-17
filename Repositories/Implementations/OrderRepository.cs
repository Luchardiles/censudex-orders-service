using Microsoft.EntityFrameworkCore;
using censudex_orders.Data.Context;
using censudex_orders.Models.Entities;
using censudex_orders.Models.DTOs;
using censudex_orders.Repositories.Interfaces;

namespace censudex_orders.Repositories.Implementations
{
    /// <summary>
    /// Repositorio para gestionar operaciones de base de datos de pedidos
    /// Implementa el patrón Repository para abstraer el acceso a datos
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly OrderDbContext _context;
        private readonly ILogger<OrderRepository> _logger;

        public OrderRepository(OrderDbContext context, ILogger<OrderRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Crea un nuevo pedido en la base de datos
        /// </summary>
        /// <param name="order">Entidad del pedido a crear</param>
        /// <returns>Pedido creado</returns>
        public async Task<Order> CreateAsync(Order order)
        {
            try
            {
                // Generar UUID V4 para el pedido
                order.Id = Guid.NewGuid();
                order.CreatedAt = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;

                // Generar UUID para cada item
                foreach (var item in order.OrderItems)
                {
                    item.Id = Guid.NewGuid();
                    item.OrderId = order.Id;
                }

                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Pedido creado exitosamente: {order.Id}");
                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear pedido");
                throw;
            }
        }

        /// <summary>
        /// Obtiene un pedido por su ID incluyendo los items
        /// </summary>
        /// <param name="id">ID del pedido</param>
        /// <returns>Pedido encontrado o null</returns>
        public async Task<Order?> GetByIdAsync(Guid id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    _logger.LogWarning($"Pedido no encontrado: {id}");
                }

                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener pedido {id}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene todos los pedidos con filtros opcionales
        /// </summary>
        /// <param name="filter">Filtros de búsqueda</param>
        /// <returns>Lista de pedidos</returns>
        public async Task<List<Order>> GetAllAsync(OrderFilterDto? filter = null)
        {
            try
            {
                var query = _context.Orders
                    .Include(o => o.OrderItems)
                    .AsQueryable();

                // Aplicar filtros si existen
                if (filter != null)
                {
                    // Filtrar por ID de pedido
                    if (filter.OrderId.HasValue)
                    {
                        query = query.Where(o => o.Id == filter.OrderId.Value);
                    }

                    // Filtrar por ID de cliente
                    if (filter.ClientId.HasValue)
                    {
                        query = query.Where(o => o.ClientId == filter.ClientId.Value);
                    }

                    // Filtrar por rango de fechas
                    if (filter.StartDate.HasValue)
                    {
                        query = query.Where(o => o.CreatedAt >= filter.StartDate.Value);
                    }

                    if (filter.EndDate.HasValue)
                    {
                        // Incluir todo el día final
                        var endDate = filter.EndDate.Value.AddDays(1);
                        query = query.Where(o => o.CreatedAt < endDate);
                    }
                }

                // Ordenar por fecha de creación descendente (más recientes primero)
                var orders = await query
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation($"Se obtuvieron {orders.Count} pedidos");
                return orders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener pedidos");
                throw;
            }
        }

        /// <summary>
        /// Actualiza un pedido existente
        /// </summary>
        /// <param name="order">Pedido con datos actualizados</param>
        /// <returns>Pedido actualizado</returns>
        public async Task<Order> UpdateAsync(Order order)
        {
            try
            {
                order.UpdatedAt = DateTime.UtcNow;
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Pedido actualizado: {order.Id}");
                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar pedido {order.Id}");
                throw;
            }
        }

        /// <summary>
        /// Verifica si existe un pedido por su ID
        /// </summary>
        /// <param name="id">ID del pedido</param>
        /// <returns>True si existe, false si no</returns>
        public async Task<bool> ExistsAsync(Guid id)
        {
            try
            {
                return await _context.Orders.AnyAsync(o => o.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al verificar existencia de pedido {id}");
                throw;
            }
        }
    }
}
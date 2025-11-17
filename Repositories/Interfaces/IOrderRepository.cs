using Microsoft.EntityFrameworkCore;
using censudex_orders.Data.Context;
using censudex_orders.Models.Entities;
using censudex_orders.Models.DTOs;

namespace censudex_orders.Repositories.Interfaces
{
    /// <summary>
    /// Interfaz para el repositorio de pedidos
    /// </summary>
    public interface IOrderRepository
    {
        Task<Order> CreateAsync(Order order);
        Task<Order?> GetByIdAsync(Guid id);
        Task<List<Order>> GetAllAsync(OrderFilterDto? filter = null);
        Task<Order> UpdateAsync(Order order);
        Task<bool> ExistsAsync(Guid id);
    }
}
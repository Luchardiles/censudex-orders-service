using censudex_orders.Data.Context;
using censudex_orders.Models.Entities;
using censudex_orders.Models.Enums;

namespace censudex_orders.Data.Seeders
{
    /// <summary>
    /// Seeder para poblar la base de datos con datos de prueba
    /// Útil para desarrollo y testing
    /// </summary>
    public static class OrderSeeder
    {
        /// <summary>
        /// Siembra datos de prueba en la base de datos
        /// </summary>
        public static async Task SeedAsync(OrderDbContext context)
        {
            // Verificar si ya hay datos
            if (context.Orders.Any())
            {
                Console.WriteLine("La base de datos ya contiene pedidos. Seeder omitido.");
                return;
            }

            Console.WriteLine("Iniciando seeder de pedidos...");

            // IDs de clientes y productos de ejemplo (deberían corresponder a IDs reales de otros servicios)
            var clientIds = new[]
            {
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Guid.Parse("33333333-3333-3333-3333-333333333333")
            };

            var productIds = new[]
            {
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd")
            };

            var random = new Random();

            // Crear 10 pedidos de ejemplo
            var orders = new List<Order>();

            for (int i = 0; i < 10; i++)
            {
                var clientId = clientIds[random.Next(clientIds.Length)];
                var numItems = random.Next(1, 5); // Entre 1 y 4 items por pedido

                var orderItems = new List<OrderItem>();
                decimal totalAmount = 0;

                // Crear items del pedido
                for (int j = 0; j < numItems; j++)
                {
                    var productId = productIds[random.Next(productIds.Length)];
                    var quantity = random.Next(1, 6);
                    var unitPrice = random.Next(10, 500) * 1.99m;
                    var subtotal = quantity * unitPrice;

                    orderItems.Add(new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = productId,
                        ProductName = $"Producto {productId.ToString()[..8]}",
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        Subtotal = subtotal
                    });

                    totalAmount += subtotal;
                }

                // Determinar estado del pedido
                var statuses = new[] 
                { 
                    OrderStatus.Pendiente.ToString(),
                    OrderStatus.EnProcesamiento.ToString(),
                    OrderStatus.Enviado.ToString(),
                    OrderStatus.Entregado.ToString()
                };
                var status = statuses[random.Next(statuses.Length)];

                // Crear el pedido
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    ClientId = clientId,
                    Status = status,
                    TotalAmount = totalAmount,
                    ShippingAddress = $"Calle Ejemplo {random.Next(100, 999)}, Antofagasta, Chile",
                    TrackingNumber = status == OrderStatus.Enviado.ToString() || status == OrderStatus.Entregado.ToString() 
                        ? $"TRACK-{random.Next(100000, 999999)}" 
                        : null,
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 30)),
                    UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 5)),
                    OrderItems = orderItems
                };

                orders.Add(order);
            }

            // Agregar algunos pedidos cancelados
            var cancelledOrder = new Order
            {
                Id = Guid.NewGuid(),
                ClientId = clientIds[0],
                Status = OrderStatus.Cancelado.ToString(),
                TotalAmount = 150.50m,
                ShippingAddress = "Av. Principal 456, Antofagasta, Chile",
                CancellationReason = "Cliente solicitó cancelación - Cambió de opinión",
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-14),
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = productIds[0],
                        ProductName = $"Producto {productIds[0].ToString()[..8]}",
                        Quantity = 2,
                        UnitPrice = 75.25m,
                        Subtotal = 150.50m
                    }
                }
            };

            orders.Add(cancelledOrder);

            // Guardar en la base de datos
            await context.Orders.AddRangeAsync(orders);
            await context.SaveChangesAsync();

            Console.WriteLine($"Seeder completado: {orders.Count} pedidos creados exitosamente");
        }
    }
}
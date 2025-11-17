using Microsoft.EntityFrameworkCore;
using censudex_orders.Models.Entities;

namespace censudex_orders.Data.Context
{
    /// <summary>
    /// Contexto de base de datos para el servicio de órdenes
    /// Utiliza MySQL como base de datos
    /// </summary>
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// DbSet de pedidos
        /// </summary>
        public DbSet<Order> Orders { get; set; }

        /// <summary>
        /// DbSet de items de pedidos
        /// </summary>
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de la entidad Order
            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("orders");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever(); // UUID generado manualmente

                entity.Property(e => e.ClientId)
                    .HasColumnName("client_id")
                    .IsRequired();

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.TotalAmount)
                    .HasColumnName("total_amount")
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();

                entity.Property(e => e.ShippingAddress)
                    .HasColumnName("shipping_address")
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(e => e.TrackingNumber)
                    .HasColumnName("tracking_number")
                    .HasMaxLength(100);

                entity.Property(e => e.CancellationReason)
                    .HasColumnName("cancellation_reason")
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("updated_at")
                    .HasColumnType("timestamp")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");

                // Índices para mejorar el rendimiento
                entity.HasIndex(e => e.ClientId).HasDatabaseName("idx_client_id");
                entity.HasIndex(e => e.Status).HasDatabaseName("idx_status");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_created_at");
            });

            // Configuración de la entidad OrderItem
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("order_items");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever(); // UUID generado manualmente

                entity.Property(e => e.OrderId)
                    .HasColumnName("order_id")
                    .IsRequired();

                entity.Property(e => e.ProductId)
                    .HasColumnName("product_id")
                    .IsRequired();

                entity.Property(e => e.ProductName)
                    .HasColumnName("product_name")
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(e => e.Quantity)
                    .HasColumnName("quantity")
                    .IsRequired();

                entity.Property(e => e.UnitPrice)
                    .HasColumnName("unit_price")
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();

                entity.Property(e => e.Subtotal)
                    .HasColumnName("subtotal")
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();

                // Relación con Order
                entity.HasOne(e => e.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Índices
                entity.HasIndex(e => e.OrderId).HasDatabaseName("idx_order_id");
                entity.HasIndex(e => e.ProductId).HasDatabaseName("idx_product_id");
            });
        }
    }
}
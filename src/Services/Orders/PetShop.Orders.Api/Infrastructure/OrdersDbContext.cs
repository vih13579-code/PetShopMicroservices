using Microsoft.EntityFrameworkCore;
using PetShop.Orders.Api.Domain;

namespace PetShop.Orders.Api.Infrastructure;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CustomerId).IsUnique();
            entity.HasMany(x => x.Items).WithOne(x => x.Cart).HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CartId, x.ProductId, x.VariantId }).IsUnique();
            entity.Property(x => x.ProductName).HasMaxLength(220).IsRequired();
            entity.Property(x => x.UnitPrice).HasPrecision(18,2);
        });
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.OrderCode).IsUnique();
            entity.HasIndex(x => new { x.CustomerId, x.CreatedAt });
            entity.HasIndex(x => new { x.ShopId, x.Status, x.CreatedAt });
            entity.Property(x => x.OrderCode).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ReceiverName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.ReceiverPhone).HasMaxLength(30).IsRequired();
            entity.Property(x => x.ShippingAddress).HasMaxLength(500).IsRequired();
            entity.Property(x => x.SubTotal).HasPrecision(18,2);
            entity.Property(x => x.ShippingFee).HasPrecision(18,2);
            entity.Property(x => x.TotalAmount).HasPrecision(18,2);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(30);
            entity.HasMany(x => x.Items).WithOne(x => x.Order).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.StatusHistories).WithOne(x => x.Order).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UnitPrice).HasPrecision(18,2);
            entity.Property(x => x.LineTotal).HasPrecision(18,2);
        });
        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        });
    }
}

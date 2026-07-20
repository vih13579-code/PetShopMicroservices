using Microsoft.EntityFrameworkCore;
using PetShop.Inventory.Api.Domain;

namespace PetShop.Inventory.Api.Infrastructure;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ShopId, x.ProductId }).IsUnique();
            entity.Property(x => x.RowVersion).IsRowVersion();
        });
        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
        });
        modelBuilder.Entity<StockReservation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.OrderId, x.ProductId }).IsUnique();
        });
    }
}

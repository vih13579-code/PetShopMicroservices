using Microsoft.EntityFrameworkCore;
using PetShop.Shops.Api.Domain;

namespace PetShop.Shops.Api.Infrastructure;

public sealed class ShopsDbContext(DbContextOptions<ShopsDbContext> options) : DbContext(options)
{
    public DbSet<ShopRegistrationRequest> ShopRegistrationRequests => Set<ShopRegistrationRequest>();
    public DbSet<Shop> Shops => Set<Shop>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShopRegistrationRequest>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.Status });
            entity.Property(x => x.ShopName).HasMaxLength(180).IsRequired();
            entity.Property(x => x.OwnerName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Address).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        });
        modelBuilder.Entity<Shop>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.OwnerUserId).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Address).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        });
    }
}

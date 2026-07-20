using Microsoft.EntityFrameworkCore;
using PetShop.Notifications.Api.Domain;

namespace PetShop.Notifications.Api.Infrastructure;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
            entity.Property(x => x.Title).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(80).IsRequired();
        });
    }
}

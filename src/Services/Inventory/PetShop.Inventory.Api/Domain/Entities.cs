using System.ComponentModel.DataAnnotations;

namespace PetShop.Inventory.Api.Domain;

public enum StockTransactionType { Initial, Import, ManualAdjust, Reserve, Commit, Release, Return }

public sealed class InventoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShopId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}

public sealed class StockTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShopId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? OrderId { get; set; }
    public int QuantityChange { get; set; }
    public StockTransactionType Type { get; set; }
    public string? Reason { get; set; }
    public Guid? PerformedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class StockReservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid ShopId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public bool IsCommitted { get; set; }
    public bool IsReleased { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

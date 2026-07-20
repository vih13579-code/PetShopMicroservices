using System.ComponentModel.DataAnnotations;

namespace PetShop.Inventory.Api.Contracts;

public sealed class SetStockRequest
{
    [Required] public Guid ProductId { get; set; }
    [Range(0, int.MaxValue)] public int Quantity { get; set; }
    [StringLength(500)] public string? Reason { get; set; }
}

public sealed class AdjustStockRequest
{
    [Required] public Guid ProductId { get; set; }
    [Range(-1_000_000, 1_000_000)] public int QuantityChange { get; set; }
    [Required, StringLength(500, MinimumLength = 3)] public string Reason { get; set; } = string.Empty;
}

public sealed record InventoryResponse(Guid ProductId, Guid ShopId, int Quantity, int ReservedQuantity,
    int AvailableQuantity, DateTime UpdatedAt);

public sealed record ReservationItem(Guid ProductId, int Quantity);
public sealed class ReserveStockRequest
{
    public Guid OrderId { get; set; }
    public Guid ShopId { get; set; }
    public List<ReservationItem> Items { get; set; } = [];
}

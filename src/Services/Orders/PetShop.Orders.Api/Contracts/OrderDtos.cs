using System.ComponentModel.DataAnnotations;
using PetShop.Orders.Api.Domain;

namespace PetShop.Orders.Api.Contracts;

public sealed class AddCartItemRequest
{
    [Required] public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    [Range(1, 999)] public int Quantity { get; set; } = 1;
}

public sealed class UpdateCartItemRequest
{
    [Range(1, 999)] public int Quantity { get; set; }
}

public sealed class CheckoutRequest
{
    [Required, StringLength(150)] public string ReceiverName { get; set; } = string.Empty;
    [Required, Phone, StringLength(30)] public string ReceiverPhone { get; set; } = string.Empty;
    [Required, StringLength(500, MinimumLength = 5)] public string ShippingAddress { get; set; } = string.Empty;
    [StringLength(1000)] public string? Note { get; set; }
    [Range(0, 1_000_000)] public decimal ShippingFeePerShop { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;
}

public sealed class ChangeStatusRequest
{
    [StringLength(500)] public string? Note { get; set; }
}

public sealed record CartItemResponse(Guid Id, Guid ProductId, Guid? VariantId, Guid ShopId,
    string ProductName, string? VariantName, string? Sku, string? ImageUrl,
    decimal UnitPrice, int Quantity, decimal LineTotal);
public sealed record CartResponse(Guid Id, Guid CustomerId, IReadOnlyCollection<CartItemResponse> Items, decimal Total);
public sealed record OrderItemResponse(Guid Id, Guid ProductId, Guid? VariantId, string ProductName,
    string? VariantName, string? Sku, string? ImageUrl, decimal UnitPrice, int Quantity, decimal LineTotal);
public sealed record OrderResponse(Guid Id, string OrderCode, Guid CustomerId, Guid ShopId,
    string ReceiverName, string ReceiverPhone, string ShippingAddress, string? Note,
    decimal SubTotal, decimal ShippingFee, decimal TotalAmount, PaymentMethod PaymentMethod,
    PaymentStatus PaymentStatus, OrderStatus Status, DateTime CreatedAt,
    IReadOnlyCollection<OrderItemResponse> Items);
public sealed record InternalOrderSummary(Guid Id, string OrderCode, Guid CustomerId, Guid ShopId,
    decimal TotalAmount, string Status, string PaymentStatus, string PaymentMethod);

using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace PetShop.Web.Models;

public sealed class LoginVm
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}
public class RegisterVm
{
    [Required] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(6)] public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
}
public sealed record UserVm(Guid Id, string FullName, string Email, string? Phone, string? Address,
    bool IsActive, DateTime CreatedAt, IReadOnlyCollection<string> Roles);
public sealed record TokenVm(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken,
    DateTime RefreshTokenExpiresAt, UserVm User);
public sealed record PagedVm<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalItems, int TotalPages);
public sealed record VariantVm(Guid Id, string Name, string Sku, decimal AdditionalPrice, bool IsActive);
public sealed record ProductVm(Guid Id, Guid ShopId, Guid CategoryId, string CategoryName, string Name,
    string? Description, decimal Price, string? ImageUrl, bool IsActive, DateTime CreatedAt,
    IReadOnlyCollection<VariantVm> Variants);
public sealed record CategoryVm(Guid Id, Guid ShopId, string Name, string? Description, bool IsActive, DateTime CreatedAt);
public sealed record CartItemVm(Guid Id, Guid ProductId, Guid? VariantId, Guid ShopId, string ProductName,
    string? VariantName, string? Sku, string? ImageUrl, decimal UnitPrice, int Quantity, decimal LineTotal);
public sealed record CartVm(Guid Id, Guid CustomerId, IReadOnlyCollection<CartItemVm> Items, decimal Total);
public sealed record OrderItemVm(Guid Id, Guid ProductId, Guid? VariantId, string ProductName, string? VariantName,
    string? Sku, string? ImageUrl, decimal UnitPrice, int Quantity, decimal LineTotal);
public sealed record OrderVm(Guid Id, string OrderCode, Guid CustomerId, Guid ShopId, string ReceiverName,
    string ReceiverPhone, string ShippingAddress, string? Note, decimal SubTotal, decimal ShippingFee,
    decimal TotalAmount, string PaymentMethod, string PaymentStatus, string Status, DateTime CreatedAt,
    IReadOnlyCollection<OrderItemVm> Items);
public sealed record ShopRequestVm(Guid Id, Guid UserId, string OwnerName, string ShopName, string? Description,
    string Phone, string Email, string Address, string? TaxCode, string Status, string? RejectionReason,
    DateTime CreatedAt, DateTime? ProcessedAt);
public sealed record ShopVm(Guid Id, Guid OwnerUserId, string Name, string? Description, string Phone, string Email,
    string Address, string? TaxCode, string Status, DateTime CreatedAt, DateTime? UpdatedAt);
public sealed record InventoryVm(Guid ProductId, Guid ShopId, int Quantity, int ReservedQuantity, int AvailableQuantity, DateTime UpdatedAt);
public sealed record NotificationVm(Guid Id, Guid UserId, string Title, string Message, string Type, bool IsRead, DateTime CreatedAt, DateTime? ReadAt);

public sealed class ShopRequestFormVm
{
    [Required] public string ShopName { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required] public string Phone { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Address { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
}
public sealed class CategoryFormVm { [Required] public string Name { get; set; } = string.Empty; public string? Description { get; set; } }
public sealed class ProductFormVm
{
    public Guid Id { get; set; }
    [Required] public Guid CategoryId { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Range(0.01, double.MaxValue)] public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
public sealed class CheckoutVm
{
    [Required] public string ReceiverName { get; set; } = string.Empty;
    [Required] public string ReceiverPhone { get; set; } = string.Empty;
    [Required] public string ShippingAddress { get; set; } = string.Empty;
    public string? Note { get; set; }
    public decimal ShippingFeePerShop { get; set; }
    public string PaymentMethod { get; set; } = "COD";
}

public sealed record PaymentVm(Guid Id, Guid OrderId, Guid CustomerId, decimal Amount, string Method, string Status, string? TransactionCode, DateTime CreatedAt, DateTime? PaidAt, DateTime? RefundedAt);

public sealed class ShopEditVm
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required] public string Phone { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Address { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
}
public sealed class StaffFormVm : RegisterVm { }

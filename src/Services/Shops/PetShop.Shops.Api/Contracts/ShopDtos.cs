using System.ComponentModel.DataAnnotations;
using PetShop.Shops.Api.Domain;

namespace PetShop.Shops.Api.Contracts;

public sealed class CreateShopRequest
{
    [Required, StringLength(180, MinimumLength = 3)] public string ShopName { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
    [Required, Phone, StringLength(30)] public string Phone { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(180)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(500, MinimumLength = 5)] public string Address { get; set; } = string.Empty;
    [StringLength(50)] public string? TaxCode { get; set; }
}

public sealed class RejectShopRequest
{
    [Required, StringLength(500, MinimumLength = 5)] public string Reason { get; set; } = string.Empty;
}

public sealed class UpdateShopRequest
{
    [Required, StringLength(180, MinimumLength = 3)] public string Name { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
    [Required, Phone, StringLength(30)] public string Phone { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(180)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(500)] public string Address { get; set; } = string.Empty;
    [StringLength(50)] public string? TaxCode { get; set; }
}

public sealed record ShopRequestResponse(Guid Id, Guid UserId, string OwnerName, string ShopName,
    string? Description, string Phone, string Email, string Address, string? TaxCode,
    ShopRequestStatus Status, string? RejectionReason, DateTime CreatedAt, DateTime? ProcessedAt);

public sealed record ShopResponse(Guid Id, Guid OwnerUserId, string Name, string? Description,
    string Phone, string Email, string Address, string? TaxCode, ShopStatus Status,
    DateTime CreatedAt, DateTime? UpdatedAt);

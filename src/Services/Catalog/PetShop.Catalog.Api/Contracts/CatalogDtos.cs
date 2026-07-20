using System.ComponentModel.DataAnnotations;

namespace PetShop.Catalog.Api.Contracts;

public sealed class CategoryRequest
{
    [Required, StringLength(150, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
}

public sealed class VariantRequest
{
    public Guid? Id { get; set; }
    [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(80)] public string Sku { get; set; } = string.Empty;
    [Range(0, 1_000_000_000)] public decimal AdditionalPrice { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ProductRequest
{
    [Required] public Guid CategoryId { get; set; }
    [Required, StringLength(220, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    [StringLength(3000)] public string? Description { get; set; }
    [Range(0.01, 1_000_000_000)] public decimal Price { get; set; }
    [Url, StringLength(1000)] public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public List<VariantRequest> Variants { get; set; } = [];
}

public sealed record CategoryResponse(Guid Id, Guid ShopId, string Name, string? Description, bool IsActive, DateTime CreatedAt);
public sealed record VariantResponse(Guid Id, string Name, string Sku, decimal AdditionalPrice, bool IsActive);
public sealed record ProductResponse(Guid Id, Guid ShopId, Guid CategoryId, string CategoryName, string Name,
    string? Description, decimal Price, string? ImageUrl, bool IsActive, DateTime CreatedAt,
    IReadOnlyCollection<VariantResponse> Variants);
public sealed record ProductSnapshot(Guid Id, Guid ShopId, string Name, string? ImageUrl, bool IsActive,
    decimal BasePrice, Guid? VariantId, string? VariantName, string? Sku, decimal UnitPrice);

public sealed class ReviewRequest
{
    [Range(1, 5)] public int Rating { get; set; }
    [StringLength(2000)] public string? Comment { get; set; }
}

public sealed record ReviewResponse(Guid Id, Guid ProductId, Guid UserId, int Rating, string? Comment, DateTime CreatedAt, DateTime? UpdatedAt);

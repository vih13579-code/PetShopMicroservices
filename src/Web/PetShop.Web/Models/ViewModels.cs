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
    bool IsActive, DateTime CreatedAt, IReadOnlyCollection<string> Roles)
{
    public bool IsInRole(string role) => Roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
    public bool HasAnyRole(params string[] roles) => roles.Any(IsInRole);
    public bool IsAdmin => IsInRole("Admin");
    public bool IsStaff => IsInRole("Staff");
    public bool IsShopOwner => IsInRole("ShopOwner");
    public bool IsCustomer => IsInRole("Customer");
}
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
public sealed class CategoryFormVm
{
    public Guid Id { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tên danh mục."), StringLength(150, MinimumLength = 2, ErrorMessage = "Tên danh mục phải từ 2 đến 150 ký tự.")]
    [Display(Name = "Tên danh mục")]
    public string Name { get; set; } = string.Empty;
    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }
    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;
}

public sealed class VariantFormVm : IValidatableObject
{
    public Guid? Id { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tên phân loại."), StringLength(150)]
    public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập SKU."), StringLength(80)]
    public string Sku { get; set; } = string.Empty;
    [Range(0, 1_000_000_000, ErrorMessage = "Giá cộng thêm phải từ 0 đến 1.000.000.000 đồng.")]
    public decimal AdditionalPrice { get; set; }
    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AdditionalPrice != decimal.Truncate(AdditionalPrice))
            yield return new ValidationResult("Giá cộng thêm phải là số nguyên theo đơn vị đồng.", [nameof(AdditionalPrice)]);
    }
}

public sealed class ProductFormVm : IValidatableObject
{
    public Guid Id { get; set; }
    [Required(ErrorMessage = "Vui lòng chọn danh mục."), Display(Name = "Danh mục")] public Guid CategoryId { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm."), StringLength(220, MinimumLength = 2), Display(Name = "Tên sản phẩm")] public string Name { get; set; } = string.Empty;
    [StringLength(3000), Display(Name = "Mô tả")] public string? Description { get; set; }
    [Range(1, 1_000_000_000, ErrorMessage = "Giá bán phải từ 1 đến 1.000.000.000 đồng."), Display(Name = "Giá bán")] public decimal Price { get; set; }
    [Url(ErrorMessage = "URL hình ảnh không hợp lệ."), StringLength(1000), Display(Name = "Ảnh sản phẩm")] public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public List<VariantFormVm> Variants { get; set; } = [];
    public List<Guid> RemovedVariantIds { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Price != decimal.Truncate(Price))
            yield return new ValidationResult("Giá bán phải là số nguyên theo đơn vị đồng.", [nameof(Price)]);
        var duplicateSkus = Variants.Where(x => !string.IsNullOrWhiteSpace(x.Sku))
            .GroupBy(x => x.Sku.Trim(), StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1);
        if (duplicateSkus) yield return new ValidationResult("SKU của các phân loại không được trùng nhau.", [nameof(Variants)]);
    }
}

public sealed record OwnerCategoriesPageVm(IReadOnlyCollection<CategoryVm> Items, CategoryFormVm Form);
public sealed record OwnerProductsPageVm(PagedVm<ProductVm> Result, IReadOnlyCollection<CategoryVm> Categories,
    string? Keyword, Guid? CategoryId, bool? IsActive);
public sealed record PublicShopVm(Guid Id, string Name);
public sealed record CatalogPageVm(PagedVm<ProductVm> Result, IReadOnlyCollection<CategoryVm> Categories,
    IReadOnlyCollection<PublicShopVm> Shops, string? Keyword, Guid? ShopId, Guid? CategoryId,
    decimal? MinPrice, decimal? MaxPrice);
public sealed record ProductDetailsPageVm(ProductVm Product, IReadOnlyCollection<ReviewVm> Reviews, ReviewFormVm ReviewForm);
public sealed record ReviewVm(Guid Id, Guid ProductId, Guid UserId, int Rating, string? Comment, DateTime CreatedAt, DateTime? UpdatedAt);
public sealed class ReviewFormVm
{
    [Range(1, 5, ErrorMessage = "Vui lòng chọn từ 1 đến 5 sao.")] public int Rating { get; set; } = 5;
    [StringLength(2000, ErrorMessage = "Nội dung không được vượt quá 2000 ký tự.")] public string? Comment { get; set; }
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
public sealed class ProfileFormVm
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    [Required] public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; }
}
public sealed record DailyRevenueVm(DateTime Date, int Orders, decimal Revenue);
public sealed record RevenueReportVm(
    decimal Revenue,
    int TotalOrders,
    int Completed,
    int Cancelled,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    IReadOnlyCollection<DailyRevenueVm>? ByDay = null)
{
    public decimal TotalRevenue => Revenue;
    public int CompletedOrders => Completed;
    public int CancelledOrders => Cancelled;
}

using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace PetShop.Web.Models;

public sealed class LoginVm
{
    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email không đúng định dạng.")] public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")] public string Password { get; set; } = string.Empty;
}
public class RegisterVm
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")] public string FullName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email không đúng định dạng.")] public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu."), MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")] public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
}
public sealed record UserVm(Guid Id, string FullName, string Email, string? Phone, string? Address,
    bool IsActive, DateTime CreatedAt, IReadOnlyCollection<string> Roles)
{
    public bool IsInRole(string role) => Roles != null && Roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
    public bool HasAnyRole(params string[] roles) => Roles != null && roles.Any(r => IsInRole(r));
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
    [Required(ErrorMessage = "Vui lòng nhập tên cửa hàng.")] public string ShopName { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")] public string Phone { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email không đúng định dạng.")] public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập địa chỉ."), StringLength(500, MinimumLength = 5, ErrorMessage = "Địa chỉ phải có từ 5 đến 500 ký tự.")] public string Address { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
}
public sealed class CategoryFormVm { public Guid Id { get; set; } [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")] public string Name { get; set; } = string.Empty; public string? Description { get; set; } public bool IsActive { get; set; } = true; }
public sealed class VariantFormVm
{
    public Guid? Id { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tên phân loại.")] public string Name { get; set; } = "Mặc định";
    [Required(ErrorMessage = "Vui lòng nhập mã SKU.")] public string Sku { get; set; } = string.Empty;
    public decimal AdditionalPrice { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public sealed class ProductFormVm
{
    public Guid Id { get; set; }
    [Required] public Guid CategoryId { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Range(0.01, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn 0.")] public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public IFormFile? ImageFile { get; set; }
    public bool IsActive { get; set; } = true;
    public List<VariantFormVm> Variants { get; set; } = new();
    public List<Guid> RemovedVariantIds { get; set; } = new();
}
public sealed class CheckoutVm
{
    [Required(ErrorMessage = "Vui lòng nhập tên người nhận.")] public string ReceiverName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại nhận hàng.")] public string ReceiverPhone { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng.")] public string ShippingAddress { get; set; } = string.Empty;
    public string? Note { get; set; }
    public decimal ShippingFeePerShop { get; set; }
    public string PaymentMethod { get; set; } = "COD";
}

public sealed record PaymentVm(Guid Id, Guid OrderId, Guid CustomerId, decimal Amount, string Method, string Status, string? TransactionCode, DateTime CreatedAt, DateTime? PaidAt, DateTime? RefundedAt);

public sealed class ShopEditVm
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tên cửa hàng.")] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")] public string Phone { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email không đúng định dạng.")] public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")] public string Address { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
}
public sealed class StaffFormVm : RegisterVm
{
    public List<string> Roles { get; set; } = ["Staff"];
}
public sealed class ProfileFormVm
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")] public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; }
    public List<string> EditableRoles { get; set; } = new();
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

public sealed record ReviewVm(Guid Id, Guid ProductId, Guid UserId, int Rating, string? Comment, DateTime CreatedAt, DateTime? UpdatedAt);
public sealed record PublicShopVm(Guid Id, string Name);
public sealed record CatalogPageVm(PagedVm<ProductVm> Result, IReadOnlyCollection<CategoryVm> Categories,
    IReadOnlyCollection<PublicShopVm> Shops, string? Keyword, Guid? ShopId, Guid? CategoryId,
    decimal? MinPrice, decimal? MaxPrice);
public sealed class ReviewFormVm
{
    [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5.")] public int Rating { get; set; } = 5;
    [StringLength(2000)] public string? Comment { get; set; }
}
public sealed record ProductDetailsPageVm(ProductVm Product, IReadOnlyCollection<ReviewVm> Reviews, ReviewFormVm ReviewForm);
public sealed record OwnerCategoriesPageVm(IReadOnlyCollection<CategoryVm> Items, CategoryFormVm Form);
public sealed record OwnerProductsPageVm(PagedVm<ProductVm> Result, IReadOnlyCollection<CategoryVm> Categories,
    string? Keyword, Guid? CategoryId, bool? IsActive);


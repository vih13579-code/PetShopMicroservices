namespace PetShop.Contracts;

/// <summary>
/// Kết quả phân trang dùng thống nhất giữa các microservice.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalItems)
{
    public int TotalPages => PageSize <= 0
        ? 0
        : (int)Math.Ceiling(TotalItems / (double)PageSize);
}

/// <summary>
/// Tên header nội bộ dùng khi các service gọi trực tiếp lẫn nhau.
/// Không gửi khóa này ra frontend.
/// </summary>
public static class InternalHeaders
{
    public const string ApiKey = "X-Internal-Key";
}

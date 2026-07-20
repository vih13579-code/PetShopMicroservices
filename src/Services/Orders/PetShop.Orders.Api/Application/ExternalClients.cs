using System.Net.Http.Json;
using PetShop.Orders.Api.Contracts;
using PetShop.ServiceDefaults;

namespace PetShop.Orders.Api.Application;

public sealed record ProductSnapshot(Guid Id, Guid ShopId, string Name, string? ImageUrl, bool IsActive,
    decimal BasePrice, Guid? VariantId, string? VariantName, string? Sku, decimal UnitPrice);
public sealed record OwnedShop(Guid Id, Guid OwnerUserId, string Name, string Status, bool IsActive);

public sealed class CatalogClient(HttpClient httpClient, IConfiguration configuration)
{
    public async Task<ProductSnapshot?> GetProductAsync(Guid productId, Guid? variantId)
    {
        httpClient.AddInternalKey(configuration);
        var url = $"internal/catalog/products/{productId}" + (variantId.HasValue ? $"?variantId={variantId}" : string.Empty);
        var response = await httpClient.GetAsync(url);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductSnapshot>();
    }
}

public sealed class InventoryClient(HttpClient httpClient, IConfiguration configuration)
{
    public async Task ReserveAsync(Guid orderId, Guid shopId, IEnumerable<(Guid ProductId, int Quantity)> items)
    {
        httpClient.AddInternalKey(configuration);
        var response = await httpClient.PostAsJsonAsync("internal/inventory/reserve", new
        {
            orderId, shopId, items = items.Select(x => new { productId = x.ProductId, quantity = x.Quantity })
        });
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Không đủ tồn kho: {error}");
        }
        response.EnsureSuccessStatusCode();
    }

    public async Task CommitAsync(Guid orderId) => await PostAsync($"internal/inventory/orders/{orderId}/commit");
    public async Task ReleaseAsync(Guid orderId) => await PostAsync($"internal/inventory/orders/{orderId}/release");
    public async Task ReturnAsync(Guid orderId) => await PostAsync($"internal/inventory/orders/{orderId}/return");

    private async Task PostAsync(string url)
    {
        httpClient.AddInternalKey(configuration);
        var response = await httpClient.PostAsync(url, null);
        response.EnsureSuccessStatusCode();
    }
}

public sealed class ShopClient(HttpClient httpClient, IConfiguration configuration)
{
    public async Task<OwnedShop?> GetByOwnerAsync(Guid userId)
    {
        httpClient.AddInternalKey(configuration);
        var response = await httpClient.GetAsync($"internal/shops/by-owner/{userId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OwnedShop>();
    }
}

public sealed class NotificationClient(HttpClient httpClient, IConfiguration configuration)
{
    public async Task SendAsync(Guid userId, string title, string message, string type)
    {
        httpClient.AddInternalKey(configuration);
        var response = await httpClient.PostAsJsonAsync("internal/notifications", new { userId, title, message, type });
        response.EnsureSuccessStatusCode();
    }
}

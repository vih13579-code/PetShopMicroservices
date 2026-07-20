using System.Net.Http.Json;
using PetShop.ServiceDefaults;

namespace PetShop.Catalog.Api.Application;

public sealed record OwnedShop(Guid Id, Guid OwnerUserId, string Name, string Status, bool IsActive);

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


public sealed class OrdersClient(HttpClient httpClient, IConfiguration configuration)
{
    public async Task<bool> HasPurchasedAsync(Guid customerId, Guid productId)
    {
        httpClient.AddInternalKey(configuration);
        var response = await httpClient.GetAsync($"internal/orders/customers/{customerId}/purchased/{productId}");
        if (!response.IsSuccessStatusCode) return false;
        var data = await response.Content.ReadFromJsonAsync<PurchaseCheck>();
        return data?.Purchased == true;
    }

    private sealed record PurchaseCheck(bool Purchased);
}

using System.Net.Http.Json;
using PetShop.ServiceDefaults;

namespace PetShop.Inventory.Api.Application;

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

using System.Net.Http.Json;
using PetShop.ServiceDefaults;

namespace PetShop.Shops.Api.Application;

public sealed class IdentityClient(HttpClient httpClient, IConfiguration configuration)
{
    public async Task GrantShopOwnerAsync(Guid userId)
    {
        httpClient.AddInternalKey(configuration);
        var response = await httpClient.PostAsync($"internal/identity/users/{userId}/roles/ShopOwner", null);
        response.EnsureSuccessStatusCode();
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

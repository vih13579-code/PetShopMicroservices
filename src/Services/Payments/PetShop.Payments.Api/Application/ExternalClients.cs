using System.Net.Http.Json;
using PetShop.ServiceDefaults;

namespace PetShop.Payments.Api.Application;

public sealed record InternalOrderSummary(Guid Id, string OrderCode, Guid CustomerId, Guid ShopId,
    decimal TotalAmount, string Status, string PaymentStatus, string PaymentMethod);

public sealed class OrdersClient(HttpClient httpClient, IConfiguration configuration)
{
    public async Task<InternalOrderSummary?> GetAsync(Guid orderId)
    {
        httpClient.AddInternalKey(configuration);
        var response = await httpClient.GetAsync($"internal/orders/{orderId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InternalOrderSummary>();
    }

    public async Task SetPaymentStatusAsync(Guid orderId, string status)
    {
        httpClient.AddInternalKey(configuration);
        var response = await httpClient.PatchAsync($"internal/orders/{orderId}/payment-status/{status}", null);
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

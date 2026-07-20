using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PetShop.Web.Services;

public sealed record ApiResult<T>(bool Success, T? Data, string? Error, int StatusCode);

public sealed class GatewayApiClient(HttpClient httpClient, IHttpContextAccessor accessor)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public void SetToken(string token, string refreshToken, string userJson)
    {
        var session = accessor.HttpContext!.Session;
        session.SetString("AccessToken", token);
        session.SetString("RefreshToken", refreshToken);
        session.SetString("CurrentUser", userJson);
    }

    public void ClearToken() => accessor.HttpContext!.Session.Clear();
    public string? CurrentUserJson => accessor.HttpContext?.Session.GetString("CurrentUser");
    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(accessor.HttpContext?.Session.GetString("AccessToken"));

    public Task<ApiResult<T>> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url, null);
    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) => SendAsync<T>(HttpMethod.Post, url, body);
    public Task<ApiResult<T>> PutAsync<T>(string url, object? body = null) => SendAsync<T>(HttpMethod.Put, url, body);
    public Task<ApiResult<T>> PatchAsync<T>(string url, object? body = null) => SendAsync<T>(HttpMethod.Patch, url, body);
    public Task<ApiResult<T>> DeleteAsync<T>(string url) => SendAsync<T>(HttpMethod.Delete, url, null);

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body)
    {
        using var request = new HttpRequestMessage(method, url);
        var token = accessor.HttpContext?.Session.GetString("AccessToken");
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body, options: JsonOptions);

        using var response = await httpClient.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            if (typeof(T) == typeof(object) || string.IsNullOrWhiteSpace(text))
                return new ApiResult<T>(true, default, null, (int)response.StatusCode);
            var data = JsonSerializer.Deserialize<T>(text, JsonOptions);
            return new ApiResult<T>(true, data, null, (int)response.StatusCode);
        }

        var error = text;
        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.TryGetProperty("message", out var msg)) error = msg.GetString() ?? text;
            else if (doc.RootElement.TryGetProperty("detail", out var detail)) error = detail.GetString() ?? text;
        }
        catch { /* giữ nội dung lỗi gốc */ }
        return new ApiResult<T>(false, default, error, (int)response.StatusCode);
    }
}

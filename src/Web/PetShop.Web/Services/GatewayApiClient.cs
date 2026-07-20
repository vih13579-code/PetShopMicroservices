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
    public void UpdateCurrentUser(object user) => accessor.HttpContext!.Session.SetString(
        "CurrentUser", JsonSerializer.Serialize(user, JsonOptions));
    public string? CurrentUserJson => accessor.HttpContext?.Session.GetString("CurrentUser");
    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(accessor.HttpContext?.Session.GetString("AccessToken"));
    public UserSession? CurrentUser
    {
        get
        {
            var json = CurrentUserJson;
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonSerializer.Deserialize<UserSession>(json, JsonOptions); }
            catch (JsonException) { return null; }
        }
    }
    public bool IsInRole(string role) => CurrentUser?.Roles.Contains(role, StringComparer.OrdinalIgnoreCase) == true;

    public Task<ApiResult<T>> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url, null);
    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) => SendAsync<T>(HttpMethod.Post, url, body);
    public Task<ApiResult<T>> PutAsync<T>(string url, object? body = null) => SendAsync<T>(HttpMethod.Put, url, body);
    public Task<ApiResult<T>> PatchAsync<T>(string url, object? body = null) => SendAsync<T>(HttpMethod.Patch, url, body);
    public Task<ApiResult<T>> DeleteAsync<T>(string url) => SendAsync<T>(HttpMethod.Delete, url, null);

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body, bool allowRefresh = true)
    {
        using var request = new HttpRequestMessage(method, url);
        var token = accessor.HttpContext?.Session.GetString("AccessToken");
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body, options: JsonOptions);

        using var response = await httpClient.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        if ((response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
             response.StatusCode == System.Net.HttpStatusCode.Forbidden) &&
            allowRefresh && await TryRefreshTokenAsync())
            return await SendAsync<T>(method, url, body, allowRefresh: false);

        if (response.IsSuccessStatusCode)
        {
            if (typeof(T) == typeof(object) || string.IsNullOrWhiteSpace(text))
                return new ApiResult<T>(true, default, null, (int)response.StatusCode);
            var data = JsonSerializer.Deserialize<T>(text, JsonOptions);
            return new ApiResult<T>(true, data, null, (int)response.StatusCode);
        }

        var error = string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.TryGetProperty("message", out var msg)) error = msg.GetString() ?? text;
            else if (doc.RootElement.TryGetProperty("detail", out var detail)) error = detail.GetString() ?? text;
            else if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                error = string.Join(" ", errors.EnumerateObject().SelectMany(x => x.Value.EnumerateArray()).Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)));
            else if (doc.RootElement.TryGetProperty("title", out var title)) error = title.GetString() ?? text;
        }
        catch { /* giữ nội dung lỗi gốc */ }
        if (string.IsNullOrWhiteSpace(error)) error = GetFallbackError((int)response.StatusCode);
        return new ApiResult<T>(false, default, error, (int)response.StatusCode);
    }

    private async Task<bool> TryRefreshTokenAsync()
    {
        var session = accessor.HttpContext?.Session;
        var refreshToken = session?.GetString("RefreshToken");
        if (session is null || string.IsNullOrWhiteSpace(refreshToken)) return false;

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh")
        {
            Content = JsonContent.Create(new { refreshToken }, options: JsonOptions)
        };
        using var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            session.Clear();
            return false;
        }

        var token = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>(JsonOptions);
        if (token is null) return false;
        session.SetString("AccessToken", token.AccessToken);
        session.SetString("RefreshToken", token.RefreshToken);
        session.SetString("CurrentUser", JsonSerializer.Serialize(token.User, JsonOptions));
        return true;
    }

    private static string GetFallbackError(int statusCode) => statusCode switch
    {
        400 => "Dữ liệu gửi lên không hợp lệ. Vui lòng kiểm tra lại thông tin.",
        401 => "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
        403 => "Tài khoản của bạn không có quyền thực hiện chức năng này.",
        404 => "Không tìm thấy dữ liệu yêu cầu.",
        409 => "Dữ liệu bị trùng hoặc đang được sử dụng.",
        502 or 503 or 504 => "Service đang tạm thời không phản hồi. Vui lòng kiểm tra các service và thử lại.",
        _ => $"Không thể xử lý yêu cầu (HTTP {statusCode})."
    };

    private sealed record RefreshTokenResponse(string AccessToken, string RefreshToken, JsonElement User);
    public sealed record UserSession(Guid Id, string FullName, string Email, IReadOnlyCollection<string> Roles);
}

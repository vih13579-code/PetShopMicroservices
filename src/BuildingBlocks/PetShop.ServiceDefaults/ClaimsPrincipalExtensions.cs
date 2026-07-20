using System.Security.Claims;

namespace PetShop.ServiceDefaults;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub");

        if (!Guid.TryParse(raw, out var userId))
        {
            throw new UnauthorizedAccessException("Token không chứa UserId hợp lệ.");
        }

        return userId;
    }

    public static string GetDisplayName(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Name)
           ?? user.FindFirstValue("name")
           ?? "Unknown";
}

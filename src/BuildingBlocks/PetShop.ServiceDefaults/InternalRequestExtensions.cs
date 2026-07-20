using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PetShop.Contracts;

namespace PetShop.ServiceDefaults;

public static class InternalRequestExtensions
{
    public static bool HasValidInternalKey(this HttpRequest request, IConfiguration configuration)
    {
        var expected = configuration["InternalApiKey"];
        if (string.IsNullOrWhiteSpace(expected)) return false;

        return request.Headers.TryGetValue(InternalHeaders.ApiKey, out var actual)
               && string.Equals(actual.ToString(), expected, StringComparison.Ordinal);
    }
}

using System.ComponentModel.DataAnnotations;

namespace PetShop.Identity.Api.Contracts;

public class RegisterRequest
{
    [Required, StringLength(150, MinimumLength = 2)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(180)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 6)] public string Password { get; set; } = string.Empty;
    [Phone, StringLength(30)] public string? Phone { get; set; }
    [StringLength(500)] public string? Address { get; set; }
}

public sealed class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public sealed class RefreshRequest
{
    [Required] public string RefreshToken { get; set; } = string.Empty;
}

public sealed class LogoutRequest
{
    [Required] public string RefreshToken { get; set; } = string.Empty;
}

public sealed class UpdateProfileRequest
{
    [Required, StringLength(150, MinimumLength = 2)] public string FullName { get; set; } = string.Empty;
    [Phone, StringLength(30)] public string? Phone { get; set; }
    [StringLength(500)] public string? Address { get; set; }
}

public sealed class CreateStaffRequest : RegisterRequest { }

public sealed class AdminUpdateAccountRequest
{
    [Required, StringLength(150, MinimumLength = 2)] public string FullName { get; set; } = string.Empty;
    [Phone, StringLength(30)] public string? Phone { get; set; }
    [StringLength(500)] public string? Address { get; set; }
}

public sealed record UserResponse(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string? Address,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyCollection<string> Roles);

public sealed record TokenResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    UserResponse User);

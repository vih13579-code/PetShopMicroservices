using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Identity.Api.Application;
using PetShop.Identity.Api.Contracts;
using PetShop.Identity.Api.Domain;
using PetShop.Identity.Api.Infrastructure;

namespace PetShop.Identity.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IdentityDbContext db, TokenService tokenService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<TokenResponse>> Register(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email))
            return Conflict(new { message = "Email đã tồn tại." });

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Phone = request.Phone?.Trim(),
            Address = request.Address?.Trim()
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.Password);

        var customerRole = await db.Roles.SingleAsync(x => x.Name == AppRoles.Customer);
        user.UserRoles.Add(new UserRole { User = user, Role = customerRole });
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Ok(await IssueTokensAsync(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.Email == email);

        if (user is null) return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
        if (!user.IsActive) return StatusCode(StatusCodes.Status403Forbidden, new { message = "Tài khoản đã bị khóa." });

        var result = new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });

        return Ok(await IssueTokensAsync(user));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> Refresh(RefreshRequest request)
    {
        var stored = await db.RefreshTokens
            .Include(x => x.User)
                .ThenInclude(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.Token == request.RefreshToken);

        if (stored is null || stored.IsRevoked || stored.ExpiresAt <= DateTime.UtcNow || !stored.User.IsActive)
            return Unauthorized(new { message = "Refresh token không hợp lệ hoặc đã hết hạn." });

        stored.IsRevoked = true;
        return Ok(await IssueTokensAsync(stored.User));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request)
    {
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(x => x.Token == request.RefreshToken);
        if (stored is not null)
        {
            stored.IsRevoked = true;
            await db.SaveChangesAsync();
        }
        return NoContent();
    }

    private async Task<TokenResponse> IssueTokensAsync(User user)
    {
        if (!db.Entry(user).Collection(x => x.UserRoles).IsLoaded)
        {
            await db.Entry(user).Collection(x => x.UserRoles).Query().Include(x => x.Role).LoadAsync();
        }

        var roles = user.UserRoles.Select(x => x.Role.Name).Distinct().OrderBy(x => x).ToArray();
        var response = tokenService.Create(user, roles);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = response.RefreshToken,
            ExpiresAt = response.RefreshTokenExpiresAt
        });
        await db.SaveChangesAsync();
        return response;
    }
}

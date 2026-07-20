using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Identity.Api.Contracts;
using PetShop.Identity.Api.Infrastructure;
using PetShop.ServiceDefaults;

namespace PetShop.Identity.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController(IdentityDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserResponse>> Get()
    {
        var userId = User.GetRequiredUserId();
        var user = await db.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .SingleAsync(x => x.Id == userId);
        return Ok(Map(user));
    }

    [HttpPut]
    public async Task<ActionResult<UserResponse>> Update(UpdateProfileRequest request)
    {
        var userId = User.GetRequiredUserId();
        var user = await db.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .SingleAsync(x => x.Id == userId);
        user.FullName = request.FullName.Trim();
        user.Phone = request.Phone?.Trim();
        user.Address = request.Address?.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(Map(user));
    }

    private static UserResponse Map(PetShop.Identity.Api.Domain.User user) => new(
        user.Id, user.FullName, user.Email, user.Phone, user.Address,
        user.IsActive, user.CreatedAt, user.UserRoles.Select(x => x.Role.Name).ToArray());
}

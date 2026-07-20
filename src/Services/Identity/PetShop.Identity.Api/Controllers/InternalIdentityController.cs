using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Identity.Api.Domain;
using PetShop.Identity.Api.Infrastructure;
using PetShop.ServiceDefaults;

namespace PetShop.Identity.Api.Controllers;

[ApiController]
[Route("internal/identity")]
public sealed class InternalIdentityController(IdentityDbContext db, IConfiguration configuration) : ControllerBase
{
    [HttpPost("users/{userId:guid}/roles/{roleName}")]
    public async Task<IActionResult> GrantRole(Guid userId, string roleName)
    {
        if (!Request.HasValidInternalKey(configuration)) return Unauthorized();
        if (!AppRoles.All.Contains(roleName, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Role không hợp lệ." });

        var user = await db.Users.Include(x => x.UserRoles).SingleOrDefaultAsync(x => x.Id == userId);
        var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == roleName);
        if (user is null || role is null) return NotFound();
        if (user.UserRoles.All(x => x.RoleId != role.Id))
        {
            user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            await db.SaveChangesAsync();
        }
        return NoContent();
    }

    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUser(Guid userId)
    {
        if (!Request.HasValidInternalKey(configuration)) return Unauthorized();
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId);
        return user is null ? NotFound() : Ok(new { user.Id, user.FullName, user.Email, user.Phone, user.IsActive });
    }
}

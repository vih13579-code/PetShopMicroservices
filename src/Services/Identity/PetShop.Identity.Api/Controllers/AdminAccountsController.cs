using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Contracts;
using PetShop.Identity.Api.Contracts;
using PetShop.Identity.Api.Domain;
using PetShop.Identity.Api.Infrastructure;
using PetShop.ServiceDefaults;

namespace PetShop.Identity.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/admin/accounts")]
public sealed class AdminAccountsController(IdentityDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserResponse>>> GetAll(
        string? keyword, string? role, bool? isActive, int page = 1, int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x => x.FullName.Contains(value) || x.Email.Contains(value));
        }
        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(x => x.UserRoles.Any(ur => ur.Role.Name == role));
        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        var total = await query.CountAsync();
        var users = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new PagedResult<UserResponse>(users.Select(Map).ToArray(), page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id)
    {
        var user = await db.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        return user is null ? NotFound() : Ok(Map(user));
    }

    [HttpPost("staff")]
    public async Task<ActionResult<UserResponse>> CreateStaff(CreateStaffRequest request)
    {
        var roles = NormalizeRoles(request.Roles);
        if (roles.Count == 0 || roles.Contains(AppRoles.Admin) || roles.Contains(AppRoles.ShopOwner))
            return BadRequest(new { message = "Tài khoản staff không được gán role Admin hoặc ShopOwner." });
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email)) return Conflict(new { message = "Email đã tồn tại." });

        var staff = new User
        {
            FullName = request.FullName.Trim(), Email = email,
            Phone = request.Phone?.Trim(), Address = request.Address?.Trim()
        };
        staff.PasswordHash = new PasswordHasher<User>().HashPassword(staff, request.Password);
        var roleEntities = await db.Roles.Where(x => roles.Contains(x.Name)).ToListAsync();
        if (roleEntities.Count != roles.Count)
            return BadRequest(new { message = "Role không hợp lệ." });
        foreach (var role in roleEntities)
            staff.UserRoles.Add(new UserRole { User = staff, Role = role });
        db.Users.Add(staff);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = staff.Id }, Map(staff));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserResponse>> Update(Guid id, AdminUpdateAccountRequest request)
    {
        var user = await db.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == id);
        if (user is null) return NotFound();
        if (user.UserRoles.Any(x => x.Role.Name == AppRoles.Admin))
            return Forbid();
        user.FullName = request.FullName.Trim();
        user.Phone = request.Phone?.Trim();
        user.Address = request.Address?.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(Map(user));
    }

    [HttpPatch("{id:guid}/roles")]
    public async Task<ActionResult<UserResponse>> UpdateRoles(Guid id, UpdateRolesRequest request)
    {
        var roles = NormalizeRoles(request.Roles);
        if (roles.Count == 0 || roles.Contains(AppRoles.Admin) || roles.Contains(AppRoles.ShopOwner))
            return BadRequest(new { message = "Không thể gán role Admin hoặc ShopOwner qua chức năng này." });

        var user = await db.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == id);
        if (user is null) return NotFound();
        if (id == User.GetRequiredUserId() || user.UserRoles.Any(x => x.Role.Name == AppRoles.Admin))
            return Forbid();

        var roleEntities = await db.Roles.Where(x => roles.Contains(x.Name)).ToListAsync();
        if (roleEntities.Count != roles.Count) return BadRequest(new { message = "Role không hợp lệ." });
        user.UserRoles.Clear();
        foreach (var role in roleEntities) user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, Role = role });
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(Map(user));
    }

    [HttpPatch("{id:guid}/lock")]
    public async Task<IActionResult> Lock(Guid id) => await SetActive(id, false);

    [HttpPatch("{id:guid}/unlock")]
    public async Task<IActionResult> Unlock(Guid id) => await SetActive(id, true);

    private async Task<IActionResult> SetActive(Guid id, bool active)
    {
        if (!active && id == User.GetRequiredUserId())
            return BadRequest(new { message = "Không thể tự khóa tài khoản của chính mình." });

        var user = await db.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role).SingleOrDefaultAsync(x => x.Id == id);
        if (user is null) return NotFound();
        if (user.UserRoles.Any(x => x.Role.Name == AppRoles.Admin)) return Forbid();
        user.IsActive = active;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static HashSet<string> NormalizeRoles(IEnumerable<string>? roles) =>
        roles?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

    private static UserResponse Map(User user) => new(
        user.Id, user.FullName, user.Email, user.Phone, user.Address,
        user.IsActive, user.CreatedAt, user.UserRoles.Select(x => x.Role.Name).ToArray());
}

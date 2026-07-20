using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Notifications.Api.Contracts;
using PetShop.Notifications.Api.Domain;
using PetShop.Notifications.Api.Infrastructure;
using PetShop.ServiceDefaults;

namespace PetShop.Notifications.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(NotificationsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<NotificationResponse>>> GetAll(bool? isRead, int take = 50)
    {
        var userId = User.GetRequiredUserId();
        take = Math.Clamp(take, 1, 200);
        var query = db.Notifications.AsNoTracking().Where(x => x.UserId == userId);
        if (isRead.HasValue) query = query.Where(x => x.IsRead == isRead.Value);
        var items = await query.OrderByDescending(x => x.CreatedAt).Take(take).ToListAsync();
        return Ok(items.Select(Map));
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var userId = User.GetRequiredUserId();
        var item = await db.Notifications.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (item is null) return NotFound();
        item.IsRead = true; item.ReadAt = DateTime.UtcNow;
        await db.SaveChangesAsync(); return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = User.GetRequiredUserId();
        var items = await db.Notifications.Where(x => x.UserId == userId && !x.IsRead).ToListAsync();
        foreach (var item in items) { item.IsRead = true; item.ReadAt = DateTime.UtcNow; }
        await db.SaveChangesAsync(); return NoContent();
    }

    internal static NotificationResponse Map(Notification x) => new(x.Id, x.UserId, x.Title,
        x.Message, x.Type, x.IsRead, x.CreatedAt, x.ReadAt);
}

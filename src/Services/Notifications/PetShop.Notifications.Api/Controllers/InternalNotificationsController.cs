using Microsoft.AspNetCore.Mvc;
using PetShop.Notifications.Api.Contracts;
using PetShop.Notifications.Api.Domain;
using PetShop.Notifications.Api.Infrastructure;
using PetShop.ServiceDefaults;

namespace PetShop.Notifications.Api.Controllers;

[ApiController]
[Route("internal/notifications")]
public sealed class InternalNotificationsController(NotificationsDbContext db, IConfiguration configuration) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateNotificationRequest request)
    {
        if (!Request.HasValidInternalKey(configuration)) return Unauthorized();
        var entity = new Notification
        {
            UserId = request.UserId,
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            Type = request.Type.Trim()
        };
        db.Notifications.Add(entity);
        await db.SaveChangesAsync();
        return Ok(NotificationsController.Map(entity));
    }
}

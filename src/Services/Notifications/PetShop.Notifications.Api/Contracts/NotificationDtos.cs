using System.ComponentModel.DataAnnotations;

namespace PetShop.Notifications.Api.Contracts;

public sealed class CreateNotificationRequest
{
    public Guid UserId { get; set; }
    [Required, StringLength(250)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(2000)] public string Message { get; set; } = string.Empty;
    [Required, StringLength(80)] public string Type { get; set; } = "Information";
}

public sealed record NotificationResponse(Guid Id, Guid UserId, string Title, string Message,
    string Type, bool IsRead, DateTime CreatedAt, DateTime? ReadAt);

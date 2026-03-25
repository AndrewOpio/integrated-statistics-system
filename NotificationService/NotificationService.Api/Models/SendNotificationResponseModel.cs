namespace NotificationService.Api.Models;

/// <summary>
/// Returned after a notification is successfully created.
/// Returns 202 Accepted
/// </summary>
public record SendNotificationResponseDto(Guid NotificationId);
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NotificationService.Client.Dtos;

namespace NotificationService.Client;


/// <summary>
/// Contract for sending notifications via the Notification Service HTTP API.
/// </summary>
public interface INotificationServiceClient
{
    /// <summary>
    /// Sends a notification request to the Notification Service.
    /// Returns the notification ID if accepted (HTTP 202).
    /// Returns null if the request failed.
    /// </summary>
    Task<Guid?> SendAsync(
        SendNotificationRequestDto request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current status of a notification by ID.
    /// Returns null if the notification was not found.
    /// </summary>
    Task<NotificationStatusDto?> GetStatusAsync(
        Guid notificationId,
        CancellationToken ct = default);
}
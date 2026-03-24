using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using NotificationService.Client.Dtos;

namespace NotificationService.Client;

/// <summary>
/// HTTP implementation of INotificationServiceClient.
/// Communicates with the Notification Service REST API.
/// </summary>
public class NotificationServiceClient(
    HttpClient httpClient,
    ILogger<NotificationServiceClient> logger) : INotificationServiceClient
{
    public async Task<Guid?> SendAsync(
        SendNotificationRequestDto request,
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "api/notifications", request, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Notification Service returned {StatusCode} for SendAsync. Title: {Title}",
                    response.StatusCode, request.Title);
                return null;
            }

            var result = await response.Content
                .ReadFromJsonAsync<NotificationResponseDto>(cancellationToken: ct);

            logger.LogInformation(
                "Notification accepted. Id: {NotificationId}. Title: {Title}",
                result?.NotificationId, request.Title);

            return result?.NotificationId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to send notification request to Notification Service. Title: {Title}",
                request.Title);
            return null;
        }
    }

    public async Task<NotificationStatusDto?> GetStatusAsync(
        Guid notificationId,
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"api/notifications/{notificationId}", ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Notification Service returned {StatusCode} for GetStatusAsync. Id: {Id}",
                    response.StatusCode, notificationId);
                return null;
            }

            return await response.Content
                .ReadFromJsonAsync<NotificationStatusDto>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to get notification status from Notification Service. Id: {Id}",
                notificationId);
            return null;
        }
    }
}


using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Client.Dtos;
using NotificationService.Client.Messaging;

namespace NotificationService.Client;
/// <summary>
/// RabbitMQ implementation of INotificationEventPublisher.
/// Uses MassTransit to publish SendNotificationMessageContract messages.
/// The Notification Service consumer picks them up automatically.
/// </summary>
public class NotificationEventPublisher(
    IPublishEndpoint publishEndpoint,
    ILogger<NotificationEventPublisher> logger) : INotificationEventPublisher
{
    public async Task PublishAsync(
        SendNotificationRequestDto request,
        CancellationToken ct = default)
    {
        try
        {
            var message = new SendNotificationMessageContract
            {
                Title = request.Title,
                Body = request.Body,
                NotificationTypeId = request.NotificationTypeId,
                Recipients = request.Recipients
                    .Select(r => new RecipientMessageContract
                    {
                        UserId = r.UserId,
                        Email = r.Email,
                        DisplayName = r.DisplayName
                    }).ToList(),
                TemplateKey = request.TemplateKey,
                TemplateData = request.TemplateData,
                ScheduledAt = request.ScheduledAt
            };

            await publishEndpoint.Publish(message, ct);

            logger.LogInformation(
                "Notification event published to RabbitMQ. Title: {Title}. Recipients: {Count}",
                request.Title, request.Recipients.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to publish notification event to RabbitMQ. Title: {Title}",
                request.Title);
        }
    }
}

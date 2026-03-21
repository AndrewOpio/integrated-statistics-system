using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;
using NotificationService.Contracts.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Infrastructure.Messaging.Consumers;


/// <summary>
/// Listens on the RabbitMQ "notification-service-queue" exchange.
/// Other microservices publish a SendNotificationMessageContract to trigger a notification without calling this service's HTTP API directly.
///
/// Invalid messages are moved to the dead-letter queue with a logged reason.
/// </summary>
public class SendNotificationConsumer(
    INotificationAppService notificationService,
    ILogger<SendNotificationConsumer> logger) : IConsumer<SendNotificationMessageContract>
{
    public async Task Consume(ConsumeContext<SendNotificationMessageContract> ctx)
    {
        var msg = ctx.Message;
        logger.LogInformation(
            "Received SendNotificationMessageContract from RabbitMQ. Title: {Title}", msg.Title);

        try
        {
            var request = new SendNotificationRequestDto
            {
                Title = msg.Title,
                Body = msg.Body,
                NotificationTypeId = msg.NotificationTypeId,
                Recipients = msg.Recipients
                    .Select(r => new RecipientRequestDto
                    {
                        UserId = r.UserId,
                        Email = r.Email,
                        DisplayName = r.DisplayName
                    }).ToList(),
                TemplateKey = msg.TemplateKey,
                TemplateData = msg.TemplateData,
                ScheduledAt = msg.ScheduledAt
            };

            await notificationService.SendAsync(request, ctx.CancellationToken);
        }
        catch (FluentValidation.ValidationException ex)
        {
            // Validation failed — is a poison message.
            var errors = string.Join(", ", ex.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning(
                "Poison message rejected — validation failed. Errors: {Errors}. Title: {Title}",
                errors, msg.Title);
        }
    }
}

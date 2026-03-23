using FluentValidation;
using Microsoft.Extensions.Logging;
using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Application.Services;

public class NotificationAppService(
    INotificationRepository repo,
    INotificationDispatcher dispatcher,
    IValidator<SendNotificationRequestDto> validator,
    IUnitOfWork uow,
    ILogger<NotificationAppService> logger) : INotificationAppService
{
    public async Task<Guid> SendAsync(SendNotificationRequestDto request, CancellationToken ct = default)
    {
        // Validate first — works identically whether called from HTTP or RabbitMQ consumer.
        // A ValidationException here means the message is rejected cleanly at the boundary before any domain objects or database calls are involved.
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var recipients = request.Recipients
            .Select(r => NotificationRecipient.Create(r.UserId, r.Email, r.DisplayName))
            .ToList();

        var notification = Notification.Create(
            request.Title,
            request.Body,
            request.NotificationTypeId,
            request.Channel,
            recipients,
            request.TemplateKey,
            request.TemplateData,
            request.ScheduledAt);

        await repo.AddAsync(notification, ct);
        await uow.CommitAsync(ct);

        logger.LogInformation(
            "Notification {Id} created for {RecipientCount} recipient(s). Channel: {Channel}. Scheduled: {Scheduled}",
            notification.Id,
            recipients.Count,
            notification.Channel,
            notification.ScheduledAt.HasValue
                ? notification.ScheduledAt.Value.ToString("u")
                : "immediate");

        await dispatcher.DispatchAsync(notification, ct);

        return notification.Id;
    }

    public async Task<NotificationDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // Repository contract guarantees Type and Recipients are eagerly loaded.
        var n = await repo.GetByIdAsync(id, ct);
        if (n is null) return null;

        return new NotificationDto(
            n.Id,
            n.Title,
            n.Body,
            n.Type.Name,
            n.Channel.ToString(),
            n.Status.ToString(),
            n.CreatedAt,
            n.SentAt,
            n.ScheduledAt,
            n.RetryCount,
            n.FailureReason,
            n.Recipients
                .Select(r => new RecipientDto(r.UserId, r.Email, r.DisplayName))
                .ToList());
    }
}

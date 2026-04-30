using NotificationService.Domain.Enums;
using NotificationService.Domain.Events;
using NotificationService.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Domain.Entities;

/// <summary>
/// Aggregate root for the notification domain.
/// All state changes go through methods on this class — nothing sets properties directly from outside.
/// </summary>

public class Notification
{
    private readonly List<NotificationRecipient> _recipients = [];
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; private set; }
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public int NotificationTypeId { get; private set; }
    public NotificationType Type { get; private set; } = default!;  
    public NotificationChannel Channel { get; private set; }
    public NotificationStatus Status { get; private set; }

    /// <summary>
    /// Optional key referencing a template (e.g. "approval-request").
    /// When set, Body is treated as the rendered output of that template.
    /// TO DO: In the future consider moving TemplateKey and TemplateData to a separate entity if we want to support multiple templates per notification or more complex template structures.
    /// </summary>
    public string? TemplateKey { get; private set; }

    /// <summary>
    /// Key/value pairs used to render the template (e.g. { "userName": "John" }).
    /// TO DO: In the future consider moving TemplateKey and TemplateData to a separate entity if we want to support multiple templates per notification or more complex template structures.
    /// </summary>
    public Dictionary<string, string> TemplateData { get; private set; } = [];

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }

    /// <summary>Null means send immediately. Future date means schedule.</summary>
    public DateTimeOffset? ScheduledAt { get; private set; }

    public int RetryCount { get; private set; }
    public string? FailureReason { get; private set; }

    public IReadOnlyCollection<NotificationRecipient> Recipients => _recipients.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Notification() { }  // Required by EF Core

 
    public static Notification Create(
        string title,
        string body,
        int notificationTypeId,
        NotificationChannel channel,
        IEnumerable<NotificationRecipient> recipients,
        string? templateKey = null,
        Dictionary<string, string>? templateData = null,
        DateTimeOffset? scheduledAt = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainValidationException(nameof(title), "Notification title is required.");
        if (string.IsNullOrWhiteSpace(body))
            throw new DomainValidationException(nameof(body), "Notification body is required.");

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Title = title,
            Body = body,
            NotificationTypeId = notificationTypeId,
            Channel = channel,
            Status = NotificationStatus.Pending,
            TemplateKey = templateKey,
            TemplateData = templateData ?? [],
            ScheduledAt = scheduledAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var recipient in recipients)
            notification._recipients.Add(recipient);

        notification._domainEvents.Add(
            new NotificationCreatedEvent(notification.Id, channel));

        return notification;
    }

    /// <summary>
    /// Dispatcher has accepted this notification and queued a job.
    /// </summary>
    public void MarkQueued()
    {
        EnsureStatus(NotificationStatus.Pending, nameof(MarkQueued));
        Status = NotificationStatus.Queued;
    }

    /// <summary>Job has successfully delivered to all recipients.</summary>
    public void MarkSent()
    {
        if (Status is not (NotificationStatus.Queued or NotificationStatus.Failed))
            throw new DomainValidationException(
                nameof(Status),
                $"MarkSent requires status 'Queued' or 'Failed' but current status is '{Status}'.");

        Status = NotificationStatus.Sent;
        SentAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Delivery attempt failed. Hangfire will retry the job.
    /// FailureReason is overwritten each attempt so you see the latest error.
    /// </summary>
    public void MarkFailed(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainValidationException(
                nameof(reason),
                "A failure reason must be provided when marking a notification as failed.");

        if (Status is not (NotificationStatus.Queued or NotificationStatus.Failed))
            throw new DomainValidationException(
                nameof(Status),
                $"MarkFailed requires status 'Queued' or 'Failed' but current status is '{Status}'.");

        Status = NotificationStatus.Failed;
        FailureReason = reason;
        RetryCount++;
    }

    /// <summary>Notification was deliberately stopped before sending.</summary>
    public void MarkCancelled()
    {
        if (Status is NotificationStatus.Sent or NotificationStatus.Failed)
            throw new DomainValidationException(
                nameof(Status),
                $"Cannot cancel a notification that is already {Status}.");

        Status = NotificationStatus.Cancelled;
    }

    /// <summary>
    /// Called by the infrastructure layer after domain events have been published to the message broker. Prevents double-publishing.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    private void EnsureStatus(NotificationStatus expected, string operation)
    {
        if (Status != expected)
            throw new DomainValidationException(
                nameof(Status),
                $"{operation} requires status '{expected}' but current status is '{Status}'.");
    }
}


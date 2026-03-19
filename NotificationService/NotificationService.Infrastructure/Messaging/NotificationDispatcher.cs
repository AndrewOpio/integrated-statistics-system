using Hangfire;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Jobs;

namespace NotificationService.Infrastructure.Messaging;

/// <summary>
/// Routes a notification to the correct Hangfire job.
/// Immediate notifications are enqueued straight away.
/// Scheduled notifications are given a delay — Hangfire fires them at the right time.
/// </summary>
public class NotificationDispatcher(
    IBackgroundJobClient jobClient,
    ILogger<NotificationDispatcher> logger) : INotificationDispatcher
{
    public Task DispatchAsync(Notification notification, CancellationToken ct = default)
    {
        var jobId = notification.Channel switch
        {
            NotificationChannel.Email => ScheduleEmailJob(notification),
            _ => throw new NotSupportedException(
                $"Channel '{notification.Channel}' is not supported by this dispatcher.")
        };

        logger.LogInformation(
            "Notification {Id} dispatched as Hangfire job {JobId}. Channel: {Channel}",
            notification.Id, jobId, notification.Channel);

        return Task.CompletedTask;
    }

    private string ScheduleEmailJob(Notification notification)
    {
        if (notification.ScheduledAt.HasValue)
        {
            var delay = notification.ScheduledAt.Value - DateTimeOffset.UtcNow;
            return jobClient.Schedule<EmailNotificationJob>(
                j => j.ProcessAsync(notification.Id, CancellationToken.None), delay);
        }

        return jobClient.Enqueue<EmailNotificationJob>(
            j => j.ProcessAsync(notification.Id, CancellationToken.None));
    }
}
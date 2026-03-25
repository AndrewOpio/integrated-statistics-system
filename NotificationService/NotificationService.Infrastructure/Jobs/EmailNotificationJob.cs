using Microsoft.Extensions.Logging;
using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Infrastructure.Jobs;


/// <summary>
/// Hangfire background job that delivers an email notification. Hangfire retries this job automatically on failure.
/// Each retry calls MarkFailed() on the notification so RetryCount and FailureReason are always up to date in the database.
/// </summary>
public class EmailNotificationJob(
    INotificationRepository repo,
    IEmailSender emailSender,
    IUnitOfWork uow,
    ILogger<EmailNotificationJob> logger)
{
    public async Task ProcessAsync(Guid notificationId, CancellationToken ct)
    {
        var notification = await repo.GetByIdAsync(notificationId, ct);
        if (notification is null)
        {
            logger.LogWarning(
                "EmailNotificationJob: Notification {Id} not found — skipping", notificationId);
            return;
        }

        // Idempotency check - Was it already sent? ()
        if (notification.Status == NotificationStatus.Sent)
        {
            logger.LogWarning(
                "Notification {Id} already sent — skipping duplicate job", notificationId);
            return;
        }

        try
        {
            if (notification.Status == NotificationStatus.Pending)
            {
                notification.MarkQueued();
                await repo.UpdateAsync(notification, ct);
                await uow.CommitAsync(ct);
            }

            foreach (var recipient in notification.Recipients)
            {
                var msg = new EmailMessageDto(
                    recipient.Email,
                    recipient.DisplayName,
                    notification.Title,
                    notification.Body);

                await emailSender.SendAsync(msg, ct);

                logger.LogInformation(
                    "Email delivered to {Email} for notification {Id}",
                    recipient.Email, notificationId);
            }

            notification.MarkSent();
        }
        catch (Exception ex)
        {
            // MarkFailed records the reason and increment RetryCount.
            // Re-throwing lets Hangfire know the job failed and should be retried.
            notification.MarkFailed(ex.Message);

            logger.LogError(ex,
                "EmailNotificationJob failed for notification {Id}. RetryCount: {RetryCount}",
                notificationId, notification.RetryCount);

            throw;
        }
        finally
        {
            await repo.UpdateAsync(notification, ct);
            await uow.CommitAsync(ct);
        }
    }
}
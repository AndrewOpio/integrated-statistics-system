using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Application.DTOs
{
    /// <summary>
    /// Read-only projection of a Notification returned to callers.
    /// Exposes only what the outside world needs to see — internal domain state like DomainEvents is never exposed.
    /// </summary>
    
    public record NotificationDto(
    Guid Id,
    string Title,
    string Body,
    string Type,
    string Channel,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt,
    DateTimeOffset? ScheduledAt,
    int RetryCount,
    string? FailureReason,
    List<RecipientDto> Recipients);
}

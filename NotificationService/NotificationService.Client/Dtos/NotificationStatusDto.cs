using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Client.Dtos
{
    public class NotificationStatusDto
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = default!;
        public string Body { get; init; } = default!;
        public string Type { get; init; } = default!;
        public string Channel { get; init; } = default!;

        public string Status { get; init; } = default!;

        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? SentAt { get; init; }
        public DateTimeOffset? ScheduledAt { get; init; }
        public int RetryCount { get; init; }
        public string? FailureReason { get; init; }
        public List<RecipientDto> Recipients { get; init; } = [];
    }
}

using NotificationService.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NotificationService.Application.DTOs
{
    /// <summary>
    /// Incoming request to create and send a notification.
    /// Used by both the API controller (HTTP) and the RabbitMQ consumer.
    /// </summary>

    public class SendNotificationRequestDto
    {
        public string Title { get; init; } = default!;
        public string Body { get; init; } = default!;
        public int NotificationTypeId { get; init; }

        public NotificationChannel Channel { get; init; } = NotificationChannel.Email; //Defaults to Email — the only supported channel at moment

        public List<RecipientRequestDto> Recipients { get; init; } = [];
        public string? TemplateKey { get; init; }
        public Dictionary<string, string>? TemplateData { get; init; }
        //public DateTimeOffset? ScheduledAt { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? ScheduledAt { get; init; } = null;
    }
}

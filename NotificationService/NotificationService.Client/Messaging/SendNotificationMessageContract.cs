using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Client.Messaging
{
    public class SendNotificationMessageContract
    {
        public SendNotificationMessageContract() { }
        public string Title { get; init; } = default!;
        public string Body { get; init; } = default!;
        public int NotificationTypeId { get; init; }
        public List<RecipientMessageContract> Recipients { get; init; } = [];
        public string? TemplateKey { get; init; }
        public Dictionary<string, string>? TemplateData { get; init; }
        public DateTimeOffset? ScheduledAt { get; init; }
    }
}

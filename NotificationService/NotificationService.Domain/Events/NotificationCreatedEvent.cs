using NotificationService.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Domain.Events
{
    /// <summary>
    /// Raised when a notification is first created.
    /// Published to RabbitMQ via MassTransit so other services can react.
    /// </summary>
    public record NotificationCreatedEvent(
        Guid NotificationId,
        NotificationChannel Channel) : IDomainEvent
    {
        public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
    }
}

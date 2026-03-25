using NotificationService.Client.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Client
{
    public interface INotificationEventPublisher
    {
        /// <summary>
        /// Contract for publishing notification events via RabbitMQ.
        /// Use this when you want fire and forget — your service does not need
        /// a response and should not wait for the notification to be processed.
        /// </summary>
        Task PublishAsync(
                SendNotificationRequestDto request,
                CancellationToken ct = default);
    }
}

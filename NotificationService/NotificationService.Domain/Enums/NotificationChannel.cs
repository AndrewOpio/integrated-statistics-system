using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Domain.Enums
{
    /// <summary>
    /// Represents the delivery channel for a notification.
    /// Next available - reserved for future channels e.g. SMS, push)
    /// </summary>

    public enum NotificationChannel
    {
        Email = 1   // Delivered via MS Exchange Online (Microsoft Graph)
    }
}

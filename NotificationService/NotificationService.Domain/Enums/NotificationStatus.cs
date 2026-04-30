using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Domain.Enums
{
    public enum NotificationStatus
    {
        Pending = 0,  // Created, not yet picked up by dispatcher
        Queued = 1,  // Handed to Hangfire, waiting to be processed
        Sent = 2,  // Successfully delivered
        Failed = 3,  // Delivery failed after all retries exhausted
        Cancelled = 4   // Deliberately cancelled before sending
    }
}

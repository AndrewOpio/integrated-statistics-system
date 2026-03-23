using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Client.Messaging
{
    public class RecipientMessageContract
    {
        public RecipientMessageContract() { }
        public string UserId { get; init; } = default!;
        public string Email { get; init; } = default!;
        public string DisplayName { get; init; } = default!;
    }
}

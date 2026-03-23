using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Application.DTOs
{
    /// <summary>
    /// Represents a single recipient inside a SendNotificationRequestDto.
    /// </summary>
    
    public class RecipientRequestDto
    {
        public string UserId { get; init; } = default!;
        public string Email { get; init; } = default!;
        public string DisplayName { get; init; } = default!;
    }
}

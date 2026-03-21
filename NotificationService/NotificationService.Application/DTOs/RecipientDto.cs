using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Application.DTOs
{
    /// <summary>
    /// Read-only projection of a NotificationRecipient returned to callers.
    /// </summary>
    public record RecipientDto(
    string UserId,
    string Email,
    string DisplayName);
}

using NotificationService.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Application.Interfaces
{
    /// <summary>
    /// Contract for the email delivery mechanism.
    /// Implementation lives in Infrastructure (Microsoft Graph / Exchange Online).
    /// </summary>
    
    public interface IEmailSender
    {
        Task SendAsync(EmailMessageDto message, CancellationToken ct = default);
    }
}

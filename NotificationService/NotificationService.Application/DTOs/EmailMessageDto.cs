using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Application.DTOs;

/// <summary>
/// Represents a fully composed email ready to be handed to the IEmailSender implementation.
/// </summary>

public record EmailMessageDto(
    string ToEmail,
    string ToName,
    string Subject,
    string HtmlBody);

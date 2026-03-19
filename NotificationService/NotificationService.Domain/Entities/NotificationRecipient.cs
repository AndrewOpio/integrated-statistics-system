using NotificationService.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Domain.Entities;

/// <summary>
/// Represents a single person who should receive a notification.
/// A notification can have multiple recipients (e.g. notifying a whole team).
/// </summary>

public class NotificationRecipient
{
    public Guid Id { get; private set; }
    public Guid NotificationId { get; private set; }

    public string UserId { get; private set; } = default!;

    public string Email { get; private set; } = default!;

    public string DisplayName { get; private set; } = default!;

    private NotificationRecipient() { } 

    public static NotificationRecipient Create(string userId, string email, string displayName)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainValidationException(nameof(userId), "Recipient user ID is required.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainValidationException(nameof(email), "Recipient email address is required.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainValidationException(nameof(displayName), "Recipient display name is required.");

        return new NotificationRecipient
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = email,
            DisplayName = displayName
        };
    }
}

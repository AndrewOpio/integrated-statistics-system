using NotificationService.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Domain.Entities;

/// <summary>
/// Lookup entity for notification classification.
/// Seeded values:
///   1 = Transactional  (triggered by a user action e.g. approval request)
///   2 = System         (internal alerts e.g. job failures, health warnings)
/// </summary>
public class NotificationType
{
    public int Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    private NotificationType() { }  

    public static NotificationType Create(int id, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException(nameof(name), "Notification type name is required.");

        return new NotificationType
        {
            Id = id,
            Name = name,
            Description = description
        };
    }
}

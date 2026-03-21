using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Domain.Events;

/// <summary>
/// Marker/Tagger interface for all domain events in this service to give a typed list on the aggregate root instead of List of objects
/// </summary>

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}

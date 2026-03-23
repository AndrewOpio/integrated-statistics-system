using NotificationService.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Application.Interfaces;

/// <summary>
/// Primary application service contract.
/// The API controller and the RabbitMQ consumer both call this — one source of truth for notification logic.
/// </summary>
public interface INotificationAppService
{
    Task<Guid> SendAsync(SendNotificationRequestDto request, CancellationToken ct = default);
    Task<NotificationDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

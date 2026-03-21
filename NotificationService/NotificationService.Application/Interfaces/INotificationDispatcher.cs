using NotificationService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Application.Interfaces;

/// <summary>
/// Decides HOW and WHEN a notification is dispatched.
/// Implementation in Infrastructure uses Hangfire for immediate and scheduled jobs.
/// </summary>
public interface INotificationDispatcher
{
    Task DispatchAsync(Notification notification, CancellationToken ct = default);
}


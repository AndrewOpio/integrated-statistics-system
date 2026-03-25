using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Infrastructure.Persistence;

public class NotificationRepository(NotificationDbContext db) : INotificationRepository
{
    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Notifications
            .Include(n => n.Type)
            .Include(n => n.Recipients)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<IEnumerable<Notification>> GetPendingAsync(
        NotificationChannel channel,
        CancellationToken ct = default)
        => await db.Notifications
            .Include(n => n.Type)
            .Include(n => n.Recipients)
            .Where(n => n.Status == NotificationStatus.Pending && n.Channel == channel)
            .ToListAsync(ct);

    public async Task AddAsync(Notification notification, CancellationToken ct = default)
        => await db.Notifications.AddAsync(notification, ct);

    public Task UpdateAsync(Notification notification, CancellationToken ct = default)
    {
        db.Notifications.Update(notification);
        return Task.CompletedTask;
    }
}

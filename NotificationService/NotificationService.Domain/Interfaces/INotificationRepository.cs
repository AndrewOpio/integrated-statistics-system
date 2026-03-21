using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Domain.Interfaces
{
    /// <summary>
    /// Repository contract for the Notification aggregate.
    /// Defined in Domain so the Application layer can depend on it without knowing anything about EF Core or SQL Server.
    /// The actual implementation lives in Infrastructure.
    ///
    /// Note: There is no SaveChangesAsync here — commit responsibility belongs to IUnitOfWork in the Application layer.
    /// </summary>
    
    public interface INotificationRepository
    {
        Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Returns all pending notifications for a given channel.
        /// Used by background jobs that need to pick up unprocessed work.
        /// </summary>
        Task<IEnumerable<Notification>> GetPendingAsync(
            NotificationChannel channel,
            CancellationToken ct = default);

        Task AddAsync(Notification notification, CancellationToken ct = default);
        Task UpdateAsync(Notification notification, CancellationToken ct = default);
    }
}

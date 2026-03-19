using NotificationService.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Infrastructure.Persistence;

/// <summary>
/// Wraps NotificationDbContext.SaveChangesAsync().
/// The Application layer commits through this — never through the DbContext directly.
/// </summary>
public class UnitOfWork(NotificationDbContext db) : IUnitOfWork
{
    public Task<int> CommitAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}

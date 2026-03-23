using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Application.Interfaces;

/// <summary>
/// Abstracts the persistence commit step.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Commits all tracked changes. Returns the number of affected rows.</summary>
    Task<int> CommitAsync(CancellationToken ct = default);
}

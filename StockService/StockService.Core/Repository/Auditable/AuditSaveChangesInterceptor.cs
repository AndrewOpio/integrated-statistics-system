using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using StockService.Data.AuditColumns;

namespace StockService.Core.Repository.Auditable;

public class AuditSaveChangesInterceptor(IHttpContextAccessor _httpContextAccessor) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAuditInformation(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditInformation(DbContext context)
    {
        if (context == null) return;

        string currentUsername = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "system";
        if (string.IsNullOrEmpty(currentUsername))
        {
            currentUsername = "system";
        }

        DateTime currentTime = DateTime.UtcNow;

        var entries = context.ChangeTracker
            .Entries()
            .Where(e => e.Entity is IAuditableEntity &&
                   (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            if (entityEntry.Entity is IAuditableEntity auditableEntity)
            {
                if (entityEntry.State == EntityState.Added)
                {
                    // Always set the creation and modification info for new entities
                    auditableEntity.CreatedDate = currentTime;
                    auditableEntity.ModifiedDate = currentTime;
                    auditableEntity.CreatedBy = currentUsername;
                    auditableEntity.ModifiedBy = currentUsername;
                }
                else // Modified
                {
                    var entry = context.Entry(auditableEntity);

                    // Ensure original creation data doesn't change
                    entry.Property(e => e.CreatedDate).IsModified = false;
                    entry.Property(e => e.CreatedBy).IsModified = false;

                    // Update modification info
                    auditableEntity.ModifiedDate = currentTime;
                    auditableEntity.ModifiedBy = currentUsername;
                }
            }
        }
    }
}
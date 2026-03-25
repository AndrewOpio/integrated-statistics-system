using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WorkflowService.Data.Entities;
using WorkflowService.Data.Entities.AuditColumns;

namespace WorkflowService.Data.Configurations;

public class ApplicationDbContext : DbContext
{
    public readonly IHttpContextAccessor _httpContextAccessor;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor)
       : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<EntityType> EntityTypes { get; set; }
    public DbSet<StatusType> StatusTypes { get; set; }
    public DbSet<WorkflowTransition> WorkflowTransitions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }


    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var user = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "unknown";
        var now = DateTime.Now;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            var entity = entry.Entity;

            if (entry.State == EntityState.Added)
            {
                // Only set if not already set (allow manual override)
                if (entity.CreatedBy == null || entity.CreatedBy == "system")
                {
                    entity.CreatedBy = user;
                    entity.CreatedDate = now;
                }

                // Always update modified fields for new entities
                entity.ModifiedBy = user;
                entity.ModifiedDate = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                // Prevent overwriting creation audit fields
                entry.Property(e => e.CreatedDate).IsModified = false;
                entry.Property(e => e.CreatedBy).IsModified = false;

                // Update modification audit fields
                entity.ModifiedBy = user;
                entity.ModifiedDate = now;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

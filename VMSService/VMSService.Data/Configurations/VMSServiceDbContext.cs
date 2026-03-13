using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using VMSService.Data.AuditColumns;
using VMSService.Data.Entities;

namespace VMSService.Data.Configurations
{
    public class VMSServiceDbContext : DbContext
    {
        public readonly IHttpContextAccessor _httpContextAccessor;
        public VMSServiceDbContext(DbContextOptions<VMSServiceDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<VendorType> VendorTypes { get; set; }
        public DbSet<CurrencyVendor> CurrencyVendors { get; set; }
        //public DbSet<Denomination> Denominations { get; set; }
        public DbSet<ApprovedCurrencyProcurement> ApprovedCurrencyProcurements { get; set; }
        public DbSet<ExPrintMint> ExPrintMints { get; set; }
        public DbSet<ApprovedCurrencyDenomination> ApprovedCurrencyDenominations { get; set; }
        //public DbSet<DenominationType> DenominationTypes { get; set; }
        public DbSet<StatusType> StatusTypes { get; set; }
        public DbSet<ApprovedCurrencyStatusHistory> ApprovedCurrencyStatusHistory { get; set; }
        public DbSet<ExPrintMintStatusHistory> ExPrintMintStatusHistory { get; set; }
        public DbSet<ReceivedStock> ReceivedStock { get; set; }
        public DbSet<WorkflowTransition> WorkflowTransitions { get; set; }
        public DbSet<ReceivedStockStatusHistory> ReceivedStockStatusHistory { get; set; }
        public DbSet<CommercialBankUser> CommercialBankUsers { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var user = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "unknown";
            var now = DateTime.Now;

            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                var entity = entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedBy = user;
                    entity.CreatedDate = now;
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
}

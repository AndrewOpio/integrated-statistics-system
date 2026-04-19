using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using VMSService.Data.AuditColumns;
using VMSService.Data.Entities;

namespace VMSService.Data.Configurations
{
    public class VMSServiceDbContext : DbContext
    {
        public readonly IHttpContextAccessor? _httpContextAccessor;
        public VMSServiceDbContext(DbContextOptions<VMSServiceDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<VendorType> VendorTypes { get; set; }
        public DbSet<CurrencyVendor> CurrencyVendors { get; set; }
        //public DbSet<Denomination> Denominations { get; set; }
        public DbSet<ApprovedCurrencyProcurement> ApprovedCurrencyProcurements { get; set; }
        public DbSet<ExPrintMint> ExPrintMints { get; set; }
        public DbSet<ExPrintMintDetail> ExPrintMintDetails { get; set; }
        public DbSet<ApprovedCurrencyDenomination> ApprovedCurrencyDenominations { get; set; }
        //public DbSet<DenominationType> DenominationTypes { get; set; }
        public DbSet<StatusType> StatusTypes { get; set; }
        public DbSet<ApprovedCurrencyStatusHistory> ApprovedCurrencyStatusHistory { get; set; }
        public DbSet<ExPrintMintStatusHistory> ExPrintMintStatusHistory { get; set; }
        public DbSet<ReceivedStock> ReceivedStock { get; set; }
        public DbSet<WorkflowTransition> WorkflowTransitions { get; set; }
        public DbSet<ReceivedStockStatusHistory> ReceivedStockStatusHistory { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<StockMovementDenomination> StockMovementDenominations { get; set; }
        public DbSet<StockMovementDenominationBoxNumberRange> StockMovementDenominationBoxNumberRanges { get; set; }
        public DbSet<StockMovementStatusHistory> StockMovementStatusHistory { get; set; }
        public DbSet<StockMovementDiscrepancy> StockMovementDiscrepancies { get; set; }
        public DbSet<Logistics> Logistics { get; set; }
        public DbSet<LogisticsCategory> LogisticsCategories { get; set; }
        public DbSet<LogisticsDetail> LogisticsDetails { get; set; }
        public DbSet<CommercialBankUser> CommercialBankUsers { get; set; }
        public DbSet<AgentDetail> AgentDetails { get; set; }

        public DbSet<LogisticsStatusHistory> LogisticsStatusHistory { get; set; }
        public DbSet<ReceivedStatusTypes> ReceivedStatusTypes { get; set; }
        public DbSet<PackingListRange> PackingListRanges { get; set; }
        public DbSet<PackingListDetails> PackingListDetails { get; set; }
        public DbSet<NewCaseFullStatusTypes> NewCaseFullStatusTypes { get; set; }
        public DbSet<NewStockMovementlog> NewStockMovementLogs { get; set; }
        public DbSet<ReceivedStockDetails> ReceivedStockDetails { get; set; }
        public DbSet<NewStockAccountingStatus> NewStockAccountingStatuses { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure ReceivedStockDetails foreign keys to prevent cascade delete conflicts
            modelBuilder.Entity<ReceivedStockDetails>()
                .HasOne(x => x.ReceivedStock)
                .WithMany(x => x.ReceivedStockDetails)
                .HasForeignKey(x => x.ReceivedStockId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ReceivedStockDetails>()
                .HasOne(x => x.PackingListRange)
                .WithMany(x => x.ReceivedStockDetails)
                .HasForeignKey(x => x.PackingListRangeId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure PackingListDetails foreign key
            modelBuilder.Entity<PackingListDetails>()
                .HasOne(x => x.PackingListRange)
                .WithMany(x => x.PackingListDetails)
                .HasForeignKey(x => x.PackingListRangeId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure StockMovementStatusHistory foreign keys to prevent cascade delete conflicts
            modelBuilder.Entity<StockMovementStatusHistory>()
                .HasOne(x => x.OldStatus)
                .WithMany()
                .HasForeignKey(x => x.OldStatusId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<StockMovementStatusHistory>()
                .HasOne(x => x.NewStatus)
                .WithMany()
                .HasForeignKey(x => x.NewStatusId)
                .OnDelete(DeleteBehavior.NoAction);
        
            modelBuilder.Entity<StockMovementStatusHistory>()
                .HasOne(x => x.StockMovement)
                .WithMany(x => x.StatusHistory)
                .HasForeignKey(x => x.StockMovementId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LogisticsStatusHistory>()
                .HasOne(l => l.OldStatus)
                .WithMany()
                .HasForeignKey(l => l.OldStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LogisticsStatusHistory>()
                .HasOne(l => l.NewStatus)
                .WithMany()
                .HasForeignKey(l => l.NewStatusId)
                .OnDelete(DeleteBehavior.Restrict);
        }

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


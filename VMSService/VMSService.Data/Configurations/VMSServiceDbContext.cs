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
        public DbSet<ApprovedCurrencyStatusHistory> ApprovedCurrencyStatusHistory { get; set; }
        public DbSet<ExPrintMintStatusHistory> ExPrintMintStatusHistory { get; set; }
        public DbSet<ReceivedStock> ReceivedStock { get; set; }
        public DbSet<ReceivedStockStatusHistory> ReceivedStockStatusHistory { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<StockMovementDenomination> StockMovementDenominations { get; set; }
        public DbSet<StockMovementDenominationBoxNumberRange> StockMovementDenominationBoxNumberRanges { get; set; }
        public DbSet<StockMovementStatusHistory> StockMovementStatusHistory { get; set; }
        public DbSet<StockMovementDiscrepancy> StockMovementDiscrepancies { get; set; }
        public DbSet<Logistics> Logistics { get; set; }
        public DbSet<LogisticsCategory> LogisticsCategories { get; set; }
        public DbSet<LogisticsDetail> LogisticsDetails { get; set; }
        public DbSet<AgentDetail> AgentDetails { get; set; }
        public DbSet<WithdrawalRequest> WithdrawalRequests { get; set; }
        public DbSet<WithdrawalRequestAgent> WithdrawalRequestAgents { get; set; }
        public DbSet<WithdrawalRequestDenomination> WithdrawalRequestDenominations { get; set; }
        public DbSet<WithdrawalRequestStatusHistory> WithdrawalRequestStatusHistory { get; set; }
        public DbSet<AgentDetailStatusHistory> AgentDetailStatusHistory { get; set; }

        public DbSet<LogisticsStatusHistory> LogisticsStatusHistory { get; set; }
        public DbSet<ReceivedStatusTypes> ReceivedStatusTypes { get; set; }
        public DbSet<PackingListRange> PackingListRanges { get; set; }
        public DbSet<PackingListDetails> PackingListDetails { get; set; }
        public DbSet<NewCaseFullStatusTypes> NewCaseFullStatusTypes { get; set; }
        public DbSet<NewStockMovementlog> NewStockMovementLogs { get; set; }
        public DbSet<ReceivedStockDetails> ReceivedStockDetails { get; set; }
        public DbSet<NewStockAccountingStatus> NewStockAccountingStatuses { get; set; }

                // Deposit
        public DbSet<DepositRequest> DepositRequests { get; set; }
        public DbSet<DepositRequestAgent> DepositRequestAgents { get; set; }
        public DbSet<DepositRequestDenomination> DepositRequestDenominations { get; set; }
        public DbSet<DepositRequestStatusHistory> DepositRequestStatusHistory { get; set; }
        public DbSet<MinDepositStandard> MinDepositStandards { get; set; }
        public DbSet<DepositProcessingFee> DepositProcessingFees { get; set; }
        public DbSet<FeeType> FeeTypes { get; set; }
        public DbSet<DepositFee> DepositFees { get; set; }

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
                .HasOne(x => x.StockMovement)
                .WithMany(x => x.StatusHistory)
                .HasForeignKey(x => x.StockMovementId)
                .OnDelete(DeleteBehavior.Cascade);


            // ── WithdrawalRequest ─────────────────────

            modelBuilder.Entity<WithdrawalRequest>()
                .HasMany(r => r.WithdrawalRequestAgents)
                .WithOne(c => c.WithdrawalRequest)
                .HasForeignKey(c => c.WithdrawalRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WithdrawalRequest>()
                .HasMany(r => r.WithdrawalRequestStatusHistory)
                .WithOne(s => s.WithdrawalRequest)
                .HasForeignKey(s => s.WithdrawalRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Indexes ───────────────────────────
            modelBuilder.Entity<WithdrawalRequest>()
                .HasIndex(r => r.TransactionRefNumber)
                .IsUnique()
                .HasFilter("[TransactionRefNumber] IS NOT NULL"); 

            modelBuilder.Entity<WithdrawalRequest>().HasIndex(r => r.CbId);
            modelBuilder.Entity<WithdrawalRequest>().HasIndex(r => r.StatusTypeId);
            modelBuilder.Entity<WithdrawalRequest>().HasIndex(r => r.CollectionDate);
            modelBuilder.Entity<WithdrawalRequest>().HasIndex(r => new { r.CbId, r.BouBranchId, r.CollectionDate });

            modelBuilder.Entity<WithdrawalRequestDenomination>().HasIndex(d => new { d.WithdrawalRequestId, d.Origin });

            modelBuilder.Entity<WithdrawalRequestAgent>().HasIndex(c => new { c.WithdrawalRequestId, c.EnteredBy });
            modelBuilder.Entity<WithdrawalRequestStatusHistory>().HasIndex(s => s.WithdrawalRequestId);

            //modelBuilder.Entity<LogisticsStatusHistory>()
            //    .HasOne(l => l.OldStatus)
            //    .WithMany()
            //    .HasForeignKey(l => l.OldStatusId)
            //    .OnDelete(DeleteBehavior.Restrict);

            //modelBuilder.Entity<LogisticsStatusHistory>()
            //    .HasOne(l => l.NewStatus)
            //    .WithMany()
            //    .HasForeignKey(l => l.NewStatusId)
            //    .OnDelete(DeleteBehavior.Restrict);
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


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
        public DbSet<StockMovementDenominationBag> StockMovementDenominationBags { get; set; }
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
        public DbSet<WithdrawalRequestDenominationBox> WithdrawalRequestDenominationBoxes => Set<WithdrawalRequestDenominationBox>();

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
        public DbSet<Attachment> Attachments { get; set; }

        public DbSet<BoxState> BoxStates { get; set; }
        // Processing
        public DbSet<ProcessingBoard> ProcessingBoards { get; set; }
        public DbSet<BPSMachine> BPSMachines { get; set; }
        public DbSet<StockProcessingPlan> StockProcessingPlans { get; set; }
        public DbSet<StockProcessingPlanStatusHistory> StockProcessingPlanStatusHistory { get; set; }
        public DbSet<StockProcessingResult> StockProcessingResults { get; set; }
        public DbSet<StockProcessingSession> StockProcessingSessions { get; set; }
        public DbSet<StockProcessingSessionStatusHistory> StockProcessingSessionStatusHistory { get; set; }
        public DbSet<Bag> Bags { get; set; }
        public DbSet<BagDenomination> BagDenominations { get; set; }
        public DbSet<Vault> Vaults { get; set; }
        public DbSet<VaultSection> VaultSections { get; set; }
        public DbSet<BaggedBag> BaggedBags { get; set; }
        public DbSet<BagMovement> BagMovements { get; set; }
        public DbSet<BagMovementBag> BagMovementBags { get; set; }
        public DbSet<BagMovementStatusHistory> BagMovementStatusHistory { get; set; }
        public DbSet<SessionBag> SessionBags { get; set; }
        public DbSet<CountingRoom> CountingRooms { get; set; }
        public DbSet<CountingRoomSession> CountingRoomSessions { get; set; }
        public DbSet<ProcessingSessionBag> ProcessingSessionBags { get; set; }
        public DbSet<BoardSessionSurplusShortageStatusHistory> BoardSessionSurplusShortageStatusHistory { get; set; }
        public DbSet<NoteExaminer> NoteExaminers { get; set; }
        public DbSet<StockProcessingSessionNoteExaminer> StockProcessingSessionNoteExaminers { get; set; }
        public DbSet<MIandFit> MIandFits { get; set; }

        // End of Day Discrepancies
        public DbSet<EodDiscrepancyReport> EodDiscrepancyReports { get; set; }
        public DbSet<EodDiscrepancyReportEntry> EodDiscrepancyReportEntries { get; set; }
        public DbSet<BoardSessionSurplusShortage> BoardSessionSurplusShortages { get; set; }
        public DbSet<BoardSessionDifferentDenom> BoardSessionDifferentDenoms { get; set; }
        public DbSet<EodDiscrepancyReportStatusHistory> EodDiscrepancyReportStatusHistories { get; set; }

        // Monthly Surplus
        public DbSet<MonthlySurplus> MonthlySurpluses { get; set; }
        public DbSet<MonthlySurplusStatusHistory> MonthlySurplusStatusHistories { get; set; }

        // Monthly Shortage
        public DbSet<MonthlyShortage> MonthlyShortages { get; set; }
        public DbSet<MonthlyShortageDetail> MonthlyShortageDetails { get; set; }
        public DbSet<MonthlyShortageStatusHistory> MonthlyShortageStatusHistories { get; set; }
        public DbSet<MonthlyShortageReimbursement> MonthlyShortageReimbursements { get; set; }
        public DbSet<MonthlyShortageReimbursementDenomination> MonthlyShortageReimbursementDenominations { get; set; }
        public DbSet<MonthlyShortageReimbursementStatusHistory> MonthlyShortageReimbursementStatusHistories { get; set; }

        // Monthly Commercial Bank Settlement
        public DbSet<MonthlyCommercialBankSettlement> MonthlyCommercialBankSettlements { get; set; }
        public DbSet<MonthlyCommercialBankSettlementStatusHistory> MonthlyCommercialBankSettlementStatusHistories { get; set; }

        // Banking Surplus
        public DbSet<BankingSurplus> BankingSurpluses { get; set; }
        public DbSet<BankingSurplusStatusHistory> BankingSurplusStatusHistories { get; set; }

        // Banking Shortage
        public DbSet<BankingShortage> BankingShortages { get; set; }
        public DbSet<BankingShortageStatusHistory> BankingShortageStatusHistories { get; set; }

        // Destruction
        public DbSet<DestructionRequest> DestructionRequests { get; set; }
        public DbSet<DestructionRequestDenomination> DestructionRequestDenominations { get; set; }
        public DbSet<DestructionRequestMember> DestructionRequestMembers { get; set; }
        public DbSet<DestructionRequestStatusHistory> DestructionRequestStatusHistory { get; set; }
        public DbSet<DestructionType> DestructionTypes { get; set; }
        public DbSet<DestructionLocation> DestructionLocations { get; set; }
        public DbSet<DestructionAuthorization> DestructionAuthorizations { get; set; }
        public DbSet<DestructionAuthorizationDenomination> DestructionAuthorizationDenominations { get; set; }
        public DbSet<DestructionAuthorizationStatusHistory> DestructionAuthorizationStatusHistory { get; set; }

        public DbSet<CfbBox> CfbBoxes => Set<CfbBox>();
        public DbSet<CfbCashHolding> CfbCashHoldings => Set<CfbCashHolding>();
        public DbSet<CfbCashMovement> CfbCashMovements => Set<CfbCashMovement>();
        public DbSet<TopupRequest> TopupRequests => Set<TopupRequest>();
        public DbSet<TopupLine> TopupLines => Set<TopupLine>();
        public DbSet<TopupRequestStatusHistory> TopupRequestStatusHistories => Set<TopupRequestStatusHistory>();

        public DbSet<Penalty> Penalties => Set<Penalty>();
        public DbSet<AppliedPenalty> AppliedPenalties => Set<AppliedPenalty>();
        public DbSet<PenaltyWaiver> PenaltyWaivers => Set<PenaltyWaiver>();
        public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();

        // Stock Take
        public DbSet<StockTake> StockTakes { get; set; }
        public DbSet<StockTakeDetail> StockTakeDetails { get; set; }
        public DbSet<StockTakeStatusHistory> StockTakeStatusHistory { get; set; }
        public DbSet<StockTakeSystemPosition> StockTakeSystemPositions { get; set; }

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

            // Two live (non-deleted) requests can't share an RTGS code — soft-deleted requests'
            // codes don't count, so they become reusable immediately (see cb-withdrawal-process.md
            // §8). This is the real guarantee; ExistsByRtgsCodeAsync is just a friendly pre-check
            // on top of it.
            modelBuilder.Entity<WithdrawalRequest>()
                .HasIndex(r => r.RtgsCode)
                .IsUnique()
                .HasFilter("[RtgsCode] IS NOT NULL AND [IsDeleted] = 0")
                .HasDatabaseName("UQ_WithdrawalRequest_RtgsCode_Live");

            modelBuilder.Entity<BoxState>()
                .HasIndex(bs => new { bs.CaseNumber, bs.DenominationId, bs.BranchIsAtId })
                .IsUnique()
                .HasDatabaseName("UQ_BoxState_Box_Denom_Branch");

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

            // ── WithdrawalRequestDenomination ─────────────────────
            modelBuilder.Entity<WithdrawalRequestDenominationBox>(e =>
            {
                e.HasOne(x => x.WithdrawalRequestDenomination)
                 .WithMany(x => x.DispatchedBoxes)
                 .HasForeignKey(x => x.WithdrawalRequestDenominationId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => x.CaseNumber);
            });


            // ── CFB ─────────────────────
            // Each branch has exactly two boxes: one for Notes and one for Coins.
            modelBuilder.Entity<CfbBox>()
                .HasIndex(x => new { x.BouBranchId, x.BoxType }).IsUnique();
            modelBuilder.Entity<CfbBox>()
                .Property(x => x.BoxType).HasMaxLength(10);

            // Holdings are versioned baselines. Only one row per (box, denomination) may be active at a time.
            // Inactive rows are retained as baseline history.
            modelBuilder.Entity<CfbCashHolding>()
                .HasIndex(x => new { x.CfbBoxId, x.DenominationId })
                .HasFilter("[IsActive] = 1")
                .IsUnique()
                .HasDatabaseName("IX_CfbCashHoldings_Active_Box_Denom");
            modelBuilder.Entity<CfbCashHolding>()
                .HasOne(x => x.CfbBox).WithMany(x => x.Holdings)
                .HasForeignKey(x => x.CfbBoxId)
                .OnDelete(DeleteBehavior.Cascade);

            // Movement ledger: deviation-type entries only (session losses/surpluses/replenishments).
            // Top-ups and destructions change the baseline, never write a movement row.
            modelBuilder.Entity<CfbCashMovement>()
                .HasOne(x => x.CfbBox).WithMany()
                .HasForeignKey(x => x.CfbBoxId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CfbCashMovement>()
                .HasIndex(x => new { x.CfbBoxId, x.DenominationId, x.OccurredAt });
            modelBuilder.Entity<CfbCashMovement>()
                .Property(x => x.MovementType).HasMaxLength(30);

            // ── TopupRequest ─────────────────────
            modelBuilder.Entity<TopupRequest>()
                .HasMany(r => r.Lines)
                .WithOne(l => l.TopupRequest)
                .HasForeignKey(l => l.TopupRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TopupRequest>()
                .HasMany(r => r.StatusHistory)
                .WithOne(h => h.TopupRequest)
                .HasForeignKey(h => h.TopupRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TopupRequest>()
                .HasIndex(r => r.BouBranchId);
            modelBuilder.Entity<TopupRequest>()
                .HasIndex(r => r.CurrentStatusId);

            // ── Penalty ─────────────────────
            modelBuilder.Entity<Penalty>()
                .HasIndex(x => x.Name)
                .IsUnique();
            modelBuilder.Entity<Penalty>()
                .Property(x => x.PenaltyPercent).HasPrecision(18, 4);
            modelBuilder.Entity<Penalty>()
                .Property(x => x.PercentCutOff).HasPrecision(18, 4);

            // ── PublicHoliday ─────────────────────
            modelBuilder.Entity<PublicHoliday>()
                .HasIndex(x => x.Date)
                .IsUnique();

            // ── AppliedPenalty ─────────────────────
            modelBuilder.Entity<AppliedPenalty>()
                .HasIndex(x => new { x.SourceId, x.PenaltyId })
                .IsUnique();
            modelBuilder.Entity<AppliedPenalty>()
                .HasOne(x => x.Penalty)
                .WithMany()
                .HasForeignKey(x => x.PenaltyId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── BoardSessionSurplusShortage → DepositRequest ─────────────────────────
            //modelBuilder.Entity<BoardSessionSurplusShortage>()
            //    .HasOne(s => s.DepositRequest)
            //    .WithMany(d => d.BoardSessionSurplusShortages)
            //    .HasForeignKey(s => s.DepositRequestId)
            //    .OnDelete(DeleteBehavior.SetNull);

            // ── EodDiscrepancyReportEntry (junction) ──────────────────────────────────
            modelBuilder.Entity<EodDiscrepancyReportEntry>(entity =>
            {
                entity.HasOne(e => e.EodDiscrepancyReport)
                      .WithMany(r => r.Entries)
                      .HasForeignKey(e => e.EodDiscrepancyReportId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.BoardSessionSurplusShortage)
                      .WithMany()
                      .HasForeignKey(e => e.BoardSessionSurplusShortageId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.BoardSessionSurplusShortageId).IsUnique();
                entity.HasIndex(e => e.EodDiscrepancyReportId);
            });

            // ── StockTake ─────────────────────
            modelBuilder.Entity<StockTakeDetail>()
                .HasOne(x => x.StockTake)
                .WithMany(x => x.StockTakeDetails)
                .HasForeignKey(x => x.StockTakeId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<StockTakeStatusHistory>()
                .HasOne(x => x.StockTake)
                .WithMany(x => x.StockTakeStatusHistory)
                .HasForeignKey(x => x.StockTakeId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<StockTakeDetail>()
                .HasOne(x => x.VaultSection)
                .WithMany()
                .HasForeignKey(x => x.VaultSectionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<StockTake>()
                .HasOne(x => x.AmendsStockTake)
                .WithMany()
                .HasForeignKey(x => x.AmendsStockTakeId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<StockTakeSystemPosition>()
                .HasOne(x => x.StockTake)
                .WithMany()
                .HasForeignKey(x => x.StockTakeId)
                .OnDelete(DeleteBehavior.NoAction);

            // ✅ ADD THIS AT THE END — catches all remaining FKs not explicitly configured above
            foreach (var relationship in modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys())
                .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
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



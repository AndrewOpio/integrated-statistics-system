using Microsoft.EntityFrameworkCore;
using StockService.Data.Entities;

namespace StockService.Data.Configurations;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<BranchCategory> BranchCategories { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<CounterPartyType> CounterPartyTypes { get; set; }
    public DbSet<Denomination> Denominations { get; set; }
    public DbSet<DenominationType> DenominationTypes { get; set; }
    public DbSet<StockLedger> StockLedger { get; set; }
    public DbSet<StockLedgerDetail> StockLedgerDetails { get; set; }
    public DbSet<SubCategory> SubCategories { get; set; }
    public DbSet<StockLedgerDetailReportItem> StockLedgerDetailReportItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<StockLedgerDetailReportItem>(b =>
        {
            b.HasNoKey();
            b.ToView(null);
        });
    }
}




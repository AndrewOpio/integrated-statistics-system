using ISSService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ISSService.Data.Configurations;

public class ISSServiceDbContext : DbContext
{
    public ISSServiceDbContext(DbContextOptions<ISSServiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<StatisticalProcess> StatisticalProcesses =>
        Set<StatisticalProcess>();

    public DbSet<ReportingCycle> ReportingCycles =>
        Set<ReportingCycle>();

    public DbSet<Respondent> Respondents =>
        Set<Respondent>();

    public DbSet<RespondentCase> RespondentCases =>
        Set<RespondentCase>();

    public DbSet<RespondentCaseStatusHistory> RespondentCaseStatusHistories =>
        Set<RespondentCaseStatusHistory>();

    public DbSet<DataCollectionTemplate> DataCollectionTemplates =>
        Set<DataCollectionTemplate>();

    public DbSet<DataCollectionTemplateVersion> DataCollectionTemplateVersions =>
        Set<DataCollectionTemplateVersion>();

    public DbSet<CycleTemplateAssignment> CycleTemplateAssignments =>
        Set<CycleTemplateAssignment>();

    public DbSet<Submission> Submissions =>
        Set<Submission>();

    public DbSet<ValidationRun> ValidationRuns =>
        Set<ValidationRun>();

    public DbSet<ValidationFinding> ValidationFindings =>
        Set<ValidationFinding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<StatisticalProcess>(entity =>
        {
            entity.ToTable("StatisticalProcesses");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Description)
                .HasMaxLength(1000);

            entity.Property(x => x.ResponsibleUnit)
                .HasMaxLength(200);

            entity.Property(x => x.CreatedBy)
                .HasMaxLength(200);

            entity.Property(x => x.ModifiedBy)
                .HasMaxLength(200);

            entity.HasIndex(x => x.Code)
                .IsUnique();

            entity.HasMany(x => x.ReportingCycles)
                .WithOne(x => x.StatisticalProcess)
                .HasForeignKey(x => x.StatisticalProcessId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.DataCollectionTemplates)
                .WithOne(x => x.StatisticalProcess)
                .HasForeignKey(x => x.StatisticalProcessId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReportingCycle>(entity =>
        {
            entity.ToTable("ReportingCycles");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Notes)
                .HasMaxLength(1000);

            entity.Property(x => x.CreatedBy)
                .HasMaxLength(200);

            entity.Property(x => x.ModifiedBy)
                .HasMaxLength(200);

            entity.HasIndex(x => new
            {
                x.StatisticalProcessId,
                x.Code
            })
            .IsUnique();

            entity.HasMany(x => x.RespondentCases)
                .WithOne(x => x.ReportingCycle)
                .HasForeignKey(x => x.ReportingCycleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.TemplateAssignments)
                .WithOne(x => x.ReportingCycle)
                .HasForeignKey(x => x.ReportingCycleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Respondent>(entity =>
        {
            entity.ToTable("Respondents");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(x => x.Region)
                .HasMaxLength(100);

            entity.Property(x => x.District)
                .HasMaxLength(100);

            entity.Property(x => x.Address)
                .HasMaxLength(500);

            entity.Property(x => x.PrimaryContactName)
                .HasMaxLength(200);

            entity.Property(x => x.PrimaryContactEmail)
                .HasMaxLength(250);

            entity.Property(x => x.PrimaryContactPhone)
                .HasMaxLength(50);

            entity.Property(x => x.CreatedBy)
                .HasMaxLength(200);

            entity.Property(x => x.ModifiedBy)
                .HasMaxLength(200);

            entity.HasIndex(x => x.Code)
                .IsUnique();

            entity.HasMany(x => x.RespondentCases)
                .WithOne(x => x.Respondent)
                .HasForeignKey(x => x.RespondentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RespondentCase>(entity =>
        {
            entity.ToTable("RespondentCases");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.CaseReference)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.CurrentStatusName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Notes)
                .HasMaxLength(1000);

            entity.Property(x => x.CreatedBy)
                .HasMaxLength(200);

            entity.Property(x => x.ModifiedBy)
                .HasMaxLength(200);

            entity.HasIndex(x => x.CaseReference)
                .IsUnique();

            entity.HasIndex(x => new
            {
                x.ReportingCycleId,
                x.RespondentId
            })
            .IsUnique();

            entity.HasIndex(x => x.CurrentStatusId);

            entity.HasMany(x => x.Submissions)
                .WithOne(x => x.RespondentCase)
                .HasForeignKey(x => x.RespondentCaseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.StatusHistory)
                .WithOne(x => x.RespondentCase)
                .HasForeignKey(x => x.RespondentCaseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RespondentCaseStatusHistory>(entity =>
        {
            entity.ToTable("RespondentCaseStatusHistories");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.OldStatusName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.NewStatusName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.ActionName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Remarks)
                .HasMaxLength(2000);

            entity.Property(x => x.ChangedBy)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.CreatedBy)
                .HasMaxLength(200);

            entity.HasIndex(x => x.RespondentCaseId);

            entity.HasIndex(x => x.WorkflowTransitionId);

            entity.HasIndex(x => new
            {
                x.RespondentCaseId,
                x.ChangedDate
            });
        });

        modelBuilder.Entity<DataCollectionTemplate>(entity =>
        {
            entity.ToTable("DataCollectionTemplates");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(x => x.Description)
                .HasMaxLength(1000);

            entity.Property(x => x.CreatedBy)
                .HasMaxLength(200);

            entity.Property(x => x.ModifiedBy)
                .HasMaxLength(200);

            entity.HasIndex(x => new
            {
                x.StatisticalProcessId,
                x.Code
            })
            .IsUnique();

            entity.HasMany(x => x.Versions)
                .WithOne(x => x.DataCollectionTemplate)
                .HasForeignKey(x => x.DataCollectionTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DataCollectionTemplateVersion>(entity =>
        {
            entity.ToTable("DataCollectionTemplateVersions");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.VersionLabel)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.FileName)
                .HasMaxLength(255);

            entity.Property(x => x.FilePath)
                .HasMaxLength(1000);

            entity.Property(x => x.FileHash)
                .HasMaxLength(128);

            entity.Property(x => x.ChangeNotes)
                .HasMaxLength(2000);

            entity.Property(x => x.CreatedBy)
                .HasMaxLength(200);

            entity.Property(x => x.ApprovedBy)
                .HasMaxLength(200);

            entity.HasIndex(x => new
            {
                x.DataCollectionTemplateId,
                x.VersionNumber
            })
            .IsUnique();

            entity.HasMany(x => x.CycleAssignments)
                .WithOne(x => x.DataCollectionTemplateVersion)
                .HasForeignKey(x => x.DataCollectionTemplateVersionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CycleTemplateAssignment>(entity =>
        {
            entity.ToTable("CycleTemplateAssignments");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.AssignedBy)
                .HasMaxLength(200);

            entity.Property(x => x.CreatedBy)
                .HasMaxLength(200);

            entity.Property(x => x.ModifiedBy)
                .HasMaxLength(200);

            entity.HasIndex(x => new
            {
                x.ReportingCycleId,
                x.DataCollectionTemplateVersionId
            })
            .IsUnique();
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.ToTable("Submissions");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.SubmittedBy)
                .HasMaxLength(200);

            entity.Property(x => x.FileName)
                .HasMaxLength(255);

            entity.Property(x => x.FilePath)
                .HasMaxLength(1000);

            entity.Property(x => x.FileHash)
                .HasMaxLength(128);

            entity.Property(x => x.Notes)
                .HasMaxLength(2000);

            entity.Property(x => x.CreatedBy)
                .HasMaxLength(200);

            entity.Property(x => x.ModifiedBy)
                .HasMaxLength(200);

            entity.HasIndex(x => new
            {
                x.RespondentCaseId,
                x.SubmissionNumber
            })
            .IsUnique();

            entity.HasIndex(x => x.DataCollectionTemplateVersionId);

            entity.HasOne(x => x.DataCollectionTemplateVersion)
                .WithMany()
                .HasForeignKey(x => x.DataCollectionTemplateVersionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.ValidationRuns)
                .WithOne(x => x.Submission)
                .HasForeignKey(x => x.SubmissionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ValidationRun>(entity =>
        {
            entity.ToTable("ValidationRuns");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Summary)
                .HasMaxLength(2000);

            entity.Property(x => x.ExecutedBy)
                .HasMaxLength(200);

            entity.Property(x => x.CreatedBy)
                .HasMaxLength(200);

            entity.Property(x => x.ModifiedBy)
                .HasMaxLength(200);

            entity.HasIndex(x => new
            {
                x.SubmissionId,
                x.RunNumber
            })
            .IsUnique();

            entity.HasMany(x => x.Findings)
                .WithOne(x => x.ValidationRun)
                .HasForeignKey(x => x.ValidationRunId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ValidationFinding>(entity =>
        {
            entity.ToTable("ValidationFindings");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.RuleCode)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.RuleName)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(x => x.Message)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(x => x.SheetName)
                .HasMaxLength(250);

            entity.Property(x => x.FieldName)
                .HasMaxLength(250);

            entity.Property(x => x.CellReference)
                .HasMaxLength(100);

            entity.Property(x => x.RecordReference)
                .HasMaxLength(250);

            entity.Property(x => x.ObservedValue)
                .HasMaxLength(2000);

            entity.Property(x => x.ExpectedValue)
                .HasMaxLength(2000);

            entity.Property(x => x.ResolutionNotes)
                .HasMaxLength(2000);

            entity.Property(x => x.ResolvedBy)
                .HasMaxLength(200);

            entity.Property(x => x.CreatedBy)
                .HasMaxLength(200);

            entity.Property(x => x.ModifiedBy)
                .HasMaxLength(200);

            entity.HasIndex(x => x.ValidationRunId);

            entity.HasIndex(x => new
            {
                x.ValidationRunId,
                x.RuleCode
            });
        });
    }
}
namespace ISSService.Data.Entities;

public class ReportingCycle
{
    public int Id { get; set; }

    public int StatisticalProcessId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTime PeriodStartDate { get; set; }

    public DateTime PeriodEndDate { get; set; }

    public DateTime? CollectionOpenDate { get; set; }

    public DateTime? SubmissionDueDate { get; set; }

    public DateTime? CollectionCloseDate { get; set; }

    public ReportingCycleStatus Status { get; set; } = ReportingCycleStatus.Draft;

    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public StatisticalProcess StatisticalProcess { get; set; } = null!;

    public ICollection<RespondentCase> RespondentCases { get; set; }
        = new List<RespondentCase>();

    public ICollection<CycleTemplateAssignment> TemplateAssignments { get; set; }
        = new List<CycleTemplateAssignment>();
}

public enum ReportingCycleStatus
{
    Draft = 1,
    Scheduled = 2,
    Open = 3,
    Closed = 4,
    Cancelled = 5
}
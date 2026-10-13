namespace ISSService.Data.Entities;

public class RespondentCase
{
    public int Id { get; set; }

    public int ReportingCycleId { get; set; }

    public int RespondentId { get; set; }

    public string CaseReference { get; set; } = string.Empty;

    // Workflow status is centrally defined by WorkflowService.
    // ISS stores the current central workflow status on the business record.
    public int CurrentStatusId { get; set; }

    public string CurrentStatusName { get; set; } = string.Empty;

    public DateTime? DistributedDate { get; set; }

    public DateTime? FirstSubmissionDate { get; set; }

    public DateTime? LatestSubmissionDate { get; set; }

    public DateTime? AcceptedDate { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public ReportingCycle ReportingCycle { get; set; } = null!;

    public Respondent Respondent { get; set; } = null!;

    public ICollection<Submission> Submissions { get; set; }
        = new List<Submission>();

    public ICollection<RespondentCaseStatusHistory> StatusHistory { get; set; }
        = new List<RespondentCaseStatusHistory>();
}
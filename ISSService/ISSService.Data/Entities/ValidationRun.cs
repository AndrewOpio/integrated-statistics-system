namespace ISSService.Data.Entities;

public class ValidationRun
{
    public int Id { get; set; }

    public int SubmissionId { get; set; }

    public int RunNumber { get; set; }

    public ValidationRunStatus Status { get; set; } = ValidationRunStatus.Pending;

    public DateTime StartedDate { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedDate { get; set; }

    public int TotalChecks { get; set; }

    public int PassedChecks { get; set; }

    public int WarningChecks { get; set; }

    public int FailedChecks { get; set; }

    public string? Summary { get; set; }

    public string? ExecutedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public Submission Submission { get; set; } = null!;

    public ICollection<ValidationFinding> Findings { get; set; }
        = new List<ValidationFinding>();
}

public enum ValidationRunStatus
{
    Pending = 1,
    Running = 2,
    Passed = 3,
    PassedWithWarnings = 4,
    Failed = 5,
    Error = 6
}
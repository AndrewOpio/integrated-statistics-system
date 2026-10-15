namespace ISSService.Data.Entities;

public class Submission
{
    public int Id { get; set; }

    public int RespondentCaseId { get; set; }

    // Records the exact template version against which this
    // submission was prepared. This preserves historical
    // traceability even if the reporting cycle's template
    // assignment changes later.
    public int DataCollectionTemplateVersionId { get; set; }

    public int SubmissionNumber { get; set; }

    public SubmissionMethod SubmissionMethod { get; set; }

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Received;

    public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;

    public string? SubmittedBy { get; set; }

    public string? FileName { get; set; }

    public string? FilePath { get; set; }

    public string? FileHash { get; set; }

    public long? FileSizeBytes { get; set; }

    public string? Notes { get; set; }

    // Only the latest valid submission for a respondent case
    // should be marked as current. Earlier submissions remain
    // available as part of the audit trail.
    public bool IsCurrent { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public RespondentCase RespondentCase { get; set; } = null!;

    public DataCollectionTemplateVersion DataCollectionTemplateVersion { get; set; }
        = null!;

    public ICollection<ValidationRun> ValidationRuns { get; set; }
        = new List<ValidationRun>();
}

public enum SubmissionMethod
{
    TemplateUpload = 1,
    ElectronicEntry = 2,
    FieldCollection = 3,
    AdministrativeImport = 4
}

public enum SubmissionStatus
{
    Received = 1,
    Validating = 2,
    ValidationFailed = 3,
    Validated = 4,
    UnderReview = 5,
    CorrectionRequired = 6,
    Accepted = 7,
    Rejected = 8,
    Superseded = 9
}
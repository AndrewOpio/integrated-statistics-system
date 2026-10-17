namespace ISSService.Data.Entities;

public class OperationalDataset
{
    public int Id { get; set; }

    // The submission from which this operational dataset
    // was produced. This preserves full source provenance.
    public int SubmissionId { get; set; }

    public OperationalDatasetStatus Status { get; set; }
        = OperationalDatasetStatus.Pending;

    // Only one dataset derived from a respondent case should
    // normally represent the current operational position.
    // Earlier datasets remain available for audit history.
    public bool IsCurrent { get; set; } = true;

    public DateTime? ExtractionStartedDate { get; set; }

    public DateTime? ExtractionCompletedDate { get; set; }

    public int ObservationCount { get; set; }

    public string? Summary { get; set; }

    public string? ExtractedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public Submission Submission { get; set; } = null!;

    public ICollection<StatisticalObservation> Observations { get; set; }
        = new List<StatisticalObservation>();
}

public enum OperationalDatasetStatus
{
    Pending = 1,
    Extracting = 2,
    Extracted = 3,
    ExtractionFailed = 4,
    Superseded = 5
}
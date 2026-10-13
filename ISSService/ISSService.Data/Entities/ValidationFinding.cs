namespace ISSService.Data.Entities;

public class ValidationFinding
{
    public int Id { get; set; }

    public int ValidationRunId { get; set; }

    public string RuleCode { get; set; } = string.Empty;

    public string RuleName { get; set; } = string.Empty;

    public ValidationSeverity Severity { get; set; }

    public ValidationFindingStatus Status { get; set; }
        = ValidationFindingStatus.Open;

    public string Message { get; set; } = string.Empty;

    public string? SheetName { get; set; }

    public string? FieldName { get; set; }

    public string? CellReference { get; set; }

    public string? RecordReference { get; set; }

    public string? ObservedValue { get; set; }

    public string? ExpectedValue { get; set; }

    public string? ResolutionNotes { get; set; }

    public DateTime? ResolvedDate { get; set; }

    public string? ResolvedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public ValidationRun ValidationRun { get; set; } = null!;
}

public enum ValidationSeverity
{
    Information = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}

public enum ValidationFindingStatus
{
    Open = 1,
    Acknowledged = 2,
    Resolved = 3,
    Waived = 4
}
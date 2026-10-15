namespace ISSService.Web.Models.Validation;

public sealed class ValidationResultsViewModel
{
    public int SubmissionId { get; set; }

    public int RespondentCaseId { get; set; }

    public int SubmissionNumber { get; set; }

    public string? SubmissionStatusName { get; set; }

    public bool IsCurrentSubmission { get; set; }

    public string? FileName { get; set; }

    public DateTime SubmittedDate { get; set; }

    public string? SubmittedBy { get; set; }

    public string? CaseReference { get; set; }

    public string? RespondentCode { get; set; }

    public string? RespondentName { get; set; }

    public string? StatisticalProcessName { get; set; }

    public string? ReportingCycleName { get; set; }

    public string? ReportingPeriod { get; set; }

    public string? TemplateName { get; set; }

    public string? TemplateVersionLabel { get; set; }

    public List<ValidationRunViewModel> ValidationRuns { get; set; }
        = new();
}

public sealed class ValidationRunViewModel
{
    public int Id { get; set; }

    public int RunNumber { get; set; }

    public string StatusName { get; set; } = string.Empty;

    public DateTime StartedDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public int TotalChecks { get; set; }

    public int PassedChecks { get; set; }

    public int WarningChecks { get; set; }

    public int FailedChecks { get; set; }

    public string? Summary { get; set; }

    public string? ExecutedBy { get; set; }

    public List<ValidationFindingViewModel> Findings { get; set; }
        = new();
}

public sealed class ValidationFindingViewModel
{
    public int Id { get; set; }

    public string RuleCode { get; set; } = string.Empty;

    public string RuleName { get; set; } = string.Empty;

    public string SeverityName { get; set; } = string.Empty;

    public string StatusName { get; set; } = string.Empty;

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
}
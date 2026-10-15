using ISSService.Data.Entities;

namespace ISSService.Web.Services.Validation;

public interface ISaccoWorkbookValidationService
{
    Task<SaccoWorkbookValidationResult> ValidateAsync(
        string filePath,
        string expectedRespondentCode,
        CancellationToken cancellationToken = default);
}

public class SaccoWorkbookValidationResult
{
    public int TotalChecks { get; set; }

    public int PassedChecks { get; set; }

    public int WarningChecks { get; set; }

    public int FailedChecks { get; set; }

    public bool HasErrors =>
        Findings.Any(x =>
            x.Severity == ValidationSeverity.Error ||
            x.Severity == ValidationSeverity.Critical);

    public List<SaccoValidationFindingResult> Findings { get; set; }
        = new();
}

public class SaccoValidationFindingResult
{
    public string RuleCode { get; set; } = string.Empty;

    public string RuleName { get; set; } = string.Empty;

    public ValidationSeverity Severity { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? SheetName { get; set; }

    public string? FieldName { get; set; }

    public string? CellReference { get; set; }

    public string? RecordReference { get; set; }

    public string? ObservedValue { get; set; }

    public string? ExpectedValue { get; set; }
}
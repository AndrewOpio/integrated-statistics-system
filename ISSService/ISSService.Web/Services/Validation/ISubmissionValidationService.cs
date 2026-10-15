namespace ISSService.Web.Services.Validation;

public interface ISubmissionValidationService
{
    Task<SubmissionValidationExecutionResult> ValidateAsync(
        int submissionId,
        string executedBy,
        CancellationToken cancellationToken = default);
}

public sealed class SubmissionValidationExecutionResult
{
    public bool Success { get; init; }

    public int? ValidationRunId { get; init; }

    public int? RunNumber { get; init; }

    public int TotalChecks { get; init; }

    public int PassedChecks { get; init; }

    public int WarningChecks { get; init; }

    public int FailedChecks { get; init; }

    public string Message { get; init; } = string.Empty;

    public static SubmissionValidationExecutionResult Failed(
        string message)
    {
        return new SubmissionValidationExecutionResult
        {
            Success = false,
            Message = message
        };
    }
}
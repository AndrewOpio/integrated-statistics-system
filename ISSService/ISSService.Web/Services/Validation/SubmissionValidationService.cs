using ISSService.Data.Configurations;
using ISSService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ISSService.Web.Services.Validation;

public sealed class SubmissionValidationService(
    ISSServiceDbContext db,
    ISaccoWorkbookValidationService saccoWorkbookValidationService,
    ILogger<SubmissionValidationService> logger)
    : ISubmissionValidationService
{
    public async Task<SubmissionValidationExecutionResult> ValidateAsync(
        int submissionId,
        string executedBy,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var username = NormalizeUsername(executedBy);

        var submission = await db.Submissions
            .Include(x => x.RespondentCase)
                .ThenInclude(x => x.Respondent)
            .Include(x => x.RespondentCase)
                .ThenInclude(x => x.ReportingCycle)
                    .ThenInclude(x => x.StatisticalProcess)
            .Include(x => x.DataCollectionTemplateVersion)
                .ThenInclude(x => x.DataCollectionTemplate)
            .FirstOrDefaultAsync(
                x => x.Id == submissionId,
                cancellationToken);

        if (submission is null)
        {
            return SubmissionValidationExecutionResult.Failed(
                "The submission could not be found.");
        }

        if (string.IsNullOrWhiteSpace(submission.FilePath))
        {
            return SubmissionValidationExecutionResult.Failed(
                "The submission does not have a stored return file.");
        }

        if (submission.SubmissionMethod !=
            SubmissionMethod.TemplateUpload)
        {
            return SubmissionValidationExecutionResult.Failed(
                "This validation service currently supports template-upload submissions only.");
        }

        if (!submission.IsCurrent)
        {
            return SubmissionValidationExecutionResult.Failed(
                "Only the current submission for a respondent case can be validated.");
        }

        if (submission.Status == SubmissionStatus.Accepted)
        {
            return SubmissionValidationExecutionResult.Failed(
                "An accepted submission cannot be revalidated.");
        }

        var now = DateTime.UtcNow;

        var existingRunNumbers = await db.ValidationRuns
            .Where(x => x.SubmissionId == submission.Id)
            .Select(x => x.RunNumber)
            .ToListAsync(cancellationToken);

        var nextRunNumber = existingRunNumbers.Count == 0
            ? 1
            : existingRunNumbers.Max() + 1;

        var validationRun = new ValidationRun
        {
            SubmissionId = submission.Id,
            RunNumber = nextRunNumber,
            Status = ValidationRunStatus.Running,
            StartedDate = now,
            ExecutedBy = username,
            CreatedDate = now,
            CreatedBy = username
        };

        db.ValidationRuns.Add(validationRun);

        submission.Status = SubmissionStatus.Validating;
        submission.ModifiedDate = now;
        submission.ModifiedBy = username;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(
                ex,
                "Could not start validation run for submission {SubmissionId}.",
                submission.Id);

            return SubmissionValidationExecutionResult.Failed(
                "The validation run could not be started.");
        }

        SaccoWorkbookValidationResult validationResult;

        try
        {
            validationResult =
                await saccoWorkbookValidationService.ValidateAsync(
                    submission.FilePath,
                    submission.RespondentCase.Respondent.Code,
                    cancellationToken);
        }
        catch (OperationCanceledException)
        {
            validationRun.Status = ValidationRunStatus.Error;
            validationRun.CompletedDate = DateTime.UtcNow;
            validationRun.Summary =
                "Validation was cancelled before completion.";
            validationRun.ModifiedDate = DateTime.UtcNow;
            validationRun.ModifiedBy = username;

            submission.Status = SubmissionStatus.Received;
            submission.ModifiedDate = DateTime.UtcNow;
            submission.ModifiedBy = username;

            await SaveFailureStateAsync();

            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error validating submission {SubmissionId}.",
                submission.Id);

            validationRun.Status = ValidationRunStatus.Error;
            validationRun.CompletedDate = DateTime.UtcNow;
            validationRun.Summary =
                "Validation could not be completed because of an unexpected system error.";
            validationRun.ModifiedDate = DateTime.UtcNow;
            validationRun.ModifiedBy = username;

            submission.Status = SubmissionStatus.Received;
            submission.ModifiedDate = DateTime.UtcNow;
            submission.ModifiedBy = username;

            await SaveFailureStateAsync();

            return SubmissionValidationExecutionResult.Failed(
                "The submission could not be validated because of an unexpected system error.");
        }

        var completedDate = DateTime.UtcNow;

        validationRun.TotalChecks =
            validationResult.TotalChecks;

        validationRun.PassedChecks =
            validationResult.PassedChecks;

        validationRun.WarningChecks =
            validationResult.WarningChecks;

        validationRun.FailedChecks =
            validationResult.FailedChecks;

        foreach (var findingResult in validationResult.Findings)
        {
            validationRun.Findings.Add(
                new ValidationFinding
                {
                    RuleCode =
                        Truncate(
                            findingResult.RuleCode,
                            100),
                    RuleName =
                        Truncate(
                            findingResult.RuleName,
                            250),
                    Severity =
                        findingResult.Severity,
                    Status =
                        ValidationFindingStatus.Open,
                    Message =
                        Truncate(
                            findingResult.Message,
                            2000),
                    SheetName =
                        TruncateNullable(
                            findingResult.SheetName,
                            250),
                    FieldName =
                        TruncateNullable(
                            findingResult.FieldName,
                            250),
                    CellReference =
                        TruncateNullable(
                            findingResult.CellReference,
                            100),
                    RecordReference =
                        TruncateNullable(
                            findingResult.RecordReference,
                            250),
                    ObservedValue =
                        TruncateNullable(
                            findingResult.ObservedValue,
                            2000),
                    ExpectedValue =
                        TruncateNullable(
                            findingResult.ExpectedValue,
                            2000),
                    CreatedDate =
                        completedDate,
                    CreatedBy =
                        username
                });
        }

        if (validationResult.HasErrors)
        {
            validationRun.Status =
                ValidationRunStatus.Failed;

            submission.Status =
                SubmissionStatus.ValidationFailed;
        }
        else if (validationResult.WarningChecks > 0)
        {
            validationRun.Status =
                ValidationRunStatus.PassedWithWarnings;

            submission.Status =
                SubmissionStatus.Validated;
        }
        else
        {
            validationRun.Status =
                ValidationRunStatus.Passed;

            submission.Status =
                SubmissionStatus.Validated;
        }

        validationRun.CompletedDate = completedDate;

        validationRun.Summary =
            BuildSummary(
                validationRun.Status,
                validationResult.TotalChecks,
                validationResult.PassedChecks,
                validationResult.WarningChecks,
                validationResult.FailedChecks);

        validationRun.ModifiedDate = completedDate;
        validationRun.ModifiedBy = username;

        submission.ModifiedDate = completedDate;
        submission.ModifiedBy = username;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(
                ex,
                "Could not persist validation results for submission {SubmissionId}, run {RunNumber}.",
                submission.Id,
                nextRunNumber);

            return SubmissionValidationExecutionResult.Failed(
                "Validation was executed, but its results could not be saved.");
        }

        return new SubmissionValidationExecutionResult
        {
            Success = true,
            ValidationRunId = validationRun.Id,
            RunNumber = validationRun.RunNumber,
            TotalChecks = validationRun.TotalChecks,
            PassedChecks = validationRun.PassedChecks,
            WarningChecks = validationRun.WarningChecks,
            FailedChecks = validationRun.FailedChecks,
            Message = validationRun.Status switch
            {
                ValidationRunStatus.Passed =>
                    "The submission passed validation.",

                ValidationRunStatus.PassedWithWarnings =>
                    "The submission passed validation with warnings.",

                ValidationRunStatus.Failed =>
                    "The submission failed validation.",

                _ =>
                    "Validation completed."
            }
        };

        async Task SaveFailureStateAsync()
        {
            try
            {
                await db.SaveChangesAsync(
                    CancellationToken.None);
            }
            catch (Exception saveException)
            {
                logger.LogError(
                    saveException,
                    "Could not persist failure state for validation run {ValidationRunId} on submission {SubmissionId}.",
                    validationRun.Id,
                    submission.Id);
            }
        }
    }

    private static string BuildSummary(
        ValidationRunStatus status,
        int totalChecks,
        int passedChecks,
        int warningChecks,
        int failedChecks)
    {
        return
            $"Validation {status}. " +
            $"Total checks: {totalChecks}; " +
            $"Passed: {passedChecks}; " +
            $"Warnings: {warningChecks}; " +
            $"Failed: {failedChecks}.";
    }

    private static string NormalizeUsername(
        string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return "development-user";
        }

        return Truncate(
            username.Trim(),
            200);
    }

    private static string Truncate(
        string value,
        int maximumLength)
    {
        if (value.Length <= maximumLength)
        {
            return value;
        }

        return value[..maximumLength];
    }

    private static string? TruncateNullable(
        string? value,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return value.Length <= maximumLength
            ? value
            : value[..maximumLength];
    }
}
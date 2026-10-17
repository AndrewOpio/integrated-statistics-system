using ISSService.Data.Configurations;
using ISSService.Data.Entities;
using ISSService.Web.Models.Submissions;
using ISSService.Web.Models.Validation;
using ISSService.Web.Services.Extraction;
using ISSService.Web.Services.Submissions;
using ISSService.Web.Services.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISSService.Web.Controllers;

public class SubmissionsController : Controller
{
    private readonly ISSServiceDbContext _db;
    private readonly IScannedSubmissionStorageService _storageService;
    private readonly ISubmissionValidationService _validationService;
    private readonly ISaccoDataExtractionService _dataExtractionService;
    private readonly ILogger<SubmissionsController> _logger;

    public SubmissionsController(
        ISSServiceDbContext db,
        IScannedSubmissionStorageService storageService,
        ISubmissionValidationService validationService,
        ISaccoDataExtractionService dataExtractionService,
        ILogger<SubmissionsController> logger)
    {
        _db = db;
        _storageService = storageService;
        _validationService = validationService;
        _dataExtractionService = dataExtractionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int respondentCaseId)
    {
        var respondentCase = await GetRespondentCaseAsync(
            respondentCaseId,
            asNoTracking: true);

        if (respondentCase == null)
        {
            return NotFound();
        }

        var submissions = await _db.Submissions
            .AsNoTracking()
            .Where(x => x.RespondentCaseId == respondentCaseId)
            .OrderByDescending(x => x.SubmissionNumber)
            .Select(x => new SubmissionViewModel
            {
                Id = x.Id,
                RespondentCaseId = x.RespondentCaseId,
                DataCollectionTemplateVersionId =
                    x.DataCollectionTemplateVersionId,
                CaseReference =
                    x.RespondentCase.CaseReference,
                StatisticalProcessName =
                    x.RespondentCase.ReportingCycle
                        .StatisticalProcess.Name,
                ReportingCycleName =
                    x.RespondentCase.ReportingCycle.Name,
                ReportingPeriod =
                    $"{x.RespondentCase.ReportingCycle.PeriodStartDate:dd MMM yyyy} - " +
                    $"{x.RespondentCase.ReportingCycle.PeriodEndDate:dd MMM yyyy}",
                RespondentCode =
                    x.RespondentCase.Respondent.Code,
                RespondentName =
                    x.RespondentCase.Respondent.Name,
                CurrentCaseStatusName =
                    x.RespondentCase.CurrentStatusName,
                TemplateName =
                    x.DataCollectionTemplateVersion
                        .DataCollectionTemplate.Name,
                TemplateVersionLabel =
                    x.DataCollectionTemplateVersion.VersionLabel,
                SubmissionNumber =
                    x.SubmissionNumber,
                SubmissionMethodName =
                    x.SubmissionMethod.ToString(),
                StatusName =
                    x.Status.ToString(),
                SubmittedDate =
                    x.SubmittedDate,
                SubmittedBy =
                    x.SubmittedBy,
                FileName =
                    x.FileName,
                FileSizeBytes =
                    x.FileSizeBytes,
                FileHash =
                    x.FileHash,
                IsCurrent =
                    x.IsCurrent,
                HasValidationRuns =
                    x.ValidationRuns.Any(),
                HasOperationalDataset =
                    x.OperationalDatasets.Any(dataset =>
                        dataset.IsCurrent &&
                        dataset.Status ==
                            OperationalDatasetStatus.Extracted),
                OperationalDatasetId =
                    x.OperationalDatasets
                        .Where(dataset =>
                            dataset.IsCurrent &&
                            dataset.Status ==
                                OperationalDatasetStatus.Extracted)
                        .OrderByDescending(dataset =>
                            dataset.Id)
                        .Select(dataset =>
                            (int?)dataset.Id)
                        .FirstOrDefault(),
                OperationalObservationCount =
                    x.OperationalDatasets
                        .Where(dataset =>
                            dataset.IsCurrent &&
                            dataset.Status ==
                                OperationalDatasetStatus.Extracted)
                        .OrderByDescending(dataset =>
                            dataset.Id)
                        .Select(dataset =>
                            dataset.ObservationCount)
                        .FirstOrDefault(),
                OperationalDatasetStatusName =
                    x.OperationalDatasets
                        .Where(dataset =>
                            dataset.IsCurrent)
                        .OrderByDescending(dataset =>
                            dataset.Id)
                        .Select(dataset =>
                            dataset.Status.ToString())
                        .FirstOrDefault(),
                Notes =
                    x.Notes
            })
            .ToListAsync();

        PopulateCaseViewData(respondentCase);

        return View(submissions);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int respondentCaseId)
    {
        var respondentCase = await GetRespondentCaseAsync(
            respondentCaseId,
            asNoTracking: true);

        if (respondentCase == null)
        {
            return NotFound();
        }

        if (!respondentCase.IsActive)
        {
            TempData["Error"] =
                "A submission cannot be created for an inactive respondent case.";

            return RedirectToAction(
                nameof(Index),
                new { respondentCaseId });
        }

        var assignment = await GetPrimaryTemplateAssignmentAsync(
            respondentCase.ReportingCycleId);

        if (assignment == null)
        {
            TempData["Error"] =
                "The reporting cycle does not have an active primary " +
                "template assignment. Assign a primary template before " +
                "receiving a return.";

            return RedirectToAction(
                nameof(Index),
                new { respondentCaseId });
        }

        var model = BuildCreateViewModel(
            respondentCase,
            assignment);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        SubmissionViewModel model,
        CancellationToken cancellationToken)
    {
        var respondentCase = await GetRespondentCaseAsync(
            model.RespondentCaseId,
            asNoTracking: false);

        if (respondentCase == null)
        {
            return NotFound();
        }

        var assignment = await GetPrimaryTemplateAssignmentAsync(
            respondentCase.ReportingCycleId);

        if (assignment == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "The reporting cycle does not have an active primary " +
                "template assignment.");
        }

        if (!respondentCase.IsActive)
        {
            ModelState.AddModelError(
                string.Empty,
                "A submission cannot be created for an inactive " +
                "respondent case.");
        }

        // Never trust the template version posted by the browser.
        // The applicable version is derived from the respondent
        // case's reporting cycle.
        if (assignment != null)
        {
            model.DataCollectionTemplateVersionId =
                assignment.DataCollectionTemplateVersionId;
        }

        PopulateCreateViewModel(
            model,
            respondentCase,
            assignment);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ScannedSubmissionStorageResult storageResult;

        try
        {
            storageResult =
                await _storageService.ScanAndSaveAsync(
                    model.SubmissionFile!,
                    cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }

        if (!storageResult.Success)
        {
            ModelState.AddModelError(
                nameof(model.SubmissionFile),
                storageResult.Error ??
                "The submission file could not be stored.");

            return View(model);
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var username = GetCurrentUsername();
            var now = DateTime.UtcNow;

            var existingSubmissions =
                await _db.Submissions
                    .Where(x =>
                        x.RespondentCaseId ==
                        respondentCase.Id)
                    .OrderByDescending(x =>
                        x.SubmissionNumber)
                    .ToListAsync(cancellationToken);

            var nextSubmissionNumber =
                existingSubmissions.Count == 0
                    ? 1
                    : existingSubmissions.Max(
                        x => x.SubmissionNumber) + 1;

            foreach (var currentSubmission in
                     existingSubmissions.Where(x => x.IsCurrent))
            {
                currentSubmission.IsCurrent = false;
                currentSubmission.Status =
                    SubmissionStatus.Superseded;
                currentSubmission.ModifiedDate = now;
                currentSubmission.ModifiedBy = username;
            }

            var submission = new Submission
            {
                RespondentCaseId =
                    respondentCase.Id,
                DataCollectionTemplateVersionId =
                    assignment!.DataCollectionTemplateVersionId,
                SubmissionNumber =
                    nextSubmissionNumber,
                SubmissionMethod =
                    SubmissionMethod.TemplateUpload,
                Status =
                    SubmissionStatus.Received,
                SubmittedDate =
                    now,
                SubmittedBy =
                    username,
                FileName =
                    storageResult.OriginalFileName,
                FilePath =
                    storageResult.FilePath,
                FileHash =
                    storageResult.FileHash,
                FileSizeBytes =
                    storageResult.FileSize,
                Notes =
                    model.Notes,
                IsCurrent =
                    true,
                CreatedDate =
                    now,
                CreatedBy =
                    username
            };

            _db.Submissions.Add(submission);

            if (!respondentCase.FirstSubmissionDate.HasValue)
            {
                respondentCase.FirstSubmissionDate = now;
            }

            respondentCase.LatestSubmissionDate = now;
            respondentCase.ModifiedDate = now;
            respondentCase.ModifiedBy = username;

            await _db.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            TempData["Success"] =
                $"Submission #{nextSubmissionNumber} for " +
                $"'{respondentCase.CaseReference}' was received successfully.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    respondentCaseId =
                        respondentCase.Id
                });
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            await TryDeleteStoredSubmissionAsync(
                storageResult.FilePath);

            throw;
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            await TryDeleteStoredSubmissionAsync(
                storageResult.FilePath);

            _logger.LogError(
                ex,
                "Database error creating a submission for " +
                "respondent case {RespondentCaseId}.",
                respondentCase.Id);

            ModelState.AddModelError(
                string.Empty,
                "The return file passed the malware scan, but the " +
                "submission record could not be saved. Please try again.");

            return View(model);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            await TryDeleteStoredSubmissionAsync(
                storageResult.FilePath);

            _logger.LogError(
                ex,
                "Unexpected error creating a submission for " +
                "respondent case {RespondentCaseId}.",
                respondentCase.Id);

            ModelState.AddModelError(
                string.Empty,
                "An unexpected error occurred while receiving the return.");

            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Validate(
        int submissionId,
        CancellationToken cancellationToken)
    {
        var submission = await _db.Submissions
            .AsNoTracking()
            .Where(x => x.Id == submissionId)
            .Select(x => new
            {
                x.Id,
                x.RespondentCaseId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (submission == null)
        {
            return NotFound();
        }

        SubmissionValidationExecutionResult result;

        try
        {
            result = await _validationService.ValidateAsync(
                submission.Id,
                GetCurrentUsername(),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error requesting validation for " +
                "submission {SubmissionId}.",
                submission.Id);

            TempData["Error"] =
                "An unexpected error occurred while validating the return.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    respondentCaseId =
                        submission.RespondentCaseId
                });
        }

        if (result.Success)
        {
            TempData["Success"] =
                result.Message +
                $" Run #{result.RunNumber}: " +
                $"{result.PassedChecks} passed, " +
                $"{result.WarningChecks} warnings, " +
                $"{result.FailedChecks} failed.";

            if (result.ValidationRunId.HasValue)
            {
                return RedirectToAction(
                    nameof(ValidationResults),
                    new
                    {
                        submissionId =
                            submission.Id
                    });
            }
        }
        else
        {
            TempData["Error"] =
                result.Message;
        }

        return RedirectToAction(
            nameof(Index),
            new
            {
                respondentCaseId =
                    submission.RespondentCaseId
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Extract(
        int submissionId,
        CancellationToken cancellationToken)
    {
        var submission = await _db.Submissions
            .AsNoTracking()
            .Where(x => x.Id == submissionId)
            .Select(x => new
            {
                x.Id,
                x.RespondentCaseId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (submission == null)
        {
            return NotFound();
        }

        SaccoDataExtractionResult result;

        try
        {
            result = await _dataExtractionService.ExtractAsync(
                submission.Id,
                GetCurrentUsername(),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error extracting operational data from " +
                "submission {SubmissionId}.",
                submission.Id);

            TempData["Error"] =
                "An unexpected error occurred while extracting " +
                "operational data from the validated return.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    respondentCaseId =
                        submission.RespondentCaseId
                });
        }

        if (result.Success)
        {
            TempData["Success"] =
                result.Message +
                $" {result.ObservationCount} operational observations " +
                "are available for downstream processing.";
        }
        else
        {
            TempData["Error"] =
                result.Message;
        }

        return RedirectToAction(
            nameof(Index),
            new
            {
                respondentCaseId =
                    submission.RespondentCaseId
            });
    }

    [HttpGet]
    public async Task<IActionResult> ValidationResults(
        int submissionId,
        CancellationToken cancellationToken)
    {
        var model = await _db.Submissions
            .AsNoTracking()
            .Where(x => x.Id == submissionId)
            .Select(x => new ValidationResultsViewModel
            {
                SubmissionId =
                    x.Id,

                RespondentCaseId =
                    x.RespondentCaseId,

                SubmissionNumber =
                    x.SubmissionNumber,

                SubmissionStatusName =
                    x.Status.ToString(),

                IsCurrentSubmission =
                    x.IsCurrent,

                FileName =
                    x.FileName,

                SubmittedDate =
                    x.SubmittedDate,

                SubmittedBy =
                    x.SubmittedBy,

                CaseReference =
                    x.RespondentCase.CaseReference,

                RespondentCode =
                    x.RespondentCase.Respondent.Code,

                RespondentName =
                    x.RespondentCase.Respondent.Name,

                StatisticalProcessName =
                    x.RespondentCase.ReportingCycle
                        .StatisticalProcess.Name,

                ReportingCycleName =
                    x.RespondentCase.ReportingCycle.Name,

                ReportingPeriod =
                    $"{x.RespondentCase.ReportingCycle.PeriodStartDate:dd MMM yyyy} - " +
                    $"{x.RespondentCase.ReportingCycle.PeriodEndDate:dd MMM yyyy}",

                TemplateName =
                    x.DataCollectionTemplateVersion
                        .DataCollectionTemplate.Name,

                TemplateVersionLabel =
                    x.DataCollectionTemplateVersion.VersionLabel,

                ValidationRuns =
                    x.ValidationRuns
                        .OrderByDescending(run =>
                            run.RunNumber)
                        .Select(run =>
                            new ValidationRunViewModel
                            {
                                Id =
                                    run.Id,

                                RunNumber =
                                    run.RunNumber,

                                StatusName =
                                    run.Status.ToString(),

                                StartedDate =
                                    run.StartedDate,

                                CompletedDate =
                                    run.CompletedDate,

                                TotalChecks =
                                    run.TotalChecks,

                                PassedChecks =
                                    run.PassedChecks,

                                WarningChecks =
                                    run.WarningChecks,

                                FailedChecks =
                                    run.FailedChecks,

                                Summary =
                                    run.Summary,

                                ExecutedBy =
                                    run.ExecutedBy,

                                Findings =
                                    run.Findings
                                        .OrderByDescending(finding =>
                                            finding.Severity)
                                        .ThenBy(finding =>
                                            finding.RuleCode)
                                        .Select(finding =>
                                            new ValidationFindingViewModel
                                            {
                                                Id =
                                                    finding.Id,

                                                RuleCode =
                                                    finding.RuleCode,

                                                RuleName =
                                                    finding.RuleName,

                                                SeverityName =
                                                    finding.Severity.ToString(),

                                                StatusName =
                                                    finding.Status.ToString(),

                                                Message =
                                                    finding.Message,

                                                SheetName =
                                                    finding.SheetName,

                                                FieldName =
                                                    finding.FieldName,

                                                CellReference =
                                                    finding.CellReference,

                                                RecordReference =
                                                    finding.RecordReference,

                                                ObservedValue =
                                                    finding.ObservedValue,

                                                ExpectedValue =
                                                    finding.ExpectedValue,

                                                ResolutionNotes =
                                                    finding.ResolutionNotes,

                                                ResolvedDate =
                                                    finding.ResolvedDate,

                                                ResolvedBy =
                                                    finding.ResolvedBy
                                            })
                                        .ToList()
                            })
                        .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    private async Task<RespondentCase?> GetRespondentCaseAsync(
        int respondentCaseId,
        bool asNoTracking)
    {
        var query = _db.RespondentCases
            .Include(x => x.ReportingCycle)
                .ThenInclude(x => x.StatisticalProcess)
            .Include(x => x.Respondent)
            .AsQueryable();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(
            x => x.Id == respondentCaseId);
    }

    private async Task<CycleTemplateAssignment?>
        GetPrimaryTemplateAssignmentAsync(
            int reportingCycleId)
    {
        return await _db.CycleTemplateAssignments
            .AsNoTracking()
            .Include(x =>
                x.DataCollectionTemplateVersion)
                .ThenInclude(x =>
                    x.DataCollectionTemplate)
            .Where(x =>
                x.ReportingCycleId ==
                    reportingCycleId &&
                x.IsPrimary &&
                x.IsActive)
            .OrderByDescending(x =>
                x.AssignedDate)
            .FirstOrDefaultAsync();
    }

    private static SubmissionViewModel BuildCreateViewModel(
        RespondentCase respondentCase,
        CycleTemplateAssignment assignment)
    {
        var model = new SubmissionViewModel
        {
            RespondentCaseId =
                respondentCase.Id,
            DataCollectionTemplateVersionId =
                assignment.DataCollectionTemplateVersionId
        };

        PopulateCreateViewModel(
            model,
            respondentCase,
            assignment);

        return model;
    }

    private static void PopulateCreateViewModel(
        SubmissionViewModel model,
        RespondentCase respondentCase,
        CycleTemplateAssignment? assignment)
    {
        model.CaseReference =
            respondentCase.CaseReference;

        model.StatisticalProcessName =
            respondentCase.ReportingCycle
                .StatisticalProcess.Name;

        model.ReportingCycleName =
            respondentCase.ReportingCycle.Name;

        model.ReportingPeriod =
            $"{respondentCase.ReportingCycle.PeriodStartDate:dd MMM yyyy} - " +
            $"{respondentCase.ReportingCycle.PeriodEndDate:dd MMM yyyy}";

        model.RespondentCode =
            respondentCase.Respondent.Code;

        model.RespondentName =
            respondentCase.Respondent.Name;

        model.CurrentCaseStatusName =
            respondentCase.CurrentStatusName;

        if (assignment != null)
        {
            model.DataCollectionTemplateVersionId =
                assignment.DataCollectionTemplateVersionId;

            model.TemplateName =
                assignment.DataCollectionTemplateVersion
                    .DataCollectionTemplate.Name;

            model.TemplateVersionLabel =
                assignment.DataCollectionTemplateVersion
                    .VersionLabel;
        }
    }

    private void PopulateCaseViewData(
        RespondentCase respondentCase)
    {
        ViewBag.RespondentCaseId =
            respondentCase.Id;

        ViewBag.CaseReference =
            respondentCase.CaseReference;

        ViewBag.RespondentCode =
            respondentCase.Respondent.Code;

        ViewBag.RespondentName =
            respondentCase.Respondent.Name;

        ViewBag.ReportingCycleId =
            respondentCase.ReportingCycleId;

        ViewBag.ReportingCycleName =
            respondentCase.ReportingCycle.Name;

        ViewBag.StatisticalProcessName =
            respondentCase.ReportingCycle
                .StatisticalProcess.Name;

        ViewBag.CurrentStatusName =
            respondentCase.CurrentStatusName;
    }

    private async Task TryDeleteStoredSubmissionAsync(
        string? filePath)
    {
        try
        {
            await _storageService.DeleteAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Could not clean up stored ISS submission " +
                "{FilePath} after persistence failure.",
                filePath);
        }
    }

    private string GetCurrentUsername()
    {
        if (User?.Identity?.IsAuthenticated == true &&
            !string.IsNullOrWhiteSpace(User.Identity.Name))
        {
            return User.Identity.Name;
        }

        return "development-user";
    }
}
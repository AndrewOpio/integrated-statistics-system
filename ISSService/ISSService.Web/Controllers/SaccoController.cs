using ISSService.Data.Configurations;
using ISSService.Data.Entities;
using ISSService.Web.Models.Sacco;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISSService.Web.Controllers;

public class SaccoController : Controller
{
    private readonly ISSServiceDbContext _db;
    private readonly ILogger<SaccoController> _logger;

    public SaccoController(
        ISSServiceDbContext db,
        ILogger<SaccoController> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int? cycleId = null)
    {
        var model = new SaccoDashboardViewModel();

        try
        {
            var process = await _db.StatisticalProcesses
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    (
                        x.Code.Contains("SACCO") ||
                        x.Name.Contains("SACCO")
                    ))
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            if (process == null)
            {
                model.ProcessConfigured = false;
                return View(model);
            }

            model.ProcessConfigured = true;
            model.ProcessName = process.Name;

            var cycles = await _db.ReportingCycles
                .AsNoTracking()
                .Where(x =>
                    x.StatisticalProcessId == process.Id)
                .OrderByDescending(x => x.PeriodEndDate)
                .ThenByDescending(x => x.Id)
                .Select(x => new SaccoCycleOptionViewModel
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Status = x.Status,
                    PeriodEndDate = x.PeriodEndDate
                })
                .ToListAsync();

            model.AvailableCycles = cycles;

            if (cycles.Count == 0)
            {
                return View(model);
            }

            var selectedCycleId =
                cycleId.HasValue &&
                cycles.Any(x => x.Id == cycleId.Value)
                    ? cycleId.Value
                    : cycles.First().Id;

            model.SelectedCycleId = selectedCycleId;

            var cycle = await _db.ReportingCycles
                .AsNoTracking()
                .Include(x => x.TemplateAssignments)
                    .ThenInclude(x =>
                        x.DataCollectionTemplateVersion)
                    .ThenInclude(x =>
                        x.DataCollectionTemplate)
                .FirstOrDefaultAsync(x =>
                    x.Id == selectedCycleId &&
                    x.StatisticalProcessId == process.Id);

            if (cycle == null)
            {
                return View(model);
            }

            var respondentCases = await _db.RespondentCases
                .AsNoTracking()
                .Where(x =>
                    x.ReportingCycleId == selectedCycleId &&
                    x.IsActive &&
                    x.Respondent.IsActive &&
                    x.Respondent.RespondentType ==
                        RespondentType.SACCO)
                .Include(x => x.Respondent)
                .Include(x => x.Submissions)
                    .ThenInclude(x => x.ValidationRuns)
                .Include(x => x.Submissions)
                    .ThenInclude(x => x.OperationalDatasets)
                .OrderBy(x => x.Respondent.Name)
                .ToListAsync();

            var primaryTemplate =
                cycle.TemplateAssignments
                    .Where(x =>
                        x.IsActive &&
                        x.IsPrimary)
                    .OrderByDescending(x =>
                        x.AssignedDate)
                    .FirstOrDefault()
                ??
                cycle.TemplateAssignments
                    .Where(x => x.IsActive)
                    .OrderByDescending(x =>
                        x.AssignedDate)
                    .FirstOrDefault();

            model.RespondentCases = respondentCases
                .Select(CreateRespondentCaseViewModel)
                .ToList();

            model.Cycle = new SaccoCycleSummaryViewModel
            {
                Id = cycle.Id,
                Code = cycle.Code,
                Name = cycle.Name,
                PeriodStartDate = cycle.PeriodStartDate,
                PeriodEndDate = cycle.PeriodEndDate,
                CollectionOpenDate =
                    cycle.CollectionOpenDate,
                SubmissionDueDate =
                    cycle.SubmissionDueDate,
                CollectionCloseDate =
                    cycle.CollectionCloseDate,
                Status = cycle.Status,

                TemplateName =
                    primaryTemplate?
                        .DataCollectionTemplateVersion
                        .DataCollectionTemplate
                        .Name,

                TemplateVersion =
                    primaryTemplate?
                        .DataCollectionTemplateVersion
                        .VersionLabel,

                TotalCases =
                    model.RespondentCases.Count,

                DistributedCases =
                    model.RespondentCases.Count(x =>
                        x.DistributedDate.HasValue),

                CasesWithSubmissions =
                    model.RespondentCases.Count(x =>
                        x.HasCurrentSubmission),

                OutstandingCases =
                    model.RespondentCases.Count(x =>
                        x.IsOutstanding),

                ValidatedCases =
                    model.RespondentCases.Count(x =>
                        x.IsValidated),

                CasesWithValidationFailures =
                    model.RespondentCases.Count(x =>
                        x.HasValidationFailure),

                ExtractedCases =
                    model.RespondentCases.Count(x =>
                        x.HasExtractedOperationalDataset),

                AcceptedCases =
                    model.RespondentCases.Count(x =>
                        x.AcceptedDate.HasValue)
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error loading the SACCO reporting dashboard.");

            TempData["Error"] =
                "The SACCO reporting dashboard could not be loaded.";

            return View(model);
        }
    }

    private static SaccoRespondentCaseViewModel
        CreateRespondentCaseViewModel(
            RespondentCase respondentCase)
    {
        var currentSubmission =
            respondentCase.Submissions
                .Where(x => x.IsCurrent)
                .OrderByDescending(x =>
                    x.SubmissionNumber)
                .ThenByDescending(x =>
                    x.SubmittedDate)
                .FirstOrDefault();

        var latestValidation =
            currentSubmission?
                .ValidationRuns
                .Where(x =>
                    x.CompletedDate.HasValue)
                .OrderByDescending(x =>
                    x.RunNumber)
                .ThenByDescending(x =>
                    x.CompletedDate)
                .FirstOrDefault();

        var currentOperationalDataset =
            currentSubmission?
                .OperationalDatasets
                .Where(x => x.IsCurrent)
                .OrderByDescending(x =>
                    x.ExtractionCompletedDate ??
                    x.ExtractionStartedDate ??
                    x.CreatedDate)
                .ThenByDescending(x =>
                    x.Id)
                .FirstOrDefault();

        return new SaccoRespondentCaseViewModel
        {
            RespondentCaseId =
                respondentCase.Id,

            CaseReference =
                respondentCase.CaseReference,

            RespondentCode =
                respondentCase.Respondent.Code,

            RespondentName =
                respondentCase.Respondent.Name,

            Region =
                respondentCase.Respondent.Region,

            District =
                respondentCase.Respondent.District,

            CurrentStatusId =
                respondentCase.CurrentStatusId,

            CurrentStatusName =
                respondentCase.CurrentStatusName,

            DistributedDate =
                respondentCase.DistributedDate,

            LatestSubmissionDate =
                currentSubmission?.SubmittedDate,

            AcceptedDate =
                respondentCase.AcceptedDate,

            SubmissionCount =
                respondentCase.Submissions.Count,

            CurrentSubmissionId =
                currentSubmission?.Id,

            CurrentSubmissionNumber =
                currentSubmission?.SubmissionNumber,

            LatestSubmissionStatus =
                currentSubmission?.Status,

            LatestValidationStatus =
                latestValidation?.Status,

            OperationalDatasetId =
                currentOperationalDataset?.Id,

            OperationalObservationCount =
                currentOperationalDataset?
                    .ObservationCount ?? 0,

            OperationalDatasetStatus =
                currentOperationalDataset?.Status
        };
    }
}
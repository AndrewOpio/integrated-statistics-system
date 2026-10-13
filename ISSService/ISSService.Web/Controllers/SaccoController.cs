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

    [HttpGet]
    public async Task<IActionResult> Index(int? cycleId = null)
    {
        try
        {
            var saccoProcess = await _db.StatisticalProcesses
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.ProcessType ==
                        StatisticalProcessType.InstitutionalReturn)
                .Where(x =>
                    x.Code.ToUpper().Contains("SACCO") ||
                    x.Name.ToUpper().Contains("SACCO"))
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            var model = new SaccoDashboardViewModel
            {
                ProcessConfigured = saccoProcess != null,
                ProcessName = saccoProcess?.Name ?? "SACCO Returns"
            };

            if (saccoProcess == null)
            {
                return View(model);
            }

            var cycles = await _db.ReportingCycles
                .AsNoTracking()
                .Where(x =>
                    x.StatisticalProcessId == saccoProcess.Id)
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
                .FirstOrDefaultAsync(
                    x => x.Id == selectedCycleId);

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
                .OrderBy(x => x.Respondent.Name)
                .ToListAsync();

            var primaryTemplate =
                cycle.TemplateAssignments
                    .Where(x =>
                        x.IsActive &&
                        x.IsPrimary)
                    .OrderByDescending(x => x.AssignedDate)
                    .FirstOrDefault()
                ??
                cycle.TemplateAssignments
                    .Where(x => x.IsActive)
                    .OrderByDescending(x => x.AssignedDate)
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
                CollectionOpenDate = cycle.CollectionOpenDate,
                SubmissionDueDate = cycle.SubmissionDueDate,
                CollectionCloseDate = cycle.CollectionCloseDate,
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

                TotalCases = respondentCases.Count,

                DistributedCases =
                    respondentCases.Count(x =>
                        x.DistributedDate.HasValue),

                CasesWithSubmissions =
                    respondentCases.Count(x =>
                        x.Submissions.Any()),

                AcceptedCases =
                    respondentCases.Count(x =>
                        x.AcceptedDate.HasValue),

                CasesWithValidationFailures =
                    respondentCases.Count(
                        HasCurrentValidationFailure)
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

            return View(
                new SaccoDashboardViewModel
                {
                    ProcessName = "SACCO Returns"
                });
        }
    }

    private static SaccoRespondentCaseViewModel
        CreateRespondentCaseViewModel(
            RespondentCase respondentCase)
    {
        var latestSubmission = respondentCase.Submissions
            .OrderByDescending(x => x.SubmissionNumber)
            .ThenByDescending(x => x.SubmittedDate)
            .FirstOrDefault();

        var latestValidation = latestSubmission?
            .ValidationRuns
            .OrderByDescending(x => x.RunNumber)
            .ThenByDescending(x => x.StartedDate)
            .FirstOrDefault();

        return new SaccoRespondentCaseViewModel
        {
            RespondentCaseId = respondentCase.Id,
            CaseReference = respondentCase.CaseReference,

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
                respondentCase.LatestSubmissionDate,

            AcceptedDate =
                respondentCase.AcceptedDate,

            SubmissionCount =
                respondentCase.Submissions.Count,

            LatestSubmissionStatus =
                latestSubmission?.Status,

            LatestValidationStatus =
                latestValidation?.Status
        };
    }

    private static bool HasCurrentValidationFailure(
        RespondentCase respondentCase)
    {
        var latestSubmission = respondentCase.Submissions
            .Where(x => x.IsCurrent)
            .OrderByDescending(x => x.SubmissionNumber)
            .ThenByDescending(x => x.SubmittedDate)
            .FirstOrDefault();

        if (latestSubmission == null)
        {
            return false;
        }

        var latestValidation = latestSubmission
            .ValidationRuns
            .OrderByDescending(x => x.RunNumber)
            .ThenByDescending(x => x.StartedDate)
            .FirstOrDefault();

        return latestValidation?.Status ==
                   ValidationRunStatus.Failed ||
               latestValidation?.Status ==
                   ValidationRunStatus.Error;
    }
}
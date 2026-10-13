using ISSService.Core.Services.Workflows;
using ISSService.Data.Configurations;
using ISSService.Data.Entities;
using ISSService.Web.Models.RespondentCases;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ISSService.Web.Controllers;

public class RespondentCasesController : Controller
{
    private const string InitialWorkflowStatusName = "Prepared";

    private readonly ISSServiceDbContext _db;
    private readonly IRespondentCaseWorkflowService _workflowService;
    private readonly ILogger<RespondentCasesController> _logger;

    public RespondentCasesController(
        ISSServiceDbContext db,
        IRespondentCaseWorkflowService workflowService,
        ILogger<RespondentCasesController> logger)
    {
        _db = db;
        _workflowService = workflowService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int reportingCycleId)
    {
        var cycle = await _db.ReportingCycles
            .AsNoTracking()
            .Include(x => x.StatisticalProcess)
            .FirstOrDefaultAsync(x => x.Id == reportingCycleId);

        if (cycle == null)
        {
            return NotFound();
        }

        var cases = await _db.RespondentCases
            .AsNoTracking()
            .Where(x => x.ReportingCycleId == reportingCycleId)
            .OrderBy(x => x.Respondent.Code)
            .Select(x => new RespondentCaseViewModel
            {
                Id = x.Id,
                ReportingCycleId = x.ReportingCycleId,
                RespondentId = x.RespondentId,
                CaseReference = x.CaseReference,

                StatisticalProcessName =
                    x.ReportingCycle.StatisticalProcess.Name,

                ReportingCycleName =
                    x.ReportingCycle.Name,

                ReportingPeriod =
                    x.ReportingCycle.PeriodStartDate
                        .ToString("dd MMM yyyy") +
                    " - " +
                    x.ReportingCycle.PeriodEndDate
                        .ToString("dd MMM yyyy"),

                RespondentCode = x.Respondent.Code,
                RespondentName = x.Respondent.Name,

                RespondentTypeName =
                    x.Respondent.RespondentType.ToString(),

                CurrentStatusId = x.CurrentStatusId,
                CurrentStatusName = x.CurrentStatusName,

                DistributedDate = x.DistributedDate,
                FirstSubmissionDate = x.FirstSubmissionDate,
                LatestSubmissionDate = x.LatestSubmissionDate,
                AcceptedDate = x.AcceptedDate,

                Notes = x.Notes,
                IsActive = x.IsActive,

                CreatedDate = x.CreatedDate,
                CreatedBy = x.CreatedBy,
                ModifiedDate = x.ModifiedDate,
                ModifiedBy = x.ModifiedBy
            })
            .ToListAsync();

        ViewBag.ReportingCycleId = cycle.Id;
        ViewBag.ReportingCycleName = cycle.Name;
        ViewBag.StatisticalProcessId =
            cycle.StatisticalProcessId;
        ViewBag.StatisticalProcessName =
            cycle.StatisticalProcess.Name;

        return View(cases);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int reportingCycleId)
    {
        var cycle = await _db.ReportingCycles
            .AsNoTracking()
            .Include(x => x.StatisticalProcess)
            .FirstOrDefaultAsync(x => x.Id == reportingCycleId);

        if (cycle == null)
        {
            return NotFound();
        }

        var model = new RespondentCaseViewModel
        {
            ReportingCycleId = cycle.Id,
            StatisticalProcessName =
                cycle.StatisticalProcess.Name,
            ReportingCycleName = cycle.Name,
            ReportingPeriod =
                FormatReportingPeriod(
                    cycle.PeriodStartDate,
                    cycle.PeriodEndDate),
            IsActive = true
        };

        await PopulateRespondentsAsync(
            model,
            cycle.StatisticalProcess.Code);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        RespondentCaseViewModel model)
    {
        var cycle = await _db.ReportingCycles
            .AsNoTracking()
            .Include(x => x.StatisticalProcess)
            .FirstOrDefaultAsync(
                x => x.Id == model.ReportingCycleId);

        if (cycle == null)
        {
            return NotFound();
        }

        model.StatisticalProcessName =
            cycle.StatisticalProcess.Name;

        model.ReportingCycleName =
            cycle.Name;

        model.ReportingPeriod =
            FormatReportingPeriod(
                cycle.PeriodStartDate,
                cycle.PeriodEndDate);

        var respondent = await _db.Respondents
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == model.RespondentId);

        if (respondent == null)
        {
            ModelState.AddModelError(
                nameof(model.RespondentId),
                "The selected respondent was not found.");
        }
        else
        {
            if (!respondent.IsActive)
            {
                ModelState.AddModelError(
                    nameof(model.RespondentId),
                    "The selected respondent is not active.");
            }

            if (!IsRespondentEligibleForProcess(
                    respondent,
                    cycle.StatisticalProcess.Code))
            {
                ModelState.AddModelError(
                    nameof(model.RespondentId),
                    "The selected respondent is not eligible " +
                    "for this statistical process.");
            }
        }

        var duplicateExists =
            await _db.RespondentCases
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.ReportingCycleId ==
                            model.ReportingCycleId &&
                        x.RespondentId ==
                            model.RespondentId);

        if (duplicateExists)
        {
            ModelState.AddModelError(
                nameof(model.RespondentId),
                "A respondent case already exists for this " +
                "respondent and reporting cycle.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateRespondentsAsync(
                model,
                cycle.StatisticalProcess.Code);

            return View(model);
        }

        try
        {
            var initialStatus =
                await _workflowService.GetInitialStatusAsync(
                    InitialWorkflowStatusName);

            var caseReference =
                BuildCaseReference(
                    cycle.StatisticalProcess.Code,
                    cycle.Code,
                    respondent!.Code);

            if (caseReference.Length > 100)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The generated case reference exceeds the " +
                    "maximum permitted length.");

                await PopulateRespondentsAsync(
                    model,
                    cycle.StatisticalProcess.Code);

                return View(model);
            }

            var caseReferenceExists =
                await _db.RespondentCases
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.CaseReference ==
                             caseReference);

            if (caseReferenceExists)
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"Case reference '{caseReference}' " +
                    "already exists.");

                await PopulateRespondentsAsync(
                    model,
                    cycle.StatisticalProcess.Code);

                return View(model);
            }

            var username = GetCurrentUsername();
            var now = DateTime.UtcNow;

            var respondentCase = new RespondentCase
            {
                ReportingCycleId =
                    model.ReportingCycleId,

                RespondentId =
                    model.RespondentId,

                CaseReference =
                    caseReference,

                CurrentStatusId =
                    initialStatus.Id,

                CurrentStatusName =
                    initialStatus.Name,

                Notes =
                    NormalizeOptionalText(model.Notes),

                IsActive = true,

                CreatedDate = now,
                CreatedBy = username
            };

            _db.RespondentCases.Add(respondentCase);

            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"Respondent case '{caseReference}' " +
                "was created successfully.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    reportingCycleId =
                        model.ReportingCycleId
                });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Workflow configuration prevented creation " +
                "of a respondent case for cycle {CycleId} " +
                "and respondent {RespondentId}.",
                model.ReportingCycleId,
                model.RespondentId);

            ModelState.AddModelError(
                string.Empty,
                ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Database error creating respondent case " +
                "for cycle {CycleId} and respondent " +
                "{RespondentId}.",
                model.ReportingCycleId,
                model.RespondentId);

            ModelState.AddModelError(
                string.Empty,
                "The respondent case could not be created. " +
                "Please verify the details and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error creating respondent case " +
                "for cycle {CycleId} and respondent " +
                "{RespondentId}.",
                model.ReportingCycleId,
                model.RespondentId);

            ModelState.AddModelError(
                string.Empty,
                "An unexpected error occurred while creating " +
                "the respondent case.");
        }

        await PopulateRespondentsAsync(
            model,
            cycle.StatisticalProcess.Code);

        return View(model);
    }

    private async Task PopulateRespondentsAsync(
        RespondentCaseViewModel model,
        string processCode)
    {
        var respondents = await _db.Respondents
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .ToListAsync();

        respondents = respondents
            .Where(
                x => IsRespondentEligibleForProcess(
                    x,
                    processCode))
            .ToList();

        var alreadyAssignedRespondentIds =
            await _db.RespondentCases
                .AsNoTracking()
                .Where(
                    x => x.ReportingCycleId ==
                         model.ReportingCycleId)
                .Select(x => x.RespondentId)
                .ToListAsync();

        model.Respondents = respondents
            .Where(
                x =>
                    x.Id == model.RespondentId ||
                    !alreadyAssignedRespondentIds.Contains(
                        x.Id))
            .Select(
                x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text =
                        $"{x.Code} - {x.Name}",
                    Selected =
                        x.Id == model.RespondentId
                })
            .ToList();
    }

    private static bool IsRespondentEligibleForProcess(
        Respondent respondent,
        string processCode)
    {
        if (string.Equals(
                processCode,
                "SACCO",
                StringComparison.OrdinalIgnoreCase))
        {
            return respondent.RespondentType ==
                   RespondentType.SACCO;
        }

        return true;
    }

    private static string BuildCaseReference(
        string processCode,
        string cycleCode,
        string respondentCode)
    {
        return string.Join(
            "-",
            NormalizeReferencePart(processCode),
            NormalizeReferencePart(cycleCode),
            NormalizeReferencePart(respondentCode));
    }

    private static string NormalizeReferencePart(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Trim()
            .Replace(" ", "-")
            .ToUpperInvariant();
    }

    private static string FormatReportingPeriod(
        DateTime startDate,
        DateTime endDate)
    {
        return
            $"{startDate:dd MMM yyyy} - " +
            $"{endDate:dd MMM yyyy}";
    }

    private static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private string GetCurrentUsername()
    {
        return User?.Identity?.Name
            ?? "development-user";
    }
}
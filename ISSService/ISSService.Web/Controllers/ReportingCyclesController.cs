using ISSService.Data.Configurations;
using ISSService.Data.Entities;
using ISSService.Web.Models.ReportingCycles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISSService.Web.Controllers;

public class ReportingCyclesController : Controller
{
    private readonly ISSServiceDbContext _db;
    private readonly ILogger<ReportingCyclesController> _logger;

    public ReportingCyclesController(
        ISSServiceDbContext db,
        ILogger<ReportingCyclesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int statisticalProcessId)
    {
        var process = await _db.StatisticalProcesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == statisticalProcessId);

        if (process == null)
        {
            return NotFound();
        }

        var cycles = await _db.ReportingCycles
            .AsNoTracking()
            .Where(x => x.StatisticalProcessId == statisticalProcessId)
            .OrderByDescending(x => x.PeriodEndDate)
            .ThenByDescending(x => x.Id)
            .Select(x => new ReportingCycleViewModel
            {
                Id = x.Id,
                StatisticalProcessId = x.StatisticalProcessId,
                StatisticalProcessCode = process.Code,
                StatisticalProcessName = process.Name,
                Code = x.Code,
                Name = x.Name,
                PeriodStartDate = x.PeriodStartDate,
                PeriodEndDate = x.PeriodEndDate,
                CollectionOpenDate = x.CollectionOpenDate,
                SubmissionDueDate = x.SubmissionDueDate,
                CollectionCloseDate = x.CollectionCloseDate,
                Status = x.Status,
                Notes = x.Notes,
                RespondentCaseCount = x.RespondentCases.Count,
                TemplateAssignmentCount = x.TemplateAssignments.Count,
                CreatedDate = x.CreatedDate,
                CreatedBy = x.CreatedBy,
                ModifiedDate = x.ModifiedDate,
                ModifiedBy = x.ModifiedBy
            })
            .ToListAsync();

        ViewBag.StatisticalProcessId = process.Id;
        ViewBag.StatisticalProcessCode = process.Code;
        ViewBag.StatisticalProcessName = process.Name;

        return View(cycles);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int statisticalProcessId)
    {
        var process = await _db.StatisticalProcesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == statisticalProcessId);

        if (process == null)
        {
            return NotFound();
        }

        var model = new ReportingCycleViewModel
        {
            StatisticalProcessId = process.Id,
            StatisticalProcessCode = process.Code,
            StatisticalProcessName = process.Name,
            Status = ReportingCycleStatus.Draft
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReportingCycleViewModel model)
    {
        var process = await _db.StatisticalProcesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == model.StatisticalProcessId);

        if (process == null)
        {
            return NotFound();
        }

        model.StatisticalProcessCode = process.Code;
        model.StatisticalProcessName = process.Name;

        ValidateDates(model);

        if (!string.IsNullOrWhiteSpace(model.Code))
        {
            var normalizedCode = model.Code.Trim();

            var duplicateExists = await _db.ReportingCycles
                .AnyAsync(x =>
                    x.StatisticalProcessId == model.StatisticalProcessId &&
                    x.Code == normalizedCode);

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    nameof(model.Code),
                    $"A reporting cycle with code '{normalizedCode}' already exists for {process.Name}.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var username = GetCurrentUsername();

            var cycle = new ReportingCycle
            {
                StatisticalProcessId = model.StatisticalProcessId,
                Code = model.Code.Trim(),
                Name = model.Name.Trim(),
                PeriodStartDate = model.PeriodStartDate.Date,
                PeriodEndDate = model.PeriodEndDate.Date,
                CollectionOpenDate = model.CollectionOpenDate?.Date,
                SubmissionDueDate = model.SubmissionDueDate?.Date,
                CollectionCloseDate = model.CollectionCloseDate?.Date,
                Status = model.Status,
                Notes = string.IsNullOrWhiteSpace(model.Notes)
                    ? null
                    : model.Notes.Trim(),
                CreatedDate = DateTime.UtcNow,
                CreatedBy = username
            };

            _db.ReportingCycles.Add(cycle);
            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"Reporting cycle '{cycle.Name}' was created successfully.";

            return RedirectToAction(
                nameof(Index),
                new { statisticalProcessId = model.StatisticalProcessId });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Database error creating reporting cycle {CycleCode} for process {ProcessId}",
                model.Code,
                model.StatisticalProcessId);

            ModelState.AddModelError(
                string.Empty,
                "The reporting cycle could not be saved. Please verify the values and try again.");

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error creating reporting cycle {CycleCode} for process {ProcessId}",
                model.Code,
                model.StatisticalProcessId);

            ModelState.AddModelError(
                string.Empty,
                "An unexpected error occurred while creating the reporting cycle.");

            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var cycle = await _db.ReportingCycles
            .AsNoTracking()
            .Include(x => x.StatisticalProcess)
            .Include(x => x.RespondentCases)
            .Include(x => x.TemplateAssignments)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (cycle == null)
        {
            return NotFound();
        }

        var model = MapToViewModel(cycle);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var cycle = await _db.ReportingCycles
            .AsNoTracking()
            .Include(x => x.StatisticalProcess)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (cycle == null)
        {
            return NotFound();
        }

        var model = MapToViewModel(cycle);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ReportingCycleViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        var cycle = await _db.ReportingCycles
            .Include(x => x.StatisticalProcess)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (cycle == null)
        {
            return NotFound();
        }

        model.StatisticalProcessId = cycle.StatisticalProcessId;
        model.StatisticalProcessCode = cycle.StatisticalProcess.Code;
        model.StatisticalProcessName = cycle.StatisticalProcess.Name;

        ValidateDates(model);

        if (!string.IsNullOrWhiteSpace(model.Code))
        {
            var normalizedCode = model.Code.Trim();

            var duplicateExists = await _db.ReportingCycles
                .AnyAsync(x =>
                    x.Id != id &&
                    x.StatisticalProcessId == cycle.StatisticalProcessId &&
                    x.Code == normalizedCode);

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    nameof(model.Code),
                    $"A reporting cycle with code '{normalizedCode}' already exists for {cycle.StatisticalProcess.Name}.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            cycle.Code = model.Code.Trim();
            cycle.Name = model.Name.Trim();
            cycle.PeriodStartDate = model.PeriodStartDate.Date;
            cycle.PeriodEndDate = model.PeriodEndDate.Date;
            cycle.CollectionOpenDate = model.CollectionOpenDate?.Date;
            cycle.SubmissionDueDate = model.SubmissionDueDate?.Date;
            cycle.CollectionCloseDate = model.CollectionCloseDate?.Date;
            cycle.Status = model.Status;
            cycle.Notes = string.IsNullOrWhiteSpace(model.Notes)
                ? null
                : model.Notes.Trim();
            cycle.ModifiedDate = DateTime.UtcNow;
            cycle.ModifiedBy = GetCurrentUsername();

            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"Reporting cycle '{cycle.Name}' was updated successfully.";

            return RedirectToAction(
                nameof(Index),
                new { statisticalProcessId = cycle.StatisticalProcessId });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Database error updating reporting cycle {CycleId}",
                id);

            ModelState.AddModelError(
                string.Empty,
                "The reporting cycle could not be updated. Please verify the values and try again.");

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error updating reporting cycle {CycleId}",
                id);

            ModelState.AddModelError(
                string.Empty,
                "An unexpected error occurred while updating the reporting cycle.");

            return View(model);
        }
    }

    private void ValidateDates(ReportingCycleViewModel model)
    {
        if (model.PeriodEndDate < model.PeriodStartDate)
        {
            ModelState.AddModelError(
                nameof(model.PeriodEndDate),
                "Period end date cannot be earlier than the period start date.");
        }

        if (model.CollectionOpenDate.HasValue &&
            model.SubmissionDueDate.HasValue &&
            model.SubmissionDueDate.Value.Date <
            model.CollectionOpenDate.Value.Date)
        {
            ModelState.AddModelError(
                nameof(model.SubmissionDueDate),
                "Submission due date cannot be earlier than the collection open date.");
        }

        if (model.CollectionOpenDate.HasValue &&
            model.CollectionCloseDate.HasValue &&
            model.CollectionCloseDate.Value.Date <
            model.CollectionOpenDate.Value.Date)
        {
            ModelState.AddModelError(
                nameof(model.CollectionCloseDate),
                "Collection close date cannot be earlier than the collection open date.");
        }

        if (model.SubmissionDueDate.HasValue &&
            model.CollectionCloseDate.HasValue &&
            model.CollectionCloseDate.Value.Date <
            model.SubmissionDueDate.Value.Date)
        {
            ModelState.AddModelError(
                nameof(model.CollectionCloseDate),
                "Collection close date cannot be earlier than the submission due date.");
        }
    }

    private string GetCurrentUsername()
    {
        if (User?.Identity?.IsAuthenticated == true &&
            !string.IsNullOrWhiteSpace(User.Identity.Name))
        {
            return User.Identity.Name;
        }

        return "system";
    }

    private static ReportingCycleViewModel MapToViewModel(
        ReportingCycle cycle)
    {
        return new ReportingCycleViewModel
        {
            Id = cycle.Id,
            StatisticalProcessId = cycle.StatisticalProcessId,
            StatisticalProcessCode = cycle.StatisticalProcess?.Code
                ?? string.Empty,
            StatisticalProcessName = cycle.StatisticalProcess?.Name
                ?? string.Empty,
            Code = cycle.Code,
            Name = cycle.Name,
            PeriodStartDate = cycle.PeriodStartDate,
            PeriodEndDate = cycle.PeriodEndDate,
            CollectionOpenDate = cycle.CollectionOpenDate,
            SubmissionDueDate = cycle.SubmissionDueDate,
            CollectionCloseDate = cycle.CollectionCloseDate,
            Status = cycle.Status,
            Notes = cycle.Notes,
            RespondentCaseCount = cycle.RespondentCases?.Count ?? 0,
            TemplateAssignmentCount =
                cycle.TemplateAssignments?.Count ?? 0,
            CreatedDate = cycle.CreatedDate,
            CreatedBy = cycle.CreatedBy,
            ModifiedDate = cycle.ModifiedDate,
            ModifiedBy = cycle.ModifiedBy
        };
    }
}
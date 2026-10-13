using ISSService.Data.Configurations;
using ISSService.Data.Entities;
using ISSService.Web.Models.CycleTemplateAssignments;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ISSService.Web.Controllers;

public class CycleTemplateAssignmentsController : Controller
{
    private readonly ISSServiceDbContext _db;
    private readonly ILogger<CycleTemplateAssignmentsController> _logger;

    public CycleTemplateAssignmentsController(
        ISSServiceDbContext db,
        ILogger<CycleTemplateAssignmentsController> logger)
    {
        _db = db;
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

        var assignments = await _db.CycleTemplateAssignments
            .AsNoTracking()
            .Where(x => x.ReportingCycleId == reportingCycleId)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x =>
                x.DataCollectionTemplateVersion
                    .DataCollectionTemplate.Code)
            .ThenByDescending(x =>
                x.DataCollectionTemplateVersion.VersionNumber)
            .Select(x => new CycleTemplateAssignmentViewModel
            {
                Id = x.Id,
                ReportingCycleId = x.ReportingCycleId,
                StatisticalProcessId =
                    x.ReportingCycle.StatisticalProcessId,
                StatisticalProcessCode =
                    x.ReportingCycle.StatisticalProcess.Code,
                StatisticalProcessName =
                    x.ReportingCycle.StatisticalProcess.Name,
                ReportingCycleCode =
                    x.ReportingCycle.Code,
                ReportingCycleName =
                    x.ReportingCycle.Name,
                PeriodStartDate =
                    x.ReportingCycle.PeriodStartDate,
                PeriodEndDate =
                    x.ReportingCycle.PeriodEndDate,
                DataCollectionTemplateVersionId =
                    x.DataCollectionTemplateVersionId,
                IsPrimary = x.IsPrimary,
                IsActive = x.IsActive,
                AssignedDate = x.AssignedDate,
                AssignedBy = x.AssignedBy,
                TemplateCode =
                    x.DataCollectionTemplateVersion
                        .DataCollectionTemplate.Code,
                TemplateName =
                    x.DataCollectionTemplateVersion
                        .DataCollectionTemplate.Name,
                VersionNumber =
                    x.DataCollectionTemplateVersion.VersionNumber,
                VersionLabel =
                    x.DataCollectionTemplateVersion.VersionLabel,
                VersionStatus =
                    x.DataCollectionTemplateVersion.Status,
                FileName =
                    x.DataCollectionTemplateVersion.FileName
            })
            .ToListAsync();

        ViewBag.ReportingCycleId = cycle.Id;
        ViewBag.ReportingCycleCode = cycle.Code;
        ViewBag.ReportingCycleName = cycle.Name;
        ViewBag.StatisticalProcessId =
            cycle.StatisticalProcessId;
        ViewBag.StatisticalProcessCode =
            cycle.StatisticalProcess.Code;
        ViewBag.StatisticalProcessName =
            cycle.StatisticalProcess.Name;
        ViewBag.PeriodStartDate =
            cycle.PeriodStartDate;
        ViewBag.PeriodEndDate =
            cycle.PeriodEndDate;

        return View(assignments);
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

        var model = new CycleTemplateAssignmentViewModel
        {
            ReportingCycleId = cycle.Id,
            StatisticalProcessId =
                cycle.StatisticalProcessId,
            StatisticalProcessCode =
                cycle.StatisticalProcess.Code,
            StatisticalProcessName =
                cycle.StatisticalProcess.Name,
            ReportingCycleCode = cycle.Code,
            ReportingCycleName = cycle.Name,
            PeriodStartDate = cycle.PeriodStartDate,
            PeriodEndDate = cycle.PeriodEndDate,
            IsPrimary = true,
            IsActive = true
        };

        await PopulateAvailableTemplateVersionsAsync(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CycleTemplateAssignmentViewModel model)
    {
        var cycle = await _db.ReportingCycles
            .Include(x => x.StatisticalProcess)
            .FirstOrDefaultAsync(
                x => x.Id == model.ReportingCycleId);

        if (cycle == null)
        {
            return NotFound();
        }

        PopulateCycleDetails(model, cycle);

        var version = await _db.DataCollectionTemplateVersions
            .AsNoTracking()
            .Include(x => x.DataCollectionTemplate)
            .FirstOrDefaultAsync(
                x =>
                    x.Id ==
                    model.DataCollectionTemplateVersionId);

        if (version == null)
        {
            ModelState.AddModelError(
                nameof(model.DataCollectionTemplateVersionId),
                "The selected template version could not be found.");
        }
        else if (
            version.DataCollectionTemplate.StatisticalProcessId !=
            cycle.StatisticalProcessId)
        {
            ModelState.AddModelError(
                nameof(model.DataCollectionTemplateVersionId),
                "The selected template version does not belong to " +
                "the same statistical process as this reporting cycle.");
        }
        else if (!version.DataCollectionTemplate.IsActive)
        {
            ModelState.AddModelError(
                nameof(model.DataCollectionTemplateVersionId),
                "The selected master template is inactive.");
        }

        if (version != null)
        {
            var duplicateExists =
                await _db.CycleTemplateAssignments
                    .AnyAsync(x =>
                        x.ReportingCycleId ==
                            model.ReportingCycleId &&
                        x.DataCollectionTemplateVersionId ==
                            model.DataCollectionTemplateVersionId);

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    nameof(model.DataCollectionTemplateVersionId),
                    "This template version is already assigned to " +
                    "the reporting cycle.");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateAvailableTemplateVersionsAsync(model);
            return View(model);
        }

        try
        {
            var username = GetCurrentUsername();

            if (model.IsPrimary)
            {
                var existingPrimaryAssignments =
                    await _db.CycleTemplateAssignments
                        .Where(x =>
                            x.ReportingCycleId ==
                                model.ReportingCycleId &&
                            x.IsPrimary)
                        .ToListAsync();

                foreach (var existingPrimary in
                         existingPrimaryAssignments)
                {
                    existingPrimary.IsPrimary = false;
                    existingPrimary.ModifiedDate =
                        DateTime.UtcNow;
                    existingPrimary.ModifiedBy =
                        username;
                }
            }

            var assignment =
                new CycleTemplateAssignment
                {
                    ReportingCycleId =
                        model.ReportingCycleId,
                    DataCollectionTemplateVersionId =
                        model.DataCollectionTemplateVersionId,
                    IsPrimary = model.IsPrimary,
                    IsActive = model.IsActive,
                    AssignedDate = DateTime.UtcNow,
                    AssignedBy = username,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = username
                };

            _db.CycleTemplateAssignments.Add(assignment);

            await _db.SaveChangesAsync();

            TempData["Success"] =
                "Template version was assigned to " +
                $"reporting cycle '{cycle.Name}' successfully.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    reportingCycleId =
                        model.ReportingCycleId
                });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Database error assigning template version " +
                "{TemplateVersionId} to reporting cycle {CycleId}.",
                model.DataCollectionTemplateVersionId,
                model.ReportingCycleId);

            ModelState.AddModelError(
                string.Empty,
                "The template version could not be assigned. " +
                "Please verify the selection and try again.");

            await PopulateAvailableTemplateVersionsAsync(model);

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error assigning template version " +
                "{TemplateVersionId} to reporting cycle {CycleId}.",
                model.DataCollectionTemplateVersionId,
                model.ReportingCycleId);

            ModelState.AddModelError(
                string.Empty,
                "An unexpected error occurred while assigning " +
                "the template version.");

            await PopulateAvailableTemplateVersionsAsync(model);

            return View(model);
        }
    }

    private async Task PopulateAvailableTemplateVersionsAsync(
        CycleTemplateAssignmentViewModel model)
    {
        var alreadyAssignedVersionIds =
            await _db.CycleTemplateAssignments
                .AsNoTracking()
                .Where(x =>
                    x.ReportingCycleId ==
                    model.ReportingCycleId)
                .Select(x =>
                    x.DataCollectionTemplateVersionId)
                .ToListAsync();

        var versions =
            await _db.DataCollectionTemplateVersions
                .AsNoTracking()
                .Where(x =>
                    x.DataCollectionTemplate
                        .StatisticalProcessId ==
                        model.StatisticalProcessId &&
                    x.DataCollectionTemplate.IsActive &&
                    !alreadyAssignedVersionIds.Contains(x.Id))
                .OrderBy(x =>
                    x.DataCollectionTemplate.Code)
                .ThenByDescending(x =>
                    x.VersionNumber)
                .Select(x => new
                {
                    x.Id,
                    TemplateCode =
                        x.DataCollectionTemplate.Code,
                    TemplateName =
                        x.DataCollectionTemplate.Name,
                    x.VersionNumber,
                    x.VersionLabel,
                    x.Status
                })
                .ToListAsync();

        model.AvailableTemplateVersions =
            versions.Select(x =>
                new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text =
                        $"{x.TemplateCode} - " +
                        $"{x.TemplateName} - " +
                        $"Version {x.VersionNumber} " +
                        $"({x.VersionLabel}, {x.Status})"
                })
                .ToList();
    }

    private static void PopulateCycleDetails(
        CycleTemplateAssignmentViewModel model,
        ReportingCycle cycle)
    {
        model.StatisticalProcessId =
            cycle.StatisticalProcessId;
        model.StatisticalProcessCode =
            cycle.StatisticalProcess.Code;
        model.StatisticalProcessName =
            cycle.StatisticalProcess.Name;
        model.ReportingCycleCode =
            cycle.Code;
        model.ReportingCycleName =
            cycle.Name;
        model.PeriodStartDate =
            cycle.PeriodStartDate;
        model.PeriodEndDate =
            cycle.PeriodEndDate;
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
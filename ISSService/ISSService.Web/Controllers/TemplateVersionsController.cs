using ISSService.Data.Configurations;
using ISSService.Data.Entities;
using ISSService.Web.Models.TemplateVersions;
using ISSService.Web.Services.Templates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISSService.Web.Controllers;

public class TemplateVersionsController : Controller
{
    private readonly ISSServiceDbContext _db;
    private readonly IScannedTemplateStorageService _storageService;
    private readonly ILogger<TemplateVersionsController> _logger;

    public TemplateVersionsController(
        ISSServiceDbContext db,
        IScannedTemplateStorageService storageService,
        ILogger<TemplateVersionsController> logger)
    {
        _db = db;
        _storageService = storageService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int dataCollectionTemplateId)
    {
        var template = await _db.DataCollectionTemplates
            .AsNoTracking()
            .Include(x => x.StatisticalProcess)
            .FirstOrDefaultAsync(
                x => x.Id == dataCollectionTemplateId);

        if (template == null)
        {
            return NotFound();
        }

        var versions = await _db.DataCollectionTemplateVersions
            .AsNoTracking()
            .Where(x =>
                x.DataCollectionTemplateId ==
                dataCollectionTemplateId)
            .OrderByDescending(x => x.VersionNumber)
            .Select(x => new TemplateVersionViewModel
            {
                Id = x.Id,
                DataCollectionTemplateId =
                    x.DataCollectionTemplateId,
                StatisticalProcessId =
                    x.DataCollectionTemplate.StatisticalProcessId,
                StatisticalProcessCode =
                    x.DataCollectionTemplate.StatisticalProcess.Code,
                StatisticalProcessName =
                    x.DataCollectionTemplate.StatisticalProcess.Name,
                TemplateCode =
                    x.DataCollectionTemplate.Code,
                TemplateName =
                    x.DataCollectionTemplate.Name,
                VersionNumber = x.VersionNumber,
                VersionLabel = x.VersionLabel,
                EffectiveFrom = x.EffectiveFrom,
                EffectiveTo = x.EffectiveTo,
                Status = x.Status,
                ChangeNotes = x.ChangeNotes,
                FileName = x.FileName,
                FilePath = x.FilePath,
                FileHash = x.FileHash,
                CreatedDate = x.CreatedDate,
                CreatedBy = x.CreatedBy,
                ApprovedDate = x.ApprovedDate,
                ApprovedBy = x.ApprovedBy,
                CycleAssignmentCount =
                    x.CycleAssignments.Count
            })
            .ToListAsync();

        ViewBag.DataCollectionTemplateId = template.Id;
        ViewBag.TemplateCode = template.Code;
        ViewBag.TemplateName = template.Name;
        ViewBag.StatisticalProcessId =
            template.StatisticalProcessId;
        ViewBag.StatisticalProcessCode =
            template.StatisticalProcess.Code;
        ViewBag.StatisticalProcessName =
            template.StatisticalProcess.Name;

        return View(versions);
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        int dataCollectionTemplateId)
    {
        var template = await _db.DataCollectionTemplates
            .AsNoTracking()
            .Include(x => x.StatisticalProcess)
            .FirstOrDefaultAsync(
                x => x.Id == dataCollectionTemplateId);

        if (template == null)
        {
            return NotFound();
        }

        var highestVersionNumber =
            await _db.DataCollectionTemplateVersions
                .AsNoTracking()
                .Where(x =>
                    x.DataCollectionTemplateId ==
                    dataCollectionTemplateId)
                .Select(x => (int?)x.VersionNumber)
                .MaxAsync() ?? 0;

        var nextVersionNumber = highestVersionNumber + 1;

        var model = new TemplateVersionViewModel
        {
            DataCollectionTemplateId = template.Id,
            StatisticalProcessId =
                template.StatisticalProcessId,
            StatisticalProcessCode =
                template.StatisticalProcess.Code,
            StatisticalProcessName =
                template.StatisticalProcess.Name,
            TemplateCode = template.Code,
            TemplateName = template.Name,
            VersionNumber = nextVersionNumber,
            VersionLabel = $"Version {nextVersionNumber}",
            EffectiveFrom = DateTime.Today,
            Status = TemplateVersionStatus.Draft
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        TemplateVersionViewModel model,
        CancellationToken cancellationToken)
    {
        NormalizeModel(model);

        var template = await _db.DataCollectionTemplates
            .AsNoTracking()
            .Include(x => x.StatisticalProcess)
            .FirstOrDefaultAsync(
                x => x.Id == model.DataCollectionTemplateId,
                cancellationToken);

        if (template == null)
        {
            return NotFound();
        }

        PopulateTemplateDetails(model, template);

        ValidateDates(model);

        if (model.TemplateFile == null ||
            model.TemplateFile.Length == 0)
        {
            ModelState.AddModelError(
                nameof(model.TemplateFile),
                "Please select the template workbook to upload.");
        }

        if (await VersionNumberExistsAsync(
                model.DataCollectionTemplateId,
                model.VersionNumber,
                cancellationToken))
        {
            ModelState.AddModelError(
                nameof(model.VersionNumber),
                "This version number already exists for the template.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ScannedTemplateStorageResult? storedFile = null;

        try
        {
            storedFile =
                await _storageService.ScanAndSaveAsync(
                    model.TemplateFile!,
                    cancellationToken);

            if (!storedFile.Success)
            {
                ModelState.AddModelError(
                    nameof(model.TemplateFile),
                    storedFile.Error ??
                    "The template workbook could not be stored.");

                return View(model);
            }

            var username = GetCurrentUsername();

            var version = new DataCollectionTemplateVersion
            {
                DataCollectionTemplateId =
                    model.DataCollectionTemplateId,
                VersionNumber = model.VersionNumber,
                VersionLabel = model.VersionLabel,
                EffectiveFrom = model.EffectiveFrom,
                EffectiveTo = model.EffectiveTo,
                Status = model.Status,
                FileName = storedFile.OriginalFileName,
                FilePath = storedFile.FilePath,
                FileHash = storedFile.FileHash,
                ChangeNotes = model.ChangeNotes,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = username
            };

            if (model.Status == TemplateVersionStatus.Approved ||
                model.Status == TemplateVersionStatus.Active)
            {
                version.ApprovedDate = DateTime.UtcNow;
                version.ApprovedBy = username;
            }

            _db.DataCollectionTemplateVersions.Add(version);

            await _db.SaveChangesAsync(cancellationToken);

            TempData["Success"] =
                $"Template version '{version.VersionLabel}' " +
                "was created successfully.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    dataCollectionTemplateId =
                        version.DataCollectionTemplateId
                });
        }
        catch (DbUpdateException ex)
        {
            if (storedFile?.Success == true)
            {
                await TryDeleteStoredFileAsync(
                    storedFile.FilePath);
            }

            _logger.LogError(
                ex,
                "Database error while creating template version " +
                "{VersionNumber} for template {TemplateId}.",
                model.VersionNumber,
                model.DataCollectionTemplateId);

            ModelState.AddModelError(
                string.Empty,
                "The template version could not be saved. " +
                "The uploaded file has not been retained.");

            return View(model);
        }
        catch (OperationCanceledException)
        {
            if (storedFile?.Success == true)
            {
                await TryDeleteStoredFileAsync(
                    storedFile.FilePath);
            }

            throw;
        }
        catch (Exception ex)
        {
            if (storedFile?.Success == true)
            {
                await TryDeleteStoredFileAsync(
                    storedFile.FilePath);
            }

            _logger.LogError(
                ex,
                "Error while creating template version " +
                "{VersionNumber} for template {TemplateId}.",
                model.VersionNumber,
                model.DataCollectionTemplateId);

            ModelState.AddModelError(
                string.Empty,
                "An unexpected error occurred while creating the " +
                "template version. The uploaded file has not been retained.");

            return View(model);
        }
    }

    private async Task<bool> VersionNumberExistsAsync(
        int templateId,
        int versionNumber,
        CancellationToken cancellationToken)
    {
        return await _db.DataCollectionTemplateVersions
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.DataCollectionTemplateId == templateId &&
                    x.VersionNumber == versionNumber,
                cancellationToken);
    }

    private static void PopulateTemplateDetails(
        TemplateVersionViewModel model,
        DataCollectionTemplate template)
    {
        model.StatisticalProcessId =
            template.StatisticalProcessId;
        model.StatisticalProcessCode =
            template.StatisticalProcess.Code;
        model.StatisticalProcessName =
            template.StatisticalProcess.Name;
        model.TemplateCode = template.Code;
        model.TemplateName = template.Name;
    }

    private static void NormalizeModel(
        TemplateVersionViewModel model)
    {
        model.VersionLabel =
            model.VersionLabel?.Trim() ?? string.Empty;

        model.ChangeNotes =
            string.IsNullOrWhiteSpace(model.ChangeNotes)
                ? null
                : model.ChangeNotes.Trim();
    }

    private void ValidateDates(
        TemplateVersionViewModel model)
    {
        if (model.EffectiveTo.HasValue &&
            model.EffectiveTo.Value.Date <
            model.EffectiveFrom.Date)
        {
            ModelState.AddModelError(
                nameof(model.EffectiveTo),
                "Effective To cannot be earlier than Effective From.");
        }
    }

    private async Task TryDeleteStoredFileAsync(
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
                "Could not clean up stored template file {FilePath} " +
                "after template-version persistence failed.",
                filePath);
        }
    }

    private string GetCurrentUsername()
    {
        return User?.Identity?.Name
               ?? "development-user";
    }
}
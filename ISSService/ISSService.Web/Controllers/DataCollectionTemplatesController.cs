using ISSService.Data.Configurations;
using ISSService.Data.Entities;
using ISSService.Web.Models.DataCollectionTemplates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISSService.Web.Controllers;

public class DataCollectionTemplatesController : Controller
{
    private readonly ISSServiceDbContext _db;
    private readonly ILogger<DataCollectionTemplatesController> _logger;

    public DataCollectionTemplatesController(
        ISSServiceDbContext db,
        ILogger<DataCollectionTemplatesController> logger)
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

        var templates = await _db.DataCollectionTemplates
            .AsNoTracking()
            .Where(x => x.StatisticalProcessId == statisticalProcessId)
            .OrderBy(x => x.Name)
            .Select(x => new DataCollectionTemplateViewModel
            {
                Id = x.Id,
                StatisticalProcessId = x.StatisticalProcessId,
                StatisticalProcessCode = process.Code,
                StatisticalProcessName = process.Name,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                TemplateType = x.TemplateType,
                IsRespondentSpecific = x.IsRespondentSpecific,
                SupportsPreviousPeriodData =
                    x.SupportsPreviousPeriodData,
                IsActive = x.IsActive,
                CreatedDate = x.CreatedDate,
                CreatedBy = x.CreatedBy,
                ModifiedDate = x.ModifiedDate,
                ModifiedBy = x.ModifiedBy,
                VersionCount = x.Versions.Count,
                ActiveVersionCount = x.Versions.Count(v =>
                    v.Status == TemplateVersionStatus.Active)
            })
            .ToListAsync();

        ViewBag.StatisticalProcessId = process.Id;
        ViewBag.StatisticalProcessCode = process.Code;
        ViewBag.StatisticalProcessName = process.Name;

        return View(templates);
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

        var model = new DataCollectionTemplateViewModel
        {
            StatisticalProcessId = process.Id,
            StatisticalProcessCode = process.Code,
            StatisticalProcessName = process.Name,
            TemplateType = DataCollectionTemplateType.ExcelTemplate,
            IsActive = true
        };

        // SACCO templates are respondent-specific and carry
        // previous-period positions forward into the next return.
        if (string.Equals(
                process.Code,
                "SACCO",
                StringComparison.OrdinalIgnoreCase))
        {
            model.IsRespondentSpecific = true;
            model.SupportsPreviousPeriodData = true;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        DataCollectionTemplateViewModel model)
    {
        NormalizeModel(model);

        var process = await _db.StatisticalProcesses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == model.StatisticalProcessId);

        if (process == null)
        {
            return NotFound();
        }

        PopulateProcessDetails(model, process);

        if (await CodeExistsAsync(
                model.StatisticalProcessId,
                model.Code))
        {
            ModelState.AddModelError(
                nameof(model.Code),
                "A data collection template with this code " +
                "already exists for this statistical process.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var username = GetCurrentUsername();

            var template = new DataCollectionTemplate
            {
                StatisticalProcessId = model.StatisticalProcessId,
                Code = model.Code,
                Name = model.Name,
                Description = model.Description,
                TemplateType = model.TemplateType,
                IsRespondentSpecific = model.IsRespondentSpecific,
                SupportsPreviousPeriodData =
                    model.SupportsPreviousPeriodData,
                IsActive = model.IsActive,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = username
            };

            _db.DataCollectionTemplates.Add(template);
            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"Data collection template '{template.Name}' " +
                "was created successfully.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    statisticalProcessId =
                        template.StatisticalProcessId
                });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Database error while creating data collection " +
                "template {Code} for process {ProcessId}.",
                model.Code,
                model.StatisticalProcessId);

            ModelState.AddModelError(
                string.Empty,
                "The data collection template could not be saved. " +
                "Please verify that the template code is unique " +
                "within this statistical process.");

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error while creating data collection template " +
                "{Code} for process {ProcessId}.",
                model.Code,
                model.StatisticalProcessId);

            ModelState.AddModelError(
                string.Empty,
                "An unexpected error occurred while creating the " +
                "data collection template.");

            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var template = await _db.DataCollectionTemplates
            .AsNoTracking()
            .Include(x => x.StatisticalProcess)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (template == null)
        {
            return NotFound();
        }

        var model = new DataCollectionTemplateViewModel
        {
            Id = template.Id,
            StatisticalProcessId = template.StatisticalProcessId,
            StatisticalProcessCode =
                template.StatisticalProcess.Code,
            StatisticalProcessName =
                template.StatisticalProcess.Name,
            Code = template.Code,
            Name = template.Name,
            Description = template.Description,
            TemplateType = template.TemplateType,
            IsRespondentSpecific =
                template.IsRespondentSpecific,
            SupportsPreviousPeriodData =
                template.SupportsPreviousPeriodData,
            IsActive = template.IsActive,
            CreatedDate = template.CreatedDate,
            CreatedBy = template.CreatedBy,
            ModifiedDate = template.ModifiedDate,
            ModifiedBy = template.ModifiedBy
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        DataCollectionTemplateViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        NormalizeModel(model);

        var process = await _db.StatisticalProcesses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == model.StatisticalProcessId);

        if (process == null)
        {
            return NotFound();
        }

        PopulateProcessDetails(model, process);

        if (await CodeExistsAsync(
                model.StatisticalProcessId,
                model.Code,
                model.Id))
        {
            ModelState.AddModelError(
                nameof(model.Code),
                "A data collection template with this code " +
                "already exists for this statistical process.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var template = await _db.DataCollectionTemplates
                .FirstOrDefaultAsync(x => x.Id == id);

            if (template == null)
            {
                return NotFound();
            }

            // A template must remain under the process where it
            // was originally created.
            if (template.StatisticalProcessId !=
                model.StatisticalProcessId)
            {
                return BadRequest();
            }

            template.Code = model.Code;
            template.Name = model.Name;
            template.Description = model.Description;
            template.TemplateType = model.TemplateType;
            template.IsRespondentSpecific =
                model.IsRespondentSpecific;
            template.SupportsPreviousPeriodData =
                model.SupportsPreviousPeriodData;
            template.IsActive = model.IsActive;
            template.ModifiedDate = DateTime.UtcNow;
            template.ModifiedBy = GetCurrentUsername();

            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"Data collection template '{template.Name}' " +
                "was updated successfully.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    statisticalProcessId =
                        template.StatisticalProcessId
                });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Database error while updating data collection " +
                "template {Id}.",
                id);

            ModelState.AddModelError(
                string.Empty,
                "The data collection template could not be updated. " +
                "Please verify that the template code is unique " +
                "within this statistical process.");

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error while updating data collection template {Id}.",
                id);

            ModelState.AddModelError(
                string.Empty,
                "An unexpected error occurred while updating the " +
                "data collection template.");

            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _db.DataCollectionTemplates
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new DataCollectionTemplateViewModel
            {
                Id = x.Id,
                StatisticalProcessId = x.StatisticalProcessId,
                StatisticalProcessCode =
                    x.StatisticalProcess.Code,
                StatisticalProcessName =
                    x.StatisticalProcess.Name,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                TemplateType = x.TemplateType,
                IsRespondentSpecific =
                    x.IsRespondentSpecific,
                SupportsPreviousPeriodData =
                    x.SupportsPreviousPeriodData,
                IsActive = x.IsActive,
                CreatedDate = x.CreatedDate,
                CreatedBy = x.CreatedBy,
                ModifiedDate = x.ModifiedDate,
                ModifiedBy = x.ModifiedBy,
                VersionCount = x.Versions.Count,
                ActiveVersionCount = x.Versions.Count(v =>
                    v.Status == TemplateVersionStatus.Active)
            })
            .FirstOrDefaultAsync();

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    private async Task<bool> CodeExistsAsync(
        int statisticalProcessId,
        string code,
        int? excludedId = null)
    {
        var query = _db.DataCollectionTemplates
            .AsNoTracking()
            .Where(x =>
                x.StatisticalProcessId == statisticalProcessId &&
                x.Code == code);

        if (excludedId.HasValue)
        {
            query = query.Where(
                x => x.Id != excludedId.Value);
        }

        return await query.AnyAsync();
    }

    private static void PopulateProcessDetails(
        DataCollectionTemplateViewModel model,
        StatisticalProcess process)
    {
        model.StatisticalProcessCode = process.Code;
        model.StatisticalProcessName = process.Name;
    }

    private static void NormalizeModel(
        DataCollectionTemplateViewModel model)
    {
        model.Code = model.Code?.Trim().ToUpperInvariant()
                     ?? string.Empty;

        model.Name = model.Name?.Trim()
                     ?? string.Empty;

        model.Description =
            string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description.Trim();
    }

    private string GetCurrentUsername()
    {
        return User?.Identity?.Name
               ?? "development-user";
    }
}
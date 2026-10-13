using ISSService.Data.Configurations;
using ISSService.Data.Entities;
using ISSService.Web.Models.StatisticalProcesses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISSService.Web.Controllers;

public class StatisticalProcessesController : Controller
{
    private readonly ISSServiceDbContext _db;
    private readonly ILogger<StatisticalProcessesController> _logger;

    public StatisticalProcessesController(
        ISSServiceDbContext db,
        ILogger<StatisticalProcessesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var processes = await _db.StatisticalProcesses
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new StatisticalProcessViewModel
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                ProcessType = x.ProcessType,
                CollectionMode = x.CollectionMode,
                ReportingFrequency = x.ReportingFrequency,
                ResponsibleUnit = x.ResponsibleUnit,
                IsActive = x.IsActive,
                CreatedDate = x.CreatedDate,
                CreatedBy = x.CreatedBy,
                ModifiedDate = x.ModifiedDate,
                ModifiedBy = x.ModifiedBy,
                ReportingCycleCount = x.ReportingCycles.Count,
                TemplateCount = x.DataCollectionTemplates.Count
            })
            .ToListAsync();

        return View(processes);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new StatisticalProcessViewModel
        {
            IsActive = true,
            ProcessType = StatisticalProcessType.InstitutionalReturn,
            CollectionMode = CollectionMode.TemplateUpload,
            ReportingFrequency = ReportingFrequency.Quarterly
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        StatisticalProcessViewModel model)
    {
        NormalizeModel(model);

        if (await CodeExistsAsync(model.Code))
        {
            ModelState.AddModelError(
                nameof(model.Code),
                "A statistical process with this code already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var username = GetCurrentUsername();

            var process = new StatisticalProcess
            {
                Code = model.Code,
                Name = model.Name,
                Description = model.Description,
                ProcessType = model.ProcessType,
                CollectionMode = model.CollectionMode,
                ReportingFrequency = model.ReportingFrequency,
                ResponsibleUnit = model.ResponsibleUnit,
                IsActive = model.IsActive,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = username
            };

            _db.StatisticalProcesses.Add(process);
            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"Statistical process '{process.Name}' was created successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Database error while creating statistical process {Code}.",
                model.Code);

            ModelState.AddModelError(
                string.Empty,
                "The statistical process could not be saved. " +
                "Please verify that the process code is unique.");

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error while creating statistical process {Code}.",
                model.Code);

            ModelState.AddModelError(
                string.Empty,
                "An unexpected error occurred while creating the " +
                "statistical process.");

            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var process = await _db.StatisticalProcesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (process == null)
        {
            return NotFound();
        }

        var model = new StatisticalProcessViewModel
        {
            Id = process.Id,
            Code = process.Code,
            Name = process.Name,
            Description = process.Description,
            ProcessType = process.ProcessType,
            CollectionMode = process.CollectionMode,
            ReportingFrequency = process.ReportingFrequency,
            ResponsibleUnit = process.ResponsibleUnit,
            IsActive = process.IsActive,
            CreatedDate = process.CreatedDate,
            CreatedBy = process.CreatedBy,
            ModifiedDate = process.ModifiedDate,
            ModifiedBy = process.ModifiedBy
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        StatisticalProcessViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        NormalizeModel(model);

        if (await CodeExistsAsync(model.Code, model.Id))
        {
            ModelState.AddModelError(
                nameof(model.Code),
                "A statistical process with this code already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var process = await _db.StatisticalProcesses
                .FirstOrDefaultAsync(x => x.Id == id);

            if (process == null)
            {
                return NotFound();
            }

            process.Code = model.Code;
            process.Name = model.Name;
            process.Description = model.Description;
            process.ProcessType = model.ProcessType;
            process.CollectionMode = model.CollectionMode;
            process.ReportingFrequency = model.ReportingFrequency;
            process.ResponsibleUnit = model.ResponsibleUnit;
            process.IsActive = model.IsActive;
            process.ModifiedDate = DateTime.UtcNow;
            process.ModifiedBy = GetCurrentUsername();

            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"Statistical process '{process.Name}' was updated successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Database error while updating statistical process {Id}.",
                id);

            ModelState.AddModelError(
                string.Empty,
                "The statistical process could not be updated. " +
                "Please verify that the process code is unique.");

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error while updating statistical process {Id}.",
                id);

            ModelState.AddModelError(
                string.Empty,
                "An unexpected error occurred while updating the " +
                "statistical process.");

            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _db.StatisticalProcesses
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new StatisticalProcessViewModel
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                ProcessType = x.ProcessType,
                CollectionMode = x.CollectionMode,
                ReportingFrequency = x.ReportingFrequency,
                ResponsibleUnit = x.ResponsibleUnit,
                IsActive = x.IsActive,
                CreatedDate = x.CreatedDate,
                CreatedBy = x.CreatedBy,
                ModifiedDate = x.ModifiedDate,
                ModifiedBy = x.ModifiedBy,
                ReportingCycleCount = x.ReportingCycles.Count,
                TemplateCount = x.DataCollectionTemplates.Count
            })
            .FirstOrDefaultAsync();

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    private async Task<bool> CodeExistsAsync(
        string code,
        int? excludedId = null)
    {
        var query = _db.StatisticalProcesses
            .AsNoTracking()
            .Where(x => x.Code == code);

        if (excludedId.HasValue)
        {
            query = query.Where(x => x.Id != excludedId.Value);
        }

        return await query.AnyAsync();
    }

    private static void NormalizeModel(
        StatisticalProcessViewModel model)
    {
        model.Code = model.Code?.Trim().ToUpperInvariant()
                     ?? string.Empty;

        model.Name = model.Name?.Trim()
                     ?? string.Empty;

        model.Description =
            string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description.Trim();

        model.ResponsibleUnit =
            string.IsNullOrWhiteSpace(model.ResponsibleUnit)
                ? null
                : model.ResponsibleUnit.Trim();
    }

    private string GetCurrentUsername()
    {
        return User?.Identity?.Name
               ?? "development-user";
    }
}
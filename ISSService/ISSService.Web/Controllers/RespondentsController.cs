using ISSService.Data.Configurations;
using ISSService.Data.Entities;
using ISSService.Web.Models.Respondents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISSService.Web.Controllers;

public class RespondentsController : Controller
{
    private readonly ISSServiceDbContext _db;
    private readonly ILogger<RespondentsController> _logger;

    public RespondentsController(
        ISSServiceDbContext db,
        ILogger<RespondentsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var respondents = await _db.Respondents
            .AsNoTracking()
            .OrderBy(x => x.RespondentType)
            .ThenBy(x => x.Code)
            .Select(x => new RespondentViewModel
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                RespondentType = x.RespondentType,
                Region = x.Region,
                District = x.District,
                Address = x.Address,
                PrimaryContactName = x.PrimaryContactName,
                PrimaryContactEmail = x.PrimaryContactEmail,
                PrimaryContactPhone = x.PrimaryContactPhone,
                IsActive = x.IsActive,
                RespondentCaseCount = x.RespondentCases.Count,
                CreatedDate = x.CreatedDate,
                CreatedBy = x.CreatedBy,
                ModifiedDate = x.ModifiedDate,
                ModifiedBy = x.ModifiedBy
            })
            .ToListAsync();

        return View(respondents);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var respondent = await _db.Respondents
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new RespondentViewModel
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                RespondentType = x.RespondentType,
                Region = x.Region,
                District = x.District,
                Address = x.Address,
                PrimaryContactName = x.PrimaryContactName,
                PrimaryContactEmail = x.PrimaryContactEmail,
                PrimaryContactPhone = x.PrimaryContactPhone,
                IsActive = x.IsActive,
                RespondentCaseCount = x.RespondentCases.Count,
                CreatedDate = x.CreatedDate,
                CreatedBy = x.CreatedBy,
                ModifiedDate = x.ModifiedDate,
                ModifiedBy = x.ModifiedBy
            })
            .FirstOrDefaultAsync();

        if (respondent == null)
        {
            return NotFound();
        }

        return View(respondent);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new RespondentViewModel
        {
            RespondentType = RespondentType.SACCO,
            IsActive = true
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        RespondentViewModel model)
    {
        NormalizeModel(model);

        if (await CodeExistsAsync(model.Code))
        {
            ModelState.AddModelError(
                nameof(model.Code),
                "A respondent with this code already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var username = GetCurrentUsername();

            var respondent = new Respondent
            {
                Code = model.Code,
                Name = model.Name,
                RespondentType = model.RespondentType,
                Region = model.Region,
                District = model.District,
                Address = model.Address,
                PrimaryContactName = model.PrimaryContactName,
                PrimaryContactEmail = model.PrimaryContactEmail,
                PrimaryContactPhone = model.PrimaryContactPhone,
                IsActive = model.IsActive,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = username
            };

            _db.Respondents.Add(respondent);

            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"Respondent '{respondent.Name}' was created successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Database error creating respondent {Code}.",
                model.Code);

            ModelState.AddModelError(
                string.Empty,
                "The respondent could not be created. " +
                "Please verify the details and try again.");

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error creating respondent {Code}.",
                model.Code);

            ModelState.AddModelError(
                string.Empty,
                "An unexpected error occurred while creating " +
                "the respondent.");

            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var respondent = await _db.Respondents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (respondent == null)
        {
            return NotFound();
        }

        var model = new RespondentViewModel
        {
            Id = respondent.Id,
            Code = respondent.Code,
            Name = respondent.Name,
            RespondentType = respondent.RespondentType,
            Region = respondent.Region,
            District = respondent.District,
            Address = respondent.Address,
            PrimaryContactName = respondent.PrimaryContactName,
            PrimaryContactEmail = respondent.PrimaryContactEmail,
            PrimaryContactPhone = respondent.PrimaryContactPhone,
            IsActive = respondent.IsActive,
            CreatedDate = respondent.CreatedDate,
            CreatedBy = respondent.CreatedBy,
            ModifiedDate = respondent.ModifiedDate,
            ModifiedBy = respondent.ModifiedBy
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        RespondentViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        NormalizeModel(model);

        if (await CodeExistsAsync(model.Code, model.Id))
        {
            ModelState.AddModelError(
                nameof(model.Code),
                "A respondent with this code already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var respondent = await _db.Respondents
            .FirstOrDefaultAsync(x => x.Id == id);

        if (respondent == null)
        {
            return NotFound();
        }

        try
        {
            respondent.Code = model.Code;
            respondent.Name = model.Name;
            respondent.RespondentType = model.RespondentType;
            respondent.Region = model.Region;
            respondent.District = model.District;
            respondent.Address = model.Address;
            respondent.PrimaryContactName =
                model.PrimaryContactName;
            respondent.PrimaryContactEmail =
                model.PrimaryContactEmail;
            respondent.PrimaryContactPhone =
                model.PrimaryContactPhone;
            respondent.IsActive = model.IsActive;
            respondent.ModifiedDate = DateTime.UtcNow;
            respondent.ModifiedBy = GetCurrentUsername();

            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"Respondent '{respondent.Name}' was updated successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Database error updating respondent {RespondentId}.",
                id);

            ModelState.AddModelError(
                string.Empty,
                "The respondent could not be updated. " +
                "Please verify the details and try again.");

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error updating respondent {RespondentId}.",
                id);

            ModelState.AddModelError(
                string.Empty,
                "An unexpected error occurred while updating " +
                "the respondent.");

            return View(model);
        }
    }

    private async Task<bool> CodeExistsAsync(
        string code,
        int? excludeId = null)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();

        return await _db.Respondents
            .AsNoTracking()
            .AnyAsync(x =>
                x.Code == normalizedCode &&
                (!excludeId.HasValue || x.Id != excludeId.Value));
    }

    private static void NormalizeModel(
        RespondentViewModel model)
    {
        model.Code =
            (model.Code ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

        model.Name =
            (model.Name ?? string.Empty).Trim();

        model.Region =
            NormalizeOptional(model.Region);

        model.District =
            NormalizeOptional(model.District);

        model.Address =
            NormalizeOptional(model.Address);

        model.PrimaryContactName =
            NormalizeOptional(model.PrimaryContactName);

        model.PrimaryContactEmail =
            NormalizeOptional(model.PrimaryContactEmail);

        model.PrimaryContactPhone =
            NormalizeOptional(model.PrimaryContactPhone);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
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
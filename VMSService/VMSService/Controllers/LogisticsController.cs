using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;
using VMSService.Controllers;
using VMSService.Core.Repositories.Logistics;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class LogisticsController : Controller
    {
        private readonly ILogisticsRepository _logisticsRepository;
        private readonly ILogger<LogisticsController> _logger;

        public LogisticsController(
            ILogisticsRepository logisticsRepository,
            ILogger<LogisticsController> logger)
        {
            _logisticsRepository = logisticsRepository;
            _logger = logger;
        }

        [Breadcrumb("Logistics", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public async Task<IActionResult> Index()
        {
            var records = await _logisticsRepository.GetAllAsync();
            return View(records);
        }

        [HttpGet]
        [Breadcrumb("Create", FromController = typeof(LogisticsController), FromAction = nameof(Index))]
        public IActionResult Create()
        {
            return View(new Data.Entities.Logistics());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] LogisticsCreateViewModel model)
        {
            if (model == null)
                return BadRequest(new { message = "Invalid request body." });

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserName = User.FindFirstValue("displayName") ?? User.Identity?.Name ?? "Unknown";

            var logistics = new Data.Entities.Logistics
            {
                Title = model.Title,
                Description = model.Description,
                ReferenceNumber = model.ReferenceNumber,
                Date = model.Date,
                CreatedBy = currentUserId,
                CreatedByName = currentUserName,
                CreatedDate = DateTime.UtcNow,
                Categories = model.Categories.Select((cat, catIndex) => new LogisticsCategory
                {
                    Name = cat.Name,
                    Description = cat.Description,
                    SortOrder = catIndex,
                    Details = cat.Details.Select((det, detIndex) => new LogisticsDetail
                    {
                        ItemName = det.ItemName,
                        Description = det.Description,
                        Quantity = det.Quantity,
                        UnitPrice = det.UnitPrice,
                        TotalPrice = det.TotalPrice,
                        Notes = det.Notes,
                        SortOrder = detIndex
                    }).ToList()
                }).ToList()
            };

            await _logisticsRepository.CreateAsync(logistics);

            return Json(new { success = true, logisticsId = logistics.Id, message = "Logistics record created successfully." });
        }

        [HttpGet]
        [Breadcrumb("Details", FromController = typeof(LogisticsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int id)
        {
            var logistics = await _logisticsRepository.GetByIdAsync(id);
            if (logistics == null)
                return NotFound();

            return View(logistics);
        }

        [HttpGet]
        [Breadcrumb("Edit", FromController = typeof(LogisticsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id)
        {
            var logistics = await _logisticsRepository.GetByIdAsync(id);
            if (logistics == null)
                return NotFound();

            return View(logistics);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromBody] LogisticsCreateViewModel model)
        {
            if (model == null)
                return BadRequest(new { message = "Invalid request body." });

            var existingLogistics = await _logisticsRepository.GetByIdAsync(id);
            if (existingLogistics == null)
                return NotFound(new { message = "Logistics record not found." });

            var logistics = new Data.Entities.Logistics
            {
                Id = id,
                Title = model.Title,
                Description = model.Description,
                ReferenceNumber = model.ReferenceNumber,
                Date = model.Date,
                Categories = model.Categories.Select((cat, catIndex) => new LogisticsCategory
                {
                    Name = cat.Name,
                    Description = cat.Description,
                    SortOrder = catIndex,
                    Details = cat.Details.Select((det, detIndex) => new LogisticsDetail
                    {
                        ItemName = det.ItemName,
                        Description = det.Description,
                        Quantity = det.Quantity,
                        UnitPrice = det.UnitPrice,
                        TotalPrice = det.TotalPrice,
                        Notes = det.Notes,
                        SortOrder = detIndex
                    }).ToList()
                }).ToList()
            };

            await _logisticsRepository.UpdateAsync(logistics);

            return Json(new { success = true, message = "Logistics record updated successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _logisticsRepository.DeleteAsync(id);
            return Json(new { success = true, message = "Logistics record deleted successfully." });
        }
    }

    // View Models
    public class LogisticsCreateViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ReferenceNumber { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public List<LogisticsCategoryViewModel> Categories { get; set; } = new();
    }

    public class LogisticsCategoryViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<LogisticsDetailViewModel> Details { get; set; } = new();
    }

    public class LogisticsDetailViewModel
    {
        public string ItemName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
    }
}

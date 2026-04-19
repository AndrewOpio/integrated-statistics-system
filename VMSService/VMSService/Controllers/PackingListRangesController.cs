using Microsoft.AspNetCore.Mvc;
using VMSService.Data.Entities;
using VMSService.Core.Repositories.PackingListRanges;
using VMSService.Core.Repositories.PackingListRangeDetails;
using VMSService.Core.Repositories.ExpectedCurrencies;
using VMSService.Core.Repositories.StatusTypes;
using Microsoft.EntityFrameworkCore;
using VMSService.Models;
using VMSService.Core.Static;
using SmartBreadcrumbs.Attributes;
using VMSService.Web.Controllers;
using VMSService.Core.Services.PackingListRangeDetails;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.ExPrintMintDetails;
using VMSService.Core.Models.ExPrintMintDetails;

namespace VMSService.Controllers
{
    public class PackingListRangesController : Controller
    {
        private readonly IPackingListRangeRepository _packingListRangeRepository;
        private readonly IPackingListDetailsRepository _packingListDetailsRepository;
        private readonly IExPrintMintRepository _exPrintMintRepository;
        private readonly IStatusTypeRepository _statusTypeRepository;
        private readonly IPackingListDetailService _packingListDetailService;
        private readonly IExPrintMintDetailRepository _exPrintMintDetailRepository;
        private readonly IDenominationRepository _denominationRepository;
        private readonly ILogger<PackingListRangesController> _logger;

        public PackingListRangesController(
            IPackingListRangeRepository packingListRangeRepository,
            IPackingListDetailsRepository packingListDetailsRepository,
            IExPrintMintRepository exPrintMintRepository,
            IStatusTypeRepository statusTypeRepository,
            IPackingListDetailService packingListDetailService,
            IExPrintMintDetailRepository exPrintMintDetailRepository,
            IDenominationRepository denominationRepository,
            ILogger<PackingListRangesController> logger)
        {
            _packingListRangeRepository = packingListRangeRepository;
            _packingListDetailsRepository = packingListDetailsRepository;
            _exPrintMintRepository = exPrintMintRepository;
            _statusTypeRepository = statusTypeRepository;
            _packingListDetailService = packingListDetailService;
            _exPrintMintDetailRepository = exPrintMintDetailRepository;
            _denominationRepository = denominationRepository;
            _logger = logger;
        }

        // GET: PackingListRanges
        [Breadcrumb("Ranges List", FromController = typeof(ExPrintMintsController), FromAction = nameof(ExPrintMintsController.Index))]
        public async Task<IActionResult> Index(int exStockId)
        {
            var packingListRanges = await _packingListRangeRepository.FindByExStockIdAsync(exStockId);
            var exPrintMint = await _exPrintMintRepository.FindById(exStockId);

            ViewData["ExStockId"] = exStockId;
            ViewData["ExPrintMint"] = exPrintMint?.ExPrintMintReference ?? "Unknown";
            
            // Load ExPrintMintDetail and denomination names for each range
            var denominationNames = new Dictionary<int, string>(); // Map of ExPrintMintDetailId -> DenominationName
            if (packingListRanges != null)
            {
                foreach (var range in packingListRanges)
                {
                    if (range.ExpectedStockDetailId > 0)
                    {
                        range.ExpectedStockDetail = await _exPrintMintDetailRepository.FindById(range.ExpectedStockDetailId);
                        
                        // Fetch denomination name from Stock Service
                        if (range.ExpectedStockDetail != null)
                        {
                            var denomination = await _denominationRepository.FindById(range.ExpectedStockDetail.DenominationId);
                            denominationNames[range.ExpectedStockDetailId] = denomination?.Name ?? $"Denomination {range.ExpectedStockDetail.DenominationId}";
                        }
                    }
                }
            }
            
            ViewBag.DenominationNames = denominationNames;

            // Check if status allows editing
            bool canEdit = false;
            bool isVerified = false;
            if (exPrintMint != null)
            {
                var draftStatus = await _statusTypeRepository.FindByName(StatusConstants.ExPrintMint.Draft, StatusConstants.Category.ExPrintMint);
                var returnedStatus = await _statusTypeRepository.FindByName(StatusConstants.ExPrintMint.Returned, StatusConstants.Category.ExPrintMint);
                var verifiedStatus = await _statusTypeRepository.FindByName(StatusConstants.ExPrintMint.Verified, StatusConstants.Category.ExPrintMint);

                canEdit = (draftStatus != null && exPrintMint.CurrentStatusId == draftStatus.Id) || 
                          (returnedStatus != null && exPrintMint.CurrentStatusId == returnedStatus.Id);
                
                isVerified = verifiedStatus != null && exPrintMint.CurrentStatusId == verifiedStatus.Id;
            }

            ViewData["CanEdit"] = canEdit;
            ViewData["IsVerified"] = isVerified;
            return View(packingListRanges);
        }

        // GET: PackingListRanges/Create
        [Breadcrumb("Create")]
        public async Task<IActionResult> Create(int exStockId)
        {
            var exPrintMint = await _exPrintMintRepository.FindById(exStockId);
            if (exPrintMint == null)
            {
                return NotFound();
            }

            var draftStatus = await _statusTypeRepository.FindByName(StatusConstants.ExPrintMint.Draft, StatusConstants.Category.ExPrintMint);
            var returnedStatus = await _statusTypeRepository.FindByName(StatusConstants.ExPrintMint.Returned, StatusConstants.Category.ExPrintMint);

            // Check if status is Draft or Returned
            if ((draftStatus == null || exPrintMint.CurrentStatusId != draftStatus.Id) && 
                (returnedStatus == null || exPrintMint.CurrentStatusId != returnedStatus.Id))
            {
                TempData["Error"] = "You can only create packing list ranges when the status is 'Draft' or 'Returned'.";
                return RedirectToAction("Index", "ExPrintMints");
            }

            // Fetch the denominations for this batch
            var details = await _exPrintMintDetailRepository.FindByExPrintMintId(exStockId);
            var denominations = new List<(int DetailId, string Name)>();
            
            if (details != null)
            {
                foreach (var detail in details.Where(d => !d.IsDeleted))
                {
                    var denomination = await _denominationRepository.FindById(detail.DenominationId);
                    denominations.Add((detail.Id, denomination?.Name ?? $"Denomination {detail.DenominationId}"));
                }
            }

            var viewModel = new PackingListRangeBatchCreateViewModel
            {
                ExStockId = exStockId
            };
            
            ViewBag.Denominations = denominations;
            
            return View(viewModel);
        }

        // POST: PackingListRanges/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create")]
        public async Task<IActionResult> Create(PackingListRangeBatchCreateViewModel model)
        {
            var exPrintMint = await _exPrintMintRepository.FindById(model.ExStockId);
            if (exPrintMint == null)
            {
                return NotFound();
            }

            var draftStatus = await _statusTypeRepository.FindByName(StatusConstants.ExPrintMint.Draft, StatusConstants.Category.ExPrintMint);
            var returnedStatus = await _statusTypeRepository.FindByName(StatusConstants.ExPrintMint.Returned, StatusConstants.Category.ExPrintMint);

            // Check if status is Draft or Returned
            if ((draftStatus == null || exPrintMint.CurrentStatusId != draftStatus.Id) && 
                (returnedStatus == null || exPrintMint.CurrentStatusId != returnedStatus.Id))
            {
                ModelState.AddModelError(string.Empty, "You can only create packing list ranges when the status is 'Draft' or 'Returned'.");
                if (model.Rows.Count == 0)
                {
                    model.Rows.Add(new PackingListRangeRowViewModel());
                }
                return View(model);
            }

            var rowsToSave = model.Rows
                .Where(row => row.HasAnyValue())
                .ToList();

            if (rowsToSave.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Add at least one packing list range row before saving.");
            }

            foreach (var row in rowsToSave)
            {
                if (string.IsNullOrWhiteSpace(row.CaseNumberPrefix)
                    || !row.StartCaseNumber.HasValue
                    || !row.EndCaseNumber.HasValue
                    || string.IsNullOrWhiteSpace(row.SNPrefix)
                    || !row.SNRangeStart.HasValue
                    || !row.SNRangeEnd.HasValue)
                {
                    ModelState.AddModelError(string.Empty, "Every entered row must have all table fields filled in.");
                    break;
                }
            }

            if (!ModelState.IsValid)
            {
                if (model.Rows.Count == 0)
                {
                    model.Rows.Add(new PackingListRangeRowViewModel());
                }

                return View(model);
            }

            foreach (var row in rowsToSave)
            {
                await _packingListRangeRepository.CreateAsync(row.ToEntity(model.ExStockId));
            }

            TempData["Success"] = $"Saved {rowsToSave.Count} packing list range entr{(rowsToSave.Count == 1 ? "y" : "ies")}.";
            return RedirectToAction(nameof(Index), new { exStockId = model.ExStockId });
        }

        // GET: PackingListRanges/Edit/5
        [Breadcrumb("Edit")]
        public async Task<IActionResult> Edit(int? id, int exStockId)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exPrintMint = await _exPrintMintRepository.FindById(exStockId);
            if (exPrintMint == null)
            {
                return NotFound();
            }

            var draftStatus = await _statusTypeRepository.FindByName(StatusConstants.ExPrintMint.Draft, StatusConstants.Category.ExPrintMint);
            var returnedStatus = await _statusTypeRepository.FindByName(StatusConstants.ExPrintMint.Returned, StatusConstants.Category.ExPrintMint);

            // Check if status is Draft or Returned
            if ((draftStatus == null || exPrintMint.CurrentStatusId != draftStatus.Id) && 
                (returnedStatus == null || exPrintMint.CurrentStatusId != returnedStatus.Id))
            {
                TempData["Error"] = "You can only edit packing list ranges when the status is 'Draft' or 'Returned'.";
                return RedirectToAction("Index", "ExPrintMints");
            }

            var packingListRange = await _packingListRangeRepository.FindByIdAsync(id.Value);
            if (packingListRange == null)
            {
                return NotFound();
            }

            ViewData["ExStockId"] = exStockId;
            return View(packingListRange);
        }

        // POST: PackingListRanges/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit")]
        public async Task<IActionResult> Edit(int id, int exStockId, PackingListRange packingListRange)
        {
            if (id != packingListRange.Id)
            {
                return NotFound();
            }

            var exPrintMint = await _exPrintMintRepository.FindById(exStockId);
            if (exPrintMint == null)
            {
                return NotFound();
            }

            var draftStatus = await _statusTypeRepository.FindByName(StatusConstants.ExPrintMint.Draft, StatusConstants.Category.ExPrintMint);
            var returnedStatus = await _statusTypeRepository.FindByName(StatusConstants.ExPrintMint.Returned, StatusConstants.Category.ExPrintMint);

            // Check if status is Draft or Returned
            if ((draftStatus == null || exPrintMint.CurrentStatusId != draftStatus.Id) && 
                (returnedStatus == null || exPrintMint.CurrentStatusId != returnedStatus.Id))
            {
                ModelState.AddModelError(string.Empty, "You can only edit packing list ranges when the status is 'Draft' or 'Returned'.");
                ViewData["ExStockId"] = exStockId;
                return View(packingListRange);
            }

            // Always set the correct ExStockId from the route parameter
            packingListRange.ExpectedStockId = exStockId;

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    _logger.LogWarning($"ModelState Error: {error.ErrorMessage}");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _packingListRangeRepository.UpdateAsync(packingListRange);
                    TempData["Success"] = "Packing list range updated successfully.";
                    return RedirectToAction(nameof(Index), new { exStockId = exStockId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating packing list range");
                    ModelState.AddModelError(string.Empty, "An error occurred while updating the packing list range.");
                }
            }

            ViewData["ExStockId"] = exStockId;
            return View(packingListRange);
        }

        // GET: PackingListRanges/Delete/5
        [Breadcrumb("Delete")]
        public async Task<IActionResult> Delete(int? id, int exStockId)
        {
            if (id == null)
            {
                return NotFound();
            }

            var packingListRange = await _packingListRangeRepository.FindByIdAsync(id.Value);

            if (packingListRange == null)
            {
                return NotFound();
            }

            ViewData["ExStockId"] = exStockId;
            return View(packingListRange);
        }

        // POST: PackingListRanges/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id, int exStockId)
        {
            await _packingListRangeRepository.DeleteAsync(id);

            return RedirectToAction(nameof(Index), new { exStockId = exStockId });
        }

        // GET: PackingListRanges/GetDetails/5
        [HttpGet]
        public async Task<IActionResult> GetDetails(int id)
        {
            var packingListRange = await _packingListRangeRepository.FindByIdAsync(id);
            if (packingListRange == null)
            {
                return NotFound();
            }

            return Json(new
            {
                caseNumberPrefix = packingListRange.CaseNumberPrefix,
                snPrefix = packingListRange.SNPrefix,
                startCaseNumber = packingListRange.StartCaseNumber,
                endCaseNumber = packingListRange.EndCaseNumber,
                snRangeStart = packingListRange.SNRangeStart,
                snRangeEnd = packingListRange.SNRangeEnd
            });
        }

        // GET: PackingListRanges/Details/5
        [Breadcrumb("Serial Numbers")]
        public async Task<IActionResult> Details(int id, int exStockId)
        {
            var packingListRange = await _packingListRangeRepository.FindByIdAsync(id);
            if (packingListRange == null)
            {
                return NotFound();
            }

            // Populate serial numbers if not already done
            await _packingListDetailService.PopulateSerialNumbersAsync(id);

            // Get all details
            var details = await _packingListDetailsRepository.FindByPackingListRangeIdAsync(id);

            ViewData["ExStockId"] = exStockId;
            ViewData["PackingListRange"] = packingListRange;
            return View(details);
        }
    }
}

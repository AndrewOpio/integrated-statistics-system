using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;
using VMSService.Controllers;
using VMSService.Core.Models.Shared;
using VMSService.Core.Models.StockTakes;
using VMSService.Core.Repositories.StockTakes;
using VMSService.Core.Repositories.StockTakeStatusHistories;
using VMSService.Core.Repositories.StockTakeSystemPositions;
using VMSService.Core.Repository.VaultSections;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.StockPositions;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using VMSService.Web.Authorization;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class StockTakesController(
        IStockTakeRepository _stockTakeRepository,
        IStockTakeStatusHistoryRepository _statusHistoryRepository,
        IStockTakeSystemPositionRepository _systemPositionRepository,
        IVaultSectionRepository _vaultSectionRepository,
        IStockService _stockService,
        IStockPositionService _stockPositionService,
        IConfigurationsServiceClient _configServiceClient,
        IWorkflowServiceClient _workflowServiceClient,
        IWorkflowService _workflowService) : Controller
    {
        [Breadcrumb("Stock Takes", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public async Task<IActionResult> Index()
        {
            var stockTakes = await _stockTakeRepository.FindAll();
            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            ViewBag.BranchNames = branches.ToDictionary(b => b.Id, b => b.Name);
            // Every record referenced by AmendsStockTakeId is itself in this same list (unless
            // soft-deleted), so this reference lookup is built for free from data already in memory.
            ViewBag.ReferenceById = stockTakes.ToDictionary(s => s.Id, s => s.ReferenceNumber);
            return View(stockTakes);
        }

        [HttpGet]
        [Breadcrumb("New Count", FromController = typeof(StockTakesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(int? amendsId)
        {
            var branches = await _configServiceClient.GetAllBouBranchesAsync();

            ViewBag.Branches = branches.ToList();
            // Categories are managed dynamically in the Stock Service (see CategoriesController) —
            // show whatever is currently configured there rather than a hardcoded list.
            ViewBag.Categories = (await _stockService.GetAllCategories())
                .OrderBy(c => c.Name)
                .ToList();
            ViewBag.Denominations = (await _stockService.GetAllDenominations())
                .OrderBy(d => d.Value)
                .ToList();
            ViewBag.VaultSections = (await _vaultSectionRepository.FindAll())
                .Where(s => s.IsActive)
                .OrderBy(s => s.Vault.Name).ThenBy(s => s.Name)
                .ToList();

            if (amendsId.HasValue)
            {
                var original = await _stockTakeRepository.FindById(amendsId.Value);
                if (original == null) return NotFound();

                if (original.IsLocked)
                {
                    TempData["Error"] = "That record has already been amended.";
                    return RedirectToAction(nameof(Details), new { id = amendsId.Value });
                }

                ViewBag.AmendsStockTakeId = original.Id;
                ViewBag.AmendsStockTakeReference = original.ReferenceNumber;
                ViewBag.AmendsBranchId = original.BranchId;
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStockTakeDto dto)
        {
            if (dto.BranchId <= 0)
                return BadRequest(new { message = "Please select a branch." });

            var countedLines = (dto.Lines ?? new List<StockTakeLineDto>())
                .Where(l => l.Value > 0)
                .ToList();

            if (!countedLines.Any())
                return BadRequest(new { message = "Enter at least one counted line before submitting." });

            if (countedLines.Any(l => l.VaultSectionId <= 0))
                return BadRequest(new { message = "Every counted line must specify a vault section." });

            StockTake? original = null;
            if (dto.AmendsStockTakeId.HasValue)
            {
                original = await _stockTakeRepository.FindById(dto.AmendsStockTakeId.Value);
                if (original == null)
                    return BadRequest(new { message = "The record being amended could not be found." });
                if (original.IsLocked)
                    return BadRequest(new { message = "That record has already been amended." });
            }

            try
            {
                int initialStatusId = 0;
                string initialStatusName = StatusConstants.StockTake.Draft;
                try
                {
                    var draftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName = StatusConstants.Entity.StockTake,
                        StatusName = StatusConstants.StockTake.Draft,
                    });
                    if (draftStatus?.Name != null)
                    {
                        initialStatusId = draftStatus.Id;
                        initialStatusName = draftStatus.Name;
                    }
                }
                catch { /* WorkflowService may not be configured yet */ }

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var countDate = dto.CountDate == default ? DateTime.Now : dto.CountDate;

                // ST-{BranchCode}-{CountDate}-{sequence for that branch+date} — same shape as the
                // other modules' reference numbers (prefix + contextual code + date + sequence),
                // rather than a raw branch id + full timestamp.
                var branch = await _configServiceClient.GetBouBranchByIdAsync(dto.BranchId);
                var branchCode = !string.IsNullOrWhiteSpace(branch?.BranchCode) ? branch.BranchCode : dto.BranchId.ToString();
                var sequence = await _stockTakeRepository.CountByBranchAndDateAsync(dto.BranchId, countDate) + 1;
                var referenceNumber = $"ST-{branchCode}-{countDate:yyyyMMdd}-{sequence:D2}";

                var stockTake = new StockTake
                {
                    ReferenceNumber = referenceNumber,
                    BranchId = dto.BranchId,
                    CountDate = countDate,
                    Remarks = dto.Remarks,
                    AmendsStockTakeId = dto.AmendsStockTakeId,
                    CurrentStatusId = initialStatusId,
                    CurrentStatusName = initialStatusName,
                    StockTakeDetails = countedLines.Select(l => new StockTakeDetail
                    {
                        CategoryId = l.CategoryId,
                        VaultSectionId = l.VaultSectionId,
                        DenominationId = l.DenominationId,
                        NumberOfPieces = l.NumberOfPieces,
                        Value = l.Value,
                    }).ToList()
                };

                await _stockTakeRepository.Create(stockTake);
                await CaptureSystemPositionSnapshot(stockTake.Id, dto.BranchId);

                if (original != null)
                {
                    original.IsLocked = true;
                    await _stockTakeRepository.Update(original);
                }

                return Ok(new { success = true, redirectUrl = Url.Action(nameof(Details), new { id = stockTake.Id }) });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message });
            }
        }

        // Snapshots the full category x denomination grid for a branch's system position,
        // right now, into StockTakeSystemPosition — used on both Create and Edit so the
        // variance comparison always reflects the ledger as of the moment the count was
        // last saved, not whatever it says whenever someone later views the record.
        private async Task CaptureSystemPositionSnapshot(int stockTakeId, int branchId)
        {
            var allCategoryIds = (await _stockService.GetAllCategories()).Select(c => c.Id).ToList();
            var allDenominationIds = (await _stockService.GetAllDenominations()).Select(d => d.Id).ToList();
            var matrix = await _stockPositionService.GetStockMatrixAsync(branchId, allCategoryIds, allDenominationIds);

            var snapshot = allDenominationIds
                .SelectMany(denomId => allCategoryIds.Select(catId => (CategoryId: catId, DenominationId: denomId)))
                .Select(pair => new StockTakeSystemPosition
                {
                    StockTakeId = stockTakeId,
                    CategoryId = pair.CategoryId,
                    DenominationId = pair.DenominationId,
                    Value = matrix.TryGetValue(pair.DenominationId, out var byCategory) && byCategory.TryGetValue(pair.CategoryId, out var v) ? v : 0m,
                })
                .ToList();
            await _systemPositionRepository.CreateRange(snapshot);
        }

        [HttpGet]
        [RequirePermission(SystemPermissions.StockTakes.Edit)]
        [Breadcrumb("Edit", FromController = typeof(StockTakesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id)
        {
            var stockTake = await _stockTakeRepository.FindById(id);
            if (stockTake == null) return NotFound();

            if (stockTake.IsLocked)
            {
                TempData["Error"] = "That record has already been amended and can no longer be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            bool isEditableStatus = stockTake.CurrentStatusName == StatusConstants.StockTake.Draft
                || stockTake.CurrentStatusName == StatusConstants.StockTake.Returned;
            if (!isEditableStatus)
            {
                TempData["Error"] = "Only records in Draft or Returned can be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            var vaultSections = (await _vaultSectionRepository.FindAll())
                .Where(s => s.IsActive)
                .OrderBy(s => s.Vault.Name).ThenBy(s => s.Name)
                .ToList();

            ViewBag.Branches = branches.ToList();
            ViewBag.Categories = (await _stockService.GetAllCategories()).OrderBy(c => c.Name).ToList();
            ViewBag.Denominations = (await _stockService.GetAllDenominations()).OrderBy(d => d.Value).ToList();
            ViewBag.VaultSections = vaultSections;

            ViewBag.EditStockTakeId = stockTake.Id;
            ViewBag.ExistingBranchId = stockTake.BranchId;
            ViewBag.ExistingCountDate = stockTake.CountDate;
            ViewBag.ExistingRemarks = stockTake.Remarks;
            ViewBag.ExistingLines = (stockTake.StockTakeDetails ?? new List<StockTakeDetail>())
                .Where(d => !d.IsDeleted)
                .Select(d => new
                {
                    categoryId = d.CategoryId,
                    vaultSectionId = d.VaultSectionId,
                    denominationId = d.DenominationId,
                    value = d.Value,
                    pieces = d.NumberOfPieces,
                })
                .ToList<object>();

            return View();
        }

        [HttpPost]
        [RequirePermission(SystemPermissions.StockTakes.Edit)]
        public async Task<IActionResult> Edit(int id, [FromBody] EditStockTakeDto dto)
        {
            if (dto.BranchId <= 0)
                return BadRequest(new { message = "Please select a branch." });

            var countedLines = (dto.Lines ?? new List<StockTakeLineDto>())
                .Where(l => l.Value > 0)
                .ToList();

            if (!countedLines.Any())
                return BadRequest(new { message = "Enter at least one counted line before submitting." });

            if (countedLines.Any(l => l.VaultSectionId <= 0))
                return BadRequest(new { message = "Every counted line must specify a vault section." });

            var stockTake = await _stockTakeRepository.FindById(id);
            if (stockTake == null)
                return NotFound(new { message = "Stock take not found." });

            if (stockTake.IsLocked)
                return BadRequest(new { message = "That record has already been amended and can no longer be edited." });

            bool isEditableStatus = stockTake.CurrentStatusName == StatusConstants.StockTake.Draft
                || stockTake.CurrentStatusName == StatusConstants.StockTake.Returned;
            if (!isEditableStatus)
                return BadRequest(new { message = "Only records in Draft or Returned can be edited." });

            try
            {
                var countDate = dto.CountDate == default ? stockTake.CountDate : dto.CountDate;

                if (stockTake.BranchId != dto.BranchId || stockTake.CountDate.Date != countDate.Date)
                {
                    var branch = await _configServiceClient.GetBouBranchByIdAsync(dto.BranchId);
                    var branchCode = !string.IsNullOrWhiteSpace(branch?.BranchCode) ? branch.BranchCode : dto.BranchId.ToString();
                    var sequence = await _stockTakeRepository.CountByBranchAndDateAsync(dto.BranchId, countDate, excludeId: stockTake.Id) + 1;
                    stockTake.ReferenceNumber = $"ST-{branchCode}-{countDate:yyyyMMdd}-{sequence:D2}";
                }

                stockTake.BranchId = dto.BranchId;
                stockTake.CountDate = countDate;
                stockTake.Remarks = dto.Remarks;

                // Replace the counted lines wholesale — soft-delete the old ones (kept for
                // audit trail) and add the newly submitted set.
                stockTake.StockTakeDetails ??= new List<StockTakeDetail>();
                foreach (var detail in stockTake.StockTakeDetails)
                {
                    detail.IsDeleted = true;
                }
                foreach (var line in countedLines)
                {
                    stockTake.StockTakeDetails.Add(new StockTakeDetail
                    {
                        CategoryId = line.CategoryId,
                        VaultSectionId = line.VaultSectionId,
                        DenominationId = line.DenominationId,
                        NumberOfPieces = line.NumberOfPieces,
                        Value = line.Value,
                    });
                }

                await _stockTakeRepository.Update(stockTake);

                // The count and/or branch may have changed, so the frozen system position
                // needs to be re-captured from scratch to stay accurate.
                await _systemPositionRepository.DeleteByStockTakeId(stockTake.Id);
                await CaptureSystemPositionSnapshot(stockTake.Id, dto.BranchId);

                return Ok(new { success = true, redirectUrl = Url.Action(nameof(Details), new { id = stockTake.Id }) });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message });
            }
        }

        [HttpGet]
        [Breadcrumb("Details", FromController = typeof(StockTakesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int id)
        {
            var stockTake = await _stockTakeRepository.FindById(id);
            if (stockTake == null) return NotFound();

            var categories = await _stockService.GetAllCategories();
            var denominations = await _stockService.GetAllDenominations();
            var vaultSections = await _vaultSectionRepository.FindAll();
            var branch = await _configServiceClient.GetBouBranchByIdAsync(stockTake.BranchId);

            var lines = (stockTake.StockTakeDetails ?? new List<StockTakeDetail>())
                .Where(d => !d.IsDeleted)
                .OrderBy(d => d.Id)
                .Select(d =>
                {
                    var vaultSection = vaultSections.FirstOrDefault(s => s.Id == d.VaultSectionId);
                    return new StockTakeDetailLineViewModel
                    {
                        CategoryId = d.CategoryId,
                        CategoryName = categories.FirstOrDefault(c => c.Id == d.CategoryId)?.Name ?? $"#{d.CategoryId}",
                        VaultSectionId = d.VaultSectionId,
                        VaultSectionName = vaultSection?.Name ?? $"#{d.VaultSectionId}",
                        VaultName = vaultSection?.Vault?.Name ?? string.Empty,
                        DenominationId = d.DenominationId,
                        DenominationName = denominations.FirstOrDefault(x => x.Id == d.DenominationId)?.Name ?? $"#{d.DenominationId}",
                        NumberOfPieces = d.NumberOfPieces,
                        Value = d.Value,
                    };
                }).ToList();

            var userRoles = GetUserRoles();
            var availableTransitions = new List<WorkflowTransitionDto>();
            foreach (var roleId in userRoles)
            {
                var transitions = await _workflowServiceClient.GetAvailableTransitions(new GetAvailableTransitionsRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    EntityTypeName = StatusConstants.Entity.StockTake,
                    CurrentStatusId = stockTake.CurrentStatusId,
                    AppRole = roleId,
                });
                if (transitions != null) availableTransitions.AddRange(transitions);
            }

            // "Acknowledge Amendment" is a separate action/role from the normal Acknowledge,
            // so the person who signed off on the original can't be the one who signs off on
            // the amendment. Both actions land on the same "Acknowledged" status, so without
            // this filter WorkflowService would offer both side by side on every record.
            bool isAmendmentRecord = stockTake.AmendsStockTakeId.HasValue;
            availableTransitions = availableTransitions
                .Where(t => t.ToStatus?.Name != StatusConstants.StockTake.Acknowledged
                    || (isAmendmentRecord
                        ? t.ActionName == StatusConstants.StockTakeActions.AcknowledgeAmendment
                        : t.ActionName != StatusConstants.StockTakeActions.AcknowledgeAmendment))
                .ToList();

            var historyEntities = await _statusHistoryRepository.FindByStockTakeId(id);
            var statusHistory = historyEntities.OrderBy(h => h.ChangedAt).Select(h => new StatusHistoryViewModel
            {
                OldStatusName = h.OldStatusName,
                NewStatusName = h.NewStatusName,
                ChangedBy = h.ChangedBy,
                ChangedAt = h.ChangedAt,
                Remarks = h.Remarks,
            }).ToList();

            string? amendsReference = null;
            if (stockTake.AmendsStockTakeId.HasValue)
            {
                var amendedOriginal = await _stockTakeRepository.FindById(stockTake.AmendsStockTakeId.Value);
                amendsReference = amendedOriginal?.ReferenceNumber;
            }

            var amendment = await _stockTakeRepository.FindAmendmentOf(stockTake.Id);

            // Compares against the system position frozen at Create time (see
            // StockTakeSystemPosition), not a live ledger query — so this always reflects
            // what the ledger said on the count date, regardless of when it's viewed. Covers
            // every snapshotted pair, not just the ones counted, so a category/denomination
            // the system has stock in but nobody counted still registers as a mismatch here —
            // consistent with what the matrix on the page itself flags.
            var systemPositions = await _systemPositionRepository.FindByStockTakeId(id);
            var systemByPair = systemPositions.ToDictionary(p => (p.CategoryId, p.DenominationId), p => p.Value);
            var physByPair = lines.GroupBy(l => (l.CategoryId, l.DenominationId)).ToDictionary(g => g.Key, g => g.Sum(l => l.Value));
            var allPairs = systemByPair.Keys.Union(physByPair.Keys);

            decimal totalSystemValue = 0;
            int mismatchCount = 0;
            foreach (var pair in allPairs)
            {
                var physical = physByPair.TryGetValue(pair, out var p) ? p : 0m;
                var system = systemByPair.TryGetValue(pair, out var v) ? v : 0m;
                totalSystemValue += system;
                if (physical != system) mismatchCount++;
            }
            var totalCountedValue = lines.Sum(l => l.Value);

            var vm = new StockTakeDetailsViewModel
            {
                Id = stockTake.Id,
                ReferenceNumber = stockTake.ReferenceNumber,
                BranchId = stockTake.BranchId,
                BranchName = branch?.Name ?? $"Branch #{stockTake.BranchId}",
                CountDate = stockTake.CountDate,
                CurrentStatusId = stockTake.CurrentStatusId,
                CurrentStatusName = stockTake.CurrentStatusName,
                Remarks = stockTake.Remarks,
                CreatedBy = stockTake.CreatedBy,
                CreatedDate = stockTake.CreatedDate,
                IsLocked = stockTake.IsLocked,
                AmendsStockTakeId = stockTake.AmendsStockTakeId,
                AmendsStockTakeReference = amendsReference,
                AmendedByStockTakeId = amendment?.Id,
                AmendedByStockTakeReference = amendment?.ReferenceNumber,
                LineCount = lines.Count,
                VaultSectionCount = lines.Select(l => l.VaultSectionId).Distinct().Count(),
                TotalCountedValue = totalCountedValue,
                TotalSystemValue = totalSystemValue,
                TotalVariance = totalCountedValue - totalSystemValue,
                MismatchCount = mismatchCount,
                Lines = lines,
                AvailableTransitions = availableTransitions.Distinct().ToList(),
                StatusHistory = statusHistory,
            };

            return View(vm);
        }

        // Returns the system stock position frozen for this StockTake at Create time (see
        // StockTakeSystemPosition), broken down the same way as the physical count grid, so
        // Details.cshtml can render the variance comparison as it stood on the count date.
        [HttpGet]
        [RequirePermission(SystemPermissions.StockTakes.ViewVariance)]
        public async Task<IActionResult> GetSystemPosition(int stockTakeId)
        {
            try
            {
                var categories = (await _stockService.GetAllCategories()).OrderBy(c => c.Name).ToList();
                var denominations = (await _stockService.GetAllDenominations())
                    .OrderBy(d => d.DenominationTypeId).ThenBy(d => d.Value).ToList();

                var snapshot = await _systemPositionRepository.FindByStockTakeId(stockTakeId);
                var valueByPair = snapshot.ToDictionary(p => (p.CategoryId, p.DenominationId), p => p.Value);

                var result = denominations.Select(d => new
                {
                    denominationId = d.Id,
                    denominationName = d.Name,
                    denominationValue = d.Value,
                    // Groups matrix columns into Notes / Coins sections client-side.
                    denominationTypeName = d.DenominationType?.Name ?? "Other",
                    categories = categories.Select(c => new
                    {
                        categoryId = c.Id,
                        categoryName = c.Name,
                        value = valueByPair.TryGetValue((c.Id, d.Id), out var v) ? v : 0m
                    })
                });

                return Json(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(int id, string actionName, string remarks)
        {
            var stockTake = await _stockTakeRepository.FindById(id);
            if (stockTake == null)
            {
                TempData["Error"] = "Stock take not found.";
                return RedirectToAction(nameof(Index));
            }

            if (stockTake.IsLocked)
            {
                TempData["Error"] = "This record has been amended and is now read-only.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var userRoles = GetUserRoles();

                var executed = await _workflowService.ExecuteTransition(
                    entityType: StatusConstants.Entity.StockTake,
                    entityId: id,
                    currentStatusId: stockTake.CurrentStatusId,
                    actionName: actionName,
                    username: username,
                    userRoles: userRoles,
                    remarks: remarks);

                TempData[executed ? "Success" : "Error"] = executed
                    ? "Action completed successfully."
                    : $"Failed to execute '{actionName}'. You may not have the required role or the action is unavailable.";
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while executing the action.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        private List<int> GetUserRoles()
            => User.FindAll("appRoleId")
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => int.TryParse(c.Value.Split(":")[1], out var roleId) ? roleId : (int?)null)
                .Where(r => r.HasValue)
                .Select(r => r!.Value)
                .ToList();
    }
}

using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VMSService.Core.Models.Cfb;
using VMSService.Core.Repositories.CfbBoxes;
using VMSService.Core.Repositories.CfbCashHoldings;
using VMSService.Core.Repositories.CfbMovements;
using VMSService.Core.Repositories.CfbTopupRequests;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Static;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class CfbController(
        ICfbBoxRepository _cfbBoxRepo,
        ICfbCashHoldingRepository _holdingRepo,
        ICfbMovementRepository _movementRepo,
        IConfigurationsServiceClient _configServiceClient,
        IStockService _stockService,
        ITopupRequestRepository _topupRepo,
        ILogger<CfbController> _logger) : Controller
    {
        // ── Role helpers ──────────────────────────────────────────────────────

        private List<string> GetUserRoles()
            => User.FindAll(SystemPolicy.ClaimType.AppRole)
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();

        private bool CanViewAllBranches(List<string> roles)
            => roles.Contains(SystemPolicy.Roles.RemittancesOfficer)
            || roles.Contains(SystemPolicy.Roles.BackOffice);

        // ── Helpers ───────────────────────────────────────────────────────────

        // Returns sum of all movements keyed by (CfbBoxId, DenominationId).
        private static Dictionary<(int BoxId, int DenomId), long> BuildMovementSums(
            IEnumerable<CfbCashMovement> movements)
            => movements
                .GroupBy(m => (m.CfbBoxId, m.DenominationId))
                .ToDictionary(g => g.Key, g => g.Sum(m => m.AmountChange));

        // ── Actions ───────────────────────────────────────────────────────────

        public async Task<IActionResult> Index()
        {
            var roles = GetUserRoles();

            if (!CanViewAllBranches(roles))
            {
                var allBranches = await _configServiceClient.GetAllBouBranchesAsync();
                var userBranch = allBranches.FirstOrDefault(b => roles.Any(r => r.Contains(b.Name)));
                if (userBranch is null)
                    return Forbid();
                return RedirectToAction(nameof(BranchDetail), new { bouBranchId = userBranch.Id });
            }

            // Supervisor: build overview grid.
            var boxes = await _cfbBoxRepo.FindAll(); // active holdings only via filtered include
            var boxIds = boxes.Select(b => b.Id).ToList();

            var allTopups = await _topupRepo.FindAll();
            var pendingTopupBranchIds = allTopups
                .Where(r => r.CurrentStatusName != CfbConstants.CfbStatusNames.Approved
                         && r.CurrentStatusName != CfbConstants.CfbStatusNames.Rejected)
                .Select(r => r.BouBranchId)
                .ToHashSet();

            var movements = await _movementRepo.GetByBoxIdsAsync(boxIds);
            var movSums = BuildMovementSums(movements);

            var denoms = (await _stockService.GetAllDenominations()).Where(d => !d.IsDeleted).ToList();
            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            var branchById = branches.ToDictionary(b => b.Id);

            var noteColumns = denoms
                .Where(d => d.DenominationType?.Name == StatusConstants.DenominationName.Note)
                .OrderBy(d => d.Value)
                .Select(d => new CfbDenomColumnDef
                {
                    DenominationId = d.Id,
                    Name = d.Name,
                    DenomValue = (int)d.Value,
                })
                .ToList();

            var branchIdsWithBoxes = boxes.Select(b => b.BouBranchId).ToHashSet();

            var rows = boxes
                .GroupBy(b => b.BouBranchId)
                .Select(g =>
                {
                    var notesBox = g.FirstOrDefault(b => b.BoxType == CfbConstants.BoxTypes.Notes);
                    var coinsBox = g.FirstOrDefault(b => b.BoxType == CfbConstants.BoxTypes.Coins);

                    var noteBaselines = new Dictionary<int, long>();
                    var noteActuals = new Dictionary<int, long>();
                    long totalNotesBaseline = 0, totalNotesActual = 0;

                    if (notesBox is not null)
                    {
                        foreach (var h in notesBox.Holdings)
                        {
                            var dev = movSums.GetValueOrDefault((notesBox.Id, h.DenominationId));
                            noteBaselines[h.DenominationId] = h.Amount;
                            noteActuals[h.DenominationId] = h.Amount + dev;
                            totalNotesBaseline += h.Amount;
                            totalNotesActual += h.Amount + dev;
                        }
                    }

                    long totalCoinsBaseline = 0, totalCoinsActual = 0;
                    if (coinsBox is not null)
                    {
                        foreach (var h in coinsBox.Holdings)
                        {
                            var dev = movSums.GetValueOrDefault((coinsBox.Id, h.DenominationId));
                            totalCoinsBaseline += h.Amount;
                            totalCoinsActual += h.Amount + dev;
                        }
                    }

                    branchById.TryGetValue(g.Key, out var br);
                    return new CfbOverviewRowViewModel
                    {
                        BouBranchId = g.Key,
                        BouBranchCode = g.First().BouBranchCode,
                        BouBranchName = br?.Name ?? g.First().BouBranchCode,
                        NoteBaselines = noteBaselines,
                        NoteActuals = noteActuals,
                        TotalNotesBaseline = totalNotesBaseline,
                        TotalNotesActual = totalNotesActual,
                        TotalCoinsBaseline = totalCoinsBaseline,
                        TotalCoinsActual = totalCoinsActual,
                        TotalBaseline = totalNotesBaseline + totalCoinsBaseline,
                        TotalActual = totalNotesActual + totalCoinsActual,
                        HasPendingTopUp = pendingTopupBranchIds.Contains(g.Key),
                        HasIncompleteSetup = g.Count() < 2,
                    };
                })
                .OrderBy(r => r.BouBranchName)
                .ToList();

            var vm = new CfbOverviewViewModel
            {
                NoteDenomColumns = noteColumns,
                BranchRows = rows,
                AllBranches = branches,
                BranchesWithoutBoxes = branches.Where(b => !branchIdsWithBoxes.Contains(b.Id)).ToList(),
                FooterNoteBaselines = noteColumns.ToDictionary(
                    c => c.DenominationId,
                    c => rows.Sum(r => r.NoteBaselines.GetValueOrDefault(c.DenominationId))),
                FooterNoteActuals = noteColumns.ToDictionary(
                    c => c.DenominationId,
                    c => rows.Sum(r => r.NoteActuals.GetValueOrDefault(c.DenominationId))),
                FooterCoinsBaseline = rows.Sum(r => r.TotalCoinsBaseline),
                FooterCoinsActual = rows.Sum(r => r.TotalCoinsActual),
                FooterGrandBaseline = rows.Sum(r => r.TotalBaseline),
                FooterGrandActual = rows.Sum(r => r.TotalActual),
            };

            return View(vm);
        }

        public async Task<IActionResult> BranchDetail(int bouBranchId)
        {
            var roles = GetUserRoles();
            var canViewAll = CanViewAllBranches(roles);

            var allBranches = await _configServiceClient.GetAllBouBranchesAsync();

            if (!canViewAll)
            {
                var userBranch = allBranches.FirstOrDefault(b => roles.Any(r => r.Contains(b.Name)));
                if (userBranch is null || userBranch.Id != bouBranchId)
                    return Forbid();
            }

            var boxes = await _cfbBoxRepo.FindByBranchId(bouBranchId); // active holdings only
            var boxIds = boxes.Select(b => b.Id).ToList();
            var movements = await _movementRepo.GetByBoxIdsAsync(boxIds);
            var movSums = BuildMovementSums(movements);

            var allDenoms = (await _stockService.GetAllDenominations()).Where(d => !d.IsDeleted).ToList();
            var denoms = allDenoms.ToDictionary(d => d.Id);
            var notesDenomCount = allDenoms.Count(d => d.DenominationType?.Name == CfbConstants.BoxTypes.Notes);
            var coinsDenomCount = allDenoms.Count(d => d.DenominationType?.Name == CfbConstants.BoxTypes.Coins);

            var branchDto = await _configServiceClient.GetBouBranchByIdAsync(bouBranchId);

            CfbBoxDetailViewModel? BuildBoxDetail(CfbBox? box, int expectedDenomCount)
            {
                if (box is null) return null;

                var lines = box.Holdings
                    .Select(h =>
                    {
                        denoms.TryGetValue(h.DenominationId, out var d);
                        var movTotal = movSums.GetValueOrDefault((box.Id, h.DenominationId));
                        return new CfbHoldingLineViewModel
                        {
                            DenominationId = h.DenominationId,
                            DenominationName = d?.Name ?? $"Denom {h.DenominationId}",
                            BaselineAmount = h.Amount,
                            MovementTotal = movTotal,
                            ActualAmount = h.Amount + movTotal,
                        };
                    })
                    .OrderBy(l => l.BaselineAmount)
                    .ToList();

                return new CfbBoxDetailViewModel
                {
                    BoxId = box.Id,
                    BoxType = box.BoxType,
                    BoxName = box.Name,
                    Lines = lines,
                    BaselineTotal = lines.Sum(l => l.BaselineAmount),
                    ActualTotal = lines.Sum(l => l.ActualAmount),
                    HasUnsetDenominations = expectedDenomCount > lines.Count,
                };
            }

            var notesBox = BuildBoxDetail(boxes.FirstOrDefault(b => b.BoxType == CfbConstants.BoxTypes.Notes), notesDenomCount);
            var coinsBox = BuildBoxDetail(boxes.FirstOrDefault(b => b.BoxType == CfbConstants.BoxTypes.Coins), coinsDenomCount);

            var vm = new CfbBranchDetailViewModel
            {
                BouBranchId = bouBranchId,
                BouBranchCode = branchDto?.BranchCode ?? string.Empty,
                BouBranchName = branchDto?.Name ?? bouBranchId.ToString(),
                NotesBox = notesBox,
                CoinsBox = coinsBox,
                BranchBaselineTotal = (notesBox?.BaselineTotal ?? 0) + (coinsBox?.BaselineTotal ?? 0),
                BranchActualTotal = (notesBox?.ActualTotal ?? 0) + (coinsBox?.ActualTotal ?? 0),
                IsRestricted = !canViewAll,
                AllBranches = canViewAll ? allBranches : [],
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetupBranch(int bouBranchId)
        {
            var roles = GetUserRoles();
            if (!CanViewAllBranches(roles))
                return Forbid();

            var existingBoxes = await _cfbBoxRepo.FindByBranchId(bouBranchId);
            var hasNotes = existingBoxes.Any(b => b.BoxType == CfbConstants.BoxTypes.Notes);
            var hasCoins = existingBoxes.Any(b => b.BoxType == CfbConstants.BoxTypes.Coins);

            if (hasNotes && hasCoins)
            {
                TempData["Info"] = "Both CFB boxes already exist for this branch.";
                return RedirectToAction(nameof(BranchDetail), new { bouBranchId });
            }

            BranchDto? branch = null;
            try
            {
                branch = await _configServiceClient.GetBouBranchByIdAsync(bouBranchId);
                if (branch?.Id == 0) branch = null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "ConfigurationsService error looking up branch {BranchId} during CFB setup", bouBranchId);
            }

            if (branch is null)
            {
                TempData["Error"] = "Branch not found or configuration service unavailable. Cannot set up boxes.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (!hasNotes)
                {
                    await _cfbBoxRepo.Create(new CfbBox
                    {
                        BouBranchId = bouBranchId,
                        BoxType = CfbConstants.BoxTypes.Notes,
                        Name = $"CFB-{branch.BranchCode}-Note",
                    });
                }
                if (!hasCoins)
                {
                    await _cfbBoxRepo.Create(new CfbBox
                    {
                        BouBranchId = bouBranchId,
                        BoxType = CfbConstants.BoxTypes.Coins,
                        Name = $"CFB-{branch.BranchCode}-Coin",
                    });
                }
                await _cfbBoxRepo.Save();
                TempData["Success"] = $"CFB boxes set up for {branch.Name}.";
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "SetupBranch failed for branch {BranchId}", bouBranchId);
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(BranchDetail), new { bouBranchId });
        }

        [HttpGet]
        public async Task<IActionResult> SetOpeningBalance(int boxId)
        {
            var roles = GetUserRoles();
            if (!CanViewAllBranches(roles))
                return Forbid();

            var box = await _cfbBoxRepo.FindById(boxId);
            if (box is null)
                return NotFound();

            var allDenoms = (await _stockService.GetAllDenominations())
                .Where(d => !d.IsDeleted && d.DenominationType?.Name == box.BoxType)
                .OrderBy(d => d.Value)
                .ToList();

            if (allDenoms.Count == 0)
            {
                TempData["Error"] = "Denomination service unavailable. Cannot load opening balance form.";
                return RedirectToAction(nameof(BranchDetail), new { box.BouBranchId });
            }

            var existingHoldings = (await _holdingRepo.FindByCfbBoxId(boxId, activeOnly: true))
                .ToDictionary(h => h.DenominationId);

            var lines = allDenoms.Select(d => new CfbOpeningBalanceDenomLineViewModel
            {
                DenominationId = d.Id,
                DenominationName = d.Name,
                DenomValue = (int)d.Value,
                ExistingAmount = existingHoldings.TryGetValue(d.Id, out var h) ? h.Amount : null,
            }).ToList();

            if (lines.All(l => l.IsSet))
            {
                TempData["Info"] = $"{box.Name} is fully initialised — all denominations have a baseline.";
                return RedirectToAction(nameof(BranchDetail), new { box.BouBranchId });
            }

            var branchDto = await _configServiceClient.GetBouBranchByIdAsync(box.BouBranchId);
            var vm = new CfbOpeningBalanceViewModel
            {
                BoxId = box.Id,
                BoxType = box.BoxType,
                BoxName = box.Name,
                BouBranchId = box.BouBranchId,
                BouBranchName = branchDto?.Name ?? box.BouBranchId.ToString(),
                Lines = lines,
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetOpeningBalance(CfbOpeningBalanceInputModel model)
        {
            var roles = GetUserRoles();
            if (!CanViewAllBranches(roles))
                return Forbid();

            var nonZeroEntries = model.Amounts
                .Where(kv => kv.Value.HasValue && kv.Value.Value > 0)
                .ToDictionary(kv => kv.Key, kv => kv.Value!.Value);

            if (nonZeroEntries.Count == 0)
            {
                TempData["Error"] = "A baseline cannot be zero. Enter at least one positive amount.";
                return RedirectToAction(nameof(SetOpeningBalance), new { boxId = model.BoxId });
            }

            try
            {
                await _holdingRepo.SetOpeningBalance(model.BoxId, nonZeroEntries);
                await _holdingRepo.Save();
                TempData["Success"] = $"{nonZeroEntries.Count} baseline(s) saved.";
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "SetOpeningBalance failed for box {BoxId}", model.BoxId);
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(SetOpeningBalance), new { boxId = model.BoxId });
        }
    }
}
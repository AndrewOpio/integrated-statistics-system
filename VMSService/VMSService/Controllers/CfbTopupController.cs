using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VMSService.Core.Models.CfbTopupRequests;
using VMSService.Core.Repositories.CfbBoxes;
using VMSService.Core.Repositories.CfbCashHoldings;
using VMSService.Core.Repositories.CfbUnitOfWorks;
using VMSService.Core.Repositories.Categories;
using VMSService.Core.Repositories.CounterPartyTypes;
using VMSService.Core.Repositories.Stocks;
using VMSService.Core.Repositories.StockMovements;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using VMSService.Web.Extensions;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class CfbTopupController(
        ICfbUnitOfWork _uow,
        ICfbBoxRepository _boxRepo,
        IStockService _stockService,
        ICounterPartyTypeRepository _counterPartyTypeRepository,
        ICategoryRepository _categoryRepository,
        IStockMovementRepository _stockMovementRepository,
        IStockLedgerRepository _stockLedgerRepository,
        IWorkflowService _workflowService,
        IWorkflowServiceClient _workflowServiceClient,
        IConfigurationsServiceClient _configServiceClient,
        ILogger<CfbTopupController> _logger) : Controller
    {
        // ── Role helpers ─────────────────────────────────────────────────────

        private List<string> GetUserRoles()
            => User.FindAll(SystemPolicy.ClaimType.AppRole)
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();

        private (bool isSupervisor, bool isVerifier, bool isApprover) GetCfbRoles(List<string> roles)
            => (roles.Contains(SystemPolicy.Roles.CfbSupervisor),
                roles.Contains(SystemPolicy.Roles.VerificationOfficer),
                roles.Contains(SystemPolicy.Roles.CurrencyApprover));

        private static (int branchId, string branchName) GetUserBranchInfo(
            List<string> roles, IEnumerable<BranchDto> allBranches)
        {
            var branch = allBranches.FirstOrDefault(b => roles.Any(r => r.Contains(b.Name)));
            return branch is null ? (0, string.Empty) : (branch.Id, branch.Name);
        }

        // ── Index ────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var roles = GetUserRoles();
            var (isSupervisor, isVerifier, isApprover) = GetCfbRoles(roles);

            if (!isSupervisor && !isVerifier && !isApprover)
                return Forbid();

            ICollection<TopupRequest> requests;
            if (isSupervisor && !isApprover && !isVerifier)
            {
                var allBranches = await _configServiceClient.GetAllBouBranchesAsync();
                var (branchId, _) = GetUserBranchInfo(roles, allBranches);
                if (branchId == 0) return Forbid();
                requests = await _uow.Requests.FindByBranchId(branchId);
            }
            else
            {
                requests = await _uow.Requests.FindAll();
            }

            var allStatuses = await _workflowServiceClient.FindAllStatuses();
            var statusMap = allStatuses
                .Where(s => s.EntityType?.Name == StatusConstants.Entity.CfbTopupRequest)
                .ToDictionary(s => s.Id, s => s.Name);

            var rows = requests.Select(r => new TopupListRowViewModel
            {
                Id = r.Id,
                StatusName = r.CurrentStatusName,
                CreatedBy = r.CreatedBy,
                CreatedDate = r.CreatedDate,
                TotalAmount = 0,
            }).ToList();

            return View(new TopupListViewModel
            {
                Rows = rows,
                IsCfbSupervisor = isSupervisor,
            });
        }

        // ── Create GET ───────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var roles = GetUserRoles();
            if (!roles.Contains(SystemPolicy.Roles.CfbSupervisor))
                return Forbid();

            var allBranches = await _configServiceClient.GetAllBouBranchesAsync();
            var (branchId, branchName) = GetUserBranchInfo(roles, allBranches);
            if (branchId == 0)
            {
                TempData["Error"] = "Your account is not associated with a branch.";
                return RedirectToAction(nameof(Index));
            }

            if (await _uow.Requests.HasPendingForBranch(branchId))
            {
                TempData["Info"] = "Your branch already has a pending top-up request.";
                return RedirectToAction(nameof(Index));
            }

            var boxes = await _boxRepo.FindByBranchId(branchId);
            var allDenoms = (await _stockService.GetAllDenominations()).Where(d => !d.IsDeleted).ToList();
            var denomById = allDenoms.ToDictionary(d => d.Id);

            var boxSections = new List<TopupBoxSectionViewModel>();
            foreach (var box in boxes)
            {
                var lines = box.Holdings.Select(h =>
                {
                    denomById.TryGetValue(h.DenominationId, out var d);
                    return new TopupCreateLineViewModel
                    {
                        DenominationId = h.DenominationId,
                        DenominationName = d?.Name ?? $"Denom {h.DenominationId}",
                        DenomValue = h.DenomValue,
                        CurrentBaseline = h.Amount,
                    };
                }).OrderBy(l => l.DenomValue).ToList();

                if (lines.Count > 0)
                {
                    boxSections.Add(new TopupBoxSectionViewModel
                    {
                        BoxId = box.Id,
                        BoxName = box.Name,
                        BoxType = box.BoxType,
                        Lines = lines,
                    });
                }
            }

            if (boxSections.Count == 0)
            {
                TempData["Error"] = "No CFB boxes with baselines found for your branch.";
                return RedirectToAction(nameof(Index));
            }

            return View(new TopupCreateViewModel
            {
                BouBranchId = branchId,
                BouBranchName = branchName,
                Boxes = boxSections,
            });
        }

        // ── Edit GET ─────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var roles = GetUserRoles();
            if (!roles.Contains(SystemPolicy.Roles.CfbSupervisor))
                return Forbid();

            var topup = await _uow.Requests.FindByIdWithLines(id);
            if (topup is null) return NotFound();

            if (topup.CurrentStatusName != CfbConstants.CfbStatusNames.Draft
                && topup.CurrentStatusName != CfbConstants.CfbStatusNames.ReturnedForRevision)
            {
                TempData["Error"] = "Only draft or returned requests can be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var allBranches = await _configServiceClient.GetAllBouBranchesAsync();
            var (branchId, branchName) = GetUserBranchInfo(roles, allBranches);
            if (topup.BouBranchId != branchId)
                return Forbid();

            var boxes = await _boxRepo.FindByBranchId(topup.BouBranchId);
            var allDenoms = (await _stockService.GetAllDenominations()).Where(d => !d.IsDeleted).ToList();
            var denomById = allDenoms.ToDictionary(d => d.Id);

            // Existing lines keyed by (box, denomination) so the view can pre-fill their saved quantity.
            var existingByKey = topup.Lines.ToDictionary(
                l => (l.CfbBoxId, l.DenominationId),
                l => l.DenomValue > 0 ? (int)(l.Amount / (l.DenomValue * 1000L)) : 0);

            var boxSections = new List<TopupBoxSectionViewModel>();
            foreach (var box in boxes)
            {
                var lines = box.Holdings.Select(h =>
                {
                    denomById.TryGetValue(h.DenominationId, out var d);
                    existingByKey.TryGetValue((box.Id, h.DenominationId), out var existingQty);
                    return new TopupCreateLineViewModel
                    {
                        DenominationId = h.DenominationId,
                        DenominationName = d?.Name ?? $"Denom {h.DenominationId}",
                        DenomValue = h.DenomValue,
                        CurrentBaseline = h.Amount,
                        ExistingQuantity = existingByKey.ContainsKey((box.Id, h.DenominationId)) ? existingQty : null,
                    };
                }).OrderBy(l => l.DenomValue).ToList();

                if (lines.Count > 0)
                {
                    boxSections.Add(new TopupBoxSectionViewModel
                    {
                        BoxId = box.Id,
                        BoxName = box.Name,
                        BoxType = box.BoxType,
                        Lines = lines,
                    });
                }
            }

            return View(new TopupCreateViewModel
            {
                Id = topup.Id,
                BouBranchId = topup.BouBranchId,
                BouBranchName = branchName,
                Boxes = boxSections,
            });
        }

        // ── SaveDraft POST ───────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft(
            int? id,
            int bouBranchId,
            List<int> denominationIds,
            List<int> boxIds,
            List<int> denomValues,
            List<int> quantities,
            List<long> baselineBeforeTopups,
            CancellationToken ct)
        {
            var roles = GetUserRoles();
            if (!roles.Contains(SystemPolicy.Roles.CfbSupervisor))
                return Forbid();

            // The increment amount is always derived from quantity * 1000 * denomValue — never
            // taken from the client — so a partial bundle/box can never be saved.
            var lines = denominationIds
                .Select((denomId, i) => (denomId, boxId: boxIds.ElementAtOrDefault(i),
                    denomValue: denomValues.ElementAtOrDefault(i), quantity: quantities.ElementAtOrDefault(i),
                    baseline: baselineBeforeTopups.ElementAtOrDefault(i)))
                .Where(x => x.quantity > 0)
                .Select(x => (x.denomId, x.boxId, x.denomValue, amount: (long)x.quantity * 1000 * x.denomValue, x.baseline))
                .ToList();

            if (lines.Count == 0)
            {
                TempData["Error"] = "Enter at least one bundle or box to top up.";
                return id.HasValue
                    ? RedirectToAction(nameof(Edit), new { id })
                    : RedirectToAction(nameof(Create));
            }

            if (id.HasValue)
            {
                // ── EDIT path ──────────────────────────────────────────────
                var existingTopup = await _uow.Requests.FindByIdWithLines(id.Value);
                if (existingTopup is null)
                {
                    TempData["Error"] = "Top-up request not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (existingTopup.CurrentStatusName != CfbConstants.CfbStatusNames.Draft
                    && existingTopup.CurrentStatusName != CfbConstants.CfbStatusNames.ReturnedForRevision)
                {
                    TempData["Error"] = "Only draft or returned requests can be edited.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var allBranches = await _configServiceClient.GetAllBouBranchesAsync();
                var (branchId, _) = GetUserBranchInfo(roles, allBranches);
                if (existingTopup.BouBranchId != branchId)
                    return Forbid();

                existingTopup.Lines.Clear();
                foreach (var l in lines)
                {
                    existingTopup.Lines.Add(new TopupLine
                    {
                        CfbBoxId = l.boxId,
                        DenominationId = l.denomId,
                        DenomValue = l.denomValue,
                        Amount = l.amount,
                        BaselineBeforeTopup = l.baseline,
                    });
                }

                try
                {
                    await _uow.Requests.Update(existingTopup);
                    TempData["Success"] = "Draft updated.";
                    return RedirectToAction(nameof(Details), new { id = existingTopup.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SaveDraft (edit) failed for request {Id}", id);
                    TempData["Error"] = "An error occurred saving the draft.";
                    return RedirectToAction(nameof(Edit), new { id });
                }
            }

            // ── CREATE path ────────────────────────────────────────────────
            var draftStatus = await _workflowServiceClient.FindStatusByName(new WorkflowService.Client.Dto.GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                EntityTypeName = StatusConstants.Entity.CfbTopupRequest,
                StatusName = CfbConstants.CfbStatusNames.Draft,
            });

            if (draftStatus is null)
            {
                TempData["Error"] = "Workflow is not configured for CFB Top-Up. Contact support.";
                return RedirectToAction(nameof(Index));
            }

            var topup = new TopupRequest
            {
                BouBranchId = bouBranchId,
                CurrentStatusId = draftStatus.Id,
                CurrentStatusName = draftStatus.Name,
                Lines = lines.Select(l => new TopupLine
                {
                    CfbBoxId = l.boxId,
                    DenominationId = l.denomId,
                    DenomValue = l.denomValue,
                    Amount = l.amount,
                    BaselineBeforeTopup = l.baseline,
                }).ToList(),
            };

            try
            {
                await _uow.Requests.Create(topup);
                await _uow.SaveAsync(ct);
                TempData["Success"] = "Draft saved.";
                return RedirectToAction(nameof(Details), new { id = topup.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveDraft failed for branch {BranchId}", bouBranchId);
                TempData["Error"] = "An error occurred saving the draft.";
                return RedirectToAction(nameof(Create));
            }
        }

        // ── Details GET ──────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var roles = GetUserRoles();
            var (isSupervisor, isVerifier, isApprover) = GetCfbRoles(roles);
            if (!isSupervisor && !isVerifier && !isApprover)
                return Forbid();

            var topup = await _uow.Requests.FindById(id);
            if (topup is null)
                return NotFound();

            // Branch-restricted supervisors may only see their own branch's requests
            if (isSupervisor && !isApprover && !isVerifier)
            {
                var allBranches = await _configServiceClient.GetAllBouBranchesAsync();
                var (branchId, _) = GetUserBranchInfo(roles, allBranches);
                if (topup.BouBranchId != branchId)
                    return Forbid();
            }

            var allBranchesForName = await _configServiceClient.GetAllBouBranchesAsync();
            var branchDto = allBranchesForName.FirstOrDefault(b => b.Id == topup.BouBranchId);

            var boxes = await _boxRepo.FindByBranchId(topup.BouBranchId);
            var boxById = boxes.ToDictionary(b => b.Id);
            var allDenoms = (await _stockService.GetAllDenominations()).Where(d => !d.IsDeleted).ToList();
            var denomById = allDenoms.ToDictionary(d => d.Id);

            var lineVms = topup.Lines.Select(l =>
            {
                denomById.TryGetValue(l.DenominationId, out var d);
                boxById.TryGetValue(l.CfbBoxId, out var box);
                return new TopupDetailsLineViewModel
                {
                    DenominationId = l.DenominationId,
                    DenominationName = d?.Name ?? $"Denom {l.DenominationId}",
                    DenomValue = l.DenomValue,
                    CurrentBaseline = l.BaselineBeforeTopup,
                    IncrementAmount = l.Amount,
                    BoxName = box?.Name ?? $"Box {l.CfbBoxId}",
                };
            }).OrderBy(l => l.BoxName).ThenBy(l => l.DenomValue).ToList();

            var userRoleIds = User.GetAppRoleIds();
            var allTransitions = new List<WorkflowTransitionDto>();
            foreach (var roleId in userRoleIds)
            {
                var roleTransitions = await _workflowService.GetAvailableTransitionsAsync(
                    StatusConstants.Entity.CfbTopupRequest,
                    topup.CurrentStatusId,
                    roleId);
                allTransitions.AddRange(roleTransitions);
            }
            var transitions = allTransitions.DistinctBy(t => t.ActionName).ToList();

            var historyVms = topup.StatusHistory
                .OrderBy(h => h.ChangedAt)
                .Select(h => new TopupStatusHistoryViewModel
                {
                    OldStatusName = h.OldStatusName,
                    NewStatusName = h.NewStatusName,
                    ChangedBy = h.ChangedBy,
                    ChangedAt = h.ChangedAt,
                    Remarks = h.Remarks,
                }).ToList();

            return View(new TopupDetailsViewModel
            {
                Id = topup.Id,
                BouBranchName = branchDto?.Name ?? topup.BouBranchId.ToString(),
                Justification = topup.Justification,
                CurrentStatusName = topup.CurrentStatusName,
                CurrentStatusId = topup.CurrentStatusId,
                CreatedBy = topup.CreatedBy,
                CreatedDate = topup.CreatedDate,
                TotalAmount = topup.Lines.Sum(l => l.Amount),
                Lines = lineVms,
                StatusHistory = historyVms,
                AvailableTransitions = transitions,
            });
        }

        // ── ExecuteAction POST ───────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(int id, string actionName, string remarks, CancellationToken ct)
        {
            var topup = await _uow.Requests.FindByIdWithLines(id);
            if (topup is null)
            {
                TempData["Error"] = "Top-up request not found.";
                return RedirectToAction(nameof(Index));
            }

            var username = User.GetUsernameOrSystem();
            var userRoles = User.GetAppRoleIds();

            var isApprove = string.Equals(actionName, CfbConstants.CfbStatusActionNames.Approve,
                StringComparison.OrdinalIgnoreCase);

            if (isApprove)
            {
                using var transaction = await _uow.BeginTransactionAsync(ct);
                try
                {
                    var ok = await _workflowService.ExecuteTransition(
                        entityType: StatusConstants.Entity.CfbTopupRequest,
                        entityId: id,
                        currentStatusId: topup.CurrentStatusId,
                        actionName: actionName,
                        username: username,
                        userRoles: userRoles,
                        remarks: remarks);

                    if (!ok)
                    {
                        TempData["Error"] = "Transition failed. Check your role or request status.";
                        return RedirectToAction(nameof(Details), new { id });
                    }

                    // Raise the baseline for each line at current live active row
                    foreach (var line in topup.Lines)
                    {
                        var existing = await _uow.Holdings.FindActiveByBoxAndDenom(line.CfbBoxId, line.DenominationId);
                        if (existing is null)
                        {
                            throw new InvalidOperationException(
                                $"No active baseline found for denomination {line.DenominationId} in box {line.CfbBoxId}.");
                        }

                        existing.IsActive = false;
                        await _uow.Holdings.Create(new CfbCashHolding
                        {
                            CfbBoxId = line.CfbBoxId,
                            DenominationId = line.DenominationId,
                            DenomValue = existing.DenomValue,
                            Amount = existing.Amount + line.Amount,
                            IsActive = true,
                        });
                    }

                    // Record the stock movement + ledger for the cash moved out of the branch's
                    // main store into each CFB box — always Reissue, since CFB top-ups draw from
                    // existing circulated stock, never fresh new notes.
                    var referenceCode = $"CFB-TR-{topup.Id:D4}";

                    var allCategories = await _categoryRepository.FindAll();
                    var reissueCategory = allCategories.FirstOrDefault(c => c.Name == StockConstants.StoreTypes.Reissue);

                    var counterPartyType = await _counterPartyTypeRepository.FindByName(StockConstants.StockCounterPartyType.Cfb);

                    var dispatchedStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        StatusName = StatusConstants.StockMovement.Dispatched,
                        EntityTypeName = StatusConstants.Entity.StockMovement,
                    });

                    var branchBoxes = await _boxRepo.FindByBranchId(topup.BouBranchId);
                    var boxById = branchBoxes.ToDictionary(b => b.Id);

                    // Bundles/boxes aren't stored on TopupLine (only Amount is), so the quantity
                    // is derived here from Amount / (1000 * DenomValue) — the same 1000-per-bundle
                    // convention withdrawal uses. This division is only guaranteed to come out
                    // whole once entry is quantity-first (Phase 2); until then it's best-effort
                    // against today's looser "multiple of 1000 UGX" validation.
                    static int DeriveQuantity(long amount, int denomValue)
                        => denomValue > 0 ? (int)(amount / (denomValue * 1000L)) : 0;

                    foreach (var boxGroup in topup.Lines.GroupBy(l => l.CfbBoxId))
                    {
                        boxById.TryGetValue(boxGroup.Key, out var box);
                        var isCoinBox = box?.BoxType == CfbConstants.BoxTypes.Coins;
                        var containerType = isCoinBox
                            ? StockConstants.ContainerTypes.Box
                            : StockConstants.ContainerTypes.Bag;

                        var movement = new StockMovement
                        {
                            MovementType = StockConstants.StockMovementTypes.BranchToCfb,
                            Date = DateTime.Now,
                            ReferenceNumber = referenceCode,
                            DispatchingBranchId = topup.BouBranchId,
                            ReceivingBranchId = null,
                            GrandTotalValue = boxGroup.Sum(l => (decimal)l.Amount),
                            CurrentStatusId = dispatchedStatus.Id,
                            CurrentStatusName = dispatchedStatus.Name,
                            Denominations = boxGroup.Select(l =>
                            {
                                var quantity = DeriveQuantity(l.Amount, l.DenomValue);
                                return new StockMovementDenomination
                                {
                                    DenominationId = l.DenominationId,
                                    NumBundles = isCoinBox ? null : quantity,
                                    NumBagsBoxes = isCoinBox ? quantity : null,
                                    CategoryId = reissueCategory?.Id,
                                    Categorization = reissueCategory?.Name,
                                    ContainerType = containerType,
                                    SubTotalValue = l.Amount,
                                    DenominationType = "Sent",
                                };
                            }).ToList(),
                        };

                        var newStockMovement = await _stockMovementRepository.CreateAsync(movement);

                        var stockLedgerDetails = boxGroup.Select(l =>
                        {
                            var quantity = DeriveQuantity(l.Amount, l.DenomValue);
                            return new StockLedgerDetail
                            {
                                CreatedDate = DateTime.Now,
                                ModifiedDate = DateTime.Now,
                                CreatedBy = username,
                                ModifiedBy = username,
                                DenominationId = l.DenominationId,
                                NumberOfBoxes = isCoinBox ? quantity : 0,
                                NumberOfBundles = isCoinBox ? null : quantity,
                                NumberOfPieces = quantity * 1000,
                                Value = l.Amount,
                                CategoryId = reissueCategory?.Id ?? 0,
                                SubCategoryId = 0,
                            };
                        }).ToList();

                        var stockLedger = new StockLedger
                        {
                            ReferenceCode = referenceCode,
                            StockMovementId = newStockMovement.Id,
                            IsIn = false,
                            CounterPartyTypeId = counterPartyType.Id,
                            CounterPartyId = boxGroup.Key,
                            StoreId = topup.BouBranchId,
                            Notes = remarks,
                            CreatedBy = username,
                            CreatedDate = DateTime.Now,
                            ModifiedBy = username,
                            ModifiedDate = DateTime.Now,
                            StockLedgerDetails = stockLedgerDetails,
                        };

                        bool createdLedger = await _stockLedgerRepository.Create(stockLedger);
                        if (!createdLedger)
                        {
                            _logger.LogWarning("Failed to create stock ledger for CFB top-up {Id}, box {BoxId}", id, boxGroup.Key);
                        }
                    }

                    await _uow.SaveAsync(ct);
                    await transaction.CommitAsync(ct);
                    TempData["Success"] = "Top-up approved and baselines updated.";
                }
                catch (InvalidOperationException ex)
                {
                    await transaction.RollbackAsync(ct);
                    _logger.LogWarning(ex, "Approve failed for TopupRequest {Id}", id);
                    TempData["Error"] = ex.Message;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(ct);
                    _logger.LogError(ex, "Approve failed for TopupRequest {Id}", id);
                    TempData["Error"] = "An error occurred during approval.";
                }
            }
            else
            {
                var ok = await _workflowService.ExecuteTransition(
                    entityType: StatusConstants.Entity.CfbTopupRequest,
                    entityId: id,
                    currentStatusId: topup.CurrentStatusId,
                    actionName: actionName,
                    username: username,
                    userRoles: userRoles,
                    remarks: remarks);

                if (ok)
                    TempData["Success"] = $"Submitted Successfully";
                else
                    TempData["Error"] = "Transition failed. Check your role or request status.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}

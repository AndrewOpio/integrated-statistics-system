using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VMSService.Core.Models.Cfb;
using VMSService.Core.Repositories.CfbBoxes;
using VMSService.Core.Repositories.CfbCashHoldings;
using VMSService.Core.Repositories.CfbUnitOfWorks;
using VMSService.Core.Services.Stocks;
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

        // ── SaveDraft POST ───────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft(
            int bouBranchId,
            List<int> denominationIds,
            List<int> boxIds,
            List<int> denomValues,
            List<long> amounts,
            List<long> baselineBeforeTopups,
            CancellationToken ct)
        {
            var roles = GetUserRoles();
            if (!roles.Contains(SystemPolicy.Roles.CfbSupervisor))
                return Forbid();

            var lines = denominationIds
                .Select((denomId, i) => (denomId, boxId: boxIds.ElementAtOrDefault(i),
                    denomValue: denomValues.ElementAtOrDefault(i), amount: amounts.ElementAtOrDefault(i),
                    baseline: baselineBeforeTopups.ElementAtOrDefault(i)))
                .Where(x => x.amount > 0)
                .ToList();

            if (lines.Count == 0)
            {
                TempData["Error"] = "Enter at least one positive increment amount.";
                return RedirectToAction(nameof(Create));
            }

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

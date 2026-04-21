using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Models.CbWithdrawRequest;
using VMSService.Core.Models.WithdrawalRequestDenominations;
using VMSService.Core.Models.WithdrawalRequests;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using WorkflowService.Client;
using static VMSService.Core.Static.WithdrawStatuses;

namespace VMSService.Web.Controllers
{
    public class WithdrawalRequestsController(
        IWithdrawalRequestRepository _withdrawalRequestRepo,
        IWorkflowServiceClient _workflowServiceClient,
        IWorkflowService _workflowService,
        IAgentDetailRepository _agentRepo,
        IConfigurationsServiceClient _configServiceClient,
        IDenominationRepository _denomRepo,
        IWithdrawalUnitOfWork _uow,
        ILogger<WithdrawalRequestsController> _logger,
        IMapper _mapper) : Controller
    {
        public async Task<IActionResult> Index()
        {
            //var all = (await _uow.Requests.FindAll()
            //    ).Where(x => !x.IsDeleted);

            var allRequests = await _withdrawalRequestRepo.FindAll();

            allRequests = allRequests.Where(x => !x.IsDeleted).ToList();

            var allStatuses = await _workflowServiceClient.FindAllStatuses();

            allStatuses = [.. allStatuses.Where(x => x.EntityType.Name == StatusConstants.Entity.WithdrawalRequest)];

            // dictionary for O(1) lookup
            var statusMap = allStatuses.ToDictionary(s => s.Id, s => s.Name);

            var allCbs = await _configServiceClient.GetAllCommercialBankAsync();

            // dictionary for O(1) lookup
            var cbMap = allCbs.ToDictionary(s => s.Id, s => s.Name);

            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            var allBouBranches = await _configServiceClient.GetAllBouBranchesAsync();

            // ── Counts ───────────────────────────────────────────────────────────────

            // Pending CB approval (Draft or ReturnedForRevision — still with the CB)
            var pendingCb = allRequests.Count(r =>
                !r.IsDeleted &&
                (r.StatusTypeName == StatusConstants.WithdrawalRequest.PendingInternalApproval));

            // Special approval pending
            var specialApproval = allRequests.Count(r =>
                !r.IsDeleted &&
                r.RequiresSpecialApproval);

            // Under bank processing (submitted to bank, recommended — not yet approved)
            var underBankProcessing = allRequests.Count(r =>
                !r.IsDeleted &&
                (r.StatusTypeName == StatusConstants.WithdrawalRequest.PendingDisbursement
                 || r.StatusTypeName == StatusConstants.WithdrawalRequest.Recommended));

            // All ever processed by bank (all approved, all time)
            var totalProcessed = allRequests
                .Where(r => !r.IsDeleted && r.StatusTypeName == StatusConstants.WithdrawalRequest.Authorized)
                .Sum(r => r.TotalAmount);

            // ── Requests table ───────────────────────────────────────────────────────
            var rows = allRequests.Select(r => new RecentRequestRowViewModel
            {
                Id = r.Id,
                TransactionRefNumber = r.TransactionRefNumber ?? "Not Generated",
                Amount = r.TotalAmount,
                StatusTypeName = statusMap.GetValueOrDefault(r.StatusTypeId, "Unknown"),
                CbName = cbMap.GetValueOrDefault(r.CbId, "No CB"),
                CollectionDate = r.CollectionDate,
                CreationDate = r.CreatedDate,
                RequiresSpecialApproval = r.RequiresSpecialApproval,
                RtgsCode = string.IsNullOrEmpty(r.RtgsCode) ? "—" : r.RtgsCode,
                BouBranchCode = r.BouBranchCode
            }).ToList();

            //var rows = await Task.WhenAll(allRequests
            //    .Where(r => !r.IsDeleted)
            //    .OrderByDescending(r => r.CreatedDate)
            //    .Select(async r =>
            //    {
            //        var status = await _workflowServiceClient.FindStatusById(r.StatusTypeId);
            //        return new RecentRequestRowViewModel
            //        {
            //            Id = r.Id,
            //            TransactionRefNumber = r.TransactionRefNumber ?? "Not Generated",
            //            Amount = r.TotalAmount,
            //            StatusTypeName = statusMap.GetValueOrDefault(r.StatusTypeId, "Unknown"),
            //            //StatusId = r.StatusTypeId,
            //            CollectionDate = r.CollectionDate,
            //            CreationDate = r.CreatedDate,
            //            RequiresSpecialApproval = r.RequiresSpecialApproval,
            //            RtgsCode = string.IsNullOrEmpty(r.RtgsCode) ? "—" : r.RtgsCode,
            //            BouBranchCode = r.BouBranchCode
            //        };
            //    }));


            var vm = new BouWithdrawalRequestViewModel
            {
                PendingCbCount = pendingCb,
                SpecialApprovalCount = specialApproval,
                UnderBouProcessing = underBankProcessing,
                TotalProcessed = totalProcessed,
                Requests = rows,
                BouBranches = allBouBranches,


            };

            return View(vm);
        }

        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {


            var request = await _uow.Requests.GetWithDetailsAsync(id, ct);
            if (request is null) return NotFound();


            var cb = await _configServiceClient.GetCbIdAsync(request.CbId);
            var bankBranch = await _configServiceClient.GetBouBranchByIdAsync(request.BouBranchId);

            // ── Agent details ─────────────────────────────────────────────────────
            var cbAgentIds = request.WithdrawalRequestAgents
                .Where(a => a.EnteredBy == WithdrawalEnteredBy.Cb)
                .Select(a => a.AgentId)
                .ToList();

            var agentDetails = new List<AgentDetailViewModel>();
            foreach (var agentId in cbAgentIds)
            {
                var agent = await _agentRepo.FindById(agentId);
                if (agent is not null)
                    agentDetails.Add(_mapper.Map<AgentDetailViewModel>(agent));
            }

            var canEdit = (request.StatusTypeId == WithdrawalStatusIds.Draft
              || request.StatusTypeId == WithdrawalStatusIds.ReturnedForRevision);

            var allDenoms = await _denomRepo.FindAll();

            var denomList = allDenoms.ToList();

            var denomVm = new DenomTableViewModel
            {
                Notes = denomList.Where(d => d.DenominationType.Name.Equals("Note")),
                Coins = denomList.Where(d => d.DenominationType.Name.Equals("Coin")),
            };

            var displayOrigin = ResolveDisplayOrigin(request.StatusTypeName);

            var displayLines = request.WithdrawalRequestDenominations
                .Where(d => d.Origin == displayOrigin)
                .ToList();

            var notes = displayLines
                .Where(d => d.Bundles.HasValue)
                .OrderByDescending(d => d.DenominationValue)
                .Select(d => new DenominationLineViewModel
                {
                    DenominationLabel = $"UGX {d.DenominationValue:N0}",
                    Bundles = d.Bundles,
                    Amount = d.Amount,
                })
                .ToList();

            var coins = displayLines
                .Where(d => d.Boxes.HasValue)
                .OrderByDescending(d => d.DenominationValue)
                .Select(d => new DenominationLineViewModel
                {
                    DenominationLabel = $"UGX {d.DenominationValue:N0}",
                    Boxes = d.Boxes,
                    Amount = d.Amount,
                })
                .ToList();

            var canEditDispatch =
                request.StatusTypeName == StatusConstants.WithdrawalRequest.PendingRecommendation
                || request.StatusTypeName == StatusConstants.WithdrawalRequest.Returned;

            var editableBankRows = canEditDispatch
                ? request.WithdrawalRequestDenominations
                    .Where(d => d.Origin == "BOU")
                    .Select(d => new BouDenominationEditRowViewModel
                    {
                        Id = d.Id,
                        DenominationId = d.DenominationId,
                        DenominationValue = d.DenominationValue,
                        DenominationTypeId = d.DenominationTypeId,
                        BundleType = d.BundleType,
                        Bundles = d.Bundles,
                        Boxes = d.Boxes,
                        Amount = d.Amount,
                        BoxNumbers = d.DispatchedBoxes?.Select(b => b.BoxNo).ToList() ?? new(),
                    })
                    .ToList()
                : new List<BouDenominationEditRowViewModel>();

            var currentStatus = await _workflowServiceClient.FindStatusById(request.StatusTypeId);


            var Steps = BuildStepViewModel(request.StatusTypeId,
                           request.StatusTypeId == WithdrawalStatusIds.Approved);

            // ── Build ViewModel ───────────────────────────────────────────────────
            var vm = new BouWithdrawalRequestDetailsViewModel
            {
                Id = request.Id,
                TransactionRefNumber = request.TransactionRefNumber,
                StatusId = request.StatusTypeId,
                StatusName = request.StatusTypeName,
                CollectionDate = request.CollectionDate,
                RtgsDate = request.RtgsDate,
                RtgsCode = request.RtgsCode,
                RequiresSpecialApproval = request.RequiresSpecialApproval,
                CbName = cb?.Name ?? "—",
                CbCode = cb?.Code ?? "—",
                BouBranchName = bankBranch?.Name ?? "—",
                BouBranchCode = request.BouBranchCode,
                WithdrawAgents = agentDetails,
                Notes = notes,
                Coins = coins,
                TotalNotesAmount = request.TotalNotesAmount,
                TotalCoinsAmount = request.TotalCoinsAmount,
                TotalAmount = request.TotalAmount,
                CurrentStatusName = currentStatus.Name,
                Denoms = denomVm,
                //CanEdit = canEdit,
                IsLumpSum = request.IsLumpSum,
                IsPostApprovalView = displayOrigin == WithdrawalOrigin.Bou,
                Steps = BuildStepViewModel(request.StatusTypeId,
                           request.StatusTypeId == WithdrawalStatusIds.Approved),
                CanEditDispatch = canEditDispatch,
                EditableBankRows = editableBankRows,
            };
            vm.AvailableTransitions = (await _workflowService.GetAvailableTransitionsAsync(
                StatusConstants.Entity.WithdrawalRequest,
                request.StatusTypeId,
                GetUserRole()))
                .Where(t => t.ActionName switch
                {
                    "Exceptional Approval" => request.RequiresSpecialApproval,
                    "Request Withdraw" => !request.RequiresSpecialApproval,
                    _ => true  // return and any others always show
                })
                .ToList();


            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveBankDenominations(Guid id, string payload, string actionName, string remarks, CancellationToken ct)
        {
            try
            {
                var request = await _uow.Requests.FindById(id);
                if (request is null)
                {
                    TempData["Error"] = "Withdrawal request not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Only editable in these statuses
                if (request.StatusTypeName != StatusConstants.WithdrawalRequest.PendingDisbursement
                    && request.StatusTypeName != StatusConstants.WithdrawalRequest.Returned)
                {
                    TempData["Error"] = "This request is not editable in its current status.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var rows = JsonSerializer.Deserialize<List<BouDenominationEditRowViewModel>>(
                    payload ?? "[]",
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new();

                // Wipe + recreate Bank-origin rows
                await _uow.Denominations.DeleteBouRowsWithChildrenAsync(id, ct);

                foreach (var r in rows)
                {
                    var newRow = new WithdrawalRequestDenomination
                    {
                        WithdrawalRequestId = id,
                        DenominationId = r.DenominationId,
                        DenominationValue = r.DenominationValue,
                        DenominationTypeId = r.DenominationTypeId,
                        Bundles = r.Bundles,
                        Boxes = r.Boxes,
                        Amount = r.Amount,
                        Origin = "Bank",
                        BundleType = r.BundleType ?? BundleTypes.NotApplicable,
                        DispatchedBoxes = r.BoxNumbers
                            .Select(bn => new WithdrawalRequestDenominationBox { BoxNo = bn })
                            .ToList()
                    };
                    await _uow.Denominations.Create(newRow);
                }

                await _uow.SaveAsync(ct);

                // Now fire the workflow transition
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var userRoles = User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith("VMS:"))
                    .Select(c => int.TryParse(c.Value.Split(":")[1], out var roleId) ? roleId : (int?)null)
                    .Where(r => r.HasValue).Select(r => r!.Value).ToList();

                var executed = await _workflowService.ExecuteTransition(
                    entityType: StatusConstants.Entity.WithdrawalRequest,
                    entityId: id,
                    currentStatusId: request.StatusTypeId,
                    actionName: actionName,
                    username: username,
                    userRoles: userRoles,
                    remarks: remarks);

                TempData[executed ? "Success" : "Error"] = executed
                    ? "Dispatch saved successfully."
                    : "Saved denominations, but could not transition the workflow.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving bank denominations for {Id}", id);
                TempData["Error"] = "An error occurred while saving.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // generic transition action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(Guid id, string actionName, string remarks, CancellationToken ct)
        {
            try
            {
                var request = await _uow.Requests.FindById(id);
                if (request is null)
                {
                    TempData["Error"] = "Withdrawal request not found.";
                    return RedirectToAction(nameof(Index));
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var userRoles = User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith("VMS:"))
                    .Select(c => int.TryParse(c.Value.Split(":")[1], out var roleId)
                        ? roleId
                        : (int?)null)
                    .Where(r => r.HasValue)
                    .Select(r => r!.Value)
                    .ToList();

                bool isExecuted = await _workflowService.ExecuteTransition(
                    entityType: StatusConstants.Entity.WithdrawalRequest,
                    entityId: id,
                    currentStatusId: request.StatusTypeId,
                    actionName: actionName,
                    username: username,
                    userRoles: userRoles,
                    remarks: remarks);

                if (isExecuted)
                    TempData["Success"] = "Action completed successfully.";
                else
                    TempData["Error"] = "Could not execute action. You may not have permission or the action is invalid for the current status.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error executing action {Action} on withdrawal request {Id}",
                    actionName, id);
                TempData["Error"] = "An error occurred while executing the action.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        private WithdrawalStepViewModel BuildStepViewModel(int statusId, bool isApproved)
            => WithdrawalStepViewModelFactory.Build(
                currentStepKey: GetCurrentStepKey(statusId),
                isRequestApproved: isApproved,
                user: User);
        private static string GetCurrentStepKey(int statusId) => statusId switch
        {
            WithdrawalStatusIds.Draft => "CbMaker",
            WithdrawalStatusIds.ReturnedForRevision => "CbMaker",
            WithdrawalStatusIds.SubmittedToSupervisor => "CbApprover",
            WithdrawalStatusIds.PendingSpecialApproval => "BOU",
            WithdrawalStatusIds.PendingRecommendation => "BOU",
            WithdrawalStatusIds.Recommended => "BOU",
            WithdrawalStatusIds.Approved => "BOU",
            WithdrawalStatusIds.Rejected => "BOU",
            _ => "CbMaker",
        };
        private int GetUserRole()
            => User.FindAll("appRoleId")
            .Where(c => c.Value.StartsWith("VMS:"))
            .Select(c => int.TryParse(c.Value.Split(":")[1], out var roleId) ? roleId : 0)
            .FirstOrDefault(r => r > 0);

    }
}

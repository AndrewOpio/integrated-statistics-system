using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VMSService.Core.Authorization;
using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Models.CbWithdrawRequest;
using VMSService.Core.Models.WithdrawalRequestDenominations;
using VMSService.Core.Models.WithdrawalRequests;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.WithdrawalRequestAgents;
using VMSService.Core.Repositories.WithdrawalRequestDenominations;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalRequestStatusHistories;
using VMSService.Core.Repositories.WithdrawalTransactionRefNumberGenerators;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Services.Identity;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using VMSService.Web.Helpers;
using WorkflowService.Client;
using WorkflowService.Client.Dto;
using static VMSService.Core.Static.WithdrawStatuses;

namespace VMSService.Web.Controllers
{

    public class CbWithdrawRequestsController(
    IAuthorizationService authorizationService,
    IAgentDetailRepository _agentRepo,
    IConfigurationsServiceClient _configServiceClient,
    IDenominationRepository _denomRepo,
    IWithdrawalTransactionRefNumberGenerator _txnrefNoGenerator,
    IWithdrawalUnitOfWork _uow,
    IWithdrawalRequestRepository _withdrawalRequestRepo,
    IWithdrawalRequestAgentRepository _withdrawalRequestAgentRepo,
    IWithdrawalRequestDenominationRepository _withdrawalRequestDenomRepo,
    IWorkflowServiceClient _workflowServiceClient,
    IWorkflowService _workflowService,
    IWithdrawalStatusResolver _statusResolver,
    IAttachmentRepository _attachmentRepository,
    IIdentityService _identityService,
    ILogger<CbWithdrawRequestsController> _logger,
    IMapper _mapper) : BaseWithdrawController(authorizationService)
    {
        public IActionResult Index()
        {
            return View();
        }

        // ── Create ───────────────────────────────
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            var cbId = GetCbIdFromClaims();

            var bouBranches = await _configServiceClient.GetAllBouBranchesAsync();
            var cb = await _configServiceClient.GetCbIdAsync(cbId);

            var agents = await _agentRepo.FindAll();
            var denoms = await _denomRepo.FindAll();
            var denomList = denoms.ToList();

            return View(new CreateWithdrawViewModel
            {
                Steps = BuildStepViewModel(WithdrawalStatusNames.Draft, false, false),
                Branches = new FromToSelectionViewModel
                {
                    Cb = cb,
                    BouBranches = bouBranches,
                },
                Agents = new AgentSelectionViewModel
                {
                    Agents = agents
                        .Where(a => a.CommercialBankId == cbId)
                        .Select(a => _mapper.Map<AgentDetailViewModel>(a))
                        .ToList(),
                },
                Denoms = new DenomTableViewModel
                {
                    Notes = denomList.Where(d => d.DenominationType?.Name?.Equals("Note") ?? false),
                    Coins = denomList.Where(d => d.DenominationType?.Name?.Equals("Coin") ?? false),
                },
            });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var request = await _uow.Requests.GetWithDetailsAsync(id, ct);
            if (request is null) return NotFound();
            //if (!await CanAccessRequestAsync(request.CbId)) return Forbid();

            // Only allow editing in Draft or ReturnedForRevision
            if (request.StatusTypeName != WithdrawalStatusNames.Draft
                && request.StatusTypeName != WithdrawalStatusNames.ReturnedForRevision)
                return RedirectToAction("Details", new { id });

            var branchVm = new FromToSelectionViewModel
            {
                Cb = await _configServiceClient.GetCbIdAsync(request.CbId),
                BouBranches = await _configServiceClient.GetAllBouBranchesAsync(),
                SelectedBouBranchId = request.BouBranchId,
                IsEdit = true,
                CollectionDate = request.CollectionDate,
                CreatedDate = request.CreatedDate,
            };


            var agentDetails = (await _agentRepo.FindAll()).Where(a => a.IsDeleted == false);

            var agentDetailsVm = _mapper.Map<List<AgentDetailViewModel>>(agentDetails);

            var requestAgents = await _withdrawalRequestAgentRepo.GetByRequestAsync(request.Id, ct);


            var agentVm = new AgentSelectionViewModel
            {
                Agents = agentDetailsVm,
                SelectedAgentIds = requestAgents.Select(a => a.AgentId).ToList(),
            };

            var allDenoms = await _denomRepo.FindAll();
            var denomList = allDenoms.ToList();
            var selectedDenoms = await _withdrawalRequestDenomRepo.GetByRequestAndOriginAsync(request.Id, WithdrawalEnteredBy.Cb);

            var denomVm = new DenomTableViewModel
            {
                Notes = denomList.Where(d => d.DenominationType.Name.Equals("Note")),
                Coins = denomList.Where(d => d.DenominationType.Name.Equals("Coin")),
                SelectedDenominations = selectedDenoms,
                IsLumpSum = request.IsLumpSum,
                LumpSumAmount = request.IsLumpSum ? request.TotalAmount : 0,
                IsEdit = request.Id != Guid.Empty,
            };


            return View(new EditWithdrawViewModel
            {
                Request = request,
                Steps = BuildStepViewModel(request.StatusTypeName, false, request.RequiresSpecialApproval),
                Branches = branchVm,
                Agents = agentVm,
                Denoms = denomVm,
            });
        }

        // ── Branch details endpoint (AJAX) ────────
        public async Task<IActionResult> BranchDetails(string type, int id)
        {
            if (type == "cb")
            {
                var cb = await _configServiceClient.GetCbIdAsync(id);

                if (cb is null)
                    return NotFound();

                return Json(new { cb.Id, name = cb.Name, code = cb.Code, cbCode = cb.Code });
            }

            if (type == "bou")
            {
                var bouBranch = await _configServiceClient.GetBouBranchByIdAsync(id);

                if (bouBranch is null)
                    return NotFound();

                return Json(new { bouBranch.Id, name = bouBranch.Name, code = bouBranch.BranchCode });
            }

            return BadRequest("Invalid branch type.");
        }

        //[HttpGet("agent-details")]
        public async Task<IActionResult> AgentDetails(int id)
        {
            var agentDetail = await _agentRepo.FindById(id);
            if (agentDetail == null)
            {
                return NotFound();
            }

            var agentDetailVm = _mapper.Map<AgentDetailViewModel>(agentDetail);

            return Json(new
            {
                agentDetailVm.AgentNames,
                agentDetailVm.NIN,
                agentDetailVm.WorkIdScan,
                agentDetailVm.PhoneNumber,
                agentDetailVm.CommercialBankBranchId,
                agentDetailVm.BranchId,
                agentDetailVm.PassportPhoto,
                agentDetailVm.SignatureScan,
            });
        }
        [HttpPost]
        [ValidateAntiForgeryTokenFromHeader]
        public async Task<IActionResult> SaveDraft([FromBody] SaveWithdrawalRequestViewModel vm, CancellationToken ct)
        {
            if (vm is null)
                return BadRequest(new { errors = new[] { "Request body is missing or invalid." } });

            var userId = GetUserId();

            WithdrawalRequest request;

            // Guard: collection date must be present and not in the past
            if (!vm.Id.HasValue)
            {
                if (!vm.CollectionDate.HasValue)
                    return BadRequest(new { errors = new[] { "Please select a collection date." } });

                if (vm.CollectionDate.Value < DateOnly.FromDateTime(DateTime.Now))
                    return BadRequest(new { errors = new[] { "Collection date cannot be in the past." } });
            }

            // Guard: lump sum must be a whole bundle (multiple of 1,000)
            if (vm.IsLumpSum)
            {
                if (vm.LumpSumAmount is null or <= 0)
                    return BadRequest(new { errors = new[] { "Please enter a valid lump sum amount." } });

                if (vm.LumpSumAmount % 1000 != 0)
                    return BadRequest(new { errors = new[] { "Lump sum amount must be a whole bundle — no partial amounts allowed." } });
            }

            if (vm.Id.HasValue)
            {
                // ── EDIT path — only touch allowed fields ─────────────────────────
                request = await _withdrawalRequestRepo.FindById(vm.Id.Value);

                var statusType = _workflowServiceClient.FindStatusById(request.StatusTypeId);

                if (request is null) return NotFound();

                //TODO : make access secure.
                //if (!await CanAccessRequestAsync(request.CbId))  // CbId not Id
                //    return Forbid();

                if (vm.RtgsDate.HasValue && vm.RtgsDate.Value > request.CollectionDate)
                    return BadRequest(new { errors = new[] { "Fund settlement value date cannot be after the collection date." } });

                request.FundSettlementValueDate = vm.RtgsDate;
                request.RtgsCode = vm.RtgsCode;
                request.ModifiedDate = DateTime.Now;
                request.ModifiedBy = userId;

                // Recompute totals from the new denom lines
                //request.TotalAmount = vm.Denominations.Sum(d => d.Amount);
                //request.TotalNotesAmount = vm.Denominations.Where(d => d.Bundles.HasValue).Sum(d => d.Amount);
                //request.TotalCoinsAmount = vm.Denominations.Where(d => d.Boxes.HasValue).Sum(d => d.Amount);
                request.IsLumpSum = vm.IsLumpSum;
                request.TotalAmount = vm.IsLumpSum ? vm.LumpSumAmount ?? 0 : vm.Denominations.Sum(d => d.Amount);
                request.TotalNotesAmount = vm.IsLumpSum ? 0 : vm.Denominations.Where(d => d.Bundles.HasValue).Sum(d => d.Amount);
                request.TotalCoinsAmount = vm.IsLumpSum ? 0 : vm.Denominations.Where(d => d.Boxes.HasValue).Sum(d => d.Amount);

                // Regenerate transaction ref number only if bank branch changed
                if (request.BouBranchId != vm.BouBranchId)
                {
                    request.BouBranchId = vm.BouBranchId;
                    request.BouBranchCode = vm.BouBranchCode;
                    request.TransactionRefNumber = await _txnrefNoGenerator
                        .GenerateAsync(request.BouBranchId, request.BouBranchCode,
                                       request.CbId, request.CbCode, request.CollectionDate);
                }
            }
            else
            {
                // ── CREATE path — full initialisation ────────────────────────────
                if (vm.RtgsDate.HasValue && vm.CollectionDate.HasValue && vm.RtgsDate.Value > vm.CollectionDate.Value)
                    return BadRequest(new { errors = new[] { "Fund settlement value date cannot be after the collection date." } });

                request = new WithdrawalRequest
                {
                    CbId = GetCbIdFromClaims(),
                    CbCode = vm.CbCode,
                    BouBranchId = vm.BouBranchId,
                    BouBranchCode = vm.BouBranchCode,
                    CollectionDate = vm.CollectionDate ?? default,
                    FundSettlementValueDate = vm.RtgsDate,
                    RtgsCode = vm.RtgsCode,
                    RequiresSpecialApproval = vm.CollectionDate.HasValue && vm.CollectionDate.Value == DateOnly.FromDateTime(DateTime.Now),
                    SpecialApprovalReason = vm.SpecialApprovalReason,
                    StatusTypeId = await _statusResolver.GetStatusIdAsync(WithdrawalStatusNames.Draft),
                    StatusTypeName = WithdrawalStatusNames.Draft,
                    CreatedBy = userId,
                    CreatedDate = DateTime.Now,
                    //TotalAmount = vm.Denominations.Sum(d => d.Amount),
                    //TotalNotesAmount = vm.Denominations.Where(d => d.Bundles.HasValue).Sum(d => d.Amount),
                    //TotalCoinsAmount = vm.Denominations.Where(d => d.Boxes.HasValue).Sum(d => d.Amount),
                    IsLumpSum = vm.IsLumpSum,
                    TotalAmount = vm.IsLumpSum ? vm.LumpSumAmount ?? 0 : vm.Denominations.Sum(d => d.Amount),
                    TotalNotesAmount = vm.IsLumpSum ? 0 : vm.Denominations.Where(d => d.Bundles.HasValue).Sum(d => d.Amount),
                    TotalCoinsAmount = vm.IsLumpSum ? 0 : vm.Denominations.Where(d => d.Boxes.HasValue).Sum(d => d.Amount),
                };

                request.TransactionRefNumber = await _txnrefNoGenerator
                    .GenerateAsync(request.BouBranchId, request.BouBranchCode,
                                   request.CbId, request.CbCode, request.CollectionDate);

                await _uow.Requests.Create(request);
            }

            // ── Shared: agents + denominations ───────────────────────────────────────
            await SaveDenominationsAsync(request.Id, vm.Denominations, userId, ct);
            await SaveAgentsAsync(request.Id, vm.AgentIds, userId, ct);

            // ── Status history — create path only ────────────────────────────────────
            // On edit the status doesn't change, so no history entry is needed.
            if (!vm.Id.HasValue)
            {
                var draftStatusId = await _statusResolver.GetStatusIdAsync(WithdrawalStatusNames.Draft);
                await AddStatusHistoryAsync(
                    request.Id,
                    oldStatusId: 0, oldStatusName: string.Empty,
                    draftStatusId,
                    WithdrawalStatusNames.Draft,
                    userId, "Saved as draft", ct);
            }

            await _uow.SaveAsync(ct);


            TempData["Success"] = vm.Id.HasValue
                ? $"Withdrawal request {request.TransactionRefNumber} updated successfully."
                : $"Withdrawal request  {request.TransactionRefNumber} created successfully.";

            return Ok(Url.Action("Details", new { id = request.Id }));
        }



        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var request = await _uow.Requests.GetWithDetailsAsync(id, ct);
            if (request is null) return NotFound();

            // ── Current status ────────────────────────────────────────────────────
            var currentStatus = await _workflowServiceClient.FindStatusById(request.StatusTypeId);

            // ── BOU vs CB user detection ──────────────────────────────────────────
            var userRoleNames = User.FindAll("appRole")
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();
            var isBouUser = userRoleNames.Any(r => RolePolicy.BouRoles.Contains(r));

            // ── Step key (differs for BOU vs CB perspective) ──────────────────────
            var stepKey = isBouUser
                ? GetCurrentStepKeyBOU(request.StatusTypeName)
                : GetCurrentStepKey(request.StatusTypeName);

            // ── Display names from microservice ───────────────────────────────────
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

            // ── Denominations ─────────────────────────────────────────────────────
            var cbLines = request.WithdrawalRequestDenominations
                .Where(d => d.Origin == WithdrawalOrigin.Cb)
                .ToList();

            var notes = cbLines
                .Where(d => d.Bundles.HasValue)
                .Select(d => new DenominationLineViewModel
                {
                    DenominationLabel = $"UGX {d.DenominationValue:N0}",
                    Bundles = d.Bundles,
                    Amount = d.Amount,
                });

            var coins = cbLines
                .Where(d => d.Boxes.HasValue)
                .Select(d => new DenominationLineViewModel
                {
                    DenominationLabel = $"UGX {d.DenominationValue:N0}",
                    Boxes = d.Boxes,
                    Amount = d.Amount,
                });

            // ── canEdit requires Draft/Returned status AND CB Maker role ──────────
            var canEdit = (request.StatusTypeName == WithdrawalStatusNames.Draft
                || request.StatusTypeName == WithdrawalStatusNames.ReturnedForRevision)
                && userRoleNames.Contains(RolePolicy.CbMaker);

            var rtgsEditableStatuses = new[]
            {
                WithdrawalStatusNames.Draft,
                WithdrawalStatusNames.PendingInternalApproval,
                WithdrawalStatusNames.ReturnedForRevision,
                WithdrawalStatusNames.PendingRTGSVerification,
            };
            var canEditRtgs = rtgsEditableStatuses.Contains(request.StatusTypeName)
                && (userRoleNames.Contains(RolePolicy.CbApprover) || userRoleNames.Contains(RolePolicy.CbMaker));

            // ── Status history ────────────────────────────────────────────────────
            var statusHistory = request.WithdrawalRequestStatusHistory
                .Where(h => !h.IsDeleted)
                .OrderBy(h => h.ChangedAt)
                .Select(h => new WithdrawalRequestStatusHistoryViewModel
                {
                    Id = h.Id,
                    OldStatusId = h.OldStatusId,
                    OldStatusName = h.OldStatusName,
                    NewStatusId = h.NewStatusId,
                    NewStatusName = h.NewStatusName,
                    ChangedBy = h.ChangedBy,
                    ChangedAt = h.ChangedAt,
                    Remarks = h.Remarks,
                })
                .ToList();

            // ── Global attachments for this request ───────────────────────────────
            var allAttachmentsRaw = await _attachmentRepository.FindByEntityId(id.ToString());
            var allAttachments = allAttachmentsRaw
                .Where(a => !a.IsDeleted && a.EntityType == StatusConstants.Entity.WithdrawalRequest)
                .Select(a => new AttachmentViewModel
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    FileType = a.FileType,
                    FileSize = a.FileSize,
                    UploadedDate = a.CreatedDate,
                })
                .ToList();

            // ── Available transitions (all user roles, filtered by special approval)
            var userRoleIds = GetUserRoles();
            var availableTransitions = new List<WorkflowTransitionDto>();
            foreach (var roleId in userRoleIds)
            {
                var transitions = await _workflowServiceClient.GetAvailableTransitions(
                    new GetAvailableTransitionsRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName = StatusConstants.Entity.WithdrawalRequest,
                        CurrentStatusId = request.StatusTypeId,
                        AppRole = roleId,
                    });

                if (transitions != null)
                    availableTransitions.AddRange(transitions);
            }

            availableTransitions = availableTransitions
                .Where(t => t.ActionName switch
                {
                    "Exceptional Approval" => request.RequiresSpecialApproval,
                    "Request Withdraw" => !request.RequiresSpecialApproval,
                    _ => true,
                })
                .ToList();

            // ── Build ViewModel ───────────────────────────────────────────────────
            var vm = new WithdrawalRequestDetailsViewModel
            {
                Id = request.Id,
                TransactionRefNumber = request.TransactionRefNumber,
                StatusId = request.StatusTypeId,
                StatusName = request.StatusTypeName,
                CollectionDate = request.CollectionDate,
                FundSettlementValueDate = request.FundSettlementValueDate,
                RtgsCode = request.RtgsCode,
                RequiresSpecialApproval = request.RequiresSpecialApproval,
                SpecialApprovalReason = request.SpecialApprovalReason,
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
                CanEdit = canEdit,
                CanEditRtgs = canEditRtgs,
                RtgsVerificationStatus = request.RtgsVerificationStatus,
                RecallRtgsPending = request.RecallRtgsPending,
                RecallRtgsCode = request.RecallRtgsCode,
                RecallRtgsDate = request.RecallRtgsDate,
                IsLumpSum = request.IsLumpSum,
                Steps = WithdrawalStepViewModelFactory.Build(stepKey, request.StatusTypeName == WithdrawalStatusNames.Approved, request.RequiresSpecialApproval, User),
                AvailableTransitions = availableTransitions,
                StatusHistory = statusHistory,
                AllAttachments = allAttachments,
            };

            return View(vm);
        }


        // generic transition action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(Guid id, string actionName, string remarks, CancellationToken ct)
        {
            try
            {
                var request = await _withdrawalRequestRepo.FindById(id);
                if (request is null)
                {
                    TempData["Error"] = "Withdrawal request not found.";
                    return RedirectToAction(nameof(Index));
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var userRoles = User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
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
                    remarks: remarks,
                    ct: ct);

                if (isExecuted)
                {
                    // Reload to get the fresh status after the workflow transition
                    var updatedRequest = await _withdrawalRequestRepo.FindById(id);

                    if (updatedRequest is not null
                        && updatedRequest.StatusTypeName is StatusConstants.WithdrawalRequest.PendingRecommendation or StatusConstants.WithdrawalRequest.PendingExceptionalApproval
                        && !updatedRequest.IsLumpSum
                        && !await _uow.Denominations.HasBouOriginAsync(id, ct))
                    {
                        try
                        {
                            await CopyDenominationsToBouAsync(id, username, ct);
                            await _uow.SaveAsync(ct);
                        }
                        catch (Exception copyEx)
                        {
                            _logger.LogError(copyEx,
                                "Approve succeeded for withdrawal {Id} but copying CB denominations to Bank failed. Retry the action to complete the copy.",
                                id);
                            TempData["Warning"] = "Approved, but could not copy denominations to the bank side. Please retry the action.";
                            return RedirectToAction(nameof(Details), new { id });
                        }
                    }

                    TempData["Success"] = "Action completed successfully.";

                }
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var request = await _uow.Requests.FindById(id);
            if (request is null) return NotFound();
            //if (!await CanAccessRequestAsync(request.CbId)) return Forbid();

            // Only allow delete in Draft or ReturnedForRevision
            if (request.StatusTypeName != WithdrawalStatusNames.Draft
                && request.StatusTypeName != WithdrawalStatusNames.ReturnedForRevision)
            {
                TempData["Error"] = "Only draft requests can be deleted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            request.IsDeleted = true;
            request.ModifiedBy = GetUserId();
            request.ModifiedDate = DateTime.Now;

            await _uow.SaveAsync(ct);

            TempData["Success"] = "Request deleted successfully.";
            return RedirectToAction("Index", "CbRequests");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRtgs(Guid id, DateOnly? rtgsDate, string? rtgsCode, CancellationToken ct)
        {
            var request = await _uow.Requests.FindById(id);
            if (request is null) return NotFound();

            var rtgsUpdateableStatuses = new[]
            {
                WithdrawalStatusNames.Draft,
                WithdrawalStatusNames.PendingInternalApproval,
                WithdrawalStatusNames.ReturnedForRevision,
                WithdrawalStatusNames.PendingRTGSVerification,
            };
            if (!rtgsUpdateableStatuses.Contains(request.StatusTypeName))
            {
                TempData["Error"] = "RTGS details can no longer be updated at this stage.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (rtgsDate.HasValue && rtgsDate.Value > request.CollectionDate)
            {
                TempData["Error"] = "Fund settlement value date cannot be after the collection date.";
                return RedirectToAction(nameof(Details), new { id });
            }

            request.FundSettlementValueDate = rtgsDate;
            request.RtgsCode = rtgsCode;
            request.ModifiedBy = GetUserId();
            request.ModifiedDate = DateTime.Now;

            await _uow.SaveAsync(ct);

            TempData["Success"] = "RTGS details updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── Print Slip ────────────────────────────
        public async Task<IActionResult> PrintRequest(Guid id, CancellationToken ct)
        {
            var request = await _uow.Requests.GetWithDetailsAsync(id, ct);
            if (request is null) return NotFound();

            if (request.StatusTypeName != WithdrawalStatusNames.Completed
                && request.StatusTypeName != WithdrawalStatusNames.Approved)
                return RedirectToAction(nameof(Details), new { id });

            var cb = await _configServiceClient.GetCbIdAsync(request.CbId);
            var bouBranch = await _configServiceClient.GetBouBranchByIdAsync(request.BouBranchId);

            // Agents
            var agentIds = request.WithdrawalRequestAgents
                .Where(a => a.EnteredBy == WithdrawalEnteredBy.Cb)
                .Select(a => a.AgentId)
                .ToList();

            var agents = new List<AgentDetailViewModel>();
            foreach (var agentId in agentIds)
            {
                var agent = await _agentRepo.FindById(agentId);
                if (agent is not null)
                    agents.Add(_mapper.Map<AgentDetailViewModel>(agent));
            }

            // BOU dispatch denominations
            var bankRows = request.WithdrawalRequestDenominations
                .Where(d => d.Origin == WithdrawalOrigin.Bou)
                .ToList();

            var newBanknotes = new List<BouDispatchCardViewModel>();
            var reissueNotes = new List<BouDispatchSimpleViewModel>();
            var coins = new List<BouDispatchSimpleViewModel>();

            foreach (var row in bankRows)
            {
                if (string.Equals(row.BundleType, BundleTypes.New, StringComparison.OrdinalIgnoreCase))
                {
                    var details = row.DispatchedBoxes?
                        .OrderBy(b => int.TryParse(b.CaseNumber, out var n) ? n : int.MaxValue)
                        .ToList() ?? [];

                    if (!details.Any()) continue;

                    var bundlesTotal = row.Bundles ?? 0;
                    var isFull = bundlesTotal == details.Count * 50;
                    var firstBox = int.TryParse(details.First().CaseNumber, out var fn) ? fn : 0;
                    var lastBox = int.TryParse(details.Last().CaseNumber, out var ln) ? ln : 0;

                    newBanknotes.Add(new BouDispatchCardViewModel
                    {
                        Id = row.Id,
                        DenominationId = row.DenominationId,
                        Denomination = row.DenominationValue,
                        StartBox = firstBox,
                        EndBox = lastBox,
                        IsFull = isFull,
                        Bundles = bundlesTotal,
                        SerialStart = details.First().SerialStartTaken,
                        SerialEnd = details.Last().SerialEndTaken,
                        SerialPrefix = details.First().SerialPrefix,
                        Amount = row.Amount,
                    });
                }
                else if (string.Equals(row.BundleType, BundleTypes.Reissue, StringComparison.OrdinalIgnoreCase))
                {
                    reissueNotes.Add(new BouDispatchSimpleViewModel
                    {
                        Id = row.Id,
                        DenominationId = row.DenominationId,
                        Denomination = row.DenominationValue,
                        Quantity = row.Bundles ?? 0,
                        Amount = row.Amount,
                    });
                }
                else if (string.Equals(row.BundleType, BundleTypes.Coin, StringComparison.OrdinalIgnoreCase))
                {
                    coins.Add(new BouDispatchSimpleViewModel
                    {
                        Id = row.Id,
                        DenominationId = row.DenominationId,
                        Denomination = row.DenominationValue,
                        Quantity = row.Boxes ?? 0,
                        Amount = row.Amount,
                    });
                }
                else
                {
                    // BundleType = "NA" — copied from CB before Panel A entered dispatch.
                    // Fall back: notes have Bundles, coins have Boxes.
                    if (row.Bundles.HasValue && row.Bundles > 0)
                        reissueNotes.Add(new BouDispatchSimpleViewModel
                        {
                            Id = row.Id,
                            DenominationId = row.DenominationId,
                            Denomination = row.DenominationValue,
                            Quantity = row.Bundles.Value,
                            Amount = row.Amount,
                        });
                    else if (row.Boxes.HasValue && row.Boxes > 0)
                        coins.Add(new BouDispatchSimpleViewModel
                        {
                            Id = row.Id,
                            DenominationId = row.DenominationId,
                            Denomination = row.DenominationValue,
                            Quantity = row.Boxes.Value,
                            Amount = row.Amount,
                        });
                }
            }

            // Panel names from status history
            var history = request.WithdrawalRequestStatusHistory.Where(h => !h.IsDeleted).ToList();

            var panelAStatusId = await _statusResolver.GetStatusIdAsync(WithdrawalStatusNames.PendingApproval);
            var panelBStatusId = await _statusResolver.GetStatusIdAsync(WithdrawalStatusNames.Approved);

            string panelAName = string.Empty;
            string panelBName = string.Empty;

            var panelAEntry = history.FirstOrDefault(h => h.NewStatusId == panelAStatusId);
            if (panelAEntry is not null)
            {
                var user = await _identityService.GetUserByUsername(panelAEntry.ChangedBy);
                if (user is not null)
                    panelAName = System.Text.RegularExpressions.Regex.Replace(
                        $"{user.FirstName} {user.MiddleName} {user.LastName}".Trim(), @"\s+", " ");
            }

            var panelBEntry = history.FirstOrDefault(h => h.NewStatusId == panelBStatusId);
            if (panelBEntry is not null)
            {
                var user = await _identityService.GetUserByUsername(panelBEntry.ChangedBy);
                if (user is not null)
                    panelBName = System.Text.RegularExpressions.Regex.Replace(
                        $"{user.FirstName} {user.MiddleName} {user.LastName}".Trim(), @"\s+", " ");
            }

            var vm = new WithdrawalPrintViewModel
            {
                TransactionRefNumber = request.TransactionRefNumber,
                CollectionDate = request.CollectionDate,
                RtgsDate = request.FundSettlementValueDate,
                RtgsCode = request.RtgsCode,
                CbName = cb?.Name ?? "—",
                BouBranchName = bouBranch?.Name ?? "—",
                BouBranchCode = request.BouBranchCode,
                TotalAmount = request.TotalAmount,
                TotalNotesAmount = request.TotalNotesAmount,
                TotalCoinsAmount = request.TotalCoinsAmount,
                Agents = agents,
                NewBanknotes = newBanknotes.OrderBy(n => n.Denomination).ThenBy(n => n.StartBox).ToList(),
                ReissueBanknotes = reissueNotes.OrderBy(n => n.Denomination).ToList(),
                Coins = coins.OrderBy(c => c.Denomination).ToList(),
                PanelAName = panelAName,
                PanelBName = panelBName,
                RequiresSpecialApproval = request.RequiresSpecialApproval,
            };

            return View(vm);
        }

        // ── Helpers ───────────────────────────────


        //private async Task<WithdrawalRequest> GetOrCreateRequestAsync(SaveWithdrawalRequestViewModel vm,
        //string userId,
        //CancellationToken ct)
        //{
        //    WithdrawalRequest request;


        //    if (vm.Id.HasValue)
        //    {
        //        request = await _uow.Requests.FindById(vm.Id.Value);
        //        //if (!await CanAccessRequestAsync(request.Id))
        //        //    throw new UnauthorizedAccessException();
        //    }
        //    else
        //    {
        //        request = new WithdrawalRequest
        //        {
        //            CbId = GetCbIdFromClaims(),
        //            StatusTypeId = WithdrawalStatusIds.Draft,
        //            CreatedBy = userId,
        //            CreatedDate = DateTime.Now,
        //        };
        //        await _uow.Requests.Create(request);
        //    }
        //    request.RequiresSpecialApproval = vm.RtgsDate.HasValue && vm.CollectionDate == vm.RtgsDate.Value;
        //    request.SpecialApprovalReason = vm.SpecialApprovalReason;

        //    request.CbCode = vm.CbCode;
        //    request.BouBranchId = vm.BouBranchId;
        //    request.BouBranchCode = vm.BouBranchCode;
        //    request.CollectionDate = vm.CollectionDate;
        //    request.RtgsDate = vm.RtgsDate;
        //    request.RtgsCode = vm.RtgsCode;

        //    // Recompute totals
        //    request.TotalAmount = vm.Denominations.Sum(d => d.Amount);
        //    request.TotalNotesAmount = vm.Denominations
        //        .Where(d => d.Bundles.HasValue)
        //        .Sum(d => d.Amount);
        //    request.TotalCoinsAmount = vm.Denominations
        //        .Where(d => d.Boxes.HasValue)
        //        .Sum(d => d.Amount);

        //    request.TransactionRefNumber = await _txnrefNoGenerator.GenerateAsync(request.BouBranchId, request.BouBranchCode, request.CbId, request.CbCode, request.CollectionDate);

        //    return request;
        //}

        private static string GetCurrentStepKey(string statusName) => statusName switch
        {
            WithdrawalStatusNames.Draft => "CbMaker",
            WithdrawalStatusNames.ReturnedForRevision => "CbMaker",
            WithdrawalStatusNames.PendingInternalApproval => "CbApprover",
            WithdrawalStatusNames.PendingExceptionalApproval => "BOU",
            WithdrawalStatusNames.PendingWithdrawalConfirmation => "Remittance",
            WithdrawalStatusNames.PendingVerification => "Remittance",
            WithdrawalStatusNames.PendingApproval => "PanelB",
            WithdrawalStatusNames.Approved => "PanelA",
            WithdrawalStatusNames.RejectExceptionalApproval => "PanelB",
            WithdrawalStatusNames.RecallRequested => "Remittance",
            WithdrawalStatusNames.RecallApproved => "Remittance",
            _ => "CbMaker",
        };

        private static string GetCurrentStepKeyBOU(string statusName) => statusName switch
        {
            WithdrawalStatusNames.Draft or
            WithdrawalStatusNames.ReturnedForRevision or
            WithdrawalStatusNames.PendingInternalApproval => "CB",
            WithdrawalStatusNames.PendingExceptionalApproval or
            WithdrawalStatusNames.PendingWithdrawalConfirmation => "Remittance",
            WithdrawalStatusNames.PendingVerification => "PanelB",
            WithdrawalStatusNames.PendingApproval or
            WithdrawalStatusNames.Approved or
            WithdrawalStatusNames.RejectExceptionalApproval => "PanelA",
            WithdrawalStatusNames.RecallRequested or
            WithdrawalStatusNames.RecallApproved => "Remittance",
            _ => "CB",
        };

        private WithdrawalStepViewModel BuildStepViewModel(string statusName, bool isApproved, bool requiresSpecialApproval)
            => WithdrawalStepViewModelFactory.Build(
                currentStepKey: GetCurrentStepKey(statusName),
                isRequestApproved: isApproved,
                requiresSpecialApproval: requiresSpecialApproval,
                user: User);

        private string GetUserId()
            => User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        private List<int> GetUserRoles()
        {
            return User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                    .Select(c => int.TryParse(c.Value.Split(":")[1], out var appRoleId) ? appRoleId : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList();
        }

       

        private async Task SaveDenominationsAsync(
       Guid requestId,
       List<SaveDenominationLine> lines,
       string userId,
       CancellationToken ct)
        {
            _uow.Denominations.DeleteByRequest(requestId, "Cb");

            foreach (var line in lines.Where(l => l.Bundles > 0 || l.Boxes > 0))
            {
                await _uow.Denominations.Create(new WithdrawalRequestDenomination
                {
                    WithdrawalRequestId = requestId,
                    DenominationId = line.DenominationId,
                    DenominationTypeId = line.DenominationTypeId,
                    DenominationValue = line.DenominationValue,
                    Bundles = line.Bundles,
                    Boxes = line.Boxes,
                    Amount = line.Amount,
                    Origin = "Cb",
                    CreatedBy = userId,
                    CreatedDate = DateTime.Now,
                });
            }
        }

        private async Task SaveAgentsAsync(
            Guid requestId,
            List<int> agentIds,
            string userId,
            CancellationToken ct)
        {
            _uow.WithdrawalAgents.DeleteByRequestAndSide(requestId, "Cb");

            foreach (var agentId in agentIds)
            {
                await _uow.WithdrawalAgents.Create(new WithdrawalRequestAgent
                {
                    WithdrawalRequestId = requestId,
                    AgentId = agentId,
                    EnteredBy = "Cb",
                    CreatedBy = userId,
                    CreatedDate = DateTime.Now,
                });
            }
        }

        private async Task CopyDenominationsToBouAsync(
            Guid requestId,
            string userId,
            CancellationToken ct)
        {
            var cbLines = await _uow.Denominations
                .GetByRequestAndOriginAsync(requestId, "Cb", ct);

            foreach (var line in cbLines)
            {
                await _uow.Denominations.Create(new WithdrawalRequestDenomination
                {
                    WithdrawalRequestId = requestId,
                    DenominationId = line.DenominationId,
                    DenominationValue = line.DenominationValue,
                    DenominationTypeId = line.DenominationTypeId,
                    Bundles = line.Bundles,
                    Boxes = line.Boxes,
                    Amount = line.Amount,
                    Origin = "BOU",
                });
            }
        }

        private async Task AddStatusHistoryAsync(
            Guid requestId,
            int oldStatusId,
            string oldStatusName,
            int newStatusId,
            string newStatusName,
            string userId,
            string remarks,
            CancellationToken ct)
        {
            await _uow.StatusHistory.Create(new WithdrawalRequestStatusHistory
            {
                WithdrawalRequestId = requestId,
                OldStatusId = oldStatusId,
                OldStatusName = oldStatusName,
                NewStatusId = newStatusId,
                NewStatusName = newStatusName,
                ChangedBy = userId,
                ChangedAt = DateTime.UtcNow,
                Remarks = remarks,
                CreatedBy = userId,
                CreatedDate = DateTime.Now,
            });
        }
        private int GetUserRole()
            => User.FindAll("appRoleId")
            .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
            .Select(c => int.TryParse(c.Value.Split(":")[1], out var roleId) ? roleId : 0)
            .FirstOrDefault(r => r > 0);

        private int GetCbIdFromClaims()
            => int.TryParse(User.Claims
                .FirstOrDefault(c => c.Type == "cbId")?.Value, out var id) ? id : 0;
    }
}

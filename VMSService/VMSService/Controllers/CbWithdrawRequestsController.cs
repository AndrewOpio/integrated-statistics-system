using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Reflection.Emit;
using System.Security.Claims;
using System.Threading.Channels;
using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Models.CbWithdrawRequest;
using VMSService.Core.Models.WithdrawalRequestDenominations;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.WithdrawalRequestAgents;
using VMSService.Core.Repositories.WithdrawalRequestDenominations;
using VMSService.Core.Repositories.WithdrawalRequestStatusHistories;
using VMSService.Core.Repositories.WithdrawalTransactionRefNumberGenerators;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using VMSService.Web.Helpers;
using WorkflowService.Client;
using WorkflowService.Client.Dto;
using static System.Runtime.InteropServices.JavaScript.JSType;
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
    IWithdrawalRequestAgentRepository _withdrawalRequestAgentRepo,
    IWithdrawalRequestDenominationRepository _withdrawalRequestDenomRepo,
    IWithdrawalRequestStatusHistoryRepository _withdrawalRequestStatusHistRepo,
    IWorkflowServiceClient _workflowServiceClient,
    IWorkflowService _workflowService,
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
            //var stepVm = WithdrawalStepViewModelFactory.Build(
            //    currentStepKey: "CbMaker",
            //    isRequestApproved: false,
            //    user: User);

            var branchVm = new FromToSelectionViewModel
            {
                Cb = await _configServiceClient.GetCbIdAsync(2),
                BouBranches = await _configServiceClient.GetAllBouBranchesAsync(),
            };

            var agentDetails = (await _agentRepo.FindAll())
                .Where(a => a.IsDeleted == false);

            var agentDetailsVm = _mapper.Map<List<AgentDetailViewModel>>(agentDetails);

            var allDenoms = await _denomRepo.FindAll();

            var denomList = allDenoms.ToList();

            var denomVm = new DenomTableViewModel
            {
                Notes = denomList.Where(d => d.DenominationType.Name.Equals("Note")),
                Coins = denomList.Where(d => d.DenominationType.Name.Equals("Coin")),
            };

            var agentVm = new AgentSelectionViewModel
            {
                Agents = agentDetailsVm,
            };
            return View(new CreateWithdrawViewModel
            {
                Steps = BuildStepViewModel(WithdrawalStatusIds.Draft, false),
                Branches = branchVm,
                Agents = agentVm,
                Denoms = denomVm,
                AvailableTransitions = new List<WorkflowTransitionDto>(),
            });

        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var request = await _uow.Requests.GetWithDetailsAsync(id, ct);
            if (request is null) return NotFound();
            //if (!await CanAccessRequestAsync(request.CbId)) return Forbid();

            // Only allow editing in Draft or ReturnedForRevision
            if (request.StatusTypeId != WithdrawalStatusIds.Draft
                && request.StatusTypeId != WithdrawalStatusIds.ReturnedForRevision)
                return RedirectToAction("Details", new { id });

            //var stepVm = WithdrawalStepViewModelFactory.Build(
            //    currentStepKey: "CbMaker",
            //    isRequestApproved: false,
            //    user: User);

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
                Steps = BuildStepViewModel(request.StatusTypeId, false),
                Branches = branchVm,
                Agents = agentVm,
                Denoms = denomVm,
            });
        }

        // ── Branch details endpoint (AJAX) ────────
        //[HttpGet("branch-details")]
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
            var userId = GetUserId();

            WithdrawalRequest request;

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
                request = await _uow.Requests.FindById(vm.Id.Value);

                var statusType = _workflowServiceClient.FindStatusById(request.StatusTypeId);

                if (request is null) return NotFound();

                //TODO : make access secure.
                //if (!await CanAccessRequestAsync(request.CbId))  // CbId not Id
                //    return Forbid();

                request.RtgsDate = vm.RtgsDate;
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
                request = new WithdrawalRequest
                {
                    CbId = GetCbIdFromClaims(),
                    CbCode = vm.CbCode,
                    BouBranchId = vm.BouBranchId,
                    BouBranchCode = vm.BouBranchCode,
                    CollectionDate = vm.CollectionDate,
                    RtgsDate = vm.RtgsDate,
                    RtgsCode = vm.RtgsCode,
                    //request.RequiresSpecialApproval = request.CollectionDate == DateOnly.FromDateTime(request.CreatedDate),
                    RequiresSpecialApproval = vm.CollectionDate == DateOnly.FromDateTime(DateTime.Now),
                    SpecialApprovalReason = vm.SpecialApprovalReason,
                    StatusTypeId = WithdrawalStatusIds.Draft,
                    CreatedBy = userId,
                    CreatedDate = DateTime.Now,
                    //TotalAmount = vm.Denominations.Sum(d => d.Amount),
                    //TotalNotesAmount = vm.Denominations.Where(d => d.Bundles.HasValue).Sum(d => d.Amount),
                    //TotalCoinsAmount = vm.Denominations.Where(d => d.Boxes.HasValue).Sum(d => d.Amount),
                    IsLumpSum = vm.IsLumpSum,
                    TotalAmount = vm.IsLumpSum ? vm.LumpSumAmount ?? 0 : vm.Denominations.Sum(d => d.Amount),
                    TotalNotesAmount = vm.IsLumpSum ? 0 : vm.Denominations.Where(d => d.Bundles.HasValue).Sum(d => d.Amount),
                    TotalCoinsAmount = vm.IsLumpSum ? 0 : vm.Denominations .Where(d => d.Boxes.HasValue).Sum(d => d.Amount),
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
                await AddStatusHistoryAsync(
                    request.Id,
                    oldStatusId: 0, oldStatusName: string.Empty,
                    WithdrawalStatusIds.Draft,
                    WithdrawStatuses.WithdrawalStatusNames.Draft,
                    userId, "Saved as draft", ct);
            }

            await _uow.SaveAsync(ct);


            TempData["Success"] = vm.Id.HasValue
                ? $"Withdrawal request {request.TransactionRefNumber} updated successfully."
                : $"Withdrawal request  {request.TransactionRefNumber} created successfully.";

            return Ok(Url.Action("Details", new { id = request.Id }));
        }



        //[HttpGet("{id}")]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var request = await _uow.Requests.GetWithDetailsAsync(id, ct);
            if (request is null) return NotFound();

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

            var canEdit = (request.StatusTypeId == WithdrawalStatusIds.Draft
              || request.StatusTypeId == WithdrawalStatusIds.ReturnedForRevision);


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

            var currentStatus = await _workflowServiceClient.FindStatusById(request.StatusTypeId);



            // ── Build ViewModel ───────────────────────────────────────────────────
            var vm = new WithdrawalRequestDetailsViewModel
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
                CanEdit = canEdit,
                IsLumpSum = request.IsLumpSum,
                Steps = BuildStepViewModel(request.StatusTypeId,
                           request.StatusTypeId == WithdrawalStatusIds.Approved),
            };
            vm.AvailableTransitions = (await _workflowService.GetAvailableTransitionsAsync(
                StatusConstants.Entity.WithdrawalRequest,
                request.StatusTypeId,
                GetUserRole()))
                .Where(t => t.ActionName switch
                {
                    "Special Approval" => request.RequiresSpecialApproval,
                    "Request Withdraw" => !request.RequiresSpecialApproval,
                    _ => true  // return and any others always show
                })
                .ToList();


            return View(vm);
        }

        private static string GetCurrentStepKey(int statusId) => statusId switch
        {
            WithdrawalStatusIds.Draft => "CbMaker",
            WithdrawalStatusIds.ReturnedForRevision => "CbMaker",
            WithdrawalStatusIds.SubmittedToSupervisor => "CbApprover",
            WithdrawalStatusIds.PendingSpecialApproval => "BOU",
            WithdrawalStatusIds.SubmittedToBank => "Remittance",
            WithdrawalStatusIds.Recommended => "PanelA",
            WithdrawalStatusIds.Approved => "PanelB",
            WithdrawalStatusIds.Rejected => "PanelB",
            _ => "CbMaker",
        };
        private WithdrawalStepViewModel BuildStepViewModel(int statusId, bool isApproved)
            => WithdrawalStepViewModelFactory.Build(
                currentStepKey: GetCurrentStepKey(statusId),
                isRequestApproved: isApproved,
                user: User);

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var request = await _uow.Requests.FindById(id);
            if (request is null) return NotFound();
            //if (!await CanAccessRequestAsync(request.CbId)) return Forbid();

            // Only allow delete in Draft or ReturnedForRevision
            if (request.StatusTypeId != WithdrawalStatusIds.Draft
                && request.StatusTypeId != WithdrawalStatusIds.ReturnedForRevision)
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


        private string GetUserId()
            => User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        public static class WithdrawalStatusIds
        {
            public const int Draft = 24;
            public const int SubmittedToSupervisor = 25;
            public const int ReturnedForRevision = 26;
            public const int SubmittedToBank = 27;
            public const int PendingSpecialApproval = 28;
            public const int Recommended = 29;
            public const int Approved = 30;
            public const int Rejected = 31;
            public const int Cancelled = 32;
        }

        //private bool ValidateSubmit(SaveWithdrawalRequestViewModel vm, out List<string> errors)
        //{
        //    errors = [];

        //    //if (vm.CbBranchId == 0)
        //    //    errors.Add("Please select a company branch.");

        //    if (vm.BouBranchId == 0)
        //        errors.Add("Please select a bank branch.");

        //    if (vm.AgentIds.Count != 2)
        //        errors.Add("Exactly 2 collectors are required.");

        //    if (!vm.Denominations.Any(d => d.Bundles > 0 || d.Boxes > 0))
        //        errors.Add("Please add at least one denomination.");

        //    if (vm.CollectionDate == default)
        //        errors.Add("Please select a collection date.");

        //    if (!string.IsNullOrEmpty(vm.RtgsCode) && !vm.RtgsDate.HasValue)
        //        errors.Add("RTGS date is required when an RTGS code is provided.");


        //    return errors.Count == 0;
        //}

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

        private async Task CopyDenominationsToBankAsync(
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
                    DenominationTypeId = line.DenominationTypeId,
                    Bundles = line.Bundles,
                    Boxes = line.Boxes,
                    Amount = line.Amount,
                    Origin = "BOU",
                    CreatedBy = userId,
                    CreatedDate = DateTime.Now,
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

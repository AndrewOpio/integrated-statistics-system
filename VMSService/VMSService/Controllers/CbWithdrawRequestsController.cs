using AutoMapper;
using ConfigurationsService.Client;
using MassTransit.Internals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Emit;
using System.Security.Claims;
using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Models.CbWithdrawRequest;
using VMSService.Core.Models.WithdrawalRequestDenominations;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{

    //[Authorize(Policy = RolePolicy.CbOnly)]
    public class CbWithdrawRequestsController(
    IAuthorizationService authorizationService,
    IAgentDetailRepository _agentRepo,
    IConfigurationsServiceClient _configServiceClient,
    IDenominationRepository _denomRepo,
    //IWithdrawalTransactionRefNumberGenerator _txnrefNoGenerator,
    IWithdrawalUnitOfWork _uow,
    //IWithdrawalRequestRepository _withdrawalRequestRepo,
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
            var stepVm = WithdrawalStepViewModelFactory.Build(
                currentStepKey: "CbMaker",
                isRequestApproved: false,
                user: User);

            var branchVm = new FromToSelectionViewModel
            {
                Cb = await _configServiceClient.GetCbIdAsync(2),
                BouBranches = await _configServiceClient.GetAllBouBranchesAsync(),
            };

            var agentDetails = (await _agentRepo.FindAll()).Where(a => a.IsDeleted == false);
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
                Steps = stepVm,
                Branches = branchVm,
                Agents = agentVm,
                Denoms = denomVm,
                AvailableTransitions = new List<WorkflowTransitionDto>(), // ← add
            });

            //return View(new CreateWithdrawViewModel
            //{
            //    Steps = stepVm,
            //    Branches = branchVm,
            //    Agents = agentVm,
            //    Denoms = denomVm,
            //});
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

                return Json(new { cb.Id, name = cb.Name, code = cb.Code, cbCode = cb.Code});
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

        // ── Save as Draft ─────────────────────────
        //[HttpPost("save-draft")]
        public async Task<IActionResult> SaveDraft([FromBody]
            SaveWithdrawalRequestViewModel vm, CancellationToken ct)
        {
            var allClaims = User.Claims
                .Select(c => new { c.Type, c.Value })
                .ToList();


            var userId = GetUserId(); 
            var request = await GetOrCreateRequestAsync(vm, userId, ct);
            var oldStatusId = request.StatusTypeId;
            var oldStatusName = request.StatusTypeName;



            // Block edit if submitted to supervisor
            if (request.StatusTypeId == WithdrawalStatusIds.SubmittedToSupervisor)
                return Forbid();

            request.StatusTypeId = WithdrawalStatusIds.Draft;
            request.ModifiedDate = DateTime.Now;
            request.ModifiedBy = userId;

            await SaveDenominationsAsync(request.Id, vm.Denominations, userId, ct);
            await SaveAgentsAsync(request.Id, vm.AgentIds, userId, ct);
            await AddStatusHistoryAsync
                (request.Id,
                oldStatusId, oldStatusName,
                WithdrawalStatusIds.Draft,
                WithdrawStatuses.WithdrawalStatusNames.Draft, userId, "Saved as draft", ct);

            await _uow.SaveAsync(ct);

            //return RedirectToAction("Index", "CbRequests");
            //return Ok(Url.Action("Edit", new { id = request.Id }));
            return Ok(Url.Action("Index", "CbRequests"));
        }

        // ── Submit to Supervisor ──────────────────
        [HttpPost("submit-to-supervisor")]
        public async Task<IActionResult> SubmitToSupervisor(SaveWithdrawalRequestViewModel vm, CancellationToken ct)
        {
            if (!ValidateSubmit(vm, out var errors))
            {
                TempData["Errors"] = errors;
                return RedirectToAction(vm.Id.HasValue ? "Edit" : "Create", new { id = vm.Id });
            }

            var userId = GetUserId(); 
            var request = await GetOrCreateRequestAsync(vm, userId, ct);


            // Block if already submitted and not returned for revision
            if (request.StatusTypeId == WithdrawalStatusIds.SubmittedToSupervisor)
                return Forbid();

            var oldStatusId = request.StatusTypeId;
            var oldStatusName = request.StatusTypeName;

            // Check for special approval — collection date == RTGS date
            var newStatusId = vm.RtgsDate.HasValue && vm.CollectionDate == vm.RtgsDate.Value
                ? WithdrawalStatusIds.PendingSpecialApproval
                : WithdrawalStatusIds.SubmittedToSupervisor;

            var newStatusName = vm.RtgsDate.HasValue && vm.CollectionDate == vm.RtgsDate.Value
                ? WithdrawStatuses.WithdrawalStatusNames.PendingSpecialApproval
                : WithdrawStatuses.WithdrawalStatusNames.SubmittedToSupervisor;


            request.StatusTypeId = newStatusId;
            request.RequiresSpecialApproval = newStatusId == WithdrawalStatusIds.PendingSpecialApproval;
            request.ModifiedDate = DateTime.Now;
            request.ModifiedBy = userId;

            await SaveDenominationsAsync(request.Id, vm.Denominations, userId, ct);
            await SaveAgentsAsync(request.Id, vm.AgentIds, userId, ct);
            await AddStatusHistoryAsync(request.Id,
                oldStatusId, oldStatusName,
                newStatusId, newStatusName,
                userId, "Submitted to supervisor", ct);

            await _uow.SaveAsync(ct);

            //return RedirectToAction("Confirmation", new { id = request.Id });
            return Ok(Url.Action("Confirmation", new { id = request.Id }));
        }



        //[HttpGet("{id}")]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var request = await _uow.Requests.GetWithDetailsAsync(id, ct);
            if (request is null) return NotFound();
            //if (!await CanAccessRequestAsync(request.cbUserId)) return Forbid();

            var userId = GetUserId();
            var userRole = "CB Maker"; // User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? string.Empty;
            //var isSupervisor = userRole == RolePolicy.Supervisor;

            // Resolve branch details from microservice
            var cbBranchs = await _configServiceClient.GetCbBranchesByCbIdAsync(2);
            var cbBranch = cbBranchs.FirstOrDefault(b => b.Id == 3);
            var bouBranches = await _configServiceClient.GetAllBouBranchesAsync();
            var bouBranch = bouBranches.FirstOrDefault(b => b.Id == 10);

            // Resolve collector details from DB
            var companyCollectors = request.WithdrawalRequestAgents
                .Where(c => c.EnteredBy == "Cb")
                .ToList();

            // Resolve denomination details from microservice
            var allDenoms = await _denomRepo.FindAll();
            var denomLookup = allDenoms.ToDictionary(d => d.Id);

            var companyLines = request.WithdrawalRequestDenominations
                .Where(d => d.Origin == "Cb")
                .ToList();

            var notes = companyLines
                .Where(d => d.Bundles.HasValue)
                .Select(d => new DenominationLineViewModel
                {
                    DenominationLabel = denomLookup.TryGetValue(d.DenominationId, out var denom)
                                        ? $"UGX {denom.Value:N0}"
                                        : d.DenominationId.ToString(),
                    Bundles = d.Bundles,
                    Amount = d.Amount,
                });

            var coins = companyLines
                .Where(d => d.Boxes.HasValue)
                .Select(d => new DenominationLineViewModel
                {
                    DenominationLabel = denomLookup.TryGetValue(d.DenominationId, out var denom)
                                        ? $"UGX {denom.Value:N0}"
                                        : d.DenominationId.ToString(),
                    Boxes = d.Boxes,
                    Amount = d.Amount,
                });

            var vm = new WithdrawalRequestDetailsViewModel
            {
                Id = request.Id,
                ReferenceNumber = string.IsNullOrEmpty(request.TransactionRefNumber) ? null : request.TransactionRefNumber,
                StatusId = request.StatusTypeId,
                CollectionDate = request.CollectionDate,
                RtgsDate = request.RtgsDate,
                RtgsCode = request.RtgsCode,
                RequiresSpecialApproval = request.RequiresSpecialApproval,
                CbBranchDistrinct = cbBranch?.District.Name ?? "—",
                CbBranchLocation = cbBranch?.Location ?? "—",
                CbName = cbBranch?.CommercialBank.Name ?? "—",
                CbBranchName = cbBranch?.Name ?? "—",
                //InitiatorName = request.InitiatorUserId,  
                BouBranchName = bouBranch?.Name ?? "—",
                BouBranchCode = bouBranch?.BranchCode ?? "—",
                WithdrawAgents = companyCollectors.Select(c => new AgentDetailViewModel
                {
                    AgentNames = c.Id.ToString(), 
                    NIN = string.Empty,
                    WorkIdScan = string.Empty,
                }),
                Notes = notes,
                Coins = coins,
                TotalNotesAmount = request.TotalNotesAmount,
                TotalCoinsAmount = request.TotalCoinsAmount,
                TotalAmount = request.TotalAmount,
                Steps = WithdrawalStepViewModelFactory.Build(
                                            GetCurrentStepKey(request.StatusTypeId),
                                            request.StatusTypeId == WithdrawalStatusIds.Approved,
                                            User),

            };

            vm.AvailableTransitions = await _workflowService.GetAvailableTransitionsAsync(
                StatusConstants.Entity.WithdrawalRequest,
                request.StatusTypeId,
                GetUserRole()
                );


            return View(vm);
        }

        private static string GetCurrentStepKey(int statusId) => statusId switch
        {
            WithdrawalStatusIds.Draft => "requestor",
            WithdrawalStatusIds.ReturnedForRevision => "requestor",
            WithdrawalStatusIds.SubmittedToSupervisor => "supervisor",
            WithdrawalStatusIds.PendingSpecialApproval => "supervisor",
            WithdrawalStatusIds.SubmittedToBank => "banker",
            WithdrawalStatusIds.Approved => "handover",
            _ => "requestor",
        };

        // Add generic transition action
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
                    .Where(c => c.Value.StartsWith("Myapp:"))
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

        // ── Helpers ───────────────────────────────


        private async Task<WithdrawalRequest> GetOrCreateRequestAsync(SaveWithdrawalRequestViewModel vm,
        string userId,
        CancellationToken ct)
        {
            WithdrawalRequest request;

            if (vm.Id.HasValue)
            {
                request = await _uow.Requests.FindById(vm.Id.Value);
                if (!await CanAccessRequestAsync(request.Id))
                    throw new UnauthorizedAccessException();
            }
            else
            {
                request = new WithdrawalRequest
                {
                    CbId = GetCbIdFromClaims(),
                    StatusTypeId = WithdrawalStatusIds.Draft,
                    CreatedBy = userId,
                    CreatedDate = DateTime.Now,
                };
                await _uow.Requests.Create(request);
            }

            //request.CbId = vm.CbId;
            request.CbCode = vm.CbCode;
            request.BouBranchId = vm.BouBranchId;
            request.BouBranchCode = vm.BouBranchCode;
            request.CollectionDate = vm.CollectionDate;
            request.RtgsDate = vm.RtgsDate;
            request.RtgsCode = vm.RtgsCode;

            // Recompute totals
            request.TotalAmount = vm.Denominations.Sum(d => d.Amount);
            request.TotalNotesAmount = vm.Denominations
                .Where(d => d.Bundles.HasValue)
                .Sum(d => d.Amount);
            request.TotalCoinsAmount = vm.Denominations
                .Where(d => d.Boxes.HasValue)
                .Sum(d => d.Amount);

            return request;
        }


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

        private bool ValidateSubmit(SaveWithdrawalRequestViewModel vm, out List<string> errors)
        {
            errors = [];

            //if (vm.CbBranchId == 0)
            //    errors.Add("Please select a company branch.");

            if (vm.BouBranchId == 0)
                errors.Add("Please select a bank branch.");

            if (vm.AgentIds.Count != 2)
                errors.Add("Exactly 2 collectors are required.");

            if (!vm.Denominations.Any(d => d.Bundles > 0 || d.Boxes > 0))
                errors.Add("Please add at least one denomination.");

            if (vm.CollectionDate == default)
                errors.Add("Please select a collection date.");

            if (!string.IsNullOrEmpty(vm.RtgsCode) && !vm.RtgsDate.HasValue)
                errors.Add("RTGS date is required when an RTGS code is provided.");


            return errors.Count == 0;
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
            .Where(c => c.Value.StartsWith("VMS:"))
            .Select(c => int.TryParse(c.Value.Split(":")[1], out var roleId) ? roleId : 0)
            .FirstOrDefault(r => r > 0);

        private int GetCbIdFromClaims()
            => int.TryParse(User.Claims
                .FirstOrDefault(c => c.Type == "cbId")?.Value, out var id) ? id : 0;
    }
}

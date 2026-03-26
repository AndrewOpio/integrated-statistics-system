using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockService.Client;
using System.Security.Claims;
using VMSService.Core.Authorization;
using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Models.CbWithdrawRequest;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.Branches;
using VMSService.Core.Repositories.CommercialBankBranches;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.WithdrawalTransactionRefNumberGenerators;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    //public class CbWithdrawRequestsController : Controller
    //{

    //[Authorize(Policy = RolePolicy.CbOnly)]
    public class CbWithdrawRequestsController(
    IAuthorizationService authorizationService,
    IAgentDetailRepository _agentRepo, 
    IConfigurationsServiceClient _configServiceClient,
    IDenominationRepository _denomRepo,
    IWithdrawalTransactionRefNumberGenerator _txnrefNoGenerator,
    IWithdrawalUnitOfWork _uow,
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

            var branchVm = new BranchSelectionViewModel
            {
                CbBranches = await _configServiceClient.GetCbBranchesByCbIdAsync(2),
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
            });
        }

        // ── Branch details endpoint (AJAX) ────────
        //[HttpGet("branch-details")]
        public async Task<IActionResult> BranchDetails(string type, int id)
        {
            if (type == "cb")
            {
                var branch = await _configServiceClient.GetCbBranchByIdAsync(id);

                if (branch is null) 
                    return NotFound();

                return Json(new { branch.Name, branch.Id });
            }

            if (type == "bou")
            {
                var branch = await _configServiceClient.GetBouBranchByIdAsync(id);
                
                if (branch is null) 
                    return NotFound();

                return Json(new { branch.Name, branch.Id });
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
        [HttpPost("save-draft")]
        public async Task<IActionResult> SaveDraft(
            SaveWithdrawalRequestViewModel vm, CancellationToken ct)
        {
            var (userId, cbUserId) = GetUserContext();
            var request = await GetOrCreateRequestAsync(vm, userId, cbUserId, ct);

            // Block edit if submitted to supervisor
            if (request.StatusTypeId == WithdrawalStatusIds.SubmittedToSupervisor)
                return Forbid();

            request.StatusTypeId = WithdrawalStatusIds.Draft;
            request.ModifiedDate = DateTime.Now;
            request.ModifiedBy = userId;

            await SaveDenominationsAsync(request.Id, vm.Denominations, userId, ct);
            await SaveAgentsAsync(request.Id, vm.AgentIds, userId, ct);
            await AddStatusHistoryAsync(request.Id, request.StatusTypeId, WithdrawalStatusIds.Draft, userId, "Saved as draft", ct);

            await _uow.SaveAsync(ct);

            return RedirectToAction("Edit", new { id = request.Id });
        }

        // ── Submit to Supervisor ──────────────────
        [HttpPost("submit-to-supervisor")]
        public async Task<IActionResult> SubmitToSupervisor(
            SaveWithdrawalRequestViewModel vm, CancellationToken ct)
        {
            if (!ValidateSubmit(vm, out var errors))
            {
                TempData["Errors"] = errors;
                return RedirectToAction(vm.Id.HasValue ? "Edit" : "Create", new { id = vm.Id });
            }

            var (userId, companyId) = GetUserContext();
            var request = await GetOrCreateRequestAsync(vm, userId, companyId, ct);

            // Block if already submitted and not returned for revision
            if (request.StatusTypeId == WithdrawalStatusIds.SubmittedToSupervisor)
                return Forbid();

            var oldStatusId = request.StatusTypeId;

            // Check for special approval — collection date == RTGS date
            var newStatusId = vm.RtgsDate.HasValue && vm.CollectionDate == vm.RtgsDate.Value
                ? WithdrawalStatusIds.PendingSpecialApproval
                : WithdrawalStatusIds.SubmittedToSupervisor;

            request.StatusTypeId = newStatusId;
            request.RequiresSpecialApproval = newStatusId == WithdrawalStatusIds.PendingSpecialApproval;
            request.ModifiedDate = DateTime.Now;
            request.ModifiedBy = userId;

            await SaveDenominationsAsync(request.Id, vm.Denominations, userId, ct);
            await SaveAgentsAsync(request.Id, vm.AgentIds, userId, ct);
            await AddStatusHistoryAsync(request.Id, oldStatusId, newStatusId, userId, "Submitted to supervisor", ct);

            await _uow.SaveAsync(ct);

            return RedirectToAction("Confirmation", new { id = request.Id });
        }

        // ── Supervisor: Return for Revision ───────
        [HttpPost("return-for-revision")]
        public async Task<IActionResult> ReturnForRevision(
            Guid id, string remarks, CancellationToken ct)
        {
            var request = await _uow.Requests.FindById(id);
            if (request is null) return NotFound();
            if (!await CanAccessRequestAsync(request.cbUserId)) return Forbid();

            if (request.StatusTypeId != WithdrawalStatusIds.SubmittedToSupervisor)
                return BadRequest("Request is not in a returnable state.");

            var userId = GetUserId();
            var oldStatusId = request.StatusTypeId;

            request.StatusTypeId = WithdrawalStatusIds.ReturnedForRevision;
            request.ModifiedDate = DateTime.Now;
            request.ModifiedBy = userId;

            await AddStatusHistoryAsync(request.Id, oldStatusId, WithdrawalStatusIds.ReturnedForRevision, userId, remarks, ct);

            await _uow.SaveAsync(ct);

            return RedirectToAction("Details", new { id });
        }

        // ── Supervisor: Submit to Bank ────────────
        [HttpPost("submit-to-bank")]
        public async Task<IActionResult> SubmitToBank(
            Guid id, string? remarks, CancellationToken ct)
        {
            var request = await _uow.Requests.FindById(id);
            if (request is null) return NotFound();
            if (!await CanAccessRequestAsync(request.cbUserId)) return Forbid();

            if (request.StatusTypeId != WithdrawalStatusIds.SubmittedToSupervisor)
                return BadRequest("Request must be submitted to supervisor before going to bank.");

            var userId = GetUserId();
            var oldStatusId = request.StatusTypeId;

            // Generate reference number — only once, immutable from here
            request.TraansactionRefNumber = await _txnrefNoGenerator.GenerateAsync(
                bouBranchId: request.BouBranchId,
                cbId: request.CbId,
                collectionDate: request.CollectionDate,
                ct: ct);

            request.StatusTypeId = WithdrawalStatusIds.SubmittedToBank;
            request.SupervisorUserId = userId;
            request.ModifiedDate = DateTime.Now;
            request.ModifiedBy = userId;

            // Copy company denomination lines to bank lines
            await CopyDenominationsToBankAsync(request.Id, userId, ct);

            await AddStatusHistoryAsync(request.Id, oldStatusId, WithdrawalStatusIds.SubmittedToBank, userId, "Submitted to bank", ct);

            await _uow.SaveAsync(ct);

            return RedirectToAction("Details", new { id });
        }

        // ── Helpers ───────────────────────────────
        private async Task<WithdrawalRequest> GetOrCreateRequestAsync(
        SaveWithdrawalRequestViewModel vm,
        string userId,
        Guid cbUserId,
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
                    cbUserId = cbUserId,
                    InitiatorUserId = userId,
                    StatusTypeId = WithdrawalStatusIds.Draft,
                    CreatedBy = userId,
                    CreatedDate = DateTime.Now,
                };
                await _uow.Requests.Create(request);
            }

            request.CbBranchId = vm.CbBranchId;
            request.BouBranchId = vm.BouBranchId;
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

        private (string userId, Guid cbUserId) GetUserContext()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var cbUserId = Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == "CbMaker")?.Value ?? Guid.Empty.ToString());
            return (userId, cbUserId);
        }

        private string GetUserId()
            => User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        public static class WithdrawalStatusIds
        {
            public const int Draft = 1;
            public const int SubmittedToSupervisor = 2;
            public const int ReturnedForRevision = 3;
            public const int SubmittedToBank = 4;
            public const int PendingSpecialApproval = 5;
            public const int Approved = 6;
            public const int Rejected = 7;
            public const int Cancelled = 8;
        }

        private bool ValidateSubmit(SaveWithdrawalRequestViewModel vm, out List<string> errors)
        {
            errors = [];

            if (vm.CbBranchId == 0)
                errors.Add("Please select a company branch.");

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
            int newStatusId,
            string userId,
            string remarks,
            CancellationToken ct)
        {
            await _uow.StatusHistory.Create(new WithdrawalRequestStatusHistory
            {
                WithdrawalRequestId = requestId,
                OldStatusId = oldStatusId,
                NewStatusId = newStatusId,
                ChangedBy = userId,
                ChangedAt = DateTime.UtcNow,
                Remarks = remarks,
                CreatedBy = userId,
                CreatedDate = DateTime.Now,
            });
        }
    }
}

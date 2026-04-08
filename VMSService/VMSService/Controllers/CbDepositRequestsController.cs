using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Models.CbDepositRequest;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.DenominationTypes;
using VMSService.Core.Repositories.DepositUnitOfWorks;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    //[Authorize(Policy = RolePolicy.CbOnly)]
    public class CbDepositRequestsController(
        IAuthorizationService authorizationService,
        IAgentDetailRepository _agentRepo,
        IConfigurationsServiceClient _configServiceClient,
        IDenominationRepository _denomRepo,
        IDepositUnitOfWork _uow,
        IWorkflowService _workflowService,
        ILogger<CbDepositRequestsController> _logger,
        IMapper _mapper,
        IDepositTransactionRefNumberGenerator _depositTransactionRefNumberGenerator,
        IWorkflowServiceClient _workflowServiceClient,
        IDenominationTypeRepository _denominationTypeRepo) : BaseWithdrawController(authorizationService)
    {
        public IActionResult Index()
        {
            return View();
        }

        // Create
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            var cbId = GetCbIdFromClaims();
            var agents = await _agentRepo.FindAll();
            var denoms = await _denomRepo.FindAll();

            var bouBranches = await _configServiceClient.GetAllBouBranchesAsync();
            var cb = await _configServiceClient.GetCbIdAsync(cbId);

            var vm = new CreateDepositViewModel
            {
                Steps = DepositStepViewModelFactory.Build("CbMaker", false, User),
                Branches = new FromToSelectionViewModel
                {
                    BouBranches = bouBranches,
                    Cb = cb,
                },
                Agents = new AgentSelectionViewModel
                {
                    Agents = agents
                        .Where(a => a.IsActive && !a.IsDeleted)
                        .Select(a => _mapper.Map<AgentDetailViewModel>(a))
                        .ToList(),
                },
                Denoms = new DepositDenomTableViewModel
                {
                    //Notes = denoms.Where(d => d.DenominationTypeId == noteTypeId)
                    //              .OrderByDescending(d => d.Value),
                    //Coins = denoms.Where(d => d.DenominationTypeId == coinTypeId)
                    //              .OrderByDescending(d => d.Value),
                    Notes = denoms.Where(d => d.DenominationType.Name.Equals("Note")),
                    Coins = denoms.Where(d => d.DenominationType.Name.Equals("Coin")),
                },
            };

            return View(vm);
        }

        // Branch details endpoint (AJAX)
        public async Task<IActionResult> BranchDetails(string type, int id)
        {
            if (type == "bou")
            {
                var branch = await _configServiceClient.GetBouBranchByIdAsync(id);
                if (branch == null) return NotFound();
                return Json(new { name = branch.Name, code = branch.BranchCode });
            }

            return BadRequest();
        }

        // Agent details endpoint (AJAX)
        public async Task<IActionResult> AgentDetails(int id)
        {
            var agent = await _agentRepo.FindById(id);
            if (agent == null) return NotFound();

            return Json(new
            {
                agentNames = agent.AgentNames,
                nin = agent.NIN,
                phoneNumber = agent.PhoneNumber,
                passportPhoto = agent.PassportPhoto,
                workIdScan = agent.WorkIdScan,
                signatureScan = agent.SignatureScan,
            });
        }

        // Save as Draft
        public async Task<IActionResult> SaveDraft([FromBody] SaveDepositRequestViewModel vm, CancellationToken ct)
        {
            try
            {
                var userId = GetUserId();
                var request = await GetOrCreateRequestAsync(vm, userId, ct);

                // Ensure draft status
                if (request.StatusTypeId == 0)
                {
                    StatusTypeDto status = await GetStatusByNameAsync(DepositStatuses.DepositStatusNames.Draft);
                    request.StatusTypeId = status.Id;
                }

                // Only call Update for existing entities; new ones are already tracked as Added
                if (vm.Id.HasValue)
                {
                    await _uow.Requests.Update(request);
                } else
                {
                    await _uow.Requests.Create(request);
                }

                await _uow.SaveAsync(ct);

                // Save denominations
                await SaveDenominationsAsync(request.Id, vm.Denominations, userId, ct);

                // Save agents
                await SaveAgentsAsync(request.Id, vm.AgentIds, userId, ct);

                return Ok($"/CbDepositRequests/Details/{request.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving deposit draft");
                return BadRequest(new { errors = new[] { "An unexpected error occurred. Please try again." } });
            }
        }

        //// Submit to Supervisor
        //[HttpPost("submit-to-supervisor")]
        //public async Task<IActionResult> SubmitToSupervisor(SaveDepositRequestViewModel vm, CancellationToken ct)
        //{
        //    if (!ValidateSubmit(vm, out var errors))
        //    {
        //        return BadRequest(new { errors });
        //    }

        //    try
        //    {
        //        var userId = GetUserId();
        //        var request = await GetOrCreateRequestAsync(vm, userId, ct);

        //        var oldStatusId = request.StatusTypeId;
        //        request.StatusTypeId = DepositStatuses.DepositStatusIds.SubmittedToSupervisor;

        //        await _uow.Requests.Update(request);
        //        await _uow.SaveAsync(ct);

        //        // Save denominations
        //        await SaveDenominationsAsync(request.Id, vm.Denominations, userId, ct);

        //        // Save agents
        //        await SaveAgentsAsync(request.Id, vm.AgentIds, userId, ct);

        //        // Copy denominations to BOU
        //        await CopyDenominationsToBankAsync(request.Id, userId, ct);

        //        // Add status history
        //        await AddStatusHistoryAsync(
        //            request.Id,
        //            oldStatusId,
        //            DepositStatuses.DepositStatusNames.Draft,
        //            DepositStatuses.DepositStatusIds.SubmittedToSupervisor,
        //            DepositStatuses.DepositStatusNames.SubmittedToSupervisor,
        //            userId,
        //            DepositStatuses.DepositRemarks.SubmittedToSupervisor,
        //            ct);

        //        return Ok($"/CbDepositRequests/Details/{request.Id}");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error submitting deposit to supervisor");
        //        return BadRequest(new { errors = new[] { "An unexpected error occurred. Please try again." } });
        //    }
        //}

        // Details
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var request = await _uow.Requests.GetWithDetailsAsync(id, ct);
            if (request == null) return NotFound();

            StatusTypeDto currentStatus = await _workflowServiceClient.FindStatusById(request.StatusTypeId);

            var currentStepKey = GetCurrentStepKey(currentStatus.Name);

            // Resolve agent details
            var agentDetails = new List<AgentDetailViewModel>();
            foreach (var ra in request.DepositRequestAgents)
            {
                var agent = await _agentRepo.FindById(ra.AgentId);
                if (agent != null)
                    agentDetails.Add(_mapper.Map<AgentDetailViewModel>(agent));
            }

            var cbDenoms = request.DepositRequestDenominations
                .Where(d => d.Origin == DepositStatuses.DepositOrigin.Cb);

            StatusTypeDto approvedStatus = await GetStatusByNameAsync(DepositStatuses.DepositStatusNames.Approved);

            var vm = new DepositRequestDetailsViewModel
            {
                Id = request.Id,
                ReferenceNumber = request.TransactionRefNumber,
                StatusId = request.StatusTypeId,
                StatusName = currentStatus.Name,
                DepositDate = request.DepositDate,
                RtgsDate = request.RtgsDate,
                RtgsCode = request.RtgsCode,
                DepositAgents = agentDetails,
                CbName = "",
                CbBranchName = "",
                FitNotes = cbDenoms
                    .Where(d => d.DenominationTypeId == 1 && d.SubCategoryId == 1)
                    .Select(d => new DepositDenominationLineViewModel
                    {
                        DenominationLabel = $"UGX {d.DenominationValue:N0}",
                        Bundles = d.Bundles,
                        Amount = d.Amount,
                        SubCategoryId = d.SubCategoryId,
                        SubCategoryName = "Fit",
                    }),
                UnfitNotes = cbDenoms
                    .Where(d => d.DenominationTypeId == 1 && d.SubCategoryId == 2)
                    .Select(d => new DepositDenominationLineViewModel
                    {
                        DenominationLabel = $"UGX {d.DenominationValue:N0}",
                        Bundles = d.Bundles,
                        Amount = d.Amount,
                        SubCategoryId = d.SubCategoryId,
                        SubCategoryName = "Unfit",
                    }),
                NANotes = cbDenoms
                    .Where(d => d.DenominationTypeId == 1 && d.SubCategoryId == 3)
                    .Select(d => new DepositDenominationLineViewModel
                    {
                        DenominationLabel = $"UGX {d.DenominationValue:N0}",
                        Bundles = d.Bundles,
                        Amount = d.Amount,
                        SubCategoryId = d.SubCategoryId,
                        SubCategoryName = "N/A",
                    }),
                Coins = cbDenoms
                    .Where(d => d.DenominationTypeId == 2)
                    .Select(d => new DepositDenominationLineViewModel
                    {
                        DenominationLabel = $"UGX {d.DenominationValue:N0}",
                        Boxes = d.Boxes,
                        Amount = d.Amount,
                        SubCategoryId = d.SubCategoryId,
                        SubCategoryName = "N/A",
                    }),
                TotalNotesAmount = request.TotalNotesAmount,
                TotalCoinsAmount = request.TotalCoinsAmount,
                TotalAmount = request.TotalAmount,
                Steps = WithdrawalStepViewModelFactory.Build(currentStepKey, 
                    request.StatusTypeId == approvedStatus.Id, User),
                StatusHistory = request.DepositRequestStatusHistory,
            };

            var userRole = GetUserRole();
            vm.AvailableTransitions = await _workflowService
                .GetAvailableTransitionsAsync("DepositRequest", request.StatusTypeId, userRole);

            return View(vm);
        }

        private static string GetCurrentStepKey(string status) => status switch
        {
            DepositStatuses.DepositStatusNames.Draft or
            DepositStatuses.DepositStatusNames.ReturnedForRevision => "CbMaker",
            DepositStatuses.DepositStatusNames.SubmittedToSupervisor => "CbApprover",
            _ => "BOU",
        };

        // Execute workflow action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(Guid id, string actionName, string remarks, CancellationToken ct)
        {
            var request = await _uow.Requests.GetWithDetailsAsync(id, ct);
            if (request == null) return NotFound();

            var userId = GetUserId();
            var userRole = GetUserRole();

            var success = await _workflowService.ExecuteTransition(
                "DepositRequest", id, request.StatusTypeId,
                actionName, userId, [userRole], remarks);

            if (!success)
                return BadRequest(new { errors = new[] { "Failed to execute action." } });

            // Reload to get new status
            var updated = await _uow.Requests.GetWithDetailsAsync(id, ct);

            await AddStatusHistoryAsync(
                id,
                request.StatusTypeId,
                updated!.StatusTypeId,
                userId,
                remarks,
                ct);

            return RedirectToAction(nameof(Details), new { id });
        }

        // Helpers

        private async Task<DepositRequest> GetOrCreateRequestAsync(
            SaveDepositRequestViewModel vm,
            string userId,
            CancellationToken ct)
        {
            DepositRequest request;

            if (vm.Id.HasValue)
            {
                request = await _uow.Requests.FindById(vm.Id.Value);
                request.DepositDate = vm.DepositDate;
                request.RtgsDate = vm.RtgsDate;
                request.RtgsCode = vm.RtgsCode;
                request.BouBranchId = vm.BouBranchId;
            }
            else
            {
                string transactionRefNumber = await _depositTransactionRefNumberGenerator.GenerateAsync(vm.BouBranchId, vm.CbId, vm.DepositDate);

                StatusTypeDto draftStatus = await GetStatusByNameAsync(DepositStatuses.DepositStatusNames.Draft);

                DenominationType noteType = (await _denominationTypeRepo.FindAll()).FirstOrDefault(x => x.Name == StockConstants.DenominationTypes.Note);
                int notTypeId = noteType != null ? noteType.Id : 0;
                DenominationType coinType = (await _denominationTypeRepo.FindAll()).FirstOrDefault(x => x.Name == StockConstants.DenominationTypes.Coin);
                int coinTypeId = coinType != null ? coinType.Id : 0;

                request = new DepositRequest
                {
                    TransactionRefNumber = transactionRefNumber,
                    CbCode = vm.CbCode,
                    InitiatorUserId = userId,
                    BouBranchId = vm.BouBranchId,
                    DepositDate = vm.DepositDate,
                    RtgsDate = vm.RtgsDate,
                    RtgsCode = vm.RtgsCode,
                    //CbId = GetCbIdFromClaims(),
                    CbId = vm.CbId,
                    StatusTypeId = draftStatus.Id,
                    // Calculate totals from denominations
                    TotalNotesAmount = vm.Denominations
                        .Where(d => d.DenominationTypeId == notTypeId)
                        .Sum(d => d.Amount),
                    TotalCoinsAmount = vm.Denominations
                        .Where(d => d.DenominationTypeId == coinTypeId)
                        .Sum(d => d.Amount)
                };
                request.TotalAmount = request.TotalNotesAmount + request.TotalCoinsAmount;

                await _uow.Requests.Create(request);
            }

            return request;
        }

        private string GetUserId()
            => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        private bool ValidateSubmit(SaveDepositRequestViewModel vm, out List<string> errors)
        {
            errors = [];

            if (vm.BouBranchId == 0)
                errors.Add("Please select a bank branch.");

            if (vm.AgentIds.Count(id => id > 0) != 2)
                errors.Add("Exactly 2 agents are required.");

            if (vm.Denominations.Count == 0)
                errors.Add("Please add at least one denomination.");

            if (vm.DepositDate == default)
                errors.Add("Please select a deposit date.");

            return errors.Count == 0;
        }

        private async Task SaveDenominationsAsync(
            Guid requestId,
            List<SaveDepositDenominationLine> lines,
            string userId,
            CancellationToken ct)
        {
            _uow.Denominations.DeleteByRequest(requestId, DepositStatuses.DepositOrigin.Cb);
            await _uow.SaveAsync(ct);

            foreach (var line in lines)
            {
                var denom = new DepositRequestDenomination
                {
                    DepositRequestId = requestId,
                    DenominationId = line.DenominationId,
                    DenominationValue = line.DenominationValue,
                    DenominationTypeId = line.DenominationTypeId,
                    Bundles = line.Bundles,
                    Boxes = line.Boxes,
                    Amount = line.Amount,
                    Origin = DepositStatuses.DepositOrigin.Cb,
                    SubCategoryId = line.SubCategoryId,
                    CreatedBy = userId,
                    ModifiedBy = userId,
                };
                await _uow.Denominations.Create(denom);
            }

            await _uow.SaveAsync(ct);
        }

        private async Task SaveAgentsAsync(
            Guid requestId,
            List<int> agentIds,
            string userId,
            CancellationToken ct)
        {
            _uow.DepositAgents.DeleteByRequestAndSide(requestId, DepositStatuses.DepositEnteredBy.Cb);
            await _uow.SaveAsync(ct);

            foreach (var agentId in agentIds.Where(id => id > 0))
            {
                var agent = new DepositRequestAgent
                {
                    DepositRequestId = requestId,
                    AgentId = agentId,
                    EnteredBy = DepositStatuses.DepositEnteredBy.Cb,
                    CreatedBy = userId,
                    ModifiedBy = userId,
                };
                await _uow.DepositAgents.Create(agent);
            }

            await _uow.SaveAsync(ct);
        }

        private async Task CopyDenominationsToBankAsync(
            Guid requestId,
            string userId,
            CancellationToken ct)
        {
            await _uow.Denominations.CopyToBouAsync(requestId, userId, ct);
            await _uow.SaveAsync(ct);
        }

        private async Task AddStatusHistoryAsync(
            Guid requestId,
            int oldStatusId,
            int newStatusId,
            string userId,
            string remarks,
            CancellationToken ct)
        {
            var history = new DepositRequestStatusHistory
            {
                DepositRequestId = requestId,
                OldStatusId = oldStatusId,
                NewStatusId = newStatusId,
                ChangedBy = userId,
                ChangedAt = DateTime.UtcNow,
                Remarks = remarks,
                CreatedBy = userId,
                ModifiedBy = userId,
            };

            await _uow.StatusHistory.Create(history);
            await _uow.SaveAsync(ct);
        }

        private int GetUserRole()
            => int.TryParse(User.FindFirst("roleId")?.Value, out var r) ? r : 0;

        private int GetCbIdFromClaims()
            => int.TryParse(User.FindFirst("cbId")?.Value, out var id) ? id : 0;

        private async Task<StatusTypeDto> GetStatusByNameAsync(string statusName)
        {
            var requestData = new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = statusName,
                EntityTypeName = DepositStatuses.EntityName
            };
            return await _workflowServiceClient.FindStatusByName(requestData);
        }
    }
}
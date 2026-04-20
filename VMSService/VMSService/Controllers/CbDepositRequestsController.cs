using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VMSService.Core.Authorization;
using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Models.CbDepositRequest;
using VMSService.Core.Models.CbDepositRequests;
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
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            try
            {
                //var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
                //var isBouUser = RolePolicy.BouRoles.Contains(userRole);
                var userRoles = User.FindAll("appRole").Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:")).Select(c => c.Value.Split(":")[1]).ToList();
                var isBouUser = false;
                foreach (var role in userRoles)
                {
                    if (RolePolicy.BouRoles.Contains(role))
                    {
                        isBouUser = true;
                        break;
                    }
                    //isBouUser = RolePolicy.BouRoles.Contains(role);
                }

                IEnumerable<DepositRequest> deposits;

                if (isBouUser)
                {
                    // BOU users see all deposit requests
                    deposits = await _uow.Requests.FindAll();
                }
                else
                {
                    // CB users see only their own deposits
                    var cbId = GetCbIdFromClaims();
                    deposits = await _uow.Requests.GetByCbAsync(cbId, ct);
                }

                var rows = await Task.WhenAll(deposits.Select(async r => new RecentRequestRowViewModel
                {
                    Id = r.Id,
                    Amount = r.TotalAmount,
                    StatusTypeName = (await _workflowServiceClient.FindStatusById(r.StatusTypeId)).Name,
                    CollectionDate = r.DepositDate,
                    TransactionRefNumber = r.TransactionRefNumber ?? "Not Generated",
                    CreationDate = r.CreatedDate,
                    RequiresSpecialApproval = r.RequiresSpecialApproval,
                    RtgsCode = string.IsNullOrEmpty(r.RtgsCode) ? "—" : r.RtgsCode,
                }));

                var vm = new DepositRequestIndexViewModel
                {
                    Deposits = rows,
                    IsBouUser = isBouUser,
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving deposit requests");
                TempData["Error"] = "An error occurred while retrieving deposit requests. Please try again.";
                return View(new DepositRequestIndexViewModel());
            }
        }

        // Create
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            var cbId = GetCbIdFromClaims();
            var agents = await _agentRepo.FindAll();
            var denoms = await _denomRepo.FindAll();

            var bouBranches = await _configServiceClient.GetAllBouBranchesAsync();
            var cb = await _configServiceClient.GetCbIdAsync(cbId);

            var requestData = new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = StatusConstants.AgentDetail.Approved,
                EntityTypeName = StatusConstants.Entity.AgentDetail,
            };
            StatusTypeDto approvedStatus =  await _workflowServiceClient.FindStatusByName(requestData);

            var vm = new CreateDepositViewModel
            {
                Steps = DepositStepViewModelFactory.Build("CbMaker", false, false, User),
                Branches = new FromToSelectionViewModel
                {
                    BouBranches = bouBranches,
                    Cb = cb,
                },
                Agents = new AgentSelectionViewModel
                {
                    Agents = agents
                        .Where(a => a.IsActive && !a.IsDeleted && a.CommercialBankId == cbId && a.CurrentStatusId == approvedStatus.Id)
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
                if (request.StatusTypeId == 0 && request.RequiresSpecialApproval)
                {
                    StatusTypeDto status = await GetStatusByNameExceptionalAsync(DepositStatuses.DepositStatusNames.Draft);
                    request.StatusTypeId = status.Id;
                } else if (request.StatusTypeId == 0 && !request.RequiresSpecialApproval)
                {
                    StatusTypeDto status = await GetStatusByNameAsync(DepositStatuses.DepositStatusNames.Draft);
                    request.StatusTypeId = status.Id;
                }

                // Only call Update for existing entities; new ones are already tracked as Added
                if (vm.Id.HasValue)
                {
                    await _uow.Requests.Update(request);
                }
                else
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

        // Details
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var request = await _uow.Requests.GetWithDetailsAsync(id, ct);
            if (request == null) return NotFound();

            // Current status
            StatusTypeDto currentStatus = await _workflowServiceClient.FindStatusById(request.StatusTypeId);

            var currentStepKey = GetCurrentStepKey(currentStatus.Name);

            var userRoles = User.FindAll("appRole").Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:")).Select(c => c.Value.Split(":")[1]).ToList();
            var isBouUser = false;
            foreach (var role in userRoles)
            {
                if (RolePolicy.BouRoles.Contains(role))
                {
                    isBouUser = true;
                    break;
                }
                //isBouUser = RolePolicy.BouRoles.Contains(role);
            }

            if (isBouUser)
            {
                currentStepKey = GetCurrentStepKeyBOU(currentStatus.Name);
            }

            // Resolve agent details
            var cbAgentIds = request.DepositRequestAgents
                .Select(a => a.AgentId)
                .ToList();

            var agentDetails = new List<AgentDetailViewModel>();
            foreach (var agentId in cbAgentIds)
            {
                var agent = await _agentRepo.FindById(agentId);
                if (agent != null)
                    agentDetails.Add(_mapper.Map<AgentDetailViewModel>(agent));
            }

            // Get denominations for CB
            var cbDenoms = request.DepositRequestDenominations
                .Where(d => d.Origin == DepositStatuses.DepositOrigin.Cb);

            StatusTypeDto approvedStatus = await GetStatusByNameAsync(DepositStatuses.DepositStatusNames.Approved);

            if (request.RequiresSpecialApproval)
            {
                approvedStatus = await GetStatusByNameExceptionalAsync(DepositStatuses.DepositStatusNames.Approved);
            }

            // Commercial bank details
            var cb = await _configServiceClient.GetCbIdAsync(request.CbId);
            string cbBranchName = string.Empty;
            if (request.CbBranchId > 0)
            {
                var cbBranch = await _configServiceClient.GetCbBranchByIdAsync(request.CbBranchId);
                cbBranchName = cbBranch.Name;
            }

            // Bou branch details
            var bouBranch = await _configServiceClient.GetBouBranchByIdAsync(request.BouBranchId);

            // Denominatiom types
            DenominationType noteType = (await _denominationTypeRepo.FindAll()).FirstOrDefault(x => x.Name == StockConstants.DenominationTypes.Note);
            int noteTypeId = noteType != null ? noteType.Id : 0;
            DenominationType coinType = (await _denominationTypeRepo.FindAll()).FirstOrDefault(x => x.Name == StockConstants.DenominationTypes.Coin);
            int coinTypeId = coinType != null ? coinType.Id : 0;

            // Comments/Status History
            var statusHistoryList = _mapper.Map<IEnumerable<DepositRequestStatusHistoryViewModel>>(request.DepositRequestStatusHistory);

            foreach (var history in statusHistoryList)
            {
                var status = await _workflowServiceClient.FindStatusById(history.NewStatusId);
                history.NewStatusName = status.Name;
                var oldStatus = await _workflowServiceClient.FindStatusById(history.OldStatusId);
                history.OldStatusName = oldStatus.Name;
            }

            // Logged in user's roles
            var userRoleIds = GetUserRoles();

            var availableTransitions = new List<WorkflowTransitionDto>();
            if (request.RequiresSpecialApproval)
            {
                foreach (var userRoleId in userRoleIds)
                {
                    var requestData = new GetAvailableTransitionsRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName = StatusConstants.Entity.DepositRequestExceptional,
                        CurrentStatusId = request.StatusTypeId,
                        AppRole = userRoleId,
                    };

                    var transitions = await _workflowServiceClient.GetAvailableTransitions(requestData);
                    if (transitions != null)
                    {
                        availableTransitions.AddRange(transitions);
                    }
                }
            } else
            {
                foreach (var userRoleId in userRoleIds)
                {
                    var requestData = new GetAvailableTransitionsRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName = StatusConstants.Entity.DepositRequest,
                        CurrentStatusId = request.StatusTypeId,
                        AppRole = userRoleId,
                    };

                    var transitions = await _workflowServiceClient.GetAvailableTransitions(requestData);
                    if (transitions != null)
                    {
                        availableTransitions.AddRange(transitions);
                    }
                }
            } 

            var vm = new DepositRequestDetailsViewModel
            {
                Id = request.Id,
                ReferenceNumber = request.TransactionRefNumber,
                StatusTypeId = request.StatusTypeId,
                StatusName = currentStatus.Name,
                DepositDate = request.DepositDate,
                RtgsDate = request.RtgsDate,
                RtgsCode = request.RtgsCode,
                DepositAgents = agentDetails,
                CbName = cb.Name,
                CbBranchName = cbBranchName,
                CbBicCode = cb.Code,
                CbCode = cb.CBCode,
                BouBranchName = bouBranch.Name,
                BouBranchCode = bouBranch.BranchCode,
                FitNotes = cbDenoms
                    .Where(d => d.DenominationTypeId == noteTypeId && d.SubCategoryId == 1)
                    .Select(d => new DepositDenominationLineViewModel
                    {
                        DenominationLabel = $"UGX {d.DenominationValue:N0}",
                        Bundles = d.Bundles,
                        Amount = d.Amount,
                        SubCategoryId = d.SubCategoryId,
                        SubCategoryName = "Fit",
                    }),
                UnfitNotes = cbDenoms
                    .Where(d => d.DenominationTypeId == noteTypeId && d.SubCategoryId == 2)
                    .Select(d => new DepositDenominationLineViewModel
                    {
                        DenominationLabel = $"UGX {d.DenominationValue:N0}",
                        Bundles = d.Bundles,
                        Amount = d.Amount,
                        SubCategoryId = d.SubCategoryId,
                        SubCategoryName = "Unfit",
                    }),
                NANotes = cbDenoms
                    .Where(d => d.DenominationTypeId == noteTypeId && d.SubCategoryId == 3)
                    .Select(d => new DepositDenominationLineViewModel
                    {
                        DenominationLabel = $"UGX {d.DenominationValue:N0}",
                        Bundles = d.Bundles,
                        Amount = d.Amount,
                        SubCategoryId = d.SubCategoryId,
                        SubCategoryName = "N/A",
                    }),
                Coins = cbDenoms
                    .Where(d => d.DenominationTypeId == coinTypeId)
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
                Steps = DepositStepViewModelFactory.Build(currentStepKey, 
                    request.StatusTypeId == approvedStatus.Id, request.RequiresSpecialApproval, User),
                StatusHistory = statusHistoryList,
                AvailableTransitions = availableTransitions,  
                RequiresSpecialApproval = request.RequiresSpecialApproval,
                SpecialApprovalReason = request.SpecialApprovalReason,
            };

            //var userRoles = GetUserRoles();
            //vm.AvailableTransitions = await _workflowService
            //    .GetAvailableTransitionsAsync("DepositRequest", request.StatusTypeId, userRole);

            return View(vm);
        }

        private static string GetCurrentStepKey(string status) => status switch
        {
            DepositStatuses.DepositStatusNames.Draft or
            DepositStatuses.DepositStatusNames.ReturnedForRevision => "CbMaker",
            DepositStatuses.DepositStatusNames.SubmittedToSupervisor => "CbApprover",
            DepositStatuses.DepositStatusNames.PendingSpecialApproval or
            DepositStatuses.DepositStatusNames.SubmittedToBank or
            DepositStatuses.DepositStatusNames.Recommended => "BOU",
            DepositStatuses.DepositStatusNames.Approved => "Agents",
            _ => "none",
        };

        private static string GetCurrentStepKeyBOU(string status) => status switch
        {
            DepositStatuses.DepositStatusNames.Draft or
            DepositStatuses.DepositStatusNames.ReturnedForRevision or
            DepositStatuses.DepositStatusNames.SubmittedToSupervisor => "cb",
            DepositStatuses.DepositStatusNames.PendingSpecialApproval => "Remittance",
            DepositStatuses.DepositStatusNames.SubmittedToBank => "PanelA",
            DepositStatuses.DepositStatusNames.Recommended => "PanelB",
            DepositStatuses.DepositStatusNames.Approved => "Agents",
            _ => "none",
        };

        // Execute workflow action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(Guid id, string actionName, string remarks, CancellationToken ct)
        {
            var request = await _uow.Requests.GetWithDetailsAsync(id, ct);
            if (request == null) return NotFound();

            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var userRoles = GetUserRoles();

                bool isExecuted = false;

                if (request.RequiresSpecialApproval)
                {
                    isExecuted = await _workflowService.ExecuteTransition(
                        StatusConstants.Entity.DepositRequestExceptional, id, request.StatusTypeId,
                        actionName, username, userRoles, remarks, ct);
                } else
                {
                    isExecuted = await _workflowService.ExecuteTransition(
                        StatusConstants.Entity.DepositRequest, id, request.StatusTypeId,
                        actionName, username, userRoles, remarks, ct);
                }

                if (isExecuted)
                {
                    TempData["Success"] = "Action completed successfully.";
                }
                else
                {
                    TempData["Error"] = $"Failed to execute {actionName} action on Deposit Request {request.TransactionRefNumber}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing action {Action} on Deposit Request {TransactionRefNumber}", actionName, request.TransactionRefNumber);
                TempData["Error"] = "An error occurred while executing the action.";
            }
            // Reload to get new status
            //var updated = await _uow.Requests.GetWithDetailsAsync(id, ct);

            //await AddStatusHistoryAsync(
            //    id,
            //    request.StatusTypeId,
            //    updated!.StatusTypeId,
            //    userId,
            //    remarks,
            //    ct);

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

                if (vm.RequiresSpecialApproval)
                {
                    draftStatus = await GetStatusByNameExceptionalAsync(DepositStatuses.DepositStatusNames.Draft);
                }

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
                    RequiresSpecialApproval = vm.RequiresSpecialApproval,
                    SpecialApprovalReason = vm.SpecialApprovalReason,
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

        private List<int> GetUserRoles()
        {
            return User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                    .Select(c => int.TryParse(c.Value.Split(":")[1], out var appRoleId) ? appRoleId : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList();
        }

        private int GetCbIdFromClaims()
            => int.TryParse(User.FindFirst("cbId")?.Value, out var id) ? id : 0;

        private async Task<StatusTypeDto> GetStatusByNameAsync(string statusName)
        {
            var requestData = new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = statusName,
                EntityTypeName = StatusConstants.Entity.DepositRequest,
            };
            return await _workflowServiceClient.FindStatusByName(requestData);
        }

        private async Task<StatusTypeDto> GetStatusByNameExceptionalAsync(string statusName)
        {
            var requestData = new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = statusName,
                EntityTypeName = StatusConstants.Entity.DepositRequestExceptional,
            };
            return await _workflowServiceClient.FindStatusByName(requestData);
        }

    }
}
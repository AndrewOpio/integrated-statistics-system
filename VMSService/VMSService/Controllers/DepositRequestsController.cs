using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;
using VMSService.Controllers;
using VMSService.Core.Authorization;
using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Models.CbDepositRequest;
using VMSService.Core.Models.CbDepositRequests;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Models.DepositRequests;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.BranchCategories;
using VMSService.Core.Repositories.Branches;
using VMSService.Core.Repositories.Categories;
using VMSService.Core.Repositories.CounterPartyTypes;
using VMSService.Core.Repositories.DenominationTypes;
using VMSService.Core.Repositories.DepositUnitOfWorks;
using VMSService.Core.Repositories.FeeTypes;
using VMSService.Core.Repositories.StockMovements;
using VMSService.Core.Repositories.Stocks;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class DepositRequestsController(
        IDepositUnitOfWork _uow,
        IWorkflowServiceClient _workflowServiceClient,
        IWorkflowService _workflowService,
        IAgentDetailRepository _agentRepo,
        IConfigurationsServiceClient _configServiceClient,
        IDenominationTypeRepository _denominationTypeRepo,
        IStockLedgerRepository _stockLedgerRepository,
        ICounterPartyTypeRepository _counterPartyTypeRepository,
        IBranchCategoryRepository _branchCategoryRepository,
        ILogger<DepositRequestsController> _logger,
        IMapper _mapper,
        IStockMovementRepository _stockMovementRepository,
        ICategoryRepository _categoryRepository,
        IFeeTypeRepository _feeTypeRepo) : Controller
    {
        [Breadcrumb("Deposit Requests", FromController = typeof(HomeController), FromAction = nameof(Index))]
        public async Task<IActionResult> Index(string? branchCode = null)
        {
            ICollection<DepositRequest> visibleRequests = Array.Empty<DepositRequest>();

            var allStatuses = await _workflowServiceClient.FindAllStatuses();
            allStatuses = [.. allStatuses.Where(x => x.EntityType.Name == StatusConstants.Entity.DepositRequest || x.EntityType.Name == StatusConstants.Entity.DepositRequestExceptional)];

            var statusMap = allStatuses.ToDictionary(s => s.Id, s => s.Name);

            try
            {
                var result = await _configServiceClient.GetAllCommercialBankAsync();
                Console.WriteLine($"Result: {result}");
                // or use _logger.LogInformation(...)
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
            }

            var allCbs = await _configServiceClient.GetAllCommercialBankAsync();
            var cbMap = allCbs.ToDictionary(s => s.Id, s => s.Name);
            var allBouBranches = await _configServiceClient.GetAllBouBranchesAsync();

            // Get user roles
            var userRoles = User.FindAll("appRole")
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();

            // Determine if user can view all locations
            bool canViewAllLocations = userRoles.Contains(SystemPolicy.Roles.RemittancesOfficer);

            // Filter branches based on user roles
            var userBranches = new List<BranchDto>();
            var draftStatus = await GetStatusByNameAsync(DepositStatuses.DepositStatusNames.Draft);
            var draftStatusSpecial = await GetStatusByNameExceptionalAsync(DepositStatuses.DepositStatusNames.Draft);

            if (canViewAllLocations)
            {
                // Remittances Officer can see all branches
                userBranches = allBouBranches.ToList();
                visibleRequests = (await _uow.Requests.FindAll()).Where(r => r.StatusTypeId != draftStatus.Id && r.StatusTypeId != draftStatusSpecial.Id).ToList();
            }
            else
            {
                // Filter branches based on role names matching branch names
                userBranches = allBouBranches
                    .Where(branch => userRoles.Any(role => role.Contains(branch.Name)))
                    .ToList();
            }

            // Filter requests by selected branch if provided
            if (!string.IsNullOrEmpty(branchCode))
            {
                var selectedBranch = allBouBranches.FirstOrDefault(b => b.BranchCode == branchCode);
                if (selectedBranch != null)
                {
                    visibleRequests = (await _uow.Requests.FindByBouBranchId(selectedBranch.Id)).Where(r => r.StatusTypeId != draftStatus.Id && r.StatusTypeId != draftStatusSpecial.Id).ToList();
                }
            }
            else if (!canViewAllLocations && userBranches.Any())
            {
                // If no branch selected and user is not RemittancesOfficer, show only their branches
                List<int> userBranchIds = userBranches.Select(b => b.Id).ToList();
                visibleRequests = (await _uow.Requests.FindByBouBranchIds(userBranchIds)).Where(r => r.StatusTypeId != draftStatus.Id && r.StatusTypeId != draftStatusSpecial.Id).ToList();
            }

            // Counts based on filtered requests
            var pendingCb = visibleRequests.Count(r =>
                statusMap.GetValueOrDefault(r.StatusTypeId) == DepositStatuses.DepositStatusNames.SubmittedToSupervisor);

            var specialApproval = visibleRequests.Count(r => r.RequiresSpecialApproval);

            var approvedRequests = visibleRequests
                .Where(r => statusMap.GetValueOrDefault(r.StatusTypeId) == DepositStatuses.DepositStatusNames.Approved)
                .ToList();

            var approvedCount = approvedRequests.Count;

            var totalProcessed = approvedRequests.Sum(r => r.TotalAmount);

            // Calculate approved count difference from last month
            var lastMonth = DateTime.Now.AddMonths(-1);
            var lastMonthApproved = approvedRequests.Count(r => r.CreatedDate.Month == lastMonth.Month && r.CreatedDate.Year == lastMonth.Year);
            var thisMonthApproved = approvedRequests.Count(r => r.CreatedDate.Month == DateTime.Now.Month && r.CreatedDate.Year == DateTime.Now.Year);
            var approvedCountDiff = thisMonthApproved - lastMonthApproved;

            // Requests table
            var rows = await Task.WhenAll(visibleRequests.Select(async r => new RecentRequestRowViewModel
            {
                Id = r.Id,
                TransactionRefNumber = r.TransactionRefNumber ?? "Not Generated",
                Amount = r.TotalAmount,
                StatusTypeName = statusMap.GetValueOrDefault(r.StatusTypeId, "Unknown"),
                CbName = cbMap.GetValueOrDefault(r.CbId, "No CB"),
                CollectionDate = r.DepositDate,
                CreationDate = r.CreatedDate,
                RequiresSpecialApproval = r.RequiresSpecialApproval,
                RtgsCode = string.IsNullOrEmpty(r.RtgsCode) ? "—" : r.RtgsCode,
                BouBranchCode = (await _configServiceClient.GetBouBranchByIdAsync(r.BouBranchId))?.BranchCode ?? "—"
            }).ToList());

            var vm = new BouDepositRequestViewModel
            {
                PendingCbCount = pendingCb,
                SpecialApprovalCount = specialApproval,
                ApprovedCount = approvedCount,
                ApprovedCountDiff = approvedCountDiff,
                TotalProcessed = totalProcessed,
                Requests = rows,
                BouBranches = userBranches,
                SelectedBranch = branchCode,
                CanViewAllLocations = canViewAllLocations
            };

            return View(vm);
        }

        [Breadcrumb("Details", FromController = typeof(DepositRequestsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var request = await _uow.Requests.GetWithDetailsAsync(id, ct);
            if (request is null) return NotFound();

            //Display names from microservice
            var cb = await _configServiceClient.GetCbIdAsync(request.CbId);
            var bouBranch = await _configServiceClient.GetBouBranchByIdAsync(request.BouBranchId);

            string cbBranchName = string.Empty;
            if (request.CbBranchId > 0)
            {
                var cbBranch = await _configServiceClient.GetCbBranchByIdAsync(request.CbBranchId);
                cbBranchName = cbBranch.Name;
            }

            //Agent details
            var cbAgentIds = request.DepositRequestAgents
                .Select(a => a.AgentId)
                .ToList();

            var agentDetails = new List<AgentDetailViewModel>();
            foreach (var agentId in cbAgentIds)
            {
                var agent = await _agentRepo.FindById(agentId);
                if (agent is not null)
                    agentDetails.Add(_mapper.Map<AgentDetailViewModel>(agent));
            }

            //Denomination types
            var noteType = (await _denominationTypeRepo.FindAll()).FirstOrDefault(x => x.Name == StockConstants.DenominationTypes.Note);
            int noteTypeId = noteType?.Id ?? 0;
            var coinType = (await _denominationTypeRepo.FindAll()).FirstOrDefault(x => x.Name == StockConstants.DenominationTypes.Coin);
            int coinTypeId = coinType?.Id ?? 0;

            //Denominations (CB side)
            var cbDenoms = request.DepositRequestDenominations
                .Where(d => d.Origin == DepositStatuses.DepositOrigin.Cb);

            //Status
            var currentStatus = await _workflowServiceClient.FindStatusById(request.StatusTypeId);
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
            }

            if (isBouUser)
            {
                currentStepKey = GetCurrentStepKeyBOU(currentStatus.Name);
            }

            var approvedStatus = await GetStatusByNameAsync(DepositStatuses.DepositStatusNames.Approved);
            var completedStatus = await GetStatusByNameAsync(DepositStatuses.DepositStatusNames.Completed);

            if (request.RequiresSpecialApproval)
            {
                approvedStatus = await GetStatusByNameExceptionalAsync(DepositStatuses.DepositStatusNames.Approved);
                completedStatus = await GetStatusByNameExceptionalAsync(DepositStatuses.DepositStatusNames.Completed);
            }

            //Status history
            var statusHistoryList = _mapper.Map<IEnumerable<DepositRequestStatusHistoryViewModel>>(request.DepositRequestStatusHistory);
            foreach (var history in statusHistoryList)
            {
                history.NewStatusName = (await _workflowServiceClient.FindStatusById(history.NewStatusId)).Name;
                history.OldStatusName = (await _workflowServiceClient.FindStatusById(history.OldStatusId)).Name;
            }

            //Available transitions
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
            }
            else
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

            // Get fees from database
            var depositFees = await _uow.DepositFees.GetByDepositRequestId(id);
            var processingFeeType = await _feeTypeRepo.FindByName(SystemPolicy.FeeTypes.ProcessingFees);
            var penaltyFeeType = await _feeTypeRepo.FindByName(SystemPolicy.FeeTypes.MinDepositFees);

            decimal processingFee = 0;
            decimal penaltyFee = 0;

            if (processingFeeType != null)
            {
                var processingFeeEntity = depositFees.FirstOrDefault(f => f.FeeTypeId == processingFeeType.Id);
                if (processingFeeEntity != null)
                {
                    processingFee = processingFeeEntity.Amount;
                }
            }

            if (penaltyFeeType != null)
            {
                var penaltyFeeEntity = depositFees.FirstOrDefault(f => f.FeeTypeId == penaltyFeeType.Id);
                if (penaltyFeeEntity != null)
                {
                    penaltyFee = penaltyFeeEntity.Amount;
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
                RequiresSpecialApproval = request.RequiresSpecialApproval,
                SpecialApprovalReason = request.SpecialApprovalReason,
                DepositAgents = agentDetails,
                CbName = cb?.Name ?? "—",
                CbBranchName = cbBranchName,
                CbBicCode = cb?.Code ?? "—",
                CbCode = cb?.CBCode ?? "—",
                BouBranchName = bouBranch?.Name ?? "—",
                BouBranchCode = bouBranch?.BranchCode ?? "—",
                BouBranchId = bouBranch?.Id ?? 0,
                FitNotes = cbDenoms
                    .Where(d => d.DenominationTypeId == noteTypeId && d.SubCategoryId == 1)
                    .Select(d => new DepositDenominationLineViewModel
                    {
                        DenominationId = d.DenominationId,
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
                        DenominationId = d.DenominationId,
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
                        DenominationId = d.DenominationId,
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
                    (request.StatusTypeId == approvedStatus.Id || request.StatusTypeId == completedStatus.Id), 
                    request.RequiresSpecialApproval, User),
                StatusHistory = statusHistoryList,
                AvailableTransitions = availableTransitions,
                ProcessingFee = processingFee,
                MinimumDepositPenaltyFee = penaltyFee,
                TotalFees = processingFee + penaltyFee,
                HasPenaltyViolations = penaltyFee > 0
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(Guid id, string actionName, string remarks, CancellationToken ct)
        {
            
            var request = await _uow.Requests.FindById(id);
            if (request is null)
            {
                TempData["Error"] = "Deposit request not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var userRoles = GetUserRoles();

                bool isExecuted = false;

                if(request.RequiresSpecialApproval)
                {
                    isExecuted = await _workflowService.ExecuteTransition(
                    entityType: StatusConstants.Entity.DepositRequestExceptional,
                    entityId: id,
                    currentStatusId: request.StatusTypeId,
                    actionName: actionName,
                    username: username,
                    userRoles: userRoles,
                    remarks: remarks,
                    ct: ct); 
                } else
                {
                    isExecuted = await _workflowService.ExecuteTransition(
                    entityType: StatusConstants.Entity.DepositRequest,
                    entityId: id,
                    currentStatusId: request.StatusTypeId,
                    actionName: actionName,
                    username: username,
                    userRoles: userRoles,
                    remarks: remarks,
                    ct: ct);
                }

                if (isExecuted)
                {
                    if (actionName.Equals("Complete"))
                    {
                        var cbDenoms = request.DepositRequestDenominations
                                .Where(d => d.Origin == DepositStatuses.DepositOrigin.Cb);
                        Category paids = (await _categoryRepository.FindAll()).FirstOrDefault(c => c.Name == StockConstants.StoreTypes.Paids);
                        //BranchDto store = await _configServiceClient
                        //        .FindByBranchAndCategoryName(request.BouBranchId, StockConstants.StoreTypes.Paids);
                        var counterPartyType = await _counterPartyTypeRepository.FindByName(StockConstants.StockCounterPartyType.CommercialBank);

                        StatusTypeDto receivedStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                        {
                            ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                            StatusName = StatusConstants.StockMovement.Received,
                            EntityTypeName = StatusConstants.Entity.StockMovement,
                        });

                        var movement = new StockMovement
                        {
                            MovementType = StockConstants.StockMovementTypes.CommBankToBranch,
                            Date = DateTime.Now,
                            ReferenceNumber = request.TransactionRefNumber,
                            DispatchingBranchId = null,
                            ReceivingBranchId = request.BouBranchId,
                            TotalBagsBoxes = null,
                            GrandTotalValue = request.TotalAmount,
                            CurrentStatusId = receivedStatus.Id,
                            CurrentStatusName = receivedStatus.Name,
                            Denominations = [.. cbDenoms.Select(x => {
                                return new StockMovementDenomination
                                {
                                    DenominationId = x.DenominationId,
                                    NumBagsBoxes = x.Boxes,
                                    NumBundles = x.Bundles,
                                    CategoryId = paids?.Id,
                                    ContainerType = x.Boxes == null ? StockConstants.ContainerTypes.Bag : StockConstants.ContainerTypes.Box,
                                    SubTotalValue = x.Amount,
                                    Categorization = paids?.Name
                                };
                            })]
                        };

                        StockMovement newStockMovement = await _stockMovementRepository.CreateAsync(movement);

                        List<StockLedgerDetail> stockLedgerDetails = new();

                        foreach (var denom in cbDenoms)
                        {
                            StockLedgerDetail stockLedgerDetail = new()
                            {
                                CreatedDate = DateTime.Now,
                                ModifiedDate = DateTime.Now,
                                CreatedBy = username ?? "unknown",
                                ModifiedBy = username ?? "unknown",
                                DenominationId = denom.DenominationId,
                                NumberOfBoxes = denom.Boxes ?? 0,
                                NumberOfBundles = denom.Bundles,
                                NumberOfPieces = ((denom.Bundles ?? 0) * 1000) + ((denom.Boxes ?? 0) * 1000),
                                Value = denom.Amount,
                                CategoryId = paids?.Id ?? 2,
                                SubCategoryId = denom.SubCategoryId
                            };
                            stockLedgerDetails.Add(stockLedgerDetail);
                        }

                        // Create StockLedger entry
                        StockLedger stockLedger = new()
                        {
                            ReferenceCode = request.TransactionRefNumber,
                            StoreId = request.BouBranchId, //Branch
                            IsIn = true,
                            CounterPartyId = request.CbId,
                            CounterPartyTypeId = counterPartyType.Id, //Commercial Bank
                            Notes = remarks,
                            CreatedBy = username ?? "unknown",
                            CreatedDate = DateTime.Now,
                            ModifiedBy = username ?? "unknown",
                            ModifiedDate = DateTime.Now,
                            StockLedgerDetails = stockLedgerDetails,
                            StockMovementId = newStockMovement.Id
                        };

                        bool createdLedgerRecord = await _stockLedgerRepository.Create(stockLedger);
                        if (!createdLedgerRecord)
                        {
                            TempData["Error"] = "Error Updating Stock Position";
                        }
                        TempData["Success"] = "Deposit Confirmed successfully. Stock position Updated";
                    } else
                    {
                        TempData["Success"] = "Action completed successfully.";
                    }
                }
                else
                {
                    TempData["Error"] = $"Failed to execute {actionName} action on Deposit Request {request.TransactionRefNumber}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error executing action {Action} on deposit request {TransactionRefNumber}",
                    actionName, request.TransactionRefNumber);
                TempData["Error"] = "An error occurred while executing the action.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        //Helpers

        private static string GetCurrentStepKey(string status) => status switch
        {
            DepositStatuses.DepositStatusNames.Draft or
            DepositStatuses.DepositStatusNames.ReturnedForRevision => "CbMaker",
            DepositStatuses.DepositStatusNames.SubmittedToSupervisor => "CbApprover",
            DepositStatuses.DepositStatusNames.PendingSpecialApproval or
            DepositStatuses.DepositStatusNames.SubmittedToBank or
            DepositStatuses.DepositStatusNames.Recommended => "BOU",
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
            _ => "none",
        };

        private List<int> GetUserRoles()
            => User.FindAll("appRoleId")
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => int.TryParse(c.Value.Split(":")[1], out var roleId) ? roleId : (int?)null)
                .Where(r => r.HasValue)
                .Select(r => r!.Value)
                .ToList();

        private async Task<StatusTypeDto> GetStatusByNameAsync(string statusName)
        {
            return await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = statusName,
                EntityTypeName = StatusConstants.Entity.DepositRequest,
            });
        }

        private async Task<StatusTypeDto> GetStatusByNameExceptionalAsync(string statusName)
        {
            return await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = statusName,
                EntityTypeName = StatusConstants.Entity.DepositRequestExceptional,
            });
        }

        // Add new action for AJAX stats updates
        [HttpGet]
        public async Task<IActionResult> Stats(string? branchCode = null)
        {
            var allRequests = await _uow.Requests.FindAll();
            allRequests = allRequests.Where(x => !x.IsDeleted).ToList();

            var allStatuses = await _workflowServiceClient.FindAllStatuses();
            allStatuses = [.. allStatuses.Where(x => x.EntityType.Name == StatusConstants.Entity.DepositRequest || x.EntityType.Name == StatusConstants.Entity.DepositRequestExceptional)];

            var statusMap = allStatuses.ToDictionary(s => s.Id, s => s.Name);
            var allBouBranches = await _configServiceClient.GetAllBouBranchesAsync();

            // Get user roles
            var userRoles = User.FindAll("appRole")
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();

            bool canViewAllLocations = userRoles.Contains(SystemPolicy.Roles.RemittancesOfficer);

            // Filter by branch if provided
            if (!string.IsNullOrEmpty(branchCode))
            {
                var selectedBranch = allBouBranches.FirstOrDefault(b => b.BranchCode == branchCode);
                if (selectedBranch != null)
                {
                    allRequests = allRequests.Where(r => r.BouBranchId == selectedBranch.Id).ToList();
                }
            }
            else if (!canViewAllLocations)
            {
                // Filter by user's branches
                var userBranches = allBouBranches
                    .Where(branch => userRoles.Any(role => role.Contains(branch.BranchCode)))
                    .ToList();
                var userBranchIds = userBranches.Select(b => b.Id).ToList();
                allRequests = allRequests.Where(r => userBranchIds.Contains(r.BouBranchId)).ToList();
            }

            // Calculate stats
            var pendingCb = allRequests.Count(r =>
                statusMap.GetValueOrDefault(r.StatusTypeId) == DepositStatuses.DepositStatusNames.SubmittedToSupervisor);

            var specialApproval = allRequests.Count(r => r.RequiresSpecialApproval);

            var approvedRequests = allRequests
                .Where(r => statusMap.GetValueOrDefault(r.StatusTypeId) == DepositStatuses.DepositStatusNames.Approved)
                .ToList();

            var approvedCount = approvedRequests.Count;
            var totalProcessed = approvedRequests.Sum(r => r.TotalAmount);

            var lastMonth = DateTime.Now.AddMonths(-1);
            var lastMonthApproved = approvedRequests.Count(r => r.CreatedDate.Month == lastMonth.Month && r.CreatedDate.Year == lastMonth.Year);
            var thisMonthApproved = approvedRequests.Count(r => r.CreatedDate.Month == DateTime.Now.Month && r.CreatedDate.Year == DateTime.Now.Year);
            var approvedCountDiff = thisMonthApproved - lastMonthApproved;

            return Json(new
            {
                pendingCbCount = pendingCb,
                specialApprovalCount = specialApproval,
                approvedCount = approvedCount,
                approvedCountDiff = approvedCountDiff,
                totalProcessed = totalProcessed
            });
        }
    }
}
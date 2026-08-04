using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VMSService.Core.Authorization;
using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.CbDepositRequest;
using VMSService.Core.Models.CbDepositRequests;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.DenominationTypes;
using VMSService.Core.Repositories.DepositProcessingFees;
using VMSService.Core.Repositories.DepositUnitOfWorks;
using VMSService.Core.Repositories.FeeTypes;
using VMSService.Core.Repositories.MinDepositStandards;
using VMSService.Core.Repositories.PublicHolidays;
using VMSService.Core.Repositories.SubCategories;
using VMSService.Core.Services.Identity;
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
        IDenominationTypeRepository _denominationTypeRepo,
        IDepositProcessingFeeRepository _depositProcessingFeeRepo,
        IMinDepositStandardRepository _minDepositStandardRepo,
        IFeeTypeRepository _feeTypeRepo,
        ISubCategoryRepository _subCategoryRepo,
        IIdentityService _identityService,
        IAttachmentRepository _attachmentRepository,
        IPublicHolidayRepository _publicHolidayRepository,
        IWebHostEnvironment _environment) : BaseWithdrawController(authorizationService)
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
                    RtgsCode = string.IsNullOrEmpty(r.RtgsCode) ? "�" : r.RtgsCode,
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

            StatusTypeDto approvedStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = StatusConstants.AgentDetail.Approved,
                EntityTypeName = StatusConstants.Entity.AgentDetail,
            });

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
                    Notes = denoms.Where(d => d.DenominationType?.Name?.Equals("Note") ?? false),
                    Coins = denoms.Where(d => d.DenominationType?.Name?.Equals("Coin") ?? false),
                    SubCategories = (await _subCategoryRepo.FindAll())
                        .Where(s => !s.IsDeleted && (s.Category?.Name == StockConstants.StoreTypes.Paids))
                },
                PublicHolidayDates = await GetPublicHolidayDatesAsync(),
            };

            return View(vm);
        }

        // Feeds the client-side same-day/exceptional grace-window check (collection-date-rules.js).
        private async Task<List<PublicHolidayDateViewModel>> GetPublicHolidayDatesAsync()
        {
            var holidays = await _publicHolidayRepository.FindByActive(true);
            return holidays.Select(h => new PublicHolidayDateViewModel
            {
                Date = h.Date.ToString("yyyy-MM-dd"),
                Name = h.Name,
            }).ToList();
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

        // NEW: Calculate Processing Fees (AJAX)
        [HttpPost]
        public async Task<IActionResult> CalculateProcessingFees([FromBody] CalculateFeesRequest request)
        {
            try
            {
                var denominationTypes = await _denominationTypeRepo.FindAll();
                var noteTypeId = denominationTypes.FirstOrDefault(x => x.Name == "Note")?.Id ?? 0;
                var coinTypeId = denominationTypes.FirstOrDefault(x => x.Name == "Coin")?.Id ?? 0;

                var processingFees = await _depositProcessingFeeRepo.FindAll();

                decimal notesFee = 0;
                decimal coinsFee = 0;

                // Calculate Notes Processing Fee
                if (request.NotesTotal > 0)
                {
                    const decimal progressiveThreshold = 1_500_000_000m;
                    const decimal baseCharge = 1_500_000m;
                    const decimal progressiveRate = 0.0005m; // 0.05%

                    if (request.NotesTotal > progressiveThreshold)
                    {
                        // Progressive calculation: base charge + 0.05% of amount exceeding threshold
                        decimal excessAmount = request.NotesTotal - progressiveThreshold;
                        notesFee = baseCharge + (excessAmount * progressiveRate);
                    }
                    else
                    {
                        // Use standard fee range lookup for amounts <= 1,500,000,000
                        var noteFeeRange = processingFees
                            .FirstOrDefault(f => f.DenominationTypeId == noteTypeId
                                && request.NotesTotal >= f.StartValue
                                && request.NotesTotal <= f.EndValue);

                        if (noteFeeRange != null)
                        {
                            notesFee = noteFeeRange.Charge;
                        }
                    }
                }

                // Calculate Coins Processing Fee
                if (request.CoinsTotal > 0)
                {
                    var coinFeeRange = processingFees
                        .FirstOrDefault(f => f.DenominationTypeId == coinTypeId
                            && request.CoinsTotal >= f.StartValue
                            && request.CoinsTotal <= f.EndValue);

                    if (coinFeeRange != null)
                    {
                        coinsFee = coinFeeRange.Charge;
                    }
                }

                return Json(new
                {
                    notesFee = notesFee,
                    coinsFee = coinsFee,
                    totalProcessingFee = notesFee + coinsFee
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating processing fees");
                return BadRequest(new { error = "Failed to calculate processing fees" });
            }
        }

        // NEW: Check Minimum Deposit Standards (AJAX)
        [HttpPost]
        public async Task<IActionResult> CheckMinimumDepositStandards([FromBody] CheckMinimumStandardsRequest request)
        {
            try
            {
                //if (request.BouBranchId == 0 || request.DenominationId == 0)
                //{
                //    return Json(new { penaltyFee = 0m, violations = new List<object>() });
                //}

                var minStandards = await _minDepositStandardRepo.FindAll();
                var violations = new List<object>();
                decimal totalPenalty = 0;

                if (request == null)
                {
                    return Json(new
                    {
                        penaltyFee = totalPenalty,
                        violations = violations
                    });
                }

                foreach (var denom in request.Denominations)
                {
                    var standard = minStandards.FirstOrDefault(s =>
                        s.BranchId == request.BouBranchId
                        && s.DenominationId == denom.DenominationId
                        && !s.IsDeleted);

                    if (standard != null && denom.Bundles < standard.MinimumBundles)
                    {
                        var shortage = standard.MinimumBundles - denom.Bundles;
                        decimal penalty = 100000;

                        violations.Add(new
                        {
                            denominationId = denom.DenominationId,
                            denominationValue = denom.DenominationValue,
                            required = standard.MinimumBundles,
                            actual = denom.Bundles,
                            shortage = shortage,
                            penalty = penalty
                        });

                        totalPenalty += penalty;
                    }
                }

                return Json(new
                {
                    penaltyFee = totalPenalty,
                    violations = violations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking minimum deposit standards");
                return BadRequest(new { error = "Failed to check minimum deposit standards" });
            }
        }

        // Save as Draft
        [HttpPost]
        public async Task<IActionResult> SaveDraft([FromBody] SaveDepositRequestViewModel vm, CancellationToken ct)
        {
            if (vm == null)
            {
                return BadRequest(new { errors = new[] { "Invalid request data." } });
            }
            try
            {
                var userId = GetUserId();
                var request = await GetOrCreateRequestAsync(vm, userId, ct);

                // Ensure draft status
                if (request.StatusTypeId == 0 && request.RequiresSpecialApproval)
                {
                    StatusTypeDto status = await GetStatusByNameExceptionalAsync(DepositStatuses.DepositStatusNames.Draft);
                    request.StatusTypeId = status.Id;
                }
                else if (request.StatusTypeId == 0 && !request.RequiresSpecialApproval)
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

                // Calculate and save fees
                await CalculateAndSaveFeesAsync(request.Id, vm, userId, ct);

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
            StatusTypeDto completedStatus = await GetStatusByNameAsync(DepositStatuses.DepositStatusNames.Completed);

            if (request.RequiresSpecialApproval)
            {
                approvedStatus = await GetStatusByNameExceptionalAsync(DepositStatuses.DepositStatusNames.Approved);
                completedStatus = await GetStatusByNameExceptionalAsync(DepositStatuses.DepositStatusNames.Completed);
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
            DenominationType? noteType = (await _denominationTypeRepo.FindAll())
                .FirstOrDefault(x => x.Name == StockConstants.DenominationTypes.Note);
            int noteTypeId = noteType?.Id ?? 0;
            DenominationType? coinType = (await _denominationTypeRepo.FindAll())
                .FirstOrDefault(x => x.Name == StockConstants.DenominationTypes.Coin);
            int coinTypeId = coinType?.Id ?? 0;

            // Comments/Status History
            var statusHistoryList = _mapper.Map<IEnumerable<DepositRequestStatusHistoryViewModel>>(request.DepositRequestStatusHistory);

            foreach (var history in statusHistoryList)
            {
                var status = await _workflowServiceClient.FindStatusById(history.NewStatusId);
                history.NewStatusName = status.Name;
                var oldStatus = await _workflowServiceClient.FindStatusById(history.OldStatusId);
                history.OldStatusName = oldStatus.Name;

                // Load attachments for this status history entry
                if (!string.IsNullOrEmpty(history.AttachmentIds))
                {
                    var attachmentIds = history.AttachmentIds.Split(',')
                        .Where(id => int.TryParse(id.Trim(), out _))
                        .Select(id => int.Parse(id.Trim()))
                        .ToList();

                    var attachments = new List<AttachmentViewModel>();
                    foreach (var attachmentId in attachmentIds)
                    {
                        var attachment = await _attachmentRepository.FindById(attachmentId);
                        if (attachment != null && !attachment.IsDeleted)
                        {
                            attachments.Add(new AttachmentViewModel
                            {
                                Id = attachment.Id,
                                FileName = attachment.FileName,
                                FilePath = attachment.FilePath,
                                FileType = attachment.FileType,
                                FileSize = attachment.FileSize,
                                UploadedDate = attachment.CreatedDate
                            });
                        }
                    }
                    history.Attachments = attachments;
                }
            }

            // Load all attachments for this deposit request
            var allAttachments = await _attachmentRepository.FindByEntityId(id.ToString());
            var depositAttachments = allAttachments
                .Where(a => !a.IsDeleted && 
                           (a.EntityType == StatusConstants.Entity.DepositRequest || 
                            a.EntityType == StatusConstants.Entity.DepositRequestExceptional))
                .Select(a => new AttachmentViewModel
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    FileType = a.FileType,
                    FileSize = a.FileSize,
                    UploadedDate = a.CreatedDate
                })
                .ToList();

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

            StatusTypeDto draftStatus = await GetStatusByNameAsync(DepositStatuses.DepositStatusNames.Draft);
            StatusTypeDto returnedStatus = await GetStatusByNameAsync(DepositStatuses.DepositStatusNames.ReturnedForRevision);
            bool canEdit = (request.StatusTypeId == draftStatus.Id || request.StatusTypeId == returnedStatus.Id)
                && userRoles.Contains(SystemPolicy.Roles.CbMaker);

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
                BouBranchId = bouBranch.Id,
                FitNotes = cbDenoms
                    .Where(d => d.DenominationTypeId == noteTypeId && d.SubCategoryId == 1)
                    .Select(d => new DepositDenominationLineViewModel
                    {
                        DenominationLabel = $"UGX {d.DenominationValue:N0}",
                        Bundles = d.Bundles,
                        Amount = d.Amount,
                        SubCategoryId = d.SubCategoryId,
                        SubCategoryName = "Fit",
                        DenominationId = d.DenominationId,
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
                        DenominationId = d.DenominationId,
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
                        DenominationId = d.DenominationId,
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
                        DenominationId = d.DenominationId,
                    }),
                TotalNotesAmount = request.TotalNotesAmount,
                TotalCoinsAmount = request.TotalCoinsAmount,
                TotalAmount = request.TotalAmount,
                Steps = DepositStepViewModelFactory.Build(currentStepKey,
                    (request.StatusTypeId == approvedStatus.Id || request.StatusTypeId == completedStatus.Id),
                    request.RequiresSpecialApproval, User),
                StatusHistory = statusHistoryList,
                AvailableTransitions = availableTransitions,
                RequiresSpecialApproval = request.RequiresSpecialApproval,
                SpecialApprovalReason = request.SpecialApprovalReason,
                ProcessingFee = processingFee,
                MinimumDepositPenaltyFee = penaltyFee,
                TotalFees = processingFee + penaltyFee,
                HasPenaltyViolations = penaltyFee > 0,
                CanEdit = canEdit,
                AllAttachments = depositAttachments
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

        // GET: CbDepositRequests/Edit/5
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            try
            {
                var request = await _uow.Requests.GetWithDetailsAsync(id, ct);
                if (request == null)
                {
                    TempData["Error"] = "Deposit request not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if user owns this request
                var cbId = GetCbIdFromClaims();
                if (request.CbId != cbId)
                {
                    TempData["Error"] = "You do not have permission to edit this request.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if request is in editable status (Draft or Returned)
                var currentStatus = await _workflowServiceClient.FindStatusById(request.StatusTypeId);
                var editableStatuses = new[]
                {
            DepositStatuses.DepositStatusNames.Draft,
            DepositStatuses.DepositStatusNames.ReturnedForRevision
        };

                if (!editableStatuses.Contains(currentStatus.Name))
                {
                    TempData["Error"] = $"Cannot edit request in '{currentStatus.Name}' status.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Get agents
                var agents = await _agentRepo.FindAll();
                var denoms = await _denomRepo.FindAll();
                var bouBranches = await _configServiceClient.GetAllBouBranchesAsync();
                var cb = await _configServiceClient.GetCbIdAsync(cbId);

                StatusTypeDto approvedStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    StatusName = StatusConstants.AgentDetail.Approved,
                    EntityTypeName = StatusConstants.Entity.AgentDetail,
                });

                // Get existing agent IDs for this request
                var existingAgentIds = request.DepositRequestAgents
                    .Where(a => a.EnteredBy == DepositStatuses.DepositEnteredBy.Cb)
                    .Select(a => a.AgentId)
                    .ToList();

                // Get existing denominations
                var existingDenominations = request.DepositRequestDenominations
                    .Where(d => d.Origin == DepositStatuses.DepositOrigin.Cb)
                    .ToList();

                var vm = new EditDepositViewModel
                {
                    RequestId = request.Id,
                    Steps = DepositStepViewModelFactory.Build("CbMaker", false, request.RequiresSpecialApproval, User),
                    Branches = new FromToSelectionViewModel
                    {
                        BouBranches = bouBranches,
                        Cb = cb,
                        SelectedBouBranchId = request.BouBranchId,
                        IsEdit = true,
                        CollectionDate = request.DepositDate,
                        CreatedDate = request.CreatedDate
                    },
                    Agents = new AgentSelectionViewModel
                    {
                        Agents = agents
                            .Where(a => a.IsActive && !a.IsDeleted && a.CommercialBankId == cbId && a.CurrentStatusId == approvedStatus.Id)
                            .Select(a => _mapper.Map<AgentDetailViewModel>(a))
                            .ToList(),
                        SelectedAgentIds = existingAgentIds
                    },
                    // In Edit action (GET) - around line 515
                    Denoms = new DepositDenomTableViewModel
                    {
                        Notes = denoms.Where(d => d.DenominationType?.Name?.Equals("Note") ?? false),
                        Coins = denoms.Where(d => d.DenominationType?.Name?.Equals("Coin") ?? false),
                        ExistingDenominations = existingDenominations.Select(d => new ExistingDenominationViewModel
                        {
                            DenominationId = d.DenominationId,
                            DenominationValue = d.DenominationValue,
                            DenominationTypeId = d.DenominationTypeId,
                            Bundles = d.Bundles,
                            Boxes = d.Boxes,
                            Amount = d.Amount,
                            SubCategoryId = d.SubCategoryId
                        }).ToList(),
                        SubCategories = (await _subCategoryRepo.FindAll())
                            .Where(s => !s.IsDeleted && (s.Category?.Name == StockConstants.StoreTypes.Paids))
                    },
                    DepositDate = request.DepositDate,
                    RtgsDate = request.RtgsDate,
                    RtgsCode = request.RtgsCode,
                    RequiresSpecialApproval = request.RequiresSpecialApproval,
                    SpecialApprovalReason = request.SpecialApprovalReason,
                    TransactionRefNumber = request.TransactionRefNumber,
                    PublicHolidayDates = await GetPublicHolidayDatesAsync(),
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading deposit request for edit with id {RequestId}", id);
                TempData["Error"] = "An error occurred while loading the deposit request. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CbDepositRequests/Edit/5
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [FromBody] SaveDepositRequestViewModel vm, CancellationToken ct)
        {
            if (vm == null || id != vm.Id)
            {
                return BadRequest(new { errors = new[] { "Invalid request data." } });
            }

            try
            {
                var existingRequest = await _uow.Requests.GetWithDetailsAsync(id, ct);
                if (existingRequest == null)
                {
                    return NotFound(new { errors = new[] { "Deposit request not found." } });
                }

                // Check if user owns this request
                var cbId = GetCbIdFromClaims();
                if (existingRequest.CbId != cbId)
                {
                    return Forbid();
                }

                // Check if request is in editable status
                var currentStatus = await _workflowServiceClient.FindStatusById(existingRequest.StatusTypeId);
                var editableStatuses = new[]
                {
            DepositStatuses.DepositStatusNames.Draft,
            DepositStatuses.DepositStatusNames.ReturnedForRevision
        };

                if (!editableStatuses.Contains(currentStatus.Name))
                {
                    return BadRequest(new { errors = new[] { $"Cannot edit request in '{currentStatus.Name}' status." } });
                }

                var userId = GetUserId();

                // Update request details
                existingRequest.DepositDate = vm.DepositDate;
                existingRequest.RtgsDate = vm.RtgsDate;
                existingRequest.RtgsCode = vm.RtgsCode;
                existingRequest.BouBranchId = vm.BouBranchId;
                existingRequest.RequiresSpecialApproval = vm.RequiresSpecialApproval;
                existingRequest.SpecialApprovalReason = vm.SpecialApprovalReason;

                // Recalculate totals
                // In GetOrCreateRequestAsync - around line 732
                DenominationType? noteType = (await _denominationTypeRepo.FindAll())
                    .FirstOrDefault(x => x.Name == StockConstants.DenominationTypes.Note);
                int noteTypeId = noteType?.Id ?? 0;
                DenominationType? coinType = (await _denominationTypeRepo.FindAll())
                    .FirstOrDefault(x => x.Name == StockConstants.DenominationTypes.Coin);
                int coinTypeId = coinType?.Id ?? 0;

                existingRequest.TotalNotesAmount = vm.Denominations
                    .Where(d => d.DenominationTypeId == noteTypeId)
                    .Sum(d => d.Amount);
                existingRequest.TotalCoinsAmount = vm.Denominations
                    .Where(d => d.DenominationTypeId == coinTypeId)
                    .Sum(d => d.Amount);
                existingRequest.TotalAmount = existingRequest.TotalNotesAmount + existingRequest.TotalCoinsAmount;

                await _uow.Requests.Update(existingRequest);
                await _uow.SaveAsync(ct);

                // Update denominations
                await SaveDenominationsAsync(existingRequest.Id, vm.Denominations, userId, ct);

                // Update agents
                await SaveAgentsAsync(existingRequest.Id, vm.AgentIds, userId, ct);

                // Recalculate and update fees
                await CalculateAndSaveFeesAsync(existingRequest.Id, vm, userId, ct);

                TempData["Success"] = "Deposit request updated successfully!";
                return Ok($"/CbDepositRequests/Details/{existingRequest.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating deposit request with id {RequestId}", id);
                return BadRequest(new { errors = new[] { "An unexpected error occurred. Please try again." } });
            }
        }

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

                // Get the transition to check if attachments are required
                var requestData = new GetTransitionByKeyRequestDto
                {
                    EntityTypeName = request.RequiresSpecialApproval 
                        ? StatusConstants.Entity.DepositRequestExceptional 
                        : StatusConstants.Entity.DepositRequest,
                    FromStatusId = request.StatusTypeId,
                    ActionName = actionName,
                };

                var transition = await _workflowServiceClient.FindByTransitionKey(requestData);
                
                // Handle file uploads if attachments are required
                List<long>? uploadedAttachmentIds = null;
                
                if (transition != null && transition.RequiresAttachments && transition.AttachmentRequirements.Any())
                {
                    uploadedAttachmentIds = await HandleAttachmentUploads(
                        transition.AttachmentRequirements.ToList(), 
                        transition.Id,
                        id, 
                        request.RequiresSpecialApproval 
                            ? StatusConstants.Entity.DepositRequestExceptional 
                            : StatusConstants.Entity.DepositRequest,
                        username);

                    // Validate required attachments
                    var requiredAttachments = transition.AttachmentRequirements.Where(r => r.IsRequired).ToList();
                    if (requiredAttachments.Any())
                    {
                        var uploadedRequirementIds = uploadedAttachmentIds?
                            .Select(aid => 
                            {
                                // Track which requirement each attachment belongs to
                                return transition.AttachmentRequirements
                                    .FirstOrDefault(r => Request.Form.Files[$"attachments_{r.Id}"]?.Length > 0)?.Id ?? 0;
                            })
                            .Where(id => id > 0)
                            .ToList();

                        var missingRequirements = requiredAttachments
                            .Where(r => !uploadedRequirementIds.Contains(r.Id))
                            .ToList();

                        if (missingRequirements.Any())
                        {
                            TempData["Error"] = $"Missing required attachments: {string.Join(", ", missingRequirements.Select(r => r.AttachmentName))}";
                            return RedirectToAction(nameof(Details), new { id });
                        }
                    }
                }

                bool isExecuted = false;

                if (request.RequiresSpecialApproval)
                {
                    isExecuted = await _workflowService.ExecuteTransition(
                        StatusConstants.Entity.DepositRequestExceptional, 
                        id, 
                        request.StatusTypeId,
                        actionName, 
                        username, 
                        userRoles, 
                        remarks, 
                        ct,
                        uploadedAttachmentIds);
                }
                else
                {
                    isExecuted = await _workflowService.ExecuteTransition(
                        StatusConstants.Entity.DepositRequest, 
                        id, 
                        request.StatusTypeId,
                        actionName, 
                        username, 
                        userRoles, 
                        remarks, 
                        ct,
                        uploadedAttachmentIds);
                }

                if (isExecuted)
                {
                    var attachmentMessage = uploadedAttachmentIds?.Any() == true 
                        ? $" {uploadedAttachmentIds.Count} attachment(s) uploaded." 
                        : "";
                    TempData["Success"] = $"Action completed successfully.{attachmentMessage}";
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

        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            try
            {
                var attachment = await _attachmentRepository.FindById(id);
                if (attachment == null || attachment.IsDeleted)
                {
                    TempData["Error"] = "Attachment not found.";
                    return NotFound();
                }

                var filePath = Path.Combine(_environment.WebRootPath, attachment.FilePath);
                
                if (!System.IO.File.Exists(filePath))
                {
                    TempData["Error"] = "File not found on server.";
                    return NotFound();
                }

                var memory = new MemoryStream();
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                return File(memory, attachment.FileType, attachment.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading attachment {AttachmentId}", id);
                TempData["Error"] = "An error occurred while downloading the file.";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task<List<long>> HandleAttachmentUploads(
            List<TransitionAttachmentRequirementDto> requirements,
            int workflowTransitionId,
            Guid entityId,
            string entityType,
            string username)
        {
            var uploadedAttachmentIds = new List<long>();

            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "deposits", entityId.ToString());
            Directory.CreateDirectory(uploadsPath);

            foreach (var requirement in requirements)
            {
                var fileInputName = $"attachments_{requirement.Id}";
                var file = Request.Form.Files[fileInputName];

                if (file != null && file.Length > 0)
                {
                    try
                    {
                        // Validate file format
                        var fileExtension = Path.GetExtension(file.FileName).TrimStart('.').ToLower();
                        var allowedFormats = requirement.AllowedFormats.Split(',')
                            .Select(f => f.Trim().ToLower())
                            .ToList();

                        // Map common format names
                        var formatMap = new Dictionary<string, List<string>>
                        {
                            { "excel", new List<string> { "xls", "xlsx" } },
                            { "word", new List<string> { "doc", "docx" } },
                            { "docx", new List<string> { "doc", "docx" } },
                            { "jpg", new List<string> { "jpg", "jpeg" } },
                            { "jpeg", new List<string> { "jpg", "jpeg" } }
                        };

                        bool isValidFormat = false;
                        foreach (var format in allowedFormats)
                        {
                            if (formatMap.ContainsKey(format))
                            {
                                if (formatMap[format].Contains(fileExtension))
                                {
                                    isValidFormat = true;
                                    break;
                                }
                            }
                            else if (format == fileExtension)
                            {
                                isValidFormat = true;
                                break;
                            }
                        }

                        if (!isValidFormat)
                        {
                            _logger.LogWarning("Invalid file format {Extension} for requirement {RequirementId}", 
                                fileExtension, requirement.Id);
                            continue;
                        }

                        // Generate unique filename
                        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                        var uniqueFileName = $"{requirement.Id}_{timestamp}_{Path.GetFileNameWithoutExtension(file.FileName)}.{fileExtension}";
                        var filePath = Path.Combine(uploadsPath, uniqueFileName);

                        // Save file
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        // Create attachment record
                        var attachment = new Attachment
                        {
                            FileName = file.FileName,
                            FilePath = Path.Combine("uploads", "deposits", entityId.ToString(), uniqueFileName),
                            FileType = file.ContentType,
                            FileSize = file.Length,
                            WorkflowTransitionId = workflowTransitionId,
                            TransitionAttachmentRequirementId = requirement.Id,
                            EntityId = entityId.ToString(),
                            EntityType = entityType,
                            CreatedBy = username,
                            CreatedDate = DateTime.Now,
                            ModifiedBy = username,
                            ModifiedDate = DateTime.Now,
                            IsDeleted = false
                        };

                        await _attachmentRepository.Create(attachment);
                        uploadedAttachmentIds.Add(attachment.Id);

                        _logger.LogInformation(
                            "Attachment uploaded successfully. File: {FileName}, Size: {Size}, AttachmentId: {AttachmentId}",
                            file.FileName, file.Length, attachment.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading attachment for requirement {RequirementId}", requirement.Id);
                    }
                }
            }

            return uploadedAttachmentIds;
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

                // In GetOrCreateRequestAsync - around line 732
                DenominationType? noteType = (await _denominationTypeRepo.FindAll())
                    .FirstOrDefault(x => x.Name == StockConstants.DenominationTypes.Note);
                int notTypeId = noteType?.Id ?? 0;
                DenominationType? coinType = (await _denominationTypeRepo.FindAll())
                    .FirstOrDefault(x => x.Name == StockConstants.DenominationTypes.Coin);
                int coinTypeId = coinType?.Id ?? 0;

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
            => User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

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

        private async Task CalculateAndSaveFeesAsync(
            Guid requestId,
            SaveDepositRequestViewModel vm,
            string userId,
            CancellationToken ct)
        {
            try
            {
                // Get denomination types
                var denominationTypes = await _denominationTypeRepo.FindAll();
                var noteTypeId = denominationTypes.FirstOrDefault(x => x.Name == "Note")?.Id ?? 0;
                var coinTypeId = denominationTypes.FirstOrDefault(x => x.Name == "Coin")?.Id ?? 0;

                var naTypeId = (await _subCategoryRepo.FindAll()).FirstOrDefault(s => s.Name == StockConstants.SubCategories.NA && !s.IsDeleted)?.Id ?? 0;

                // Calculate totals
                var notesTotal = vm.Denominations
                    .Where(d => d.DenominationTypeId == noteTypeId && d.SubCategoryId == naTypeId)
                    .Sum(d => d.Amount);

                var coinsTotal = vm.Denominations
                    .Where(d => d.DenominationTypeId == coinTypeId)
                    .Sum(d => d.Amount);

                // 1. Calculate Processing Fees
                decimal processingFee = await CalculateProcessingFeeAsync(notesTotal, coinsTotal, noteTypeId, coinTypeId);

                // 2. Calculate Minimum Deposit Penalty Fees
                decimal penaltyFee = await CalculatePenaltyFeeAsync(vm.BouBranchId, vm.Denominations);

                // Get fee type IDs
                var processingFeeType = await _feeTypeRepo.FindByName(SystemPolicy.FeeTypes.ProcessingFees);
                var penaltyFeeType = await _feeTypeRepo.FindByName(SystemPolicy.FeeTypes.MinDepositFees);

                if (processingFeeType == null || penaltyFeeType == null)
                {
                    _logger.LogError("Fee types not found in database. Ensure '{ProcessingFees}' and '{MinDepositFees}' exist in FeeTypes table.",
                        SystemPolicy.FeeTypes.ProcessingFees, SystemPolicy.FeeTypes.MinDepositFees);
                    return;
                }

                // Delete existing fees for this deposit request
                var existingFees = await _uow.DepositFees.GetByDepositRequestId(requestId);
                foreach (var existingFee in existingFees)
                {
                    existingFee.IsDeleted = true;
                    await _uow.DepositFees.Update(existingFee);
                }

                // Save Processing Fee
                if (processingFee > 0)
                {
                    var processingFeeEntity = new DepositFee
                    {
                        Name = SystemPolicy.FeeTypes.ProcessingFees,
                        Description = $"Processing fee for notes (UGX {notesTotal:N0}) and coins (UGX {coinsTotal:N0})",
                        FeeTypeId = processingFeeType.Id,
                        DepositRequestId = requestId,
                        Amount = processingFee,
                        IsWaived = false
                    };

                    processingFeeEntity.CreatedBy = userId;
                    processingFeeEntity.ModifiedBy = userId;
                    await _uow.DepositFees.Create(processingFeeEntity);
                }

                // Save Penalty Fee
                if (penaltyFee > 0)
                {
                    var penaltyFeeEntity = new DepositFee
                    {
                        Name = SystemPolicy.FeeTypes.MinDepositFees,
                        Description = "Penalty fee for not meeting minimum deposit standards",
                        FeeTypeId = penaltyFeeType.Id,
                        DepositRequestId = requestId,
                        Amount = penaltyFee,
                        IsWaived = false
                    };

                    penaltyFeeEntity.CreatedBy = userId;
                    penaltyFeeEntity.ModifiedBy = userId;
                    await _uow.DepositFees.Create(penaltyFeeEntity);
                }

                await _uow.SaveAsync(ct);

                _logger.LogInformation("Fees calculated and saved for deposit request {RequestId}: Processing={ProcessingFee}, Penalty={PenaltyFee}",
                    requestId, processingFee, penaltyFee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating and saving fees for deposit request {RequestId}", requestId);
                throw;
            }
        }

        private async Task<decimal> CalculateProcessingFeeAsync(decimal notesTotal, decimal coinsTotal, int noteTypeId, int coinTypeId)
        {
            var processingFees = await _depositProcessingFeeRepo.FindAll();
            decimal notesFee = 0;
            decimal coinsFee = 0;

            // Calculate Notes Processing Fee
            if (notesTotal > 0)
            {
                const decimal progressiveThreshold = 1_500_000_000m;
                const decimal baseCharge = 1_500_000m;
                const decimal progressiveRate = 0.0005m; // 0.05%

                if (notesTotal > progressiveThreshold)
                {
                    decimal excessAmount = notesTotal - progressiveThreshold;
                    notesFee = baseCharge + (excessAmount * progressiveRate);
                }
                else
                {
                    var noteFeeRange = processingFees
                        .FirstOrDefault(f => f.DenominationTypeId == noteTypeId
                            && notesTotal >= f.StartValue
                            && notesTotal <= f.EndValue);

                    if (noteFeeRange != null)
                    {
                        notesFee = noteFeeRange.Charge;
                    }
                }
            }

            // Calculate Coins Processing Fee
            if (coinsTotal > 0)
            {
                var coinFeeRange = processingFees
                    .FirstOrDefault(f => f.DenominationTypeId == coinTypeId
                        && coinsTotal >= f.StartValue
                        && coinsTotal <= f.EndValue);

                if (coinFeeRange != null)
                {
                    coinsFee = coinFeeRange.Charge;
                }
            }

            return notesFee + coinsFee;
        }

        private async Task<decimal> CalculatePenaltyFeeAsync(int bouBranchId, List<SaveDepositDenominationLine> denominations)
        {
            var minStandards = await _minDepositStandardRepo.FindAll();
            decimal totalPenalty = 0;

            foreach (var denom in denominations.Where(d => d.Bundles.HasValue))
            {
                var standard = minStandards.FirstOrDefault(s =>
                    s.BranchId == bouBranchId
                    && s.DenominationId == denom.DenominationId
                    && !s.IsDeleted);

                if (standard != null && denom.Bundles < standard.MinimumBundles)
                {
                    decimal penalty = 100000; // Fixed penalty per violation
                    totalPenalty += penalty;
                }
            }

            return totalPenalty;
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
                    .Select(id => id!.Value)
                    .ToList();
        }

        private int GetCbIdFromClaims()
            => int.TryParse(User.FindFirst("cbId")?.Value, out var id) ? id : 0;

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

        // GET: CbDepositRequests/PrintRequest/5
        public async Task<IActionResult> PrintRequest(Guid id, CancellationToken ct)
        {
            try
            {
                var request = await _uow.Requests.GetWithDetailsAsync(id, ct);
                if (request == null)
                {
                    TempData["Error"] = "Deposit request not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Get agent details
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

                // Get denominations
                var cbDenoms = request.DepositRequestDenominations
                    .Where(d => d.Origin == DepositStatuses.DepositOrigin.Cb);

                // Get branch details
                var cb = await _configServiceClient.GetCbIdAsync(request.CbId);
                var bouBranch = await _configServiceClient.GetBouBranchByIdAsync(request.BouBranchId);

                // Get denomination types
                DenominationType? noteType = (await _denominationTypeRepo.FindAll())
                    .FirstOrDefault(x => x.Name == StockConstants.DenominationTypes.Note);
                int noteTypeId = noteType?.Id ?? 0;
                DenominationType? coinType = (await _denominationTypeRepo.FindAll())
                    .FirstOrDefault(x => x.Name == StockConstants.DenominationTypes.Coin);
                int coinTypeId = coinType?.Id ?? 0;

                // Get fees
                var depositFees = await _uow.DepositFees.GetByDepositRequestId(id);
                var processingFeeType = await _feeTypeRepo.FindByName(SystemPolicy.FeeTypes.ProcessingFees);

                decimal processingFee = 0;
                if (processingFeeType != null)
                {
                    var processingFeeEntity = depositFees.FirstOrDefault(f => f.FeeTypeId == processingFeeType.Id);
                    if (processingFeeEntity != null)
                    {
                        processingFee = processingFeeEntity.Amount;
                    }
                }

                // Get panelists from status history
                string panelAName = string.Empty;
                string panelBName = string.Empty;

                var statusHistory = request.DepositRequestStatusHistory;

                // Get Panel A (Recommended status)
                var recommendedStatus = await GetStatusByNameAsync(DepositStatuses.DepositStatusNames.Recommended);
                var panelAHistory = statusHistory
                    .FirstOrDefault(h => h.NewStatusId == recommendedStatus.Id);

                if (panelAHistory != null && !string.IsNullOrEmpty(panelAHistory.ChangedBy))
                {
                    var panelAUser = await _identityService.GetUserByUsername(panelAHistory.ChangedBy);
                    if (panelAUser != null)
                    {
                        panelAName = $"{panelAUser.FirstName} {panelAUser.MiddleName} {panelAUser.LastName}".Trim();
                        // Remove extra spaces if MiddleName is empty
                        panelAName = System.Text.RegularExpressions.Regex.Replace(panelAName, @"\s+", " ");
                    }
                }

                // Get Panel B (Approved status)
                var approvedStatus = await GetStatusByNameAsync(DepositStatuses.DepositStatusNames.Approved);
                if (request.RequiresSpecialApproval)
                {
                    approvedStatus = await GetStatusByNameExceptionalAsync(DepositStatuses.DepositStatusNames.Approved);
                }

                var panelBHistory = statusHistory
                    .FirstOrDefault(h => h.NewStatusId == approvedStatus.Id);

                if (panelBHistory != null && !string.IsNullOrEmpty(panelBHistory.ChangedBy))
                {
                    var panelBUser = await _identityService.GetUserByUsername(panelBHistory.ChangedBy);
                    if (panelBUser != null)
                    {
                        panelBName = $"{panelBUser.FirstName} {panelBUser.MiddleName} {panelBUser.LastName}".Trim();
                        // Remove extra spaces if MiddleName is empty
                        panelBName = System.Text.RegularExpressions.Regex.Replace(panelBName, @"\s+", " ");
                    }
                }

                var vm = new DepositRequestDetailsViewModel
                {
                    Id = request.Id,
                    ReferenceNumber = request.TransactionRefNumber,
                    DepositDate = request.DepositDate,
                    DepositAgents = agentDetails,
                    CbName = cb.Name,
                    BouBranchName = bouBranch.Name,
                    BouBranchCode = bouBranch.BranchCode,
                    FitNotes = cbDenoms
                        .Where(d => d.DenominationTypeId == noteTypeId && d.SubCategoryId == 1)
                        .Select(d => new DepositDenominationLineViewModel
                        {
                            DenominationLabel = $"UGX {d.DenominationValue:N0}",
                            Bundles = d.Bundles,
                            Amount = d.Amount,
                            SubCategoryName = "FIT"
                        }),
                    UnfitNotes = cbDenoms
                        .Where(d => d.DenominationTypeId == noteTypeId && d.SubCategoryId == 2)
                        .Select(d => new DepositDenominationLineViewModel
                        {
                            DenominationLabel = $"UGX {d.DenominationValue:N0}",
                            Bundles = d.Bundles,
                            Amount = d.Amount,
                            SubCategoryName = "UNFIT"
                        }),
                    NANotes = cbDenoms
                        .Where(d => d.DenominationTypeId == noteTypeId && d.SubCategoryId == 3)
                        .Select(d => new DepositDenominationLineViewModel
                        {
                            DenominationLabel = $"UGX {d.DenominationValue:N0}",
                            Bundles = d.Bundles,
                            Amount = d.Amount,
                            SubCategoryName = "N/A"
                        }),
                    Coins = cbDenoms
                        .Where(d => d.DenominationTypeId == coinTypeId)
                        .Select(d => new DepositDenominationLineViewModel
                        {
                            DenominationLabel = $"UGX {d.DenominationValue:N0}",
                            Boxes = d.Boxes,
                            Amount = d.Amount,
                            SubCategoryName = "N/A"
                        }),
                    TotalNotesAmount = request.TotalNotesAmount,
                    TotalCoinsAmount = request.TotalCoinsAmount,
                    TotalAmount = request.TotalAmount,
                    ProcessingFee = processingFee,
                    PanelAName = panelAName,
                    PanelBName = panelBName
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading deposit request for printing with id {RequestId}", id);
                TempData["Error"] = "An error occurred while loading the deposit request. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
    }

    // Request models for new API endpoints
    public class CalculateFeesRequest
    {
        public decimal NotesTotal { get; set; }
        public decimal CoinsTotal { get; set; }
    }

    public class CheckMinimumStandardsRequest
    {
        public int BouBranchId { get; set; }
        //public int DenominationId { get; set; }
        public List<DenominationCheck> Denominations { get; set; } = new();
    }

    public class DenominationCheck
    {
        public int DenominationId { get; set; }
        public int DenominationValue { get; set; }
        public int Bundles { get; set; }
    }
}
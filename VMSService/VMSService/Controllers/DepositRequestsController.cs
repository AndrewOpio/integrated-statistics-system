using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using System.Reflection.Emit;
using System.Security.Claims;
using VMSService.Controllers;
using VMSService.Core.Authorization;
using VMSService.Core.Extensions;
using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.Bags;
using VMSService.Core.Models.CbDepositRequest;
using VMSService.Core.Models.CbDepositRequests;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Models.DepositRequests;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Models.Penalties;
using VMSService.Core.Repositories.AppliedPenalties;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.Bags;
using VMSService.Core.Repositories.BranchCategories;
using VMSService.Core.Repositories.Categories;
using VMSService.Core.Repositories.CounterPartyTypes;
using VMSService.Core.Repositories.DenominationTypes;
using VMSService.Core.Repositories.DepositUnitOfWorks;
using VMSService.Core.Repositories.FeeTypes;
using VMSService.Core.Repositories.Penalties;
using VMSService.Core.Repositories.PenaltyWaivers;
using VMSService.Core.Repositories.StockMovements;
using VMSService.Core.Repositories.Stocks;
using VMSService.Core.Services.Identity;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using VMSService.Web.Extensions;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class DepositRequestsController(
        IAuthorizationService authorizationService,
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
        IFeeTypeRepository _feeTypeRepo,
        IAttachmentRepository _attachmentRepository,
        IWebHostEnvironment _environment,
        IBagRepository _bagRepository,
        IPenaltyRepository _penaltyRepository,
        IPenaltyWaiverRepository _penaltyWaiverRepository,
        IAppliedPenaltyRepository _appliedPenaltyRepository,
        IIdentityService _identityService) : BaseWithdrawController(authorizationService)
    {
        // Penalties may only be captured while BOU is confirming the physical deposit.
        private const string PenaltyCaptureStatus = DepositStatuses.DepositStatusNames.SubmittedToBank;

        /// <summary>Workflow entity name for a request — exceptional deposits run a separate workflow.</summary>
        private static string EntityTypeNameFor(bool requiresSpecialApproval)
            => requiresSpecialApproval
                ? StatusConstants.Entity.DepositRequestExceptional
                : StatusConstants.Entity.DepositRequest;

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
                BranchDto selectBranch = userBranches.FirstOrDefault();
                List<int> userBranchIds = [ selectBranch?.Id ?? 0 ];
                branchCode = selectBranch?.BranchCode;
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

            rows = [.. rows.OrderByDescending(r => r.CollectionDate)];

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

            // ── Penalties ─────────────────────
            // Tracked separately from the FeeType-based fees above; the view presents a combined total.
            var appliedPenaltyVms = new List<AppliedPenaltyViewModel>();
            var applicablePenaltyVms = new List<PenaltyChecklistItemViewModel>();
            var canManagePenalties = false;

            if (!string.IsNullOrEmpty(request.TransactionRefNumber))
            {
                var appliedPenalties = await _appliedPenaltyRepository.FindBySourceId(request.TransactionRefNumber);
                appliedPenaltyVms = appliedPenalties.Select(a => new AppliedPenaltyViewModel
                {
                    Id = a.Id,
                    PenaltyId = a.PenaltyId,
                    PenaltyName = a.Penalty?.Name ?? string.Empty,
                    ChargeValue = a.ChargeValue,
                    Quantity = a.Quantity,
                    IsSettledOnRtgs = a.IsSettledOnRtgs,
                    RtgsCode = a.RtgsCode,
                    RtgsSettlingMessage = a.RtgsSettlingMessage,
                    AppliedBy = a.CreatedBy,
                    AppliedAt = a.CreatedDate,
                    ToBeWaived = a.ToBeWaived,
                    WaiverReason = a.WaiverReason,
                    WaivedBy = a.WaivedBy,
                    WaivedDate = a.WaivedDate,
                }).ToList();

                // The checklist only appears to someone who can actually act on the request now.
                canManagePenalties = currentStatus.Name == PenaltyCaptureStatus && availableTransitions.Count > 0;

                if (canManagePenalties)
                {
                    var appliedPenaltyIds = appliedPenalties.Select(a => a.PenaltyId).ToHashSet();
                    var applicablePenalties = await _penaltyRepository.FindApplicableFor(PenaltyConstants.AppliesTo.PreTransaction);

                    foreach (var p in applicablePenalties)
                    {
                        // A branch-scoped waiver, checked against the request's deposit date,
                        // suppresses the penalty from being selectable — it stays visible so BOU
                        // can see why it's not offered rather than it just vanishing.
                        var waiver = await _penaltyWaiverRepository.FindActiveAsync(p.Id, request.BouBranchId, request.DepositDate);
                        applicablePenaltyVms.Add(new PenaltyChecklistItemViewModel
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Description = p.Description,
                            Unit = p.Unit,
                            PenaltyValue = p.PenaltyValue,
                            PenaltyPercent = p.PenaltyPercent,
                            PercentCutOff = p.PercentCutOff,
                            PenaltyMultiplier = p.PenaltyMultiplier,
                            IsChecked = appliedPenaltyIds.Contains(p.Id),
                            IsWaived = waiver is not null,
                            WaiverEndDate = waiver?.EndDate,
                        });
                    }
                }
            }

            // Fetch bags associated with this deposit request
            var depositBags = await _bagRepository.GetBagsByDepositRequestIdAsync(id);
            var cbDisplayName = cb?.Name ?? string.Empty;
            foreach (var bag in depositBags)
                foreach (var item in bag.Items)
                    item.CbName = cbDisplayName;

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
                HasPenaltyViolations = penaltyFee > 0,
                AppliedPenalties = appliedPenaltyVms,
                ApplicablePenalties = applicablePenaltyVms,
                CanManagePenalties = canManagePenalties,
                AllAttachments = depositAttachments,
                Bags = depositBags,
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyPenalties(Guid id, List<int>? penaltyIds)
        {
            var request = await _uow.Requests.FindById(id);
            if (request is null)
            {
                TempData["Error"] = "Deposit request not found.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var currentStatus = await _workflowServiceClient.FindStatusById(request.StatusTypeId);
            if (currentStatus?.Name != PenaltyCaptureStatus)
            {
                TempData["Error"] = $"Penalties can only be applied while the request is {PenaltyCaptureStatus}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrEmpty(request.TransactionRefNumber))
            {
                TempData["Error"] = "This request does not have a transaction reference number yet.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!await CallerHasAvailableTransition(request))
            {
                TempData["Error"] = "You are not authorized to act on this request at its current status.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                var checkedPenaltyIds = (penaltyIds ?? []).ToHashSet();

                // Restrict to the Pre-Transaction catalogue so a tampered post cannot apply a
                // penalty from another category.
                var allowedPenaltyIds = (await _penaltyRepository.FindApplicableFor(PenaltyConstants.AppliesTo.PreTransaction))
                    .Select(p => p.Id)
                    .ToHashSet();
                checkedPenaltyIds.IntersectWith(allowedPenaltyIds);

                var currentlyApplied = await _appliedPenaltyRepository.FindBySourceId(request.TransactionRefNumber);

                // Human-readable summary stored on each penalty so it reads standalone in listings
                // and settlement reports, away from the deposit request itself.
                var cb = await _configServiceClient.GetCbIdAsync(request.CbId);
                var bouBranch = await _configServiceClient.GetBouBranchByIdAsync(request.BouBranchId);
                var description =
                    $"Penalty for Non Compliance on Deposit {request.TransactionRefNumber} from {cb?.Name ?? "Unknown Bank"} " +
                    $"{bouBranch?.Name ?? "Unknown Branch"} " +
                    $"of UGX {request.TotalAmount:N0} dated {request.DepositDate:dd MMM yyyy}";

                foreach (var penaltyId in checkedPenaltyIds)
                {
                    // Re-check server-side even though waived penalties aren't offered on the
                    // checklist — a waiver could have been added after the page was loaded.
                    if (await _penaltyWaiverRepository.IsWaivedAsync(penaltyId, request.BouBranchId, request.DepositDate))
                        continue;

                    await _appliedPenaltyRepository.Apply(
                        PenaltyConstants.Source.Deposit,
                        request.TransactionRefNumber,
                        penaltyId,
                        baseValue: request.TotalAmount,
                        description: description);
                }

                foreach (var applied in currentlyApplied.Where(a => !checkedPenaltyIds.Contains(a.PenaltyId)))
                {
                    await _appliedPenaltyRepository.Remove(applied.Id);
                }

                await _appliedPenaltyRepository.Save();
                TempData["Success"] = "Penalties updated.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying penalties to deposit request {Id}", id);
                TempData["Error"] = "An error occurred while updating penalties.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>Waives one specific applied penalty in place, with a mandatory reason — the charge
        /// stays on record and visible in the audit trail rather than being removed. Deliberately
        /// separate from the branch-and-date-ranged <see cref="ApplyPenalties"/>/<c>PenaltyWaiver</c>
        /// machinery: this forgives just this one applied charge, not the penalty type going forward.
        /// Gated the same way as ApplyPenalties — only while the request is at the penalty-capture
        /// status, and only for a role with an available transition on it right now.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WaivePenalty(Guid id, int appliedPenaltyId, string waiverReason)
        {
            var request = await _uow.Requests.FindById(id);
            if (request is null)
            {
                TempData["Error"] = "Deposit request not found.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var currentStatus = await _workflowServiceClient.FindStatusById(request.StatusTypeId);
            if (currentStatus?.Name != PenaltyCaptureStatus)
            {
                TempData["Error"] = $"Penalties can only be waived while the request is {PenaltyCaptureStatus}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(waiverReason))
            {
                TempData["Error"] = "A reason is required to waive a penalty.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrEmpty(request.TransactionRefNumber))
            {
                TempData["Error"] = "This request does not have a transaction reference number yet.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!await CallerHasAvailableTransition(request))
            {
                TempData["Error"] = "You are not authorized to act on this request at its current status.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                // Confirm the applied penalty actually belongs to this request before touching it —
                // appliedPenaltyId is user-supplied and otherwise nothing ties it to id/the current
                // request's own charges.
                var appliedToThisRequest = (await _appliedPenaltyRepository.FindBySourceId(request.TransactionRefNumber))
                    .Any(a => a.Id == appliedPenaltyId);

                if (!appliedToThisRequest)
                {
                    TempData["Error"] = "That penalty is not applied to this request.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                await _appliedPenaltyRepository.Waive(appliedPenaltyId, waiverReason.Trim(), User.GetUsernameOrSystem());
                await _appliedPenaltyRepository.Save();
                TempData["Success"] = "Penalty waived.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error waiving applied penalty {AppliedPenaltyId} on deposit request {Id}", appliedPenaltyId, id);
                TempData["Error"] = "An error occurred while waiving the penalty.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>The same check that gates the workflow action buttons — only someone whose role
        /// has an available transition on this request right now may apply, adjust or waive its
        /// penalties.</summary>
        private async Task<bool> CallerHasAvailableTransition(DepositRequest request)
        {
            var entityTypeName = EntityTypeNameFor(request.RequiresSpecialApproval);

            foreach (var roleId in GetUserRoles())
            {
                var roleTransitions = await _workflowService.GetAvailableTransitionsAsync(
                    entityTypeName, request.StatusTypeId, roleId);
                if (roleTransitions.Count > 0) return true;
            }

            return false;
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
                                // You'll need to track which requirement each attachment belongs to
                                // This is a simplified check - you may need to enhance this
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
                    if (actionName.Equals("Complete"))
                    {
                        var cbDenoms = request.DepositRequestDenominations
                                .Where(d => d.Origin == DepositStatuses.DepositOrigin.Cb);

                        //Create Bags
                        List<Bag> createdBags = await CreateBagsFromDenominations(request.DepositRequestDenominations, request, username);

                        bool bagsCreated = await _bagRepository.CreateBagsAsync(createdBags);

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
                        if(bagsCreated)
                        {
                            TempData["Success"] = $"Deposit Confirmed successfully. Stock position Updated. {createdBags.Count} bags created";
                        }
                    } else
                    {
                        var attachmentMessage = uploadedAttachmentIds?.Any() == true 
                            ? $" {uploadedAttachmentIds.Count} attachment(s) uploaded." 
                            : "";
                        TempData["Success"] = $"Action completed successfully.{attachmentMessage}";
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
            var uploadsPath = Path.Combine(FileStorageConstants.UploadsRoot, "deposits", entityId.ToString());
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

        // GET: DepositRequests/PrintRequest/5
        //
        // Mirrors ticket 23's treatment of the withdrawal print page (WithdrawalPrintController):
        // the printed deposit slip renders identically regardless of who's viewing it, so it lives
        // in one place that both the CB and BOU Details pages link to rather than being duplicated.
        // It previously sat in CbDepositRequestsController behind `if (!User.IsCbUser())`, which
        // made the button on the BOU Details page a guaranteed Access Denied.
        //
        // CanAccessRequestAsync is the same own-bank-CB-or-any-BOU-staff check the withdrawal print
        // page uses. Note it is also strictly tighter than the check it replaces: the old one
        // confirmed the caller was *a* CB user but never that the request belonged to their bank.
        public async Task<IActionResult> PrintRequest(Guid id, CancellationToken ct)
        {
            try
            {
                var request = await _uow.Requests.GetWithDetailsAsync(id, ct);
                if (request == null)
                {
                    TempData["Error"] = "Deposit request not found.";
                    return RedirectToCallerDetails(id);
                }

                if (!await CanAccessRequestAsync(request.CbId)) return Forbid();

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
                return RedirectToCallerDetails(id);
            }
        }

        /// <summary>Sends the caller back to the Details page on their own side of the CB/BOU split.
        /// The print page is reached from both, so a fixed redirect would strand one of them.</summary>
        private IActionResult RedirectToCallerDetails(Guid id)
            => RedirectToAction(nameof(Details), User.IsCbUser() ? "CbDepositRequests" : "DepositRequests", new { id });

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

        /// <summary>
        /// Generates a bag reference number in the format: [CbCode]-[Date ddmmyy]-[DenominationShortForm]-[Sequence]
        /// Example: 01-080626-1K-1
        /// </summary>
        /// <param name="cbCode">Commercial bank code</param>
        /// <param name="date">Date for the bag</param>
        /// <param name="denominationValue">Denomination value (e.g., 1000, 2000, 5000)</param>
        /// <param name="sequence">Sequence number for the bag</param>
        /// <returns>Generated bag reference number</returns>
        private string GenerateBagReferenceNumber(string cbCode, DateOnly date, decimal denominationValue, int sequence)
        {
            // Format date as ddmmyy
            string dateStr = date.ToString("ddMMyy");

            // Generate denomination short form
            string denomShortForm = GetDenominationShortForm(denominationValue);

            // Format: CbCode-Date-DenomShortForm-Sequence
            return $"{cbCode}-{dateStr}-{denomShortForm}-{sequence}";
        }

        /// <summary>
        /// Converts denomination value to short form
        /// Examples: 1000 -> 1K, 2000 -> 2K, 50000 -> 50K, 500 -> 500, 100 -> 100
        /// </summary>
        /// <param name="denominationValue">Denomination value</param>
        /// <returns>Short form string</returns>
        private string GetDenominationShortForm(decimal denominationValue)
        {
            if (denominationValue >= 1000)
            {
                // Convert to thousands and append 'K'
                int thousands = (int)(denominationValue / 1000);
                return $"{thousands}K";
            }
            else
            {
                // For values less than 1000, return as is
                return denominationValue.ToString("0");
            }
        }

        /// <summary>
        /// Gets the next sequence number for bags for a specific CB, date, and denomination
        /// </summary>
        /// <param name="cbCode">Commercial bank code</param>
        /// <param name="date">Date for the bag</param>
        /// <param name="denominationValue">Denomination value</param>
        /// <returns>Next sequence number</returns>
        private async Task<int> GetNextBagSequenceNumber(string cbCode, DateTime date, int denominationId, List<Bag> createdBags)
        {
            // Get all bags for this CB, date, and denomination
            int dbSequence = await _bagRepository.BagSequence(date, denominationId, cbCode);

            int createdBagsSequence = createdBags.Where(b => b.DenominationId == denominationId && b.CbCode == cbCode).Count();
            return dbSequence + createdBagsSequence;
        }

        /// <summary>
        /// Creates bags from deposit request denominations
        /// Divides denominations into bags of 50 bundles each
        /// </summary>
        /// <param name="depositRequestDenominations">List of deposit request denominations</param>
        /// <param name="depositRequest">The deposit request entity</param>
        /// <param name="username">Username for audit fields</param>
        /// <returns>List of created bags</returns>
        private async Task<List<Bag>> CreateBagsFromDenominations(
            IEnumerable<DepositRequestDenomination> depositRequestDenominations,
            DepositRequest depositRequest,
            string username)
        {
            const int BUNDLES_PER_BAG = 50;
            var createdBags = new List<Bag>();

            foreach (var denomination in depositRequestDenominations)
            {
                // For coins
                if (!denomination.Bundles.HasValue || denomination.Bundles.Value <= 0)
                {
                    //continue;
                    denomination.Bundles = denomination.Boxes;
                }

                int totalBundles = denomination.Bundles.Value;
                int bundlesRemaining = totalBundles;
                int bagCount = 0;

                Category paids = (await _categoryRepository.FindAll()).FirstOrDefault(c => c.Name == StockConstants.StoreTypes.Paids);

                while (bundlesRemaining > 0)
                {
                    bagCount++;
                    int bundlesInThisBag = Math.Min(bundlesRemaining, BUNDLES_PER_BAG);
                    bundlesRemaining -= bundlesInThisBag;

                    // Get next sequence number for this denomination
                    int sequence = await GetNextBagSequenceNumber(
                        depositRequest.CbCode,
                        depositRequest.DepositDate.ToDateTime(TimeOnly.MinValue),
                        denomination.DenominationId,
                        createdBags);

                    // Generate bag reference number
                    string bagReferenceNumber = GenerateBagReferenceNumber(
                        depositRequest.CbCode,
                        depositRequest.DepositDate,
                        denomination.DenominationValue,
                        sequence);

                    // Create bag entity
                    var bag = new Bag
                    {
                        ReferenceNumber = bagReferenceNumber,
                        DenominationId = denomination.DenominationId,
                        BranchId = depositRequest.BouBranchId,
                        CbCode = depositRequest.CbCode,
                        IsDeleted = false,
                        SubCategoryId = denomination.SubCategoryId,
                        CategoryId = paids?.Id
                    };

                    // Create bag denomination link
                    var bagDenomination = new BagDenomination
                    {
                        Bag = bag,
                        DepositRequestDenominationId = denomination.Id,
                        IsDeleted = false,
                        NumberOfBundlesOrBoxes = bundlesInThisBag
                    };

                    bag.BagDenominations.Add(bagDenomination);
                    createdBags.Add(bag);

                    _logger.LogInformation(
                        "Created bag {BagReference} with {Bundles} bundles for denomination {DenominationValue}",
                        bagReferenceNumber, bundlesInThisBag, denomination.DenominationValue);
                }
            }

            return createdBags;
        }

        /// <summary>
        /// Saves the created bags to the database
        /// </summary>
        /// <param name="bags">List of bags to save</param>
        /// <returns>True if successful</returns>
        private async Task<bool> SaveBagsToDatabase(List<Bag> bags)
        {
            try
            {
                foreach (var bag in bags)
                {
                    var createBagModel = new CreateBagViewModel
                    {
                        ReferenceNumber = bag.ReferenceNumber,
                        DenominationId = bag.DenominationId,
                        BranchId = bag.BranchId,
                        VaultSectionId = bag.VaultSectionId,
                        CbCode = bag.CbCode,
                    };

                    var createdBag = await _bagRepository.CreateBagAsync(createBagModel);
                    
                    if (createdBag == null)
                    {
                        _logger.LogError("Failed to create bag with reference number {ReferenceNumber}", bag.ReferenceNumber);
                        return false;
                    }

                    // Add items to the bag
                    foreach (var bagDenom in bag.BagDenominations)
                    {
                        var addItemsModel = new AddItemsToBagViewModel
                        {
                            BagId = createdBag.Id,
                            DepositRequestDenominationIds = new List<Guid> { bagDenom.DepositRequestDenominationId }
                        };

                        bool itemsAdded = await _bagRepository.AddItemsToBagAsync(addItemsModel);
                        
                        if (!itemsAdded)
                        {
                            _logger.LogError(
                                "Failed to add denomination {DenominationId} to bag {BagId}",
                                bagDenom.DepositRequestDenominationId, createdBag.Id);
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving bags to database");
                return false;
            }
        }

        /// <summary>
        /// Generates bags for a deposit request
        /// POST: DepositRequests/GenerateBags/5
        /// </summary>
        /// <param name="id">Deposit request ID</param>
        /// <returns>Redirect to details page</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateBags(Guid id)
        {
            try
            {
                var request = await _uow.Requests.GetWithDetailsAsync(id, CancellationToken.None);
                if (request == null)
                {
                    TempData["Error"] = "Deposit request not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if request is approved
                var approvedStatus = await GetStatusByNameAsync(DepositStatuses.DepositStatusNames.Approved);
                var completedStatus = await GetStatusByNameAsync(DepositStatuses.DepositStatusNames.Completed);

                if (request.RequiresSpecialApproval)
                {
                    approvedStatus = await GetStatusByNameExceptionalAsync(DepositStatuses.DepositStatusNames.Approved);
                    completedStatus = await GetStatusByNameExceptionalAsync(DepositStatuses.DepositStatusNames.Completed);
                }

                if (request.StatusTypeId != approvedStatus.Id && request.StatusTypeId != completedStatus.Id)
                {
                    TempData["Error"] = "Bags can only be generated for approved or completed deposit requests.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";

                // Get only the CB denominations (notes with bundles)
                var cbDenoms = request.DepositRequestDenominations
                    .Where(d => d.Origin == DepositStatuses.DepositOrigin.Cb && d.Bundles.HasValue && d.Bundles.Value > 0)
                    .ToList();

                if (!cbDenoms.Any())
                {
                    TempData["Error"] = "No denominations with bundles found for bag generation.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Create bags
                var bags = await CreateBagsFromDenominations(cbDenoms, request, username);

                if (!bags.Any())
                {
                    TempData["Error"] = "No bags were created. Please check the deposit request denominations.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Save bags to database
                bool saved = await SaveBagsToDatabase(bags);

                if (saved)
                {
                    TempData["Success"] = $"Successfully generated {bags.Count} bag(s) for deposit request {request.TransactionRefNumber}.";
                    _logger.LogInformation(
                        "Generated {BagCount} bags for deposit request {TransactionRefNumber}",
                        bags.Count, request.TransactionRefNumber);
                }
                else
                {
                    TempData["Error"] = "Failed to save bags. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating bags for deposit request {DepositRequestId}", id);
                TempData["Error"] = "An error occurred while generating bags. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
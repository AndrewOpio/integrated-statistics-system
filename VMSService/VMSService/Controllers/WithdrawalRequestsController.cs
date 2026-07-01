using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.CbDepositRequests;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Models.CbWithdrawRequest;
using VMSService.Core.Models.WithdrawalRequestDenominations;
using VMSService.Core.Models.WithdrawalRequests;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.WithdrawalRequestDenominations;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Repositories.Categories;
using VMSService.Core.Repositories.CounterPartyTypes;
using VMSService.Core.Repositories.StockMovements;
using VMSService.Core.Repositories.Stocks;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using VMSService.Web.Extensions;
using WorkflowService.Client;
using WorkflowService.Client.Dto;
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
        IWithdrawalRequestDenominationRepository _wrDenomRepo,
        IAttachmentRepository _attachmentRepository,
        IWithdrawalUnitOfWork _uow,
        IWithdrawalStatusResolver _statusResolver,
        IWebHostEnvironment _environment,
        IStockMovementRepository _stockMovementRepository,
        IStockLedgerRepository _stockLedgerRepository,
        ICategoryRepository _categoryRepository,
        ICounterPartyTypeRepository _counterPartyTypeRepository,
        ILogger<WithdrawalRequestsController> _logger,
        IMapper _mapper) : Controller
    {
        public async Task<IActionResult> Index(string? branchCode = null)
        {
            //var all = (await _uow.Requests.FindAll()
            //    ).Where(x => !x.IsDeleted);

            ICollection<WithdrawalRequest> visibleRequests = Array.Empty<WithdrawalRequest>();

            var allRequests = await _withdrawalRequestRepo.FindAll();

            allRequests = allRequests.Where(x => !x.IsDeleted).ToList();

            var allStatuses = await _workflowServiceClient.FindAllStatuses();

            allStatuses = [.. allStatuses.Where(x => x.EntityType.Name == StatusConstants.Entity.WithdrawalRequest)];

            var statusMap = allStatuses.ToDictionary(s => s.Id, s => s.Name);

            var allCbs = await _configServiceClient.GetAllCommercialBankAsync();

            var cbMap = allCbs.ToDictionary(s => s.Id, s => s.Name);

            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

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

            // For non-RemittancesOfficer, lock to their branch
            if (!canViewAllLocations)
            {
                if (string.IsNullOrEmpty(branchCode) || !userBranches.Any(b => b.BranchCode == branchCode))
                    branchCode = userBranches.FirstOrDefault()?.BranchCode;
            }

            // Apply branch filter
            var filteredRequests = allRequests.AsEnumerable();
            if (!string.IsNullOrEmpty(branchCode))
                filteredRequests = allRequests.Where(r => r.BouBranchCode == branchCode);

            // ── Individual status counts ─────────────────────────────────────────────
            var active = filteredRequests.Where(r => !r.IsDeleted).ToList();

            var pendingInternalApproval = active.Count(r => r.StatusTypeName == StatusConstants.WithdrawalRequest.PendingInternalApproval);
            var pendingWithdrawalConfirmation = active.Count(r => r.StatusTypeName == StatusConstants.WithdrawalRequest.PendingWithdrawalConfirmation);
            var pendingExceptionalApproval = active.Count(r => r.StatusTypeName == StatusConstants.WithdrawalRequest.PendingExceptionalApproval);
            var pendingRecommendation = active.Count(r => r.StatusTypeName == StatusConstants.WithdrawalRequest.PendingRecommendation);
            var pendingConfirmation = active.Count(r => r.StatusTypeName == StatusConstants.WithdrawalRequest.PendingConfirmation);
            var withdrawalConfirmed = active.Count(r => r.StatusTypeName == StatusConstants.WithdrawalRequest.WithdrawalConfirmed);
            var recallRequested = active.Count(r => r.StatusTypeName == StatusConstants.WithdrawalRequest.RecallRequested);
            var recalled = active.Count(r => r.StatusTypeName == StatusConstants.WithdrawalRequest.Recalled);

            // ── Hero card aggregates ─────────────────────────────────────────────────
            var pendingCb = pendingInternalApproval;
            var underBankProcessing = pendingWithdrawalConfirmation + pendingExceptionalApproval + pendingRecommendation + pendingConfirmation;
            var recallCount = recallRequested + recalled;
            var totalProcessed = active
                .Where(r => r.StatusTypeName == StatusConstants.WithdrawalRequest.WithdrawalConfirmed)
                .Sum(r => r.TotalAmount);

            // ── Requests table ───────────────────────────────────────────────────────
            var rows = filteredRequests.Select(r => new RecentRequestRowViewModel
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
                BouBranchCode = r.BouBranchCode,
            }).ToList();


            var vm = new BouWithdrawalRequestViewModel
            {
                PendingCbCount = pendingCb,
                UnderBouProcessing = underBankProcessing,
                RecallCount = recallCount,
                TotalProcessed = totalProcessed,
                PendingInternalApprovalCount = pendingInternalApproval,
                PendingWithdrawalConfirmationCount = pendingWithdrawalConfirmation,
                PendingExceptionalApprovalCount = pendingExceptionalApproval,
                PendingRecommendationCount = pendingRecommendation,
                PendingConfirmationCount = pendingConfirmation,
                WithdrawalConfirmedCount = withdrawalConfirmed,
                RecallRequestedCount = recallRequested,
                RecalledCount = recalled,
                Requests = rows,
                BouBranches = userBranches,
                SelectedBranch = branchCode,
                CanViewAllLocations = canViewAllLocations,
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

            var canEdit = (request.StatusTypeName == WithdrawalStatusNames.Draft
              || request.StatusTypeName == WithdrawalStatusNames.ReturnedForRevision);

            var allDenoms = await _denomRepo.FindAll();

            var denomList = allDenoms.ToList();

            var denomVm = new DenomTableViewModel
            {
                Notes = denomList.Where(d => d.DenominationType.Name.Equals("Note")),
                Coins = denomList.Where(d => d.DenominationType.Name.Equals("Coin")),
            };

            var displayOrigin = ResolveDisplayOrigin(request.StatusTypeName);

            var cbDisplayLines = request.WithdrawalRequestDenominations
                .Where(d => d.Origin == WithdrawalOrigin.Cb)
                .ToList();


            var notes = cbDisplayLines
                .Where(d => d.Bundles.HasValue)
                .OrderByDescending(d => d.DenominationValue)
                .Select(d => new DenominationLineViewModel
                {
                    DenominationLabel = $"UGX {d.DenominationValue:N0}",
                    Bundles = d.Bundles,
                    Amount = d.Amount,
                })
                .ToList();

            var coins = cbDisplayLines
                .Where(d => d.Boxes.HasValue)
                .OrderByDescending(d => d.DenominationValue)
                .Select(d => new DenominationLineViewModel
                {
                    DenominationLabel = $"UGX {d.DenominationValue:N0}",
                    Boxes = d.Boxes,
                    Amount = d.Amount,
                })
                .ToList();

            var userRoles = User.FindAll("appRole")
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();

            var isPanelA = userRoles.Contains(SystemPolicy.Roles.PanelA);

            var canEditDispatch = isPanelA
                && (request.StatusTypeName == StatusConstants.WithdrawalRequest.PendingRecommendation
                    || request.StatusTypeName == StatusConstants.WithdrawalRequest.WithdrawalReturned);

            var bankRows = request.WithdrawalRequestDenominations
                .Where(d => d.Origin == WithdrawalOrigin.Bou)
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
                    BoxNumbers = d.DispatchedBoxes?.Select(b => b.CaseNumber).ToList() ?? new(),
                    DispatchedBoxDetails = d.DispatchedBoxes?.Select(b => new DispatchedBoxDetail
                    {
                        CaseNumber = b.CaseNumber,
                        BundlesTaken = b.BundlesTaken,
                        SerialStartTaken = b.SerialStartTaken,
                        SerialEndTaken = b.SerialEndTaken,
                        SerialPrefix = b.SerialPrefix,  
                    }).ToList() ?? new()
                })
                .ToList();

            // Only expose them as "editable"
            var editableBankRows = canEditDispatch ? bankRows : new List<BouDenominationEditRowViewModel>();

            // Display lists — always populated from bankRows
            var dispatchedNewBanknotes = new List<BouDispatchCardViewModel>();
            var dispatchedReissueBanknotes = new List<BouDispatchSimpleViewModel>();
            var dispatchedCoins = new List<BouDispatchSimpleViewModel>();


            foreach (var row in bankRows)
            {
                if (string.Equals(row.BundleType, BundleTypes.New, StringComparison.OrdinalIgnoreCase))
                {
                    var orderedDetails = row.DispatchedBoxDetails
                        .OrderBy(b => int.TryParse(b.CaseNumber, out var n) ? n : int.MaxValue)
                        .ToList();

                    if (!orderedDetails.Any()) continue;

                    var bundlesTotal = row.Bundles ?? 0;
                    var expectedFullBundles = orderedDetails.Count * 50;
                    var isFull = bundlesTotal == expectedFullBundles;

                    if (isFull)
                    {
                        // Collapse contiguous ascending box numbers into ranges of details
                        var segment = new List<DispatchedBoxDetail> { orderedDetails[0] };
                        for (int i = 1; i < orderedDetails.Count; i++)
                        {
                            var prevNum = int.Parse(segment.Last().CaseNumber);
                            var thisNum = int.Parse(orderedDetails[i].CaseNumber);

                            if (thisNum == prevNum + 1)
                            {
                                segment.Add(orderedDetails[i]);
                            }
                            else
                            {
                                dispatchedNewBanknotes.Add(BuildFullCard(row, segment));
                                segment = new List<DispatchedBoxDetail> { orderedDetails[i] };
                            }
                        }
                        dispatchedNewBanknotes.Add(BuildFullCard(row, segment));
                    }
                    else
                    {
                        if (orderedDetails.Count != 1)
                        {
                            _logger.LogWarning(
                                "Dispatch row {RowId} has {BoxCount} boxes but bundles {Bundles} != {Expected}.",
                                row.Id, orderedDetails.Count, bundlesTotal, expectedFullBundles);
                            continue;
                        }
                        dispatchedNewBanknotes.Add(BuildPartialCard(row, orderedDetails[0]));
                    }
                }
                else if (string.Equals(row.BundleType, BundleTypes.Reissue, StringComparison.OrdinalIgnoreCase))
                {
                    dispatchedReissueBanknotes.Add(new BouDispatchSimpleViewModel
                    {
                        Id = row.Id ?? Guid.Empty,
                        DenominationId = row.DenominationId,
                        Denomination = row.DenominationValue,
                        Quantity = row.Bundles ?? 0,
                        Amount = row.Amount,
                    });
                }
                else if (string.Equals(row.BundleType, BundleTypes.Coin, StringComparison.OrdinalIgnoreCase))
                {
                    dispatchedCoins.Add(new BouDispatchSimpleViewModel
                    {
                        Id = row.Id ?? Guid.Empty,
                        DenominationId = row.DenominationId,
                        Denomination = row.DenominationValue,
                        Quantity = row.Boxes ?? 0,
                        Amount = row.Amount,
                    });
                }
            }

            var currentStatus = await _workflowServiceClient.FindStatusById(request.StatusTypeId);

            var Steps = BuildStepViewModel(request.StatusTypeName,
                           request.StatusTypeName == WithdrawalStatusNames.Approved, request.RequiresSpecialApproval);

            //Status history
            var statusHistoryList = _mapper.Map<IEnumerable<WithdrawalRequestStatusHistoryViewModel>>(request.WithdrawalRequestStatusHistory);
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
            var withdrawalAttachments = allAttachments
                .Where(a => !a.IsDeleted &&
                           a.EntityType == StatusConstants.Entity.WithdrawalRequest)
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

            // ── Build ViewModel ───────────────────────────────────────────────────
            var vm = new BouWithdrawalRequestDetailsViewModel
            {
                Id = request.Id,
                TransactionRefNumber = request.TransactionRefNumber,
                StatusId = request.StatusTypeId,
                StatusName = request.StatusTypeName,
                CollectionDate = request.CollectionDate,
                RtgsDate = request.FundSettlementValueDate,
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
                CbDenominations = cbDisplayLines.OrderBy(x => x.Bundles).ThenByDescending(x => x.DenominationValue),
                //CanEdit = canEdit,
                IsLumpSum = request.IsLumpSum,
                IsPostApprovalView = displayOrigin == WithdrawalOrigin.Bou,
                Steps = BuildStepViewModel(request.StatusTypeName,
                           request.StatusTypeName == WithdrawalStatusNames.Approved, request.RequiresSpecialApproval),
                RtgsVerificationStatus = request.RtgsVerificationStatus,
                RtgsVerifiedDate = request.RtgsVerifiedDate,
                CanVerifyRtgs = (string.IsNullOrEmpty(request.RtgsVerificationStatus) || request.RtgsVerificationStatus == RtgsVerification.Pending)
                    && request.FundSettlementValueDate.HasValue
                    && !string.IsNullOrEmpty(request.RtgsCode),
                RecallRtgsPending = request.RecallRtgsPending,
                RecallRtgsCode = request.RecallRtgsCode,
                RecallRtgsDate = request.RecallRtgsDate,
                CanEditDispatch = canEditDispatch,
                HasDispatchData = bankRows.Count > 0,
                EditableBankRows = editableBankRows,
                DispatchedNewBanknotes = dispatchedNewBanknotes,
                DispatchedReissueBanknotes = dispatchedReissueBanknotes,
                DispatchedCoins = dispatchedCoins,
                AllAttachments = withdrawalAttachments,
                StatusHistory = statusHistoryList
            };
            vm.AvailableTransitions = (await _workflowService.GetAvailableTransitionsAsync(
                StatusConstants.Entity.WithdrawalRequest,
                request.StatusTypeId,
                GetUserRole()))
                .Where(t => t.ActionName switch
                {
                    "Exceptional Approval" => request.RequiresSpecialApproval,
                    "Request Withdraw" => !request.RequiresSpecialApproval,
                    "Pending_Exceptional_Approval" => request.RequiresSpecialApproval,
                    "Pending_Recommendation" => !request.RequiresSpecialApproval,
                    "Verified" => !request.RequiresSpecialApproval && request.RtgsVerificationStatus is RtgsVerification.Confirmed or RtgsVerification.DateMismatch,
                    "Verified Exceptional" => request.RequiresSpecialApproval && request.RtgsVerificationStatus is RtgsVerification.Confirmed or RtgsVerification.DateMismatch,
                    "Verification Failed" => request.RtgsVerificationStatus is RtgsVerification.AmountMismatch or RtgsVerification.NotFound,
                    "Reject Recall" => !request.RequiresSpecialApproval,
                    "Reject Recall Exceptional" => request.RequiresSpecialApproval,
                    _ => true
                })
                .ToList();


            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> AvailableBoxes(int denominationId, Guid requestId, CancellationToken ct)
        {
            var request = await _uow.Requests.FindById(requestId);
            if (request is null) return NotFound();

            var boxes = await _uow.Denominations
                .GetAvailableBoxesAsync(denominationId, request.BouBranchId, requestId, ct);

            return Json(boxes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveBankDenominations(
            Guid id, string payload, CancellationToken ct)
        {
            using var transaction = await _uow.BeginTransactionAsync(ct);

            try
            {
                // ── Step 1: Authorize and load ──────────────────────────────
                var request = await _uow.Requests.FindById(id);
                if (request is null)
                {
                    TempData["Error"] = "Withdrawal request not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (request.StatusTypeName != StatusConstants.WithdrawalRequest.PendingRecommendation
                    && request.StatusTypeName != StatusConstants.WithdrawalRequest.WithdrawalReturned)
                {
                    TempData["Error"] = "This request is not editable in its current status.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var rows = JsonSerializer.Deserialize<List<BouDenominationEditRowViewModel>>(
                    payload ?? "[]",
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new();

                // ── Step 2: Reverse BoxState changes for request's existing dispatch rows ──
                await ReverseBoxStateForRequestAsync(id, ct);

                // ── Step 3: Wipe existing Bank-origin denomination rows + their box children ──
                await _uow.Denominations.DeleteBouRowsWithChildrenAsync(id, ct);
                await _uow.SaveAsync(ct);  // flush deletes before re-applying

                // ── Step 4: Validate + apply new payload ────────────────────
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
                        Origin = WithdrawalOrigin.Bou,
                        BundleType = r.BundleType ?? BundleTypes.NotApplicable,
                        DispatchedBoxes = new List<WithdrawalRequestDenominationBox>(),
                    };

                    if (string.Equals(r.BundleType, "New", StringComparison.OrdinalIgnoreCase))
                    {
                        if (r.BoxNumbers is null || !r.BoxNumbers.Any())
                        {
                            throw new InvalidOperationException(
                                $"New banknote row for denomination {r.DenominationValue:N0} has no box numbers.");
                        }

                        var totalBundlesInRow = 0;

                        foreach (var boxNo in r.BoxNumbers)
                        {

                            var (boxState, serialPrefix) = await _uow.BoxStates
                                .FindByBoxAndDenominationWithPrefixAsync(boxNo, r.DenominationId, ct);

                            if (boxState is null || !boxState.IsAvailable)
                            {
                                throw new InvalidOperationException(
                                    $"Box {boxNo} is not available for denomination {r.DenominationValue:N0}.");
                            }

                            var isPartial = r.BoxNumbers.Count == 1 && r.Bundles < 50;
                            var bundlesFromThisBox = isPartial ? (r.Bundles ?? 0) : 50;

                            if (bundlesFromThisBox > boxState.RemainingBundles)
                            {
                                throw new InvalidOperationException(
                                    $"Box {boxNo} has {boxState.RemainingBundles} bundles available; " +
                                    $"requested {bundlesFromThisBox}.");
                            }

                            var serialStart = boxState.CurrentSerialCursor;
                            var serialEnd = serialStart + (bundlesFromThisBox * 1000) - 1;

                            newRow.DispatchedBoxes.Add(new WithdrawalRequestDenominationBox
                            {
                                CaseNumber = boxNo,
                                BundlesTaken = bundlesFromThisBox,
                                SerialStartTaken = serialStart,
                                SerialEndTaken = serialEnd,
                                SerialPrefix = serialPrefix,
                            });

                            boxState.CurrentSerialCursor = serialEnd + 1;
                            boxState.RemainingBundles -= bundlesFromThisBox;
                            boxState.LastDispatchedDate = DateTime.UtcNow;
                            boxState.LastDispatchedRequestId = id;

                            totalBundlesInRow += bundlesFromThisBox;
                        }

                        if (r.Bundles.HasValue && r.Bundles.Value != totalBundlesInRow)
                        {
                            throw new InvalidOperationException(
                                $"Bundle mismatch on denomination {r.DenominationValue:N0}: " +
                                $"payload says {r.Bundles.Value}, computed {totalBundlesInRow}.");
                        }
                    }
                    else
                    {
                        // Reissue or Coin: no boxes, just persist as-is
                        if (r.BoxNumbers?.Any() == true)
                        {
                            _logger.LogWarning(
                                "Bank dispatch row of type {Type} for denomination {Denom} has unexpected box numbers; ignoring.",
                                r.BundleType, r.DenominationValue);
                        }
                    }

                    await _uow.Denominations.Create(newRow);
                }

                await _uow.SaveAsync(ct);
                await transaction.CommitAsync(ct);

                TempData["Success"] = "Saved denominations successfully.";
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogWarning(ex, "Validation failed saving bank denominations for {Id}", id);
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error saving bank denominations for {Id}", id);
                TempData["Error"] = "An error occurred while saving.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task ReverseBoxStateForRequestAsync(Guid requestId, CancellationToken ct)
        {
            // Pull this request's dispatched box rows
            var heldByBox = await _wrDenomRepo.GetHeldByBoxAsync(requestId);

            if (heldByBox.Count == 0) return;

            // Bulk-load all the BoxState rows we need to reverse
            var keys = heldByBox.Select(h => (h.CaseNumber, h.DenominationId)).ToList();
            var boxStates = await _uow.BoxStates.FindManyForReversalAsync(keys, ct);

            var stateByKey = boxStates.ToDictionary(b => (b.CaseNumber, b.DenominationId));

            foreach (var held in heldByBox)
            {
                if (!stateByKey.TryGetValue((held.CaseNumber, held.DenominationId), out var boxState))
                {
                    _logger.LogWarning(
                        "ReverseBoxStateForRequestAsync: no BoxState for BoxNo={BoxNo} DenominationId={DenominationId}",
                        held.CaseNumber, held.DenominationId);
                    continue;
                }

                boxState.CurrentSerialCursor = held.MinSerialStartHeld;
                boxState.RemainingBundles += held.BundlesHeld;

                if (boxState.RemainingBundles > 50)
                {
                    _logger.LogWarning(
                        "Box {BoxNo} reversal produced RemainingBundles = {Remaining}; clamping to 50.",
                        held.CaseNumber, boxState.RemainingBundles);
                    boxState.RemainingBundles = 50;
                }
            }

            await _uow.SaveAsync(ct);
        }
        
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


                var username = User.GetUsernameOrSystem();
                var userRoles = User.GetAppRoleIds();

                // Block transitions if RTGS data is missing at BOU review stages
                var statusesRequiringRtgs = new[]
                {
                    WithdrawalStatusNames.PendingVerification,
                    WithdrawalStatusNames.PendingExceptionalApproval,
                    WithdrawalStatusNames.PendingApproval,
                    WithdrawalStatusNames.PendingWithdrawalConfirmation,
                };
                if (statusesRequiringRtgs.Contains(request.StatusTypeName)
                    && (!request.FundSettlementValueDate.HasValue || string.IsNullOrEmpty(request.RtgsCode)))
                {
                    TempData["Error"] = "RTGS date and RTGS code must be provided before this action can be taken.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Look up transition for attachment requirements
                var transitionKey = new GetTransitionByKeyRequestDto
                {
                    EntityTypeName = StatusConstants.Entity.WithdrawalRequest,
                    FromStatusId = request.StatusTypeId,
                    ActionName = actionName,
                };
                var transition = await _workflowServiceClient.FindByTransitionKey(transitionKey);

                List<long>? uploadedAttachmentIds = null;
                if (transition != null && transition.RequiresAttachments && transition.AttachmentRequirements.Any())
                {
                    uploadedAttachmentIds = await HandleAttachmentUploadsAsync(
                        transition.AttachmentRequirements.ToList(),
                        transition.Id,
                        id,
                        StatusConstants.Entity.WithdrawalRequest,
                        username);
                }

                bool isExecuted = await _workflowService.ExecuteTransition(
                    entityType: StatusConstants.Entity.WithdrawalRequest,
                    entityId: id,
                    currentStatusId: request.StatusTypeId,
                    actionName: actionName,
                    username: username,
                    userRoles: userRoles,
                    remarks: remarks,
                    ct: ct,
                    attachmentIds: uploadedAttachmentIds);

                if (isExecuted)
                {
                    if (actionName == "Approve Recall")
                    {
                        request.RecallRtgsPending = request.RtgsVerificationStatus is RtgsVerification.Confirmed or RtgsVerification.DateMismatch;
                        request.ModifiedBy = username;
                        request.ModifiedDate = DateTime.Now;
                        await _uow.SaveAsync(ct);
                    }

                    // Create stock movement when withdrawal is confirmed
                    var updatedRequest = await _uow.Requests.GetWithDetailsAsync(id, ct);
                    if (updatedRequest?.StatusTypeName == WithdrawalStatusNames.Completed)
                    {
                        var bouDenoms = updatedRequest.WithdrawalRequestDenominations
                            .Where(d => d.Origin == WithdrawalOrigin.Bou)
                            .ToList();

                        var allCategories = await _categoryRepository.FindAll();
                        var newCategory = allCategories.FirstOrDefault(c => c.Name == StockConstants.StoreTypes.New);
                        var reissueCategory = allCategories.FirstOrDefault(c => c.Name == StockConstants.StoreTypes.Reissue);
                        var counterPartyType = await _counterPartyTypeRepository.FindByName(StockConstants.StockCounterPartyType.CommercialBank);

                        var dispatchedStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                        {
                            ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                            StatusName = StatusConstants.StockMovement.Dispatched,
                            EntityTypeName = StatusConstants.Entity.StockMovement,
                        });

                        var movement = new StockMovement
                        {
                            MovementType = StockConstants.StockMovementTypes.BranchToCommBank,
                            Date = DateTime.Now,
                            ReferenceNumber = updatedRequest.TransactionRefNumber ?? string.Empty,
                            DispatchingBranchId = updatedRequest.BouBranchId,
                            ReceivingBranchId = null,
                            CommercialBankBranchId = updatedRequest.CbId,
                            TotalBagsBoxes = null,
                            GrandTotalValue = updatedRequest.TotalAmount,
                            CurrentStatusId = dispatchedStatus.Id,
                            CurrentStatusName = dispatchedStatus.Name,
                            Denominations = bouDenoms.Select(d =>
                            {
                                var isNew = string.Equals(d.BundleType, BundleTypes.New, StringComparison.OrdinalIgnoreCase);
                                var category = isNew ? newCategory : reissueCategory;

                                return new StockMovementDenomination
                                {
                                    DenominationId = d.DenominationId,
                                    NumBagsBoxes = d.Boxes,
                                    NumBundles = d.Bundles,
                                    CategoryId = category?.Id,
                                    Categorization = category?.Name,
                                    ContainerType = d.Boxes.HasValue
                                        ? StockConstants.ContainerTypes.Box
                                        : StockConstants.ContainerTypes.Bag,
                                    SubTotalValue = d.Amount,
                                    DenominationType = "Sent",
                                };
                            }).ToList(),
                        };

                        var newStockMovement = await _stockMovementRepository.CreateAsync(movement);

                        var stockLedgerDetails = bouDenoms.Select(d =>
                        {
                            var isNew = string.Equals(d.BundleType, BundleTypes.New, StringComparison.OrdinalIgnoreCase);
                            var category = isNew ? newCategory : reissueCategory;

                            return new StockLedgerDetail
                            {
                                CreatedDate = DateTime.Now,
                                ModifiedDate = DateTime.Now,
                                CreatedBy = username,
                                ModifiedBy = username,
                                DenominationId = d.DenominationId,
                                NumberOfBoxes = d.Boxes ?? 0,
                                NumberOfBundles = d.Bundles,
                                NumberOfPieces = ((d.Bundles ?? 0) * 1000) + ((d.Boxes ?? 0) * 1000),
                                Value = d.Amount,
                                CategoryId = category?.Id ?? 0,
                                SubCategoryId = 0,
                            };
                        }).ToList();

                        var stockLedger = new StockLedger
                        {
                            ReferenceCode = updatedRequest.TransactionRefNumber ?? string.Empty,
                            StockMovementId = newStockMovement.Id,
                            IsIn = false,
                            CounterPartyTypeId = counterPartyType.Id,
                            CounterPartyId = updatedRequest.CbId,
                            StoreId = updatedRequest.BouBranchId,
                            Notes = remarks,
                            CreatedBy = username,
                            CreatedDate = DateTime.Now,
                            ModifiedBy = username,
                            ModifiedDate = DateTime.Now,
                            StockLedgerDetails = stockLedgerDetails,
                        };

                        bool createdLedger = await _stockLedgerRepository.Create(stockLedger);
                        if (!createdLedger)
                        {
                            _logger.LogWarning("Failed to create stock ledger for withdrawal {Id}", id);
                        }

                        TempData["Success"] = "Withdrawal confirmed. Stock position updated.";
                    }
                    else
                    {
                        TempData["Success"] = "Action completed successfully.";
                    }
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
        public async Task<IActionResult> VerifyRtgs(Guid id, string verificationStatus, DateOnly? rtgsVerifiedDate, CancellationToken ct)
        {
            var request = await _uow.Requests.FindById(id);
            if (request is null) return NotFound();

            var validStatuses = new[] { RtgsVerification.Confirmed, RtgsVerification.AmountMismatch, RtgsVerification.DateMismatch, RtgsVerification.NotFound };
            if (!validStatuses.Contains(verificationStatus))
            {
                TempData["Error"] = "Invalid verification status.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (verificationStatus == RtgsVerification.DateMismatch && !rtgsVerifiedDate.HasValue)
            {
                TempData["Error"] = "Please provide the correct RTGS date.";
                return RedirectToAction(nameof(Details), new { id });
            }

            request.RtgsVerificationStatus = verificationStatus;
            request.ModifiedBy = User.GetUsernameOrSystem();
            request.ModifiedDate = DateTime.Now;

            if (verificationStatus == RtgsVerification.DateMismatch)
                request.RtgsVerifiedDate = rtgsVerifiedDate;

            await _uow.SaveAsync(ct);

            var message = verificationStatus switch
            {
                RtgsVerification.Confirmed => "RTGS verified successfully.",
                RtgsVerification.DateMismatch => "RTGS date mismatch recorded. Correct date saved.",
                RtgsVerification.AmountMismatch => "RTGS amount mismatch recorded.",
                RtgsVerification.NotFound => "RTGS not found recorded.",
                _ => "Verification status updated."
            };
            TempData["Success"] = message;
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRecall(Guid id, DateOnly recallRtgsDate, string recallRtgsCode, CancellationToken ct)
        {
            var request = await _uow.Requests.FindById(id);
            if (request is null) return NotFound();

            if (request.StatusTypeName != WithdrawalStatusNames.RecallApproved)
            {
                TempData["Error"] = "Recall can only be completed when the request is in Recalled status.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrEmpty(recallRtgsCode))
            {
                TempData["Error"] = "Recall RTGS code is required.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var username = User.GetUsernameOrSystem();
            var userRoles = User.GetAppRoleIds();

            request.RecallRtgsCode = recallRtgsCode;
            request.RecallRtgsDate = recallRtgsDate;
            request.RecallRtgsPending = false;
            request.ModifiedBy = username;
            request.ModifiedDate = DateTime.Now;

            bool isExecuted = await _workflowService.ExecuteTransition(
                entityType: StatusConstants.Entity.WithdrawalRequest,
                entityId: id,
                currentStatusId: request.StatusTypeId,
                actionName: "Complete Recall",
                username: username,
                userRoles: userRoles,
                remarks: string.Empty,
                ct: ct);

            if (isExecuted)
                TempData["Success"] = "Recall completed successfully. Reverse RTGS details recorded.";
            else
                TempData["Error"] = "Failed to complete recall. You may not have permission for this action.";

            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<List<long>> HandleAttachmentUploadsAsync(
            List<TransitionAttachmentRequirementDto> requirements,
            int workflowTransitionId,
            Guid entityId,
            string entityType,
            string username)
        {
            var uploadedAttachmentIds = new List<long>();
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "withdrawals", entityId.ToString());
            Directory.CreateDirectory(uploadsPath);

            foreach (var requirement in requirements)
            {
                var file = Request.Form.Files[$"attachments_{requirement.Id}"];
                if (file is null || file.Length == 0) continue;

                var fileExtension = Path.GetExtension(file.FileName).TrimStart('.').ToLower();
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var uniqueFileName = $"{requirement.Id}_{timestamp}_{Path.GetFileNameWithoutExtension(file.FileName)}.{fileExtension}";
                var filePath = Path.Combine(uploadsPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var attachment = new Attachment
                {
                    FileName = file.FileName,
                    FilePath = Path.Combine("uploads", "withdrawals", entityId.ToString(), uniqueFileName),
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
            }

            return uploadedAttachmentIds;
        }

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

        private WithdrawalStepViewModel BuildStepViewModel(string statusName, bool isApproved, bool requiresSpecialApproval)
            => WithdrawalStepViewModelFactory.Build(
                currentStepKey: GetCurrentStepKey(statusName),
                isRequestApproved: isApproved,
                requiresSpecialApproval: requiresSpecialApproval,
                user: User);


        private static string GetCurrentStepKey(string statusName) => statusName switch
        {
            WithdrawalStatusNames.Draft => "CbMaker",
            WithdrawalStatusNames.ReturnedForRevision => "CbMaker",
            WithdrawalStatusNames.PendingInternalApproval => "CbApprover",
            WithdrawalStatusNames.PendingExceptionalApproval or
            WithdrawalStatusNames.PendingWithdrawalConfirmation => "Remittance",
            WithdrawalStatusNames.PendingVerification => "PanelA",
            WithdrawalStatusNames.PendingApproval => "PanelB",
            WithdrawalStatusNames.Approved => "BOU",
            WithdrawalStatusNames.RejectExceptionalApproval => "BOU",
            WithdrawalStatusNames.RecallRequested or
            WithdrawalStatusNames.RecallApproved => "Remittance",
            _ => "CbMaker",
        };
        private int GetUserRole()
            => User.FindAll("appRoleId")
            .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
            .Select(c => int.TryParse(c.Value.Split(":")[1], out var roleId) ? roleId : 0)
            .FirstOrDefault(r => r > 0);

        static BouDispatchCardViewModel BuildFullCard(BouDenominationEditRowViewModel row,
    List<DispatchedBoxDetail> boxesInSegment)
        {
            var ordered = boxesInSegment.OrderBy(b => int.Parse(b.CaseNumber)).ToList();
            var first = ordered.First();
            var last = ordered.Last();
            var bundles = ordered.Sum(b => b.BundlesTaken);
            return new BouDispatchCardViewModel
            {
                Id = row.Id ?? Guid.Empty,
                DenominationId = row.DenominationId,
                Denomination = row.DenominationValue,
                StartBox = int.Parse(first.CaseNumber),
                EndBox = int.Parse(last.CaseNumber),
                IsFull = true,
                Bundles = bundles,
                SerialStart = first.SerialStartTaken,
                SerialEnd = last.SerialEndTaken,
                SerialPrefix = first.SerialPrefix,
                Amount = bundles * 1000 * row.DenominationValue,
            };
        }

        static BouDispatchCardViewModel BuildPartialCard(BouDenominationEditRowViewModel row, DispatchedBoxDetail box)
        {
            return new BouDispatchCardViewModel
            {
                Id = row.Id ?? Guid.Empty,
                DenominationId = row.DenominationId,
                Denomination = row.DenominationValue,
                StartBox = int.Parse(box.CaseNumber),
                EndBox = int.Parse(box.CaseNumber),
                IsFull = false,
                Bundles = box.BundlesTaken,
                SerialStart = box.SerialStartTaken,
                SerialEnd = box.SerialEndTaken,
                SerialPrefix = box.SerialPrefix,
                Amount = box.BundlesTaken * 1000 * row.DenominationValue,
            };
        }

    }
}

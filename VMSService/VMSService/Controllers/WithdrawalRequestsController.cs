using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using VMSService.Core.Authorization;
using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.CbDepositRequests;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Models.CbWithdrawRequest;
using VMSService.Core.Models.Penalties;
using VMSService.Core.Models.WithdrawalRequestDenominations;
using VMSService.Core.Models.WithdrawalRequests;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.AppliedPenalties;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.Categories;
using VMSService.Core.Repositories.CounterPartyTypes;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.Penalties;
using VMSService.Core.Repositories.StockMovements;
using VMSService.Core.Repositories.Stocks;
using VMSService.Core.Repositories.WithdrawalRequestDenominations;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Repositories.PenaltyWaivers;
using VMSService.Core.Repositories.PublicHolidays;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Core.Services.Submissions;
using VMSService.Core.Services.Withdrawals;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using VMSService.Web.Extensions;
using WorkflowService.Client;
using WorkflowService.Client.Dto;
using static VMSService.Core.Static.SystemPolicy;
using static VMSService.Core.Static.WithdrawConstants;

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
        //IWithdrawalStatusResolver _statusResolver,
        IWebHostEnvironment _environment,
        IStockMovementRepository _stockMovementRepository,
        IStockLedgerRepository _stockLedgerRepository,
        ICategoryRepository _categoryRepository,
        ICounterPartyTypeRepository _counterPartyTypeRepository,
        IPenaltyRepository _penaltyRepository,
        IAppliedPenaltyRepository _appliedPenaltyRepository,
        IPenaltyWaiverRepository _penaltyWaiverRepository,
        ILogger<WithdrawalRequestsController> _logger,
        IMapper _mapper,
        IPublicHolidayRepository _publicHolidayRepository) : Controller
    {
        /// <summary>Resolves which BOU branches this user may see and which branch is currently
        /// selected — shared by <see cref="Index"/> and <see cref="History"/> so both apply the
        /// exact same visibility rule.</summary>
        private (bool CanViewAllLocations, List<BranchDto> UserBranches, string? BranchCode, bool NotAttachedToBranch) ResolveBranchScope(
            string? branchCode, IEnumerable<BranchDto> allBouBranches)
        {
            var userRoles = User.GetAppRoleNames();

            bool canViewAllLocations = userRoles.Contains(Roles.RemittancesOfficer) || userRoles.Contains(Roles.BackOffice);

            // A Panel A/Panel B (BM/ABM) user is separately assigned a role named after their
            // branch — e.g. "Kampala Branch" — alongside their panel role. Confirmed directly
            // against IdentityDB (ticket 16): every Panel A/B user holds exactly one such role,
            // and its name is always "{branch.Name} Branch". This isn't in RolePolicy's catalog
            // (ticket 34 flagged these as unaccounted-for roles) since it's a per-branch variant,
            // not a fixed role name.
            var userBranches = canViewAllLocations
                ? allBouBranches.ToList()
                : allBouBranches.Where(branch => userRoles.Contains($"{branch.Name} Branch")).ToList();

            // For users without full cross-branch visibility, lock to their own branch
            if (!canViewAllLocations)
            {
                if (string.IsNullOrEmpty(branchCode) || !userBranches.Any(b => b.BranchCode == branchCode))
                    branchCode = userBranches.FirstOrDefault()?.BranchCode;
            }

            // A branch-scoped user with no resolvable branch would otherwise hit the repo with a
            // null branchCode below, which reads as "no filter" and returns every branch's rows —
            // the opposite of what a misconfigured user should see.
            bool notAttachedToBranch = !canViewAllLocations && userBranches.Count == 0;

            return (canViewAllLocations, userBranches, branchCode, notAttachedToBranch);
        }

        private static RecentRequestRowViewModel MapRow(
            WithdrawalRequest r, Dictionary<int, string> cbMap, DateOnly today, HashSet<Guid> withBouOrigin)
            => new()
            {
                Id = r.Id,
                TransactionRefNumber = r.TransactionRefNumber ?? "Not Generated",
                Amount = r.TotalAmount,
                StatusTypeName = r.StatusTypeName,
                CbName = cbMap.GetValueOrDefault(r.CbId, "No CB"),
                CollectionDate = r.CollectionDate,
                CreationDate = r.CreatedDate,
                RequiresSpecialApproval = r.RequiresSpecialApproval,
                RtgsCode = string.IsNullOrEmpty(r.RtgsCode) ? "—" : r.RtgsCode,
                BouBranchCode = r.BouBranchCode,
                ReturnRtgsPending = r.ReturnRtgsDate == null &&
                    (r.StatusTypeName == WithdrawalStatusNames.Cancelled ||
                     r.StatusTypeName == WithdrawalStatusNames.Rejected ||
                     r.StatusTypeName == WithdrawalStatusNames.RejectExceptionalApproval ||
                     r.StatusTypeName == WithdrawalStatusNames.RecallApproved),
                IsAbandoned = r.StatusTypeName == WithdrawalStatusNames.PendingVerification
                    && r.CollectionDate < today
                    && withBouOrigin.Contains(r.Id),
            };

        public async Task<IActionResult> Index(string? branchCode = null)
        {
            if (!IsWithdrawalBouRole()) return Forbid();

            var today = DateOnly.FromDateTime(DateTime.Today);

            var allCbs = await _configServiceClient.GetAllCommercialBankAsync();
            var cbMap = allCbs.ToDictionary(s => s.Id, s => s.Name);

            var allBouBranches = await _configServiceClient.GetAllBouBranchesAsync();
            var (canViewAllLocations, userBranches, resolvedBranchCode, notAttachedToBranch) = ResolveBranchScope(branchCode, allBouBranches);
            branchCode = resolvedBranchCode;

            if (notAttachedToBranch)
                return View(new BouWithdrawalRequestViewModel { NotAttachedToBranch = true });

            // Main list: excludes requests that are both terminal and past their collection date —
            // those live in History instead (ticket 19). Branch scoping pushed to the query itself.
            var active = (await _withdrawalRequestRepo.GetActiveAsync(today, branchCode)).ToList();

            // ── Individual status counts ─────────────────────────────────────────────
            var pendingInternalApproval = active.Count(r => r.StatusTypeName == WithdrawalStatusNames.PendingInternalApproval);
            var pendingWithdrawalConfirmation = active.Count(r => r.StatusTypeName == WithdrawalStatusNames.PendingWithdrawalConfirmation);
            var pendingExceptionalApproval = active.Count(r => r.StatusTypeName == WithdrawalStatusNames.PendingExceptionalApproval);
            var pendingRecommendation = active.Count(r => r.StatusTypeName == WithdrawalStatusNames.PendingVerification);
            var pendingConfirmation = active.Count(r => r.StatusTypeName == WithdrawalStatusNames.PendingApproval);
            var withdrawalConfirmed = active.Count(r => r.StatusTypeName == WithdrawalStatusNames.Approved);
            var recallRequested = active.Count(r => r.StatusTypeName == WithdrawalStatusNames.RecallRequested);
            var recalled = active.Count(r => r.StatusTypeName == WithdrawalStatusNames.RecallApproved);
            var pendingRtgsVerification = active.Count(r => r.StatusTypeName == WithdrawalStatusNames.PendingRTGSVerification);
            var returnRtgsPending = active.Count(r =>
                r.ReturnRtgsDate == null &&
                (r.StatusTypeName == WithdrawalStatusNames.Cancelled ||
                 r.StatusTypeName == WithdrawalStatusNames.Rejected ||
                 r.StatusTypeName == WithdrawalStatusNames.RejectExceptionalApproval ||
                 r.StatusTypeName == WithdrawalStatusNames.RecallApproved));

            // ── Hero card aggregates ─────────────────────────────────────────────────
            var pendingCb = pendingInternalApproval;
            var underBankProcessing = pendingWithdrawalConfirmation + pendingExceptionalApproval + pendingRecommendation + pendingConfirmation;
            var recallCount = recallRequested + recalled;
            // Deliberately a lifetime sum, independent of the active/history split — "Total
            // Processed" is a running total, not a recent-activity count.
            var totalProcessed = await _withdrawalRequestRepo.SumApprovedAmountAsync(branchCode);

            // ── Requests table ───────────────────────────────────────────────────────
            var pendingVerificationIds = active
                .Where(r => r.StatusTypeName == WithdrawalStatusNames.PendingVerification && r.CollectionDate < today)
                .Select(r => r.Id);
            var withBouOrigin = await _wrDenomRepo.GetRequestIdsWithBouOriginAsync(pendingVerificationIds);

            var rows = active
                .Select(r => MapRow(r, cbMap, today, withBouOrigin))
                .OrderBy(r => r.CollectionDate)
                .ToList();


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
                PendingRtgsVerificationCount = pendingRtgsVerification,
                ReturnRtgsPendingCount = returnRtgsPending,
                Requests = rows,
                BouBranches = userBranches,
                SelectedBranch = branchCode,
                CanViewAllLocations = canViewAllLocations,
            };

            return View(vm);
        }

        /// <summary>Terminal, past-collection-date requests excluded from the main list —
        /// caller-selected date range, defaulting to the last 7 days. Unlike the CB-facing
        /// equivalent, BOU is the authoritative side and the range carries no floor: BOU can pull
        /// any period. Same branch-scoping rules as <see cref="Index"/>.</summary>
        public async Task<IActionResult> History(string? branchCode = null, DateOnly? from = null, DateOnly? to = null)
        {
            if (!IsWithdrawalBouRole()) return Forbid();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var range = DateRangeFilterViewModel.Resolve(today, from, to);

            var allCbs = await _configServiceClient.GetAllCommercialBankAsync();
            var cbMap = allCbs.ToDictionary(s => s.Id, s => s.Name);

            var allBouBranches = await _configServiceClient.GetAllBouBranchesAsync();
            var (canViewAllLocations, userBranches, resolvedBranchCode, notAttachedToBranch) = ResolveBranchScope(branchCode, allBouBranches);
            branchCode = resolvedBranchCode;

            if (notAttachedToBranch)
                return View(new BouWithdrawalRequestViewModel { NotAttachedToBranch = true });

            var history = await _withdrawalRequestRepo.GetHistoryAsync(today, range.From, range.To, branchCode);

            var rows = history
                .Select(r => MapRow(r, cbMap, today, withBouOrigin: []))
                .OrderByDescending(r => r.CollectionDate)
                .ToList();

            return View(new BouWithdrawalRequestViewModel
            {
                Requests = rows,
                BouBranches = userBranches,
                SelectedBranch = branchCode,
                CanViewAllLocations = canViewAllLocations,
                DateRange = range,
            });
        }

        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            if (!IsWithdrawalBouRole()) return Forbid();



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
                Notes = denomList.Where(d => d.DenominationType.Name.Equals(StatusConstants.DenominationName.Note)),
                Coins = denomList.Where(d => d.DenominationType.Name.Equals(StatusConstants.DenominationName.Coin)),
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

            var userRoles = User.FindAll(SystemPolicy.ClaimType.AppRole)
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();

            var isPanelB = userRoles.Contains(SystemPolicy.Roles.PanelB);

            var canEditDispatch = isPanelB
                && request.StatusTypeName == WithdrawConstants.WithdrawalStatusNames.PendingVerification;

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

            // Late submission: request first reached Pending RTGS Confirmation after 11am
            var firstPwc = statusHistoryList
                .Where(h => h.NewStatusName == WithdrawConstants.WithdrawalStatusNames.PendingWithdrawalConfirmation)
                .OrderBy(h => h.ChangedAt)
                .FirstOrDefault();
            bool isLateSubmission = firstPwc != null && firstPwc.ChangedAt.Hour >= 11;

            // Penalty #5, waived case: the ABM's most recent return for revision happened at or
            // after 11:00 EAT — BOU's own lateness, so the penalty never applies here regardless of
            // how long CB takes to resubmit. See WithdrawalAmendmentPenaltyService for the "applies"
            // case (ABM returned before 11:00, still unresubmitted at 11:00).
            var lastAbmReturn = statusHistoryList
                .Where(h => h.NewStatusName == WithdrawConstants.WithdrawalStatusNames.ReturnedForRevision
                         && h.OldStatusName == WithdrawConstants.WithdrawalStatusNames.PendingVerification)
                .OrderByDescending(h => h.ChangedAt)
                .FirstOrDefault();
            bool isAmendmentPenaltyWaived = lastAbmReturn != null
                && TimeOnly.FromDateTime(lastAbmReturn.ChangedAt.AddHours(3)) >= new TimeOnly(11, 0, 0);

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

            // ── BOU-facing history trail ────────────────────────────────────────────
            // A fixed 5-milestone backbone (WithdrawalMilestoneMap) rather than one pill per
            // StatusHistory row — a request returned any number of times still renders the same
            // handful of pills. See ticket 36.
            var historyTrail = WithdrawalHistoryTrailBuilder.Build(statusHistoryList, request.StatusTypeName);

            // ── Build ViewModel ───────────────────────────────────────────────────
            var vm = new BouWithdrawalRequestDetailsViewModel
            {
                Id = request.Id,
                TransactionRefNumber = request.TransactionRefNumber,
                StatusId = request.StatusTypeId,
                StatusName = request.StatusTypeName,
                CollectionDate = request.CollectionDate,
                FundSettlementValueDate = request.FundSettlementValueDate,
                RtgsCode = request.RtgsCode,
                RequiresSpecialApproval = request.RequiresSpecialApproval,
                FundsNotIssued = request.FundsNotIssued,
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
                IsLateSubmission = isLateSubmission,
                IsAmendmentPenaltyWaived = isAmendmentPenaltyWaived,
                HistoryTrail = historyTrail,
                RtgsVerificationStatus = request.RtgsVerificationStatus,
                RtgsVerifiedDate = request.RtgsVerifiedDate,
                CanVerifyRtgs = userRoles.Contains(Roles.BackOffice)
                    && (request.StatusTypeName == WithdrawalStatusNames.PendingWithdrawalConfirmation
                        || request.StatusTypeName == WithdrawalStatusNames.RecallRequested)
                    && (string.IsNullOrEmpty(request.RtgsVerificationStatus) || request.RtgsVerificationStatus == RtgsVerification.Pending)
                    && !string.IsNullOrEmpty(request.RtgsCode),
                RecallRtgsPending = request.RecallRtgsPending,
                RecallRtgsCode = request.RecallRtgsCode,
                RecallRtgsDate = request.RecallRtgsDate,
                ReturnRtgsCode = request.ReturnRtgsCode,
                ReturnRtgsDate = request.ReturnRtgsDate,
                CanEditDispatch = canEditDispatch,
                HasDispatchData = bankRows.Count > 0,
                DispatchVariance = dispatchedNewBanknotes.Sum(r => r.Amount)
                    + dispatchedReissueBanknotes.Sum(r => r.Amount)
                    + dispatchedCoins.Sum(r => r.Amount)
                    - request.TotalAmount,
                EditableBankRows = editableBankRows,
                DispatchedNewBanknotes = dispatchedNewBanknotes,
                DispatchedReissueBanknotes = dispatchedReissueBanknotes,
                DispatchedCoins = dispatchedCoins,
                AllAttachments = withdrawalAttachments,
                // BOU sees everything except the CB Maker's initial submission entry — the
                // trail above already starts there, so it adds nothing as a comment either.
                StatusHistory = statusHistoryList
                    .Where(h => h.NewStatusName != WithdrawalStatusNames.Draft)
                    .OrderBy(h => h.ChangedAt),
                SubmissionDate = statusHistoryList
                    .Where(h => h.NewStatusName == WithdrawalStatusNames.PendingRTGSVerification
                             || h.NewStatusName == WithdrawalStatusNames.PendingWithdrawalConfirmation)
                    .OrderBy(h => h.ChangedAt)
                    .Select(h => (DateTime?)h.ChangedAt.AddHours(3))
                    .FirstOrDefault(),
                ApprovalDate = statusHistoryList
                    .Where(h => h.NewStatusName == WithdrawalStatusNames.Approved)
                    .Select(h => (DateTime?)h.ChangedAt.AddHours(3))
                    .FirstOrDefault(),
            };
            var allRoleTransitions = new List<WorkflowTransitionDto>();
            foreach (var roleId in GetUserRoleIds())
            {
                var roleTransitions = await _workflowService.GetAvailableTransitionsAsync(
                    StatusConstants.Entity.WithdrawalRequest,
                    request.StatusTypeId,
                    roleId);
                allRoleTransitions.AddRange(roleTransitions);
            }

            vm.AvailableTransitions = allRoleTransitions
                .DistinctBy(t => t.ActionName)
                .Where(t => t.ActionName switch
                {
                    "Exceptional Approval" => request.RequiresSpecialApproval,
                    "Request Withdraw" => !request.RequiresSpecialApproval,
                    "Pending_Exceptional_Approval" => request.RequiresSpecialApproval,
                    "Pending_Recommendation" => !request.RequiresSpecialApproval,
                    "Verified" => !request.RequiresSpecialApproval && request.RtgsVerificationStatus is RtgsVerification.Confirmed or RtgsVerification.DateMismatch,
                    "Verified Exceptional" => request.RequiresSpecialApproval && request.RtgsVerificationStatus is RtgsVerification.Confirmed or RtgsVerification.DateMismatch,
                    "Reject Recall" => !request.RequiresSpecialApproval,
                    "Reject Recall Exceptional" => request.RequiresSpecialApproval,
                    _ => true
                })
                .ToList();

            // ── Penalties ─────────────────────
            if (!string.IsNullOrEmpty(request.TransactionRefNumber))
            {
                var appliedPenalties = await _appliedPenaltyRepository.FindBySourceId(request.TransactionRefNumber);
                vm.AppliedPenalties = appliedPenalties.Select(a => new AppliedPenaltyViewModel
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

                vm.CanManagePenalties = request.StatusTypeName == WithdrawalStatusNames.PendingVerification && vm.AvailableTransitions.Any();

                if (vm.CanManagePenalties)
                {
                    var appliedPenaltyIds = appliedPenalties.Select(a => a.PenaltyId).ToHashSet();
                    var applicablePenalties = await _penaltyRepository.FindApplicableFor(PenaltyConstants.AppliesTo.PreTransaction);

                    var checklist = new List<PenaltyChecklistItemViewModel>();
                    foreach (var p in applicablePenalties)
                    {
                        // A branch-scoped waiver, checked against the request's collection date,
                        // suppresses the penalty from being selectable — it stays visible so
                        // Back Office can see why it's not offered rather than it just vanishing.
                        var waiver = await _penaltyWaiverRepository.FindActiveAsync(p.Id, request.BouBranchId, request.CollectionDate);
                        checklist.Add(new PenaltyChecklistItemViewModel
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Description = p.Description,
                            PenaltyValue = p.PenaltyValue,
                            IsChecked = appliedPenaltyIds.Contains(p.Id),
                            IsWaived = waiver is not null,
                            WaiverEndDate = waiver?.EndDate,
                        });
                    }
                    vm.ApplicablePenalties = checklist;
                }
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyPenalties(Guid id, List<int>? penaltyIds)
        {
            if (!IsWithdrawalBouRole()) return Forbid();

            var request = await _uow.Requests.FindById(id);
            if (request is null)
            {
                TempData["Error"] = "Withdrawal request not found.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (request.StatusTypeName != WithdrawalStatusNames.PendingVerification)
            {
                TempData["Error"] = "Penalties can only be applied while the request is Pending Verification.";
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
                var currentlyApplied = await _appliedPenaltyRepository.FindBySourceId(request.TransactionRefNumber);

                foreach (var penaltyId in checkedPenaltyIds)
                {
                    // Re-check server-side even though waived penalties aren't offered on the
                    // checklist — a waiver could have been added after the page was loaded.
                    if (await _penaltyWaiverRepository.IsWaivedAsync(penaltyId, request.BouBranchId, request.CollectionDate))
                        continue;

                    await _appliedPenaltyRepository.Apply(
                        PenaltyConstants.Source.Withdrawal, request.TransactionRefNumber, penaltyId);
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
                _logger.LogError(ex, "Error applying penalties to withdrawal request {Id}", id);
                TempData["Error"] = "An error occurred while updating penalties.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>Waives one specific applied penalty in place (e.g. an automatically-applied one
        /// the ABM has reviewed and decided shouldn't stand), with a mandatory reason. Deliberately
        /// separate from the branch-and-date-ranged <see cref="ApplyPenalties"/>/<c>PenaltyWaiver</c>
        /// machinery — this waives just this one applied charge, not the penalty type going
        /// forward. Gated the same way as ApplyPenalties: only while Pending Verification, and only
        /// for a role with an available transition on the request right now (in practice, the
        /// ABM).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WaivePenalty(Guid id, int appliedPenaltyId, string waiverReason)
        {
            if (!IsWithdrawalBouRole()) return Forbid();

            var request = await _uow.Requests.FindById(id);
            if (request is null)
            {
                TempData["Error"] = "Withdrawal request not found.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (request.StatusTypeName != WithdrawalStatusNames.PendingVerification)
            {
                TempData["Error"] = "Penalties can only be waived while the request is Pending Verification.";
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
                _logger.LogError(ex, "Error waiving applied penalty {AppliedPenaltyId} on withdrawal request {Id}", appliedPenaltyId, id);
                TempData["Error"] = "An error occurred while waiving the penalty.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>The ABM's one-way declaration that no funds will be dispatched for this
        /// request. Deliberately separate from <see cref="SaveBankDenominations"/> — it's not a
        /// dispatch edit, so it doesn't belong behind that modal. Requires a penalty to already be
        /// applied (a "no funds issued" outcome always carries a reason/charge, per the business
        /// rule: dispatch without penalty, dispatch with penalty, or no funds issued with penalty —
        /// never no funds issued with nothing at all) and immediately releases any boxes already
        /// held via the same sequence Reject/Return use.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFundsNotIssued(Guid id, CancellationToken ct)
        {
            if (!IsWithdrawalBouRole()) return Forbid();

            var request = await _uow.Requests.FindById(id);
            if (request is null)
            {
                TempData["Error"] = "Withdrawal request not found.";
                return RedirectToAction(nameof(Index));
            }

            if (request.StatusTypeName != WithdrawalStatusNames.PendingVerification)
            {
                TempData["Error"] = "This request is not editable in its current status.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var appliedPenalties = string.IsNullOrEmpty(request.TransactionRefNumber)
                ? []
                : await _appliedPenaltyRepository.FindBySourceId(request.TransactionRefNumber);
            if (appliedPenalties.Count == 0)
            {
                TempData["Error"] = "Apply at least one penalty before marking this request as Funds Not Issued.";
                return RedirectToAction(nameof(Details), new { id });
            }

            request.FundsNotIssued = true;
            await ReleaseBouHoldAsync(id, ct);

            TempData["Success"] = "Request marked as Funds Not Issued.";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>Reverses <see cref="MarkFundsNotIssued"/> — the request becomes dispatch-editable
        /// again via <see cref="SaveBankDenominations"/>. Deliberately does not try to restore the
        /// boxes that were released: those may already have been reassigned to another request by
        /// now, so the ABM just re-enters dispatch from scratch, picking whatever boxes are currently
        /// available, the same as any other dispatch entry. Applied penalties are left untouched —
        /// those were legitimately incurred and this doesn't relitigate them.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UndoFundsNotIssued(Guid id, CancellationToken ct)
        {
            if (!IsWithdrawalBouRole()) return Forbid();

            var request = await _uow.Requests.FindById(id);
            if (request is null)
            {
                TempData["Error"] = "Withdrawal request not found.";
                return RedirectToAction(nameof(Index));
            }

            if (request.StatusTypeName != WithdrawalStatusNames.PendingVerification)
            {
                TempData["Error"] = "This request is not editable in its current status.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!request.FundsNotIssued)
            {
                TempData["Error"] = "This request has not been marked as Funds Not Issued.";
                return RedirectToAction(nameof(Details), new { id });
            }

            request.FundsNotIssued = false;
            await _uow.SaveAsync(ct);

            TempData["Success"] = "Funds Not Issued undone. Dispatch can be re-entered.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> AvailableBoxes(int denominationId, Guid requestId, CancellationToken ct)
        {
            if (!IsWithdrawalBouRole()) return Forbid();

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
            if (!IsWithdrawalBouRole()) return Forbid();

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

                if (request.StatusTypeName != WithdrawalStatusNames.PendingVerification)
                {
                    TempData["Error"] = "This request is not editable in its current status.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (request.FundsNotIssued)
                {
                    TempData["Error"] = "This request has been marked as Funds Not Issued and can no longer be edited.";
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

        /// <summary>Releases a request's held BOU-origin dispatch data — box-state reversal, then
        /// the denomination rows themselves. Row-clearing must run second: the reversal reads those
        /// rows to know what to reverse, so clearing first would leave it nothing to work from.
        /// Safe to call unconditionally — both steps no-op if the request never reached dispatch.</summary>
        private async Task ReleaseBouHoldAsync(Guid requestId, CancellationToken ct)
        {
            await ReverseBoxStateForRequestAsync(requestId, ct);
            await _uow.Denominations.DeleteBouRowsWithChildrenAsync(requestId, ct);
            await _uow.SaveAsync(ct);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(Guid id, string actionName, string remarks, CancellationToken ct)
        {
            if (!IsWithdrawalBouRole()) return Forbid();

            try
            {
                var request = await _uow.Requests.FindById(id);
                if (request is null)
                {
                    TempData["Error"] = "Withdrawal request not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Captured before ExecuteTransition mutates the entity — ReturnForRevision shares
                // one ToStatus across three origins (ADR-0001), so this is how the ABM's own return
                // (from Pending Verification) is told apart from the CB Approver's/Back Office's.
                var originalStatusName = request.StatusTypeName;

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
                    && string.IsNullOrEmpty(request.RtgsCode))
                {
                    TempData["Error"] = "RTGS code must be provided before this action can be taken.";
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

                    // Reject server-side if a required attachment was never actually uploaded —
                    // the client-side check in Details.cshtml can be bypassed (JS disabled, direct
                    // POST), and HandleAttachmentUploadsAsync itself just skips missing files rather
                    // than failing, so this is the only real enforcement of "can't approve without
                    // attaching the receipt".
                    var requiredAttachments = transition.AttachmentRequirements.Where(r => r.IsRequired).ToList();
                    if (requiredAttachments.Any())
                    {
                        var uploadedRequirementIds = uploadedAttachmentIds
                            .Select(aid => transition.AttachmentRequirements
                                .FirstOrDefault(r => Request.Form.Files[$"attachments_{r.Id}"]?.Length > 0)?.Id ?? 0)
                            .Where(reqId => reqId > 0)
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
                    //if (actionName == WithdrawalStatusActionNames.ApproveRecall)
                    //{
                    //    request.RecallRtgsPending = request.RtgsVerificationStatus is RtgsVerification.Confirmed or RtgsVerification.DateMismatch;
                    //    request.ModifiedBy = username;
                    //    request.ModifiedDate = DateTime.Now;
                    //    await _uow.SaveAsync(ct);
                    //}

                    // Create stock movement when withdrawal is confirmed
                    var updatedRequest = await _uow.Requests.GetWithDetailsAsync(id, ct);
                    if (updatedRequest?.StatusTypeName == WithdrawalStatusNames.Approved)
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
                    else if (updatedRequest?.StatusTypeName == WithdrawalStatusNames.Rejected)
                    {
                        // Dispatch was recorded at Pending Verification (boxes held, serials advanced)
                        // but the request was rejected before reaching Approved — nothing physically
                        // left, so release the hold.
                        await ReleaseBouHoldAsync(id, ct);
                        TempData["Success"] = "Action completed successfully.";
                    }
                    else if (updatedRequest?.StatusTypeName == WithdrawalStatusNames.ReturnedForRevision
                        && originalStatusName == WithdrawalStatusNames.PendingVerification)
                    {
                        // ABM's own return (as opposed to the CB Approver's or Back Office's, which
                        // land on this same shared status — see ADR-0001): dispatch data already
                        // exists here, and the returned request might never come back to BOU the
                        // same day (or at all), so release the held boxes the same way a Reject does
                        // rather than leaving them reserved indefinitely.
                        await ReleaseBouHoldAsync(id, ct);
                        TempData["Success"] = "Action completed successfully.";
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
        public async Task<IActionResult> SubmitVerification(Guid id, CancellationToken ct)
        {
            if (!IsWithdrawalBouRole()) return Forbid();

            var request = await _uow.Requests.FindById(id);
            if (request is null) return NotFound();

            var userRoles = User.FindAll(ClaimType.AppRole)
                .Where(c => c.Value.StartsWith($"{SystemSettings.AppAcronym}:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();

            if (!userRoles.Contains(Roles.BackOffice))
            {
                TempData["Error"] = "Only Back Office users can submit verification.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (request.StatusTypeName != WithdrawalStatusNames.PendingWithdrawalConfirmation
                && request.StatusTypeName != WithdrawalStatusNames.RecallRequested)
            {
                TempData["Error"] = "Verification can only be submitted when the request is Pending RTGS Confirmation or Recall Requested.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrEmpty(request.RtgsCode))
            {
                TempData["Error"] = "RTGS code must be provided before submitting verification.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Defensive re-check: RequiresSpecialApproval should have been fixed by the time the
            // CB Maker submitted (see CbWithdrawRequestsController.ExecuteAction), but same-day-
            // exceptional is only meaningful relative to that submission moment — BOU can't
            // re-derive it against its own, later clock. Re-run the classifier against the
            // recorded SubmitForInternalApproval timestamp instead of trusting the stored flag
            // silently, so a mis-flagged request can't ride the standard path with no exceptional
            // sign-off just because something upstream missed it.
            var submittedAt = await _uow.StatusHistory.FindMostRecentByNewStatusNameAsync(
                id, WithdrawalStatusNames.PendingInternalApproval, ct);
            if (submittedAt is not null)
            {
                var classification = await SubmissionClassifier.ClassifyAsync(
                    _publicHolidayRepository, submittedAt.ChangedAt, request.CollectionDate);
                var shouldHaveBeenExceptional = classification == SubmissionClassification.SameDayExceptional;

                if (shouldHaveBeenExceptional && !request.RequiresSpecialApproval)
                {
                    TempData["Error"] = "This request should have been flagged as same-day exceptional at submission but wasn't — return it to the CB for correction before verifying.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            var username = User.GetUsernameOrSystem();
            var userRoleIds = User.GetAppRoleIds();

            request.RtgsVerificationStatus = RtgsVerification.Confirmed;
            request.RtgsVerifiedDate = DateOnly.FromDateTime(DateTime.Now);
            request.ModifiedBy = username;
            request.ModifiedDate = DateTime.Now;

            if (request.StatusTypeName == WithdrawalStatusNames.RecallRequested)
            {
                await _uow.SaveAsync(ct);
                TempData["Success"] = "RTGS verification recorded. The recall request can now proceed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Mark RTGS confirmed on the tracked entity — no SaveAsync yet.
            // If the transition succeeds it saves everything in one go.
            // If it fails nothing is committed and the DB stays consistent.
            var actionName = request.RequiresSpecialApproval
                ? WithdrawalStatusActionNames.Submit_Verification_Exceptional
                : WithdrawalStatusActionNames.Submit_Verification;

            var autoRemarks = request.RequiresSpecialApproval
                ? $"Verification submitted by {username}. Request requires exceptional approval."
                : $"Verification submitted by {username}.";

            bool isExecuted = await _workflowService.ExecuteTransition(
                entityType: StatusConstants.Entity.WithdrawalRequest,
                entityId: id,
                currentStatusId: request.StatusTypeId,
                actionName: actionName,
                username: username,
                userRoles: userRoleIds,
                remarks: autoRemarks,
                ct: ct);

            if (isExecuted)
            {
                TempData["Success"] = request.RequiresSpecialApproval
                    ? "Verification submitted. Request moved to Pending Exceptional Approval."
                    : "Verification submitted. Request moved to Pending Verification.";
            }
            else
            {
                TempData["Error"] = "Failed to submit verification. You may not have permission or the action is invalid.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyRtgs(Guid id, string verificationStatus, DateOnly? rtgsVerifiedDate, CancellationToken ct)
        {
            if (!IsWithdrawalBouRole()) return Forbid();

            var request = await _uow.Requests.FindById(id);
            if (request is null) return NotFound();

            // VerifyRtgs has no UI entry point today (confirmed: nothing in Details.cshtml posts
            // to it — CanVerifyRtgs actually gates the live "Submit Verification" form instead) and
            // SubmitVerification doesn't read anything this action would have set, so this is
            // defense on an orphaned-but-reachable endpoint rather than a mirror of a real caller.
            // Role and status conditions match SubmitVerification's own guard below — the closest
            // thing to an authoritative "when is RTGS verification meaningful" answer in this
            // controller — plus CanVerifyRtgs's not-already-verified/has-a-code conditions.
            var userRoles = User.FindAll(ClaimType.AppRole)
                .Where(c => c.Value.StartsWith($"{SystemSettings.AppAcronym}:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();

            if (!userRoles.Contains(Roles.BackOffice))
            {
                TempData["Error"] = "Only Back Office users can verify RTGS.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if ((request.StatusTypeName != WithdrawalStatusNames.PendingWithdrawalConfirmation
                    && request.StatusTypeName != WithdrawalStatusNames.RecallRequested)
                || (!string.IsNullOrEmpty(request.RtgsVerificationStatus) && request.RtgsVerificationStatus != RtgsVerification.Pending)
                || string.IsNullOrEmpty(request.RtgsCode))
            {
                TempData["Error"] = "RTGS verification is not available for this request at its current state.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var collectionDateSameAsFundSettlementValueDate = request.CollectionDate == request.FundSettlementValueDate;

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
        public async Task<IActionResult> CompleteCancel(Guid id, CancellationToken ct)
            => await CompleteReturn(id, WithdrawalStatusNames.Cancelled, "Complete Cancel",
                   "CXL", "Cancel return completed. Reverse RTGS details recorded.", ct);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteReject(Guid id, CancellationToken ct)
            => await CompleteReturn(id, WithdrawalStatusNames.Rejected, "Complete Reject",
                   "RJT", "Rejection return completed. Reverse RTGS details recorded.", ct);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRejectExceptional(Guid id, CancellationToken ct)
            => await CompleteReturn(id, WithdrawalStatusNames.RejectExceptionalApproval, "Complete Reject Exceptional",
                   "REA", "Exceptional rejection return completed. Reverse RTGS details recorded.", ct);

        private async Task<IActionResult> CompleteReturn(
            Guid id, string expectedStatus, string actionName,
            string codePrefix, string successMessage, CancellationToken ct)
        {
            if (!IsWithdrawalBouRole()) return Forbid();

            var request = await _uow.Requests.FindById(id);
            if (request is null) return NotFound();

            if (request.StatusTypeName != expectedStatus)
            {
                TempData["Error"] = $"Return can only be completed when the request is in '{expectedStatus}' status.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var now = DateTime.Now;
            var returnRtgsCode = $"{codePrefix}-{now:ddMMyyyyHHmmssfff}";

            var username = User.GetUsernameOrSystem();
            var userRoles = User.GetAppRoleIds();

            request.ReturnRtgsCode = returnRtgsCode;
            request.ReturnRtgsDate = DateOnly.FromDateTime(now);
            request.ModifiedBy = username;
            request.ModifiedDate = now;

            bool isExecuted = await _workflowService.ExecuteTransition(
                entityType: StatusConstants.Entity.WithdrawalRequest,
                entityId: id,
                currentStatusId: request.StatusTypeId,
                actionName: actionName,
                username: username,
                userRoles: userRoles,
                remarks: $"Reverse RTGS processed. Return Code: {returnRtgsCode}, Date: {DateOnly.FromDateTime(now):dd MMM yyyy}",
                ct: ct);

            if (isExecuted)
                TempData["Success"] = successMessage;
            else
                TempData["Error"] = "Failed to complete return. You may not have permission for this action.";

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRecall(Guid id, CancellationToken ct)
        {
            if (!IsWithdrawalBouRole()) return Forbid();

            var request = await _uow.Requests.FindById(id);
            if (request is null) return NotFound();

            if (request.StatusTypeName != WithdrawalStatusNames.RecallApproved)
            {
                TempData["Error"] = "Recall can only be completed when the request is in Recalled status.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var now = DateTime.Now;
            var recallRtgsDate = DateOnly.FromDateTime(now);
            var recallRtgsCode = $"RCC-{now:ddMMyyyyHHmmssfff}";

            var username = User.GetUsernameOrSystem();
            var userRoles = User.GetAppRoleIds();

            request.RecallRtgsCode = recallRtgsCode;
            request.RecallRtgsDate = recallRtgsDate;
            request.RecallRtgsPending = false;
            request.ModifiedBy = username;
            request.ModifiedDate = now;

            bool isExecuted = await _workflowService.ExecuteTransition(
                entityType: StatusConstants.Entity.WithdrawalRequest,
                entityId: id,
                currentStatusId: request.StatusTypeId,
                actionName: "Complete Recall",
                username: username,
                userRoles: userRoles,
                remarks: $"Reverse RTGS processed. Recall Code: {recallRtgsCode}, Date: {recallRtgsDate:dd MMM yyyy}",
                ct: ct);

            if (isExecuted)
            {
                // Ends the dispatch the same way a Reject does — release any held boxes. Confirmed
                // (ticket 06): Recall can be requested from Pending Approval, where dispatch is
                // guaranteed to have happened, and from Pending Verification, where it may already
                // have — so leaving boxes held all the way to Recall Complete is a real, not just
                // hypothetical, instance of the same dangling-boxes problem Reject and the ABM's
                // ReturnForRevision fix.
                await ReleaseBouHoldAsync(id, ct);
                TempData["Success"] = "Recall completed successfully. Reverse RTGS details recorded.";
            }
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
            var uploadsPath = Path.Combine(FileStorageConstants.UploadsRoot, "withdrawals", entityId.ToString());
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

        //private int GetUserRole()
        //    => User.FindAll(SystemSettings.AppRoleClaimType)
        //    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
        //    .Select(c => int.TryParse(c.Value.Split(":")[1], out var roleId) ? roleId : 0)
        //    .FirstOrDefault(r => r > 0);

        private List<int> GetUserRoleIds()
            => User.FindAll(SystemSettings.AppRoleClaimType)
            .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
            .Select(c => int.TryParse(c.Value.Split(":")[1], out var roleId) ? roleId : 0)
            .Where(r => r > 0)
            .ToList();

        /// <summary>Same check used to gate the workflow action buttons — only someone whose role
        /// has an available transition on this request right now may act on its penalties
        /// (ApplyPenalties/WaivePenalty).</summary>
        private async Task<bool> CallerHasAvailableTransition(WithdrawalRequest request)
        {
            foreach (var roleId in GetUserRoleIds())
            {
                var roleTransitions = await _workflowService.GetAvailableTransitionsAsync(
                    StatusConstants.Entity.WithdrawalRequest, request.StatusTypeId, roleId);
                if (roleTransitions.Any())
                    return true;
            }
            return false;
        }

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

        // Had no authorization guard at all until ticket 09's follow-up — any authenticated user,
        // including CB, could reach every action here. RolePolicy.WithdrawalBouRoles is
        // withdrawal-scoped, not the general BouRoles (shared with deposits) — Bou Checker and
        // Head of Back Office are deliberately excluded, confirmed by the user. See ticket 34.
        private bool IsWithdrawalBouRole() => User.GetAppRoleNames().Any(RolePolicy.WithdrawalBouRoles.Contains);
    }
}

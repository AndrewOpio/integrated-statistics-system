using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Models.CbWithdrawRequest;
using VMSService.Core.Models.WithdrawalRequestDenominations;
using VMSService.Core.Models.WithdrawalRequests;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.WithdrawalRequestDenominations;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using VMSService.Web.Extensions;
using WorkflowService.Client;
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
        IWithdrawalUnitOfWork _uow,
        ILogger<WithdrawalRequestsController> _logger,
        IMapper _mapper) : Controller
    {
        public async Task<IActionResult> Index()
        {
            //var all = (await _uow.Requests.FindAll()
            //    ).Where(x => !x.IsDeleted);

            var allRequests = await _withdrawalRequestRepo.FindAll();

            allRequests = allRequests.Where(x => !x.IsDeleted).ToList();

            var allStatuses = await _workflowServiceClient.FindAllStatuses();

            allStatuses = [.. allStatuses.Where(x => x.EntityType.Name == StatusConstants.Entity.WithdrawalRequest)];

            // dictionary for O(1) lookup
            var statusMap = allStatuses.ToDictionary(s => s.Id, s => s.Name);

            var allCbs = await _configServiceClient.GetAllCommercialBankAsync();

            // dictionary for O(1) lookup
            var cbMap = allCbs.ToDictionary(s => s.Id, s => s.Name);

            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            var allBouBranches = await _configServiceClient.GetAllBouBranchesAsync();

            // ── Counts ───────────────────────────────────────────────────────────────

            // Pending CB approval (Draft or ReturnedForRevision — still with the CB)
            var pendingCb = allRequests.Count(r =>
                !r.IsDeleted &&
                (r.StatusTypeName == StatusConstants.WithdrawalRequest.PendingInternalApproval));

            // Special approval pending
            var specialApproval = allRequests.Count(r =>
                !r.IsDeleted &&
                r.RequiresSpecialApproval);

            // Under bank processing (submitted to bank, recommended — not yet approved)
            var underBankProcessing = allRequests.Count(r =>
                !r.IsDeleted &&
                (r.StatusTypeName == StatusConstants.WithdrawalRequest.PendingDisbursement
                 || r.StatusTypeName == StatusConstants.WithdrawalRequest.Recommended));

            // All ever processed by bank (all approved, all time)
            var totalProcessed = allRequests
                .Where(r => !r.IsDeleted && r.StatusTypeName == StatusConstants.WithdrawalRequest.Authorized)
                .Sum(r => r.TotalAmount);

            // ── Requests table ───────────────────────────────────────────────────────
            var rows = allRequests.Select(r => new RecentRequestRowViewModel
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
                BouBranchCode = r.BouBranchCode
            }).ToList();


            var vm = new BouWithdrawalRequestViewModel
            {
                PendingCbCount = pendingCb,
                SpecialApprovalCount = specialApproval,
                UnderBouProcessing = underBankProcessing,
                TotalProcessed = totalProcessed,
                Requests = rows,
                BouBranches = allBouBranches,


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

            var canEdit = (request.StatusTypeId == WithdrawalStatusIds.Draft
              || request.StatusTypeId == WithdrawalStatusIds.ReturnedForRevision);

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

            var displayLines = request.WithdrawalRequestDenominations
                .Where(d => d.Origin == WithdrawalOrigin.Bou)
                .ToList();

            var notes = displayLines
                .Where(d => d.Bundles.HasValue)
                .OrderByDescending(d => d.DenominationValue)
                .Select(d => new DenominationLineViewModel
                {
                    DenominationLabel = $"UGX {d.DenominationValue:N0}",
                    Bundles = d.Bundles,
                    Amount = d.Amount,
                })
                .ToList();

            var coins = displayLines
                .Where(d => d.Boxes.HasValue)
                .OrderByDescending(d => d.DenominationValue)
                .Select(d => new DenominationLineViewModel
                {
                    DenominationLabel = $"UGX {d.DenominationValue:N0}",
                    Boxes = d.Boxes,
                    Amount = d.Amount,
                })
                .ToList();

            var canEditDispatch =
                request.StatusTypeName == StatusConstants.WithdrawalRequest.PendingRecommendation
                || request.StatusTypeName == StatusConstants.WithdrawalRequest.Returned;

            //var bankRows = request.WithdrawalRequestDenominations
            //        .Where(d => d.Origin == WithdrawalOrigin.Bou)
            //        .Select(d => new BouDenominationEditRowViewModel
            //        {
            //            Id = d.Id,
            //            DenominationId = d.DenominationId,
            //            DenominationValue = d.DenominationValue,
            //            DenominationTypeId = d.DenominationTypeId,
            //            BundleType = d.BundleType,
            //            Bundles = d.Bundles,
            //            Boxes = d.Boxes,
            //            Amount = d.Amount,
            //            BoxNumbers = d.DispatchedBoxes?.Select(b => b.CaseNumber).ToList() ?? new(),
            //        })
            //        .ToList();
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

            // Only expose them as "editable" when the status permits editing
            var editableBankRows = canEditDispatch ? bankRows : new List<BouDenominationEditRowViewModel>();

            // Display lists — always populated from bankRows
            var dispatchedNewBanknotes = new List<BouDispatchCardViewModel>();
            var dispatchedReissueBanknotes = new List<BouDispatchSimpleViewModel>();
            var dispatchedCoins = new List<BouDispatchSimpleViewModel>();

            //foreach (var row in bankRows)
            //{
            //    if (string.Equals(row.BundleType, BundleTypes.New, StringComparison.OrdinalIgnoreCase))
            //    {
            //        var boxNumbers = (row.BoxNumbers ?? new List<string>())
            //            .Select(b => int.TryParse(b, out var n) ? n : 0)
            //            .Where(n => n > 0)
            //            .OrderBy(n => n)
            //            .ToList();

            //        if (!boxNumbers.Any()) continue;

            //        var bundlesTotal = row.Bundles ?? 0;
            //        var expectedFullBundles = boxNumbers.Count * 50;
            //        var isFull = bundlesTotal == expectedFullBundles;

            //        if (isFull)
            //        {
            //            // Collapse contiguous ascending box numbers into ranges
            //            var segStart = boxNumbers[0];
            //            var segEnd = boxNumbers[0];
            //            for (int i = 1; i < boxNumbers.Count; i++)
            //            {
            //                if (boxNumbers[i] == segEnd + 1)
            //                {
            //                    segEnd = boxNumbers[i];
            //                }
            //                else
            //                {
            //                    dispatchedNewBanknotes.Add(BuildFullCard(row, segStart, segEnd));
            //                    segStart = boxNumbers[i];
            //                    segEnd = boxNumbers[i];
            //                }
            //            }
            //            dispatchedNewBanknotes.Add(BuildFullCard(row, segStart, segEnd));
            //        }
            //        else
            //        {
            //            // Partial: must be exactly one box, with Bundles < 50
            //            if (boxNumbers.Count != 1)
            //            {
            //                // Data integrity issue — log and skip, or treat as Full
            //                _logger.LogWarning(
            //                    "Dispatch row {RowId} has {BoxCount} boxes but bundles {Bundles} != {Expected}. Expected single box for partial.",
            //                    row.Id, boxNumbers.Count, bundlesTotal, expectedFullBundles);
            //                continue;
            //            }
            //            dispatchedNewBanknotes.Add(BuildPartialCard(row, boxNumbers[0], bundlesTotal));
            //        }
            //    }
            //    else if (string.Equals(row.BundleType, BundleTypes.Reissue, StringComparison.OrdinalIgnoreCase))
            //    {
            //        dispatchedReissueBanknotes.Add(new BouDispatchSimpleViewModel
            //        {
            //            Id = row.Id ?? Guid.Empty,
            //            DenominationId = row.DenominationId,
            //            Denomination = row.DenominationValue,
            //            Quantity = row.Bundles ?? 0,
            //            Amount = row.Amount,
            //        });
            //    }
            //    else if (string.Equals(row.BundleType, BundleTypes.Coin, StringComparison.OrdinalIgnoreCase))
            //    {
            //        dispatchedCoins.Add(new BouDispatchSimpleViewModel
            //        {
            //            Id = row.Id ?? Guid.Empty,
            //            DenominationId = row.DenominationId,
            //            Denomination = row.DenominationValue,
            //            Quantity = row.Boxes ?? 0,
            //            Amount = row.Amount,
            //        });
            //    }
            //}
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


            var Steps = BuildStepViewModel(request.StatusTypeId,
                           request.StatusTypeId == WithdrawalStatusIds.Approved);

            // ── Build ViewModel ───────────────────────────────────────────────────
            var vm = new BouWithdrawalRequestDetailsViewModel
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
                Denoms = denomVm,
                CbDenominations = cbDisplayLines.OrderBy(x => x.Bundles).ThenByDescending(x => x.DenominationValue),
                //CanEdit = canEdit,
                IsLumpSum = request.IsLumpSum,
                IsPostApprovalView = displayOrigin == WithdrawalOrigin.Bou,
                Steps = BuildStepViewModel(request.StatusTypeId,
                           request.StatusTypeId == WithdrawalStatusIds.Approved),
                CanEditDispatch = canEditDispatch,
                EditableBankRows = editableBankRows,
                DispatchedNewBanknotes = dispatchedNewBanknotes,
                DispatchedReissueBanknotes = dispatchedReissueBanknotes,
                DispatchedCoins = dispatchedCoins,
            };
            vm.AvailableTransitions = (await _workflowService.GetAvailableTransitionsAsync(
                StatusConstants.Entity.WithdrawalRequest,
                request.StatusTypeId,
                GetUserRole()))
                .Where(t => t.ActionName switch
                {
                    "Exceptional Approval" => request.RequiresSpecialApproval,
                    "Request Withdraw" => !request.RequiresSpecialApproval,
                    _ => true  // return and any others always show
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
                    && request.StatusTypeName != StatusConstants.WithdrawalRequest.Returned)
                {
                    TempData["Error"] = "This request is not editable in its current status.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var rows = JsonSerializer.Deserialize<List<BouDenominationEditRowViewModel>>(
                    payload ?? "[]",
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new();

                // ── Step 2: Reverse BoxState changes for THIS request's existing dispatch rows ──
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
                            //var boxState = await _uow.BoxStates
                            //    .FindByBoxAndDenominationAsync(boxNo, r.DenominationId, ct);
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
                            // Defensive: payload shouldn't carry box numbers for non-New rows
                            _logger.LogWarning(
                                "Bank dispatch row of type {Type} for denomination {Denom} has unexpected box numbers; ignoring.",
                                r.BundleType, r.DenominationValue);
                        }
                    }

                    await _uow.Denominations.Create(newRow);
                }

                await _uow.SaveAsync(ct);
                await transaction.CommitAsync(ct);

                // ── Step 5: Workflow transition (same as before) ────────────
                //var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                //var userRoles = User.FindAll("appRoleId")
                //    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                //    .Select(c => int.TryParse(c.Value.Split(":")[1], out var roleId) ? roleId : (int?)null)
                //    .Where(r => r.HasValue).Select(r => r!.Value).ToList();
                //var username = User.GetUsernameOrSystem();
                //var userRoles = User.GetAppRoleIds();

                //var executed = await _workflowService.ExecuteTransition(
                //    entityType: StatusConstants.Entity.WithdrawalRequest,
                //    entityId: id,
                //    currentStatusId: request.StatusTypeId,
                //    actionName: actionName,
                //    username: username,
                //    userRoles: userRoles,
                //    remarks: remarks,
                //    ct: ct);

                TempData["Success"] = "Saved denominations successfully.";
            }
            catch (InvalidOperationException ex)
            {
                // User-facing validation errors — show the message
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
            // Pull this request's dispatched box rows, grouped by (BoxNo, DenominationId)
            var heldByBox = await _wrDenomRepo.GetHeldByBoxAsync(requestId);

            if (heldByBox.Count == 0) return;

            // Bulk-load all the BoxState rows we need to reverse, in one query
            var keys = heldByBox.Select(h => (h.CaseNumber, h.DenominationId)).ToList();
            var boxStates = await _uow.BoxStates.FindManyForReversalAsync(keys, ct);

            // Index for fast lookup during the loop
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
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> SaveBankDenominations(Guid id, string payload, string actionName, string remarks, CancellationToken ct)
        //{
        //    try
        //    {
        //        var request = await _uow.Requests.FindById(id);
        //        if (request is null)
        //        {
        //            TempData["Error"] = "Withdrawal request not found.";
        //            return RedirectToAction(nameof(Index));
        //        }

        //        // Only editable in these statuses
        //        if (request.StatusTypeName != StatusConstants.WithdrawalRequest.PendingDisbursement
        //            && request.StatusTypeName != StatusConstants.WithdrawalRequest.Returned)
        //        {
        //            TempData["Error"] = "This request is not editable in its current status.";
        //            return RedirectToAction(nameof(Details), new { id });
        //        }

        //        var rows = JsonSerializer.Deserialize<List<BouDenominationEditRowViewModel>>(
        //            payload ?? "[]",
        //            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        //            ?? new();

        //        // Wipe + recreate Bank-origin rows
        //        await _uow.Denominations.DeleteBouRowsWithChildrenAsync(id, ct);

        //        foreach (var r in rows)
        //        {
        //            var newRow = new WithdrawalRequestDenomination
        //            {
        //                WithdrawalRequestId = id,
        //                DenominationId = r.DenominationId,
        //                DenominationValue = r.DenominationValue,
        //                DenominationTypeId = r.DenominationTypeId,
        //                Bundles = r.Bundles,
        //                Boxes = r.Boxes,
        //                Amount = r.Amount,
        //                Origin = "BOU",
        //                BundleType = r.BundleType ?? BundleTypes.NotApplicable,
        //                DispatchedBoxes = r.BoxNumbers
        //                    .Select(bn => new WithdrawalRequestDenominationBox { CaseNumber = bn })
        //                    .ToList()
        //            };
        //            await _uow.Denominations.Create(newRow);
        //        }

        //        await _uow.SaveAsync(ct);

        //        // Now fire the workflow transition
        //        var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
        //        var userRoles = User.FindAll("appRoleId")
        //            .Where(c => c.Value.StartsWith("VMS:"))
        //            .Select(c => int.TryParse(c.Value.Split(":")[1], out var roleId) ? roleId : (int?)null)
        //            .Where(r => r.HasValue).Select(r => r!.Value).ToList();


        //        var executed = await _workflowService.ExecuteTransition(
        //            entityType: StatusConstants.Entity.WithdrawalRequest,
        //            entityId: id,
        //            currentStatusId: request.StatusTypeId,
        //            actionName: actionName,
        //            username: username,
        //            userRoles: userRoles,
        //            remarks: remarks,
        //            ct: ct);

        //        TempData[executed ? "Success" : "Error"] = executed
        //            ? "Dispatch saved successfully."
        //            : "Saved denominations, but could not transition the workflow.";
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error saving bank denominations for {Id}", id);
        //        TempData["Error"] = "An error occurred while saving.";
        //    }

        //    return RedirectToAction(nameof(Details), new { id });
        //}

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

                //var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                //var userRoles = User.FindAll("appRoleId")
                //    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                //    .Select(c => int.TryParse(c.Value.Split(":")[1], out var roleId)
                //        ? roleId
                //        : (int?)null)
                //    .Where(r => r.HasValue)
                //    .Select(r => r!.Value)
                //    .ToList();
                var username = User.GetUsernameOrSystem();
                var userRoles = User.GetAppRoleIds();


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

        private WithdrawalStepViewModel BuildStepViewModel(int statusId, bool isApproved)
            => WithdrawalStepViewModelFactory.Build(
                currentStepKey: GetCurrentStepKey(statusId),
                isRequestApproved: isApproved,
                user: User);
        private static string GetCurrentStepKey(int statusId) => statusId switch
        {
            WithdrawalStatusIds.Draft => "CbMaker",
            WithdrawalStatusIds.ReturnedForRevision => "CbMaker",
            WithdrawalStatusIds.SubmittedToSupervisor => "CbApprover",
            WithdrawalStatusIds.PendingSpecialApproval => "BOU",
            WithdrawalStatusIds.PendingRecommendation => "BOU",
            WithdrawalStatusIds.Recommended => "BOU",
            WithdrawalStatusIds.Approved => "BOU",
            WithdrawalStatusIds.Rejected => "BOU",
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

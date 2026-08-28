using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VMSService.Core.Authorization;
using VMSService.Core.Extensions;
using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Models.CbWithdrawRequest;
using VMSService.Core.Models.Penalties;
using VMSService.Core.Models.WithdrawalRequestDenominations;
using VMSService.Core.Models.WithdrawalRequests;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.AppliedPenalties;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.WithdrawalRequestAgents;
using VMSService.Core.Repositories.WithdrawalRequestDenominations;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalRequestStatusHistories;
using VMSService.Core.Repositories.WithdrawalTransactionRefNumberGenerators;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Repositories.PublicHolidays;
using VMSService.Core.Services.Submissions;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using VMSService.Web.Authorization;
using VMSService.Web.Extensions;
using VMSService.Web.Helpers;
using WorkflowService.Client;
using WorkflowService.Client.Dto;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Web.Controllers
{
    // Ticket 34: dropped the CbOnly attribute-based policy entirely — attribute-based authorization
    // isn't exercised by a direct unit-test method call, so it was never actually covered by
    // anything. IsCbUser()/IsCbMaker() below are what CbWithdrawRequestsControllerOwnershipTests/
    // MakerCheckerTests exercise, and are the sole enforcement now.
    [RequirePermission(SystemPermissions.CbWithdrawRequests.Details)]
    public class CbWithdrawRequestsController(
    IAuthorizationService authorizationService,
    IAgentDetailRepository _agentRepo,
    IConfigurationsServiceClient _configServiceClient,
    IDenominationRepository _denomRepo,
    IWithdrawalTransactionRefNumberGenerator _txnrefNoGenerator,
    IWithdrawalUnitOfWork _uow,
    IWithdrawalRequestRepository _withdrawalRequestRepo,
    IWithdrawalRequestAgentRepository _withdrawalRequestAgentRepo,
    IWithdrawalRequestDenominationRepository _withdrawalRequestDenomRepo,
    IWorkflowServiceClient _workflowServiceClient,
    IWorkflowService _workflowService,
    IWithdrawalStatusResolver _statusResolver,
    IAttachmentRepository _attachmentRepository,
    IPublicHolidayRepository _publicHolidayRepository,
    IAppliedPenaltyRepository _appliedPenaltyRepository,
    ILogger<CbWithdrawRequestsController> _logger,
    IMapper _mapper) : BaseWithdrawController(authorizationService)
    {
        // ── Create ───────────────────────────────
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            if (!IsCbMaker()) return Forbid();

            var cbId = GetCbIdFromClaims();

            var bouBranches = await _configServiceClient.GetAllBouBranchesAsync();
            var cb = await _configServiceClient.GetCbIdAsync(cbId);

            var agents = await _agentRepo.FindAll();
            var denoms = await _denomRepo.FindAll();
            var denomList = denoms.ToList();
            var publicHolidayDates = await GetPublicHolidayDatesAsync();

            return View(new CreateWithdrawViewModel
            {
                Steps = BuildStepViewModel(WithdrawalStatusNames.Draft, false, false),
                Branches = new FromToSelectionViewModel
                {
                    Cb = cb,
                    BouBranches = bouBranches,
                },
                Agents = new AgentSelectionViewModel
                {
                    Agents = agents
                        .Where(a => a.CommercialBankId == cbId)
                        .Select(a => _mapper.Map<AgentDetailViewModel>(a))
                        .ToList(),
                },
                Denoms = new DenomTableViewModel
                {
                    Notes = denomList.Where(d => d.DenominationType?.Name?.Equals("Note") ?? false),
                    Coins = denomList.Where(d => d.DenominationType?.Name?.Equals("Coin") ?? false),
                },
                PublicHolidayDates = publicHolidayDates,
            });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            if (!IsCbMaker()) return Forbid();

            var request = await _uow.Requests.GetWithDetailsAsync(id, ct);
            if (request is null) return NotFound();
            if (!await CanAccessRequestAsync(request.CbId)) return Forbid();

            // Only allow editing in Draft or ReturnedForRevision — or, stuck stale pre-submission
            // (any of the three statuses), so the Maker has a way to fix the collection date even
            // from Pending Internal Approval, where editing is otherwise not allowed.
            if (request.StatusTypeName != WithdrawalStatusNames.Draft
                && request.StatusTypeName != WithdrawalStatusNames.ReturnedForRevision
                && !IsStalePreSubmission(request))
                return RedirectToAction("Details", new { id });

            var branchVm = new FromToSelectionViewModel
            {
                Cb = await _configServiceClient.GetCbIdAsync(request.CbId),
                BouBranches = await _configServiceClient.GetAllBouBranchesAsync(),
                SelectedBouBranchId = request.BouBranchId,
                IsEdit = true,
                CollectionDate = request.CollectionDate,
                SubmissionDate = null,
                RequiresSpecialApproval = request.RequiresSpecialApproval,
                SpecialApprovalReason = request.SpecialApprovalReason,
            };


            var agentDetails = (await _agentRepo.FindAll()).Where(a => a.IsDeleted == false);

            var agentDetailsVm = _mapper.Map<List<AgentDetailViewModel>>(agentDetails);

            var requestAgents = await _withdrawalRequestAgentRepo.GetByRequestAsync(request.Id, ct);


            var agentVm = new AgentSelectionViewModel
            {
                Agents = agentDetailsVm,
                SelectedAgentIds = requestAgents.Select(a => a.AgentId).ToList(),
            };

            var allDenoms = await _denomRepo.FindAll();
            var denomList = allDenoms.ToList();
            var selectedDenoms = await _withdrawalRequestDenomRepo.GetByRequestAndOriginAsync(request.Id, WithdrawalEnteredBy.Cb);

            var denomVm = new DenomTableViewModel
            {
                Notes = denomList.Where(d => d.DenominationType.Name.Equals("Note")),
                Coins = denomList.Where(d => d.DenominationType.Name.Equals("Coin")),
                SelectedDenominations = selectedDenoms,
                IsLumpSum = request.IsLumpSum,
                LumpSumAmount = request.IsLumpSum ? request.TotalAmount : 0,
                IsEdit = request.Id != Guid.Empty,
            };


            return View(new EditWithdrawViewModel
            {
                Request = _mapper.Map<WithdrawalRequestEditViewModel>(request),
                Steps = BuildStepViewModel(request.StatusTypeName, false, request.RequiresSpecialApproval),
                Branches = branchVm,
                Agents = agentVm,
                Denoms = denomVm,
                PublicHolidayDates = await GetPublicHolidayDatesAsync(),
            });
        }

        // ── Branch details endpoint (AJAX) ────────
        public async Task<IActionResult> BranchDetails(string type, int id)
        {
            if (type == "cb")
            {
                var cb = await _configServiceClient.GetCbIdAsync(id);

                if (cb is null)
                    return NotFound();

                return Json(new { cb.Id, name = cb.Name, code = cb.Code, cbCode = cb.Code });
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
                WorkIdScan = VMSService.Core.Static.FileUrlHelper.ToWebPath(agentDetailVm.WorkIdScan),
                agentDetailVm.PhoneNumber,
                agentDetailVm.CommercialBankBranchId,
                agentDetailVm.BranchId,
                PassportPhoto = VMSService.Core.Static.FileUrlHelper.ToWebPath(agentDetailVm.PassportPhoto),
                SignatureScan = VMSService.Core.Static.FileUrlHelper.ToWebPath(agentDetailVm.SignatureScan),
            });
        }
        [HttpPost]
        [ValidateAntiForgeryTokenFromHeader]
        public async Task<IActionResult> SaveDraft([FromBody] SaveWithdrawalRequestViewModel vm, CancellationToken ct)
        {
            if (!IsCbMaker()) return Forbid();

            if (vm is null)
                return BadRequest(new { errors = new[] { "Request body is missing or invalid." } });

            var userId = GetUserId();

            WithdrawalRequest request;

            // Guard: collection date must be present and not in the past (applies to create and edit)
            if (!vm.CollectionDate.HasValue)
                return BadRequest(new { errors = new[] { "Please select a collection date." } });

            if (vm.CollectionDate.Value < DateOnly.FromDateTime(DateTime.Now))
                return BadRequest(new { errors = new[] { "Collection date cannot be in the past." } });

            // Authoritative same-day/exceptional-approval determination — recomputed here from
            // the actual collection date and holiday calendar rather than trusted from the
            // client, so it can't be bypassed and never goes stale after a date change.
            var classification = await SubmissionClassifier.ClassifyAsync(
                _publicHolidayRepository, DateTime.UtcNow, vm.CollectionDate.Value);
            var requiresSpecialApproval = classification == SubmissionClassification.SameDayExceptional;

            if (requiresSpecialApproval && string.IsNullOrWhiteSpace(vm.SpecialApprovalReason))
                return BadRequest(new { errors = new[] { "An exceptional approval reason is required for same-day collection requests outside the grace period." } });

            if (requiresSpecialApproval && vm.SpecialApprovalReason!.Trim().Length < ReasonRules.MinLength)
                return BadRequest(new { errors = new[] { $"The exceptional approval reason must be at least {ReasonRules.MinLength} characters." } });

            if (requiresSpecialApproval && string.IsNullOrWhiteSpace(vm.RtgsCode))
                return BadRequest(new { errors = new[] { "RTGS code is required when a request requires exceptional approval." } });

            var specialApprovalReasonToStore = requiresSpecialApproval ? vm.SpecialApprovalReason?.Trim() : null;

            // Blank/whitespace means "not entered" — normalized to null throughout, since the
            // database's uniqueness guarantee is a filtered index on RtgsCode IS NOT NULL.
            var normalizedRtgsCode = NormalizeRtgsCode(vm.RtgsCode);

            // Fast pre-check for a friendly message — the filtered unique index is the real
            // guarantee and still applies at save time below in case of a concurrent submission.
            if (normalizedRtgsCode is not null
                && await _withdrawalRequestRepo.ExistsByRtgsCodeAsync(normalizedRtgsCode, excludeId: vm.Id, ct))
                return BadRequest(new { errors = new[] { RtgsCodeConflictMessage(normalizedRtgsCode) } });

            // Guard: total amount (lump sum or itemized) must be a whole bundle and meet the
            // minimum withdrawal threshold.
            var totalAmount = vm.IsLumpSum ? vm.LumpSumAmount ?? 0 : vm.Denominations.Sum(d => d.Amount);

            if (totalAmount <= 0)
                return BadRequest(new { errors = new[] { "Please enter a valid withdrawal amount." } });

            if (totalAmount % AmountRules.BundleMultiple != 0)
                return BadRequest(new { errors = new[] { "Withdrawal amount must be a whole bundle — no partial amounts allowed." } });

            if (totalAmount < AmountRules.MinimumWithdrawalAmount)
                return BadRequest(new { errors = new[] { $"Minimum withdrawal amount is UGX {AmountRules.MinimumWithdrawalAmount:N0}." } });

            if (vm.Id.HasValue)
            {
                // ── EDIT path — only touch allowed fields ─────────────────────────
                request = await _withdrawalRequestRepo.FindById(vm.Id.Value);

                var statusType = _workflowServiceClient.FindStatusById(request.StatusTypeId);

                if (request is null) return NotFound();

                if (!await CanAccessRequestAsync(request.CbId))
                    return Forbid();

                // Same set of statuses Edit (GET) allows access to — without this, a direct POST
                // could mutate a request's content at any status regardless of what the UI offers,
                // including ones already past CB's stage entirely.
                if (request.StatusTypeName != WithdrawalStatusNames.Draft
                    && request.StatusTypeName != WithdrawalStatusNames.ReturnedForRevision
                    && !IsStalePreSubmission(request))
                {
                    return BadRequest(new { errors = new[] { "This request can no longer be edited at its current status." } });
                }

                // Submission date is derived from status history — the moment the request
                // first entered "Pending RTGS Code". The settlement-date rule only
                // applies once the request has actually been submitted; before that the
                // settlement date simply mirrors the collection date.
                var statusHistory = await _uow.StatusHistory.GetByRequestAsync(request.Id, ct);
                var submissionDate = statusHistory
                    .Where(h => h.NewStatusName == WithdrawalStatusNames.PendingRTGSVerification)
                    .OrderBy(h => h.ChangedAt)
                    .Select(h => (DateTime?)h.ChangedAt)
                    .FirstOrDefault();

                if (submissionDate.HasValue
                    && vm.RtgsDate.HasValue
                    && vm.RtgsDate.Value > vm.CollectionDate.Value)
                    return BadRequest(new { errors = new[] { "Fund settlement value date cannot be after the collection date." } });

                var collectionDateChanged = request.CollectionDate != vm.CollectionDate.Value;
                request.CollectionDate = vm.CollectionDate.Value;
                request.FundSettlementValueDate = vm.RtgsDate;
                request.RtgsCode = normalizedRtgsCode;
                request.RequiresSpecialApproval = requiresSpecialApproval;
                request.SpecialApprovalReason = specialApprovalReasonToStore;
                request.ModifiedDate = DateTime.Now;
                request.ModifiedBy = userId;

                // Recompute totals from the new denom lines
                //request.TotalAmount = vm.Denominations.Sum(d => d.Amount);
                //request.TotalNotesAmount = vm.Denominations.Where(d => d.Bundles.HasValue).Sum(d => d.Amount);
                //request.TotalCoinsAmount = vm.Denominations.Where(d => d.Boxes.HasValue).Sum(d => d.Amount);
                request.IsLumpSum = vm.IsLumpSum;
                request.TotalAmount = totalAmount;
                request.TotalNotesAmount = vm.IsLumpSum ? 0 : vm.Denominations.Where(d => d.Bundles.HasValue).Sum(d => d.Amount);
                request.TotalCoinsAmount = vm.IsLumpSum ? 0 : vm.Denominations.Where(d => d.Boxes.HasValue).Sum(d => d.Amount);

                // Regenerate transaction ref number if bank branch or collection date changed
                // (both feed into the ref number).
                var branchChanged = request.BouBranchId != vm.BouBranchId;
                if (branchChanged)
                {
                    request.BouBranchId = vm.BouBranchId;
                    request.BouBranchCode = vm.BouBranchCode;
                }

                if (branchChanged || collectionDateChanged)
                {
                    request.TransactionRefNumber = await _txnrefNoGenerator
                        .GenerateAsync(request.BouBranchId, request.BouBranchCode,
                                       request.CbId, request.CbCode, request.CollectionDate);
                }
            }
            else
            {
                // ── CREATE path — full initialisation ────────────────────────────
                if (vm.RtgsDate.HasValue && vm.CollectionDate.HasValue && vm.RtgsDate.Value > vm.CollectionDate.Value)
                    return BadRequest(new { errors = new[] { "Fund settlement value date cannot be after the collection date." } });

                request = new WithdrawalRequest
                {
                    CbId = GetCbIdFromClaims(),
                    CbCode = vm.CbCode,
                    BouBranchId = vm.BouBranchId,
                    BouBranchCode = vm.BouBranchCode,
                    CollectionDate = vm.CollectionDate ?? default,
                    FundSettlementValueDate = vm.RtgsDate,
                    RtgsCode = normalizedRtgsCode,
                    RequiresSpecialApproval = requiresSpecialApproval,
                    SpecialApprovalReason = specialApprovalReasonToStore,
                    StatusTypeId = await _statusResolver.GetStatusIdAsync(WithdrawalStatusNames.Draft),
                    StatusTypeName = WithdrawalStatusNames.Draft,
                    CreatedBy = userId,
                    CreatedDate = DateTime.Now,
                    //TotalAmount = vm.Denominations.Sum(d => d.Amount),
                    //TotalNotesAmount = vm.Denominations.Where(d => d.Bundles.HasValue).Sum(d => d.Amount),
                    //TotalCoinsAmount = vm.Denominations.Where(d => d.Boxes.HasValue).Sum(d => d.Amount),
                    IsLumpSum = vm.IsLumpSum,
                    TotalAmount = totalAmount,
                    TotalNotesAmount = vm.IsLumpSum ? 0 : vm.Denominations.Where(d => d.Bundles.HasValue).Sum(d => d.Amount),
                    TotalCoinsAmount = vm.IsLumpSum ? 0 : vm.Denominations.Where(d => d.Boxes.HasValue).Sum(d => d.Amount),
                };

                request.TransactionRefNumber = await _txnrefNoGenerator
                    .GenerateAsync(request.BouBranchId, request.BouBranchCode,
                                   request.CbId, request.CbCode, request.CollectionDate);

                await _uow.Requests.Create(request);
            }

            // ── Shared: agents + denominations ───────────────────────────────────────
            await SaveDenominationsAsync(request.Id, vm.Denominations, userId, ct);
            await SaveAgentsAsync(request.Id, vm.AgentIds, userId, ct);

            // ── Status history — create path only ────────────────────────────────────
            // On edit the status doesn't change, so no history entry is needed.
            if (!vm.Id.HasValue)
            {
                var draftStatusId = await _statusResolver.GetStatusIdAsync(WithdrawalStatusNames.Draft);
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                await AddStatusHistoryAsync(
                    request.Id,
                    oldStatusId: 0, oldStatusName: string.Empty,
                    draftStatusId,
                    WithdrawalStatusNames.Draft,
                    userId, "Saved as draft", ct, changedBy: username);
            }

            try
            {
                await _uow.SaveAsync(ct);
            }
            catch (DbUpdateException ex) when (IsRtgsCodeUniqueViolation(ex))
            {
                return BadRequest(new { errors = new[] { RtgsCodeConflictMessage(normalizedRtgsCode!) } });
            }

            TempData["Success"] = vm.Id.HasValue
                ? $"Withdrawal request {request.TransactionRefNumber} updated successfully."
                : $"Withdrawal request  {request.TransactionRefNumber} created successfully.";

            return Ok(Url.Action("Details", new { id = request.Id }));
        }

        // Blank/whitespace means "not entered" — normalized to null throughout, since the
        // database's uniqueness guarantee is a filtered index on RtgsCode IS NOT NULL.
        private static string? NormalizeRtgsCode(string? raw)
            => string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();

        private static string RtgsCodeConflictMessage(string rtgsCode)
            => $"RTGS code '{rtgsCode}' is already in use by another request.";

        // Race between ExistsByRtgsCodeAsync's pre-check and this save landing concurrently —
        // the filtered unique index (UQ_WithdrawalRequest_RtgsCode_Live) is the real guarantee;
        // this turns its violation into the same friendly message instead of a raw 500. Checked
        // by SQL error number (2601/nonclustered, 2627/constraint) rather than message text alone,
        // since SQL Server's message wording can vary by locale/version — the index name in the
        // message narrows it to this specific index, not any other unique constraint.
        private static bool IsRtgsCodeUniqueViolation(DbUpdateException ex)
            => ex.InnerException is SqlException sqlEx
            && (sqlEx.Number == 2601 || sqlEx.Number == 2627)
            && sqlEx.Message.Contains("UQ_WithdrawalRequest_RtgsCode_Live", StringComparison.OrdinalIgnoreCase);



        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            if (!IsCbUser()) return Forbid();

            var request = await _uow.Requests.GetWithDetailsAsync(id, ct);
            if (request is null) return NotFound();
            if (!await CanAccessRequestAsync(request.CbId)) return Forbid();

            // ── Current status ────────────────────────────────────────────────────
            var currentStatus = await _workflowServiceClient.FindStatusById(request.StatusTypeId);

            // ── BOU vs CB user detection ──────────────────────────────────────────
            var userRoleNames = User.FindAll("appRole")
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();
            var isBouUser = userRoleNames.Any(r => RolePolicy.BouRoles.Contains(r));

            // ── Step key (differs for BOU vs CB perspective) ──────────────────────
            var stepKey = isBouUser
                ? GetCurrentStepKeyBOU(request.StatusTypeName)
                : GetCurrentStepKey(request.StatusTypeName);

            // ── Display names from microservice ───────────────────────────────────
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

            // ── Denominations ─────────────────────────────────────────────────────
            var cbLines = request.WithdrawalRequestDenominations
                .Where(d => d.Origin == WithdrawalOrigin.Cb)
                .ToList();

            var notes = cbLines
                .Where(d => d.Bundles.HasValue)
                .Select(d => new DenominationLineViewModel
                {
                    DenominationLabel = $"UGX {d.DenominationValue:N0}",
                    Bundles = d.Bundles,
                    Amount = d.Amount,
                });

            var coins = cbLines
                .Where(d => d.Boxes.HasValue)
                .Select(d => new DenominationLineViewModel
                {
                    DenominationLabel = $"UGX {d.DenominationValue:N0}",
                    Boxes = d.Boxes,
                    Amount = d.Amount,
                });

            var isStalePreSubmission = IsStalePreSubmission(request);

            // ── canEdit requires Draft/Returned status AND CB Maker role — also true while
            // stuck stale pre-submission (any of the three statuses), so the Maker has a way to
            // fix the collection date even from Pending Internal Approval, where editing is
            // otherwise not allowed. Drives the Edit *and* Delete button visibility together.
            var canEdit = (request.StatusTypeName == WithdrawalStatusNames.Draft
                || request.StatusTypeName == WithdrawalStatusNames.ReturnedForRevision
                || isStalePreSubmission)
                && userRoleNames.Contains(RolePolicy.CbMaker);

            var rtgsEditableStatuses = new[]
            {
                WithdrawalStatusNames.Draft,
                WithdrawalStatusNames.PendingInternalApproval,
                WithdrawalStatusNames.ReturnedForRevision,
                WithdrawalStatusNames.PendingRTGSVerification,
            };
            var canEditRtgs = rtgsEditableStatuses.Contains(request.StatusTypeName)
                && (userRoleNames.Contains(RolePolicy.CbApprover) || userRoleNames.Contains(RolePolicy.CbMaker))
                && !isStalePreSubmission;

            // ── Status history ────────────────────────────────────────────────────
            var statusHistory = request.WithdrawalRequestStatusHistory
                .Where(h => !h.IsDeleted)
                .OrderBy(h => h.ChangedAt)
                .Select(h => new WithdrawalRequestStatusHistoryViewModel
                {
                    Id = h.Id,
                    OldStatusId = h.OldStatusId,
                    OldStatusName = h.OldStatusName,
                    NewStatusId = h.NewStatusId,
                    NewStatusName = h.NewStatusName,
                    ChangedBy = h.ChangedBy,
                    ChangedAt = h.ChangedAt,
                    Remarks = h.Remarks,
                    AttachmentIds = h.AttachmentIds,
                })
                .ToList();

            // Load attachments for each status history entry — same lookup as the BOU Details
            // action, so a receipt attached on Approve shows on both sides, not just BOU's.
            foreach (var history in statusHistory)
            {
                if (string.IsNullOrEmpty(history.AttachmentIds)) continue;

                var attachmentIds = history.AttachmentIds.Split(',')
                    .Where(aid => int.TryParse(aid.Trim(), out _))
                    .Select(aid => int.Parse(aid.Trim()))
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

            // CB users see their own side of the workflow (up through RTGS verification)
            // and whichever terminal outcome the request lands on. The BOU-internal
            // verification/approval churn in between is not shown to them.
            if (!isBouUser)
            {
                var cbVisibleStatuses = new HashSet<string>
                {
                    WithdrawalStatusNames.Draft,
                    WithdrawalStatusNames.PendingInternalApproval,
                    WithdrawalStatusNames.ReturnedForRevision,
                    WithdrawalStatusNames.PendingRTGSVerification,
                    WithdrawalStatusNames.Approved,
                    WithdrawalStatusNames.Rejected,
                    WithdrawalStatusNames.Cancelled,
                    WithdrawalStatusNames.RecallApproved,
                };
                statusHistory = statusHistory.Where(h => cbVisibleStatuses.Contains(h.NewStatusName)).ToList();
            }

            // ── Global attachments for this request ───────────────────────────────
            var allAttachmentsRaw = await _attachmentRepository.FindByEntityId(id.ToString());
            var allAttachments = allAttachmentsRaw
                .Where(a => !a.IsDeleted && a.EntityType == StatusConstants.Entity.WithdrawalRequest)
                .Select(a => new AttachmentViewModel
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    FileType = a.FileType,
                    FileSize = a.FileSize,
                    UploadedDate = a.CreatedDate,
                })
                .ToList();

            // ── Same-day/exceptional grace-window inputs — computed early so both the
            // transitions filter below and ShowSameDayMakerWarning can share one classification
            // instead of re-deriving it (and re-fetching the same holiday) twice.
            var dayBeforeHoliday = await _publicHolidayRepository.FindByDateAsync(request.CollectionDate.AddDays(-1));
            var isDayBeforeHoliday = dayBeforeHoliday is not null;
            var gracePeriodReason = GetGracePeriodReason(request.CollectionDate, dayBeforeHoliday);
            var sameDayClassification = SubmissionClassifier.Classify(DateTime.UtcNow, request.CollectionDate, isDayBeforeHoliday);
            // Same condition ExecuteAction's submit-time guard rejects on — without also hiding
            // the button here, a Maker clicks Submit only to be bounced back with an error instead
            // of being routed to Edit up front.
            var submitBlockedBySameDayReason = sameDayClassification == SubmissionClassification.SameDayExceptional
                && (!request.RequiresSpecialApproval || string.IsNullOrWhiteSpace(request.SpecialApprovalReason));

            // ── Available transitions (all user roles, filtered by special approval)
            var userRoleIds = GetUserRoles();
            var availableTransitions = new List<WorkflowTransitionDto>();
            foreach (var roleId in userRoleIds)
            {
                var transitions = await _workflowServiceClient.GetAvailableTransitions(
                    new GetAvailableTransitionsRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName = StatusConstants.Entity.WithdrawalRequest,
                        CurrentStatusId = request.StatusTypeId,
                        AppRole = roleId,
                    });

                if (transitions != null)
                    availableTransitions.AddRange(transitions);
            }

            // Determine once whether the workflow exposes the exceptional CB-approval path.
            var hasExceptionalApprovalTransition = availableTransitions
                .Any(t => t.ActionName == WithdrawalStatusActionNames.ExceptionalWithdrawalRequest);

            availableTransitions = availableTransitions
                .Where(t => t.ActionName switch
                {
                    // Show the exceptional path only when the flag is set.
                    var a when a == WithdrawalStatusActionNames.ExceptionalWithdrawalRequest => request.RequiresSpecialApproval,
                    // Show the normal approval on the normal path, OR as a fallback when
                    // RequiresSpecialApproval is true but no dedicated exceptional transition exists.
                    var a when a == WithdrawalStatusActionNames.ApproveToRTGSVerification => !request.RequiresSpecialApproval || !hasExceptionalApprovalTransition,
                    // Hide the submit itself when it would just bounce back with the same-day
                    // reason error — send the Maker to Edit instead of a failed round trip.
                    var a when a == WithdrawalStatusActionNames.SubmitForInternalApproval
                        || a == WithdrawalStatusActionNames.ResubmitForApproval => !submitBlockedBySameDayReason,
                    _ => true,
                })
                .ToList();

            // Hard block: nothing but Edit/Delete while stuck stale pre-submission — RTGS entry
            // is gated separately (canEditRtgs above), workflow transitions are cleared here.
            if (isStalePreSubmission)
                availableTransitions.Clear();

            // Override display labels based on the destination status.
            foreach (var t in availableTransitions)
            {
                t.DisplayLabel = t.ToStatus?.Name switch
                {
                    WithdrawalStatusNames.PendingWithdrawalConfirmation => "Approve Exceptional Request",
                    WithdrawalStatusNames.PendingRTGSVerification       => "Approve (Pending RTGS Code)",
                    _ => t.DisplayLabel,
                };
            }

            // ── Build ViewModel ───────────────────────────────────────────────────
            var vm = new WithdrawalRequestDetailsViewModel
            {
                Id = request.Id,
                TransactionRefNumber = request.TransactionRefNumber,
                StatusId = request.StatusTypeId,
                StatusName = request.StatusTypeName,
                CollectionDate = request.CollectionDate,
                FundSettlementValueDate = request.FundSettlementValueDate,
                RtgsCode = request.RtgsCode,
                RequiresSpecialApproval = request.RequiresSpecialApproval,
                ExceptionalApprovalReason = request.SpecialApprovalReason,
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
                CanEdit = canEdit,
                CanEditRtgs = canEditRtgs,
                RtgsVerificationStatus = request.RtgsVerificationStatus,
                RecallRtgsPending = request.RecallRtgsPending,
                RecallRtgsCode = request.RecallRtgsCode,
                RecallRtgsDate = request.RecallRtgsDate,
                ReturnRtgsCode = request.ReturnRtgsCode,
                ReturnRtgsDate = request.ReturnRtgsDate,
                IsLumpSum = request.IsLumpSum,
                Steps = WithdrawalStepViewModelFactory.Build(stepKey, request.StatusTypeName == WithdrawalStatusNames.Approved, request.RequiresSpecialApproval, User),
                AvailableTransitions = availableTransitions,
                StatusHistory = statusHistory,
                AllAttachments = allAttachments,
                //ShowExceptionalApprovalWarning = availableTransitions.Any() &&
                //    userRoleNames.Contains(RolePolicy.CbApprover) &&
                //    SubmissionClassifier.Classify(DateTime.UtcNow, request.CollectionDate, isDayBeforeHoliday: false)
                //        == SubmissionClassification.SameDayExceptional,
                ShowSameDayMakerWarning = (userRoleNames.Contains(RolePolicy.CbMaker) || userRoleNames.Contains(RolePolicy.CbApprover)) &&
                    sameDayClassification == SubmissionClassification.SameDayExceptional,
                CanEditSameDayReason = request.RequiresSpecialApproval &&
                    userRoleNames.Contains(RolePolicy.CbApprover) &&
                    request.StatusTypeName == WithdrawalStatusNames.PendingInternalApproval,
                GracePeriodReason = gracePeriodReason,
                IsStalePreSubmission = isStalePreSubmission,
                SubmissionDate = statusHistory
                    .Where(h => h.NewStatusName == WithdrawalStatusNames.PendingRTGSVerification
                             || h.NewStatusName == WithdrawalStatusNames.PendingWithdrawalConfirmation)
                    .OrderBy(h => h.ChangedAt)
                    .Select(h => (DateTime?)h.ChangedAt.AddHours(3))
                    .FirstOrDefault(),
            };

            // ── Penalties ─────────────────────────────────────────────────────────
            // Read-only for CB — visible whenever penalties exist, regardless of the request's
            // current status, since the "when" is already controlled on the BOU side (penalties
            // are only entered right before Panel B approval).
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
                }).ToList();
            }

            return View(vm);
        }


        // generic transition action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(Guid id, string actionName, string remarks, CancellationToken ct)
        {
            if (!IsCbUser()) return Forbid();

            try
            {
                var request = await _withdrawalRequestRepo.FindById(id);
                if (request is null)
                {
                    TempData["Error"] = "Withdrawal request not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (!await CanAccessRequestAsync(request.CbId)) return Forbid();

                if (IsStalePreSubmission(request))
                {
                    TempData["Error"] = "This request's collection date has passed. Update the collection date via Edit before taking any other action.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // SaveDraft re-derives same-day-exceptional status from CollectionDate and
                // requires a reason before it'll persist — but that only runs when an actual save
                // happens. A request created with a future date and left unsubmitted until that
                // date arrives (or edited past the client) can reach the actual submit action with
                // RequiresSpecialApproval/SpecialApprovalReason still reflecting whatever was true
                // at last save, not now. Re-check here, at the one point submission itself happens.
                if (actionName == WithdrawalStatusActionNames.SubmitForInternalApproval
                    || actionName == WithdrawalStatusActionNames.ResubmitForApproval)
                {
                    var classification = await SubmissionClassifier.ClassifyAsync(
                        _publicHolidayRepository, DateTime.UtcNow, request.CollectionDate);
                    var requiresSpecialApproval = classification == SubmissionClassification.SameDayExceptional;

                    if (requiresSpecialApproval
                        && (!request.RequiresSpecialApproval || string.IsNullOrWhiteSpace(request.SpecialApprovalReason)))
                    {
                        TempData["Error"] = "An exceptional approval reason is required for same-day collection requests outside the grace period. Update it via Edit first.";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var userRoles = User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
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
                {
                    // Reload to get the fresh status after the workflow transition
                    var updatedRequest = await _withdrawalRequestRepo.FindById(id);

                    // If the CB Approver sent the request to RTGS Verification but the RTGS code
                    // is already filled in, skip that step and advance directly to
                    // Pending RTGS Confirmation — unless this approval follows an ABM return
                    // (Pending Verification origin) with the RTGS code unchanged since that return,
                    // in which case Back Office already verified this exact code and it skips
                    // straight to Pending Verification instead. A changed code (or any other
                    // origin) still goes through the normal RTGS Confirmation cycle. See ADR-0001.
                    if (updatedRequest is not null
                        && updatedRequest.StatusTypeName == WithdrawalStatusNames.PendingRTGSVerification
                        && !string.IsNullOrWhiteSpace(updatedRequest.RtgsCode))
                    {
                        var oldStatusId   = updatedRequest.StatusTypeId;
                        var oldStatusName = updatedRequest.StatusTypeName;

                        var lastReturn = await _uow.StatusHistory.FindMostRecentByNewStatusNameAsync(
                            id, WithdrawalStatusNames.ReturnedForRevision, ct);
                        var isAbmOrigin = lastReturn?.OldStatusName == WithdrawalStatusNames.PendingVerification;
                        var codeUnchangedSinceAbmReturn = isAbmOrigin && updatedRequest.RtgsCode == updatedRequest.RtgsCodeAtLastReturn;

                        string newStatusName;
                        string autoAdvanceRemark;
                        if (codeUnchangedSinceAbmReturn)
                        {
                            newStatusName = WithdrawalStatusNames.PendingVerification;
                            autoAdvanceRemark = $"RTGS code {updatedRequest.RtgsCode} unchanged since the ABM's return — already verified, skipped straight to Pending Verification.";
                        }
                        else
                        {
                            newStatusName = WithdrawalStatusNames.PendingWithdrawalConfirmation;
                            autoAdvanceRemark = "RTGS code already provided — automatically advanced to Pending RTGS Confirmation.";

                            if (isAbmOrigin)
                            {
                                // ABM origin, but the code changed since the return — the prior
                                // verification no longer describes the current code.
                                updatedRequest.RtgsVerificationStatus = RtgsVerification.Pending;
                            }
                        }

                        var newStatusId = await _statusResolver.GetStatusIdAsync(newStatusName);

                        updatedRequest.StatusTypeId   = newStatusId;
                        updatedRequest.StatusTypeName = newStatusName;
                        updatedRequest.ModifiedBy     = username;
                        updatedRequest.ModifiedDate   = DateTime.Now;

                        await AddStatusHistoryAsync(
                            updatedRequest.Id,
                            oldStatusId, oldStatusName,
                            newStatusId, newStatusName,
                            GetUserId(),
                            autoAdvanceRemark,
                            ct, changedBy: username);

                        await _uow.SaveAsync(ct);
                    }

                    if (updatedRequest is not null
                        && updatedRequest.StatusTypeName is WithdrawConstants.WithdrawalStatusNames.PendingVerification or WithdrawConstants.WithdrawalStatusNames.PendingExceptionalApproval
                        && !updatedRequest.IsLumpSum
                        && !await _uow.Denominations.HasBouOriginAsync(id, ct))
                    {
                        try
                        {
                            await CopyDenominationsToBouAsync(id, username, ct);
                            await _uow.SaveAsync(ct);
                        }
                        catch (Exception copyEx)
                        {
                            _logger.LogError(copyEx,
                                "Approve succeeded for withdrawal {Id} but copying CB denominations to Bank failed. Retry the action to complete the copy.",
                                id);
                            TempData["Warning"] = "Approved, but could not copy denominations to the bank side. Please retry the action.";
                            return RedirectToAction(nameof(Details), new { id });
                        }
                    }

                    TempData["Success"] = "Action completed successfully.";

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
        public async Task<IActionResult> SetSameDayReason(Guid id, string reason, CancellationToken ct)
        {
            // Only reachable in the UI via Details.cshtml's CanEditSameDayReason-gated "Edit
            // Reason" modal, which requires the CB Approver role specifically — CB Maker is never
            // offered it.
            if (!IsCbApprover()) return Forbid();

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "A reason is required for same-day exceptional approval.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (reason.Trim().Length < ReasonRules.MinLength)
            {
                TempData["Error"] = $"The exceptional approval reason must be at least {ReasonRules.MinLength} characters.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var request = await _uow.Requests.FindById(id);
            if (request is null)
            {
                TempData["Error"] = "Withdrawal request not found.";
                return RedirectToAction(nameof(Index));
            }

            if (!await CanAccessRequestAsync(request.CbId)) return Forbid();

            if (IsStalePreSubmission(request))
            {
                TempData["Error"] = "This request's collection date has passed. Update the collection date via Edit before taking any other action.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Full CanEditSameDayReason condition (status + RequiresSpecialApproval already true —
            // this action only ever corrects an existing reason, the initial true/reason pair is
            // set by SaveDraft) — without this, a direct POST could set the reason (and flip
            // RequiresSpecialApproval on an otherwise-ordinary request) at any status.
            if (request.StatusTypeName != WithdrawalStatusNames.PendingInternalApproval
                || !request.RequiresSpecialApproval)
            {
                TempData["Error"] = "The same-day reason can only be edited while the request is Pending Internal Approval and already marked as same-day exceptional.";
                return RedirectToAction(nameof(Details), new { id });
            }

            request.RequiresSpecialApproval = true;
            request.SpecialApprovalReason   = reason.Trim();
            request.ModifiedDate            = DateTime.Now;
            request.ModifiedBy              = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";

            await _uow.SaveAsync(ct);

            TempData["Success"] = "Same-day reason saved. You may now proceed with the approval action.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            if (!IsCbMaker()) return Forbid();

            var request = await _uow.Requests.FindById(id);
            if (request is null) return NotFound();
            if (!await CanAccessRequestAsync(request.CbId)) return Forbid();

            // Only allow delete in Draft or ReturnedForRevision — or, stuck stale pre-submission,
            // same exception as Edit above.
            if (request.StatusTypeName != WithdrawalStatusNames.Draft
                && request.StatusTypeName != WithdrawalStatusNames.ReturnedForRevision
                && !IsStalePreSubmission(request))
            {
                TempData["Error"] = "Only draft requests can be deleted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            request.IsDeleted = true;
            request.ModifiedBy = GetUserId();
            request.ModifiedDate = DateTime.Now;

            await _uow.SaveAsync(ct);

            TempData["Success"] = "Request deleted successfully.";
            return RedirectToAction("Index", "CbRequests");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRtgs(Guid id, DateOnly? rtgsDate, string? rtgsCode, CancellationToken ct)
        {
            if (!IsCbUser()) return Forbid();

            var request = await _uow.Requests.FindById(id);
            if (request is null) return NotFound();
            if (!await CanAccessRequestAsync(request.CbId)) return Forbid();

            if (IsStalePreSubmission(request))
            {
                TempData["Error"] = "This request's collection date has passed. Update the collection date via Edit before taking any other action.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var rtgsUpdateableStatuses = new[]
            {
                WithdrawalStatusNames.Draft,
                WithdrawalStatusNames.PendingInternalApproval,
                WithdrawalStatusNames.ReturnedForRevision,
                WithdrawalStatusNames.PendingRTGSVerification,
            };
            if (!rtgsUpdateableStatuses.Contains(request.StatusTypeName))
            {
                TempData["Error"] = "RTGS details can no longer be updated at this stage.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (request.RequiresSpecialApproval && string.IsNullOrWhiteSpace(rtgsCode))
            {
                TempData["Error"] = "RTGS code is required when a request requires exceptional approval.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (rtgsDate.HasValue && rtgsDate.Value > request.CollectionDate)
            {
                TempData["Error"] = "Fund settlement value date cannot be after the collection date.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Blank/whitespace means "not entered" — normalized to null throughout, since the
            // database's uniqueness guarantee is a filtered index on RtgsCode IS NOT NULL.
            var normalizedRtgsCode = NormalizeRtgsCode(rtgsCode);

            // Fast pre-check for a friendly message — the filtered unique index is the real
            // guarantee and still applies at save time below in case of a concurrent submission.
            if (normalizedRtgsCode is not null
                && await _uow.Requests.ExistsByRtgsCodeAsync(normalizedRtgsCode, excludeId: id, ct))
            {
                TempData["Error"] = RtgsCodeConflictMessage(normalizedRtgsCode);
                return RedirectToAction(nameof(Details), new { id });
            }

            request.FundSettlementValueDate = rtgsDate;
            request.RtgsCode = normalizedRtgsCode;
            request.ModifiedBy = GetUserId();
            request.ModifiedDate = DateTime.Now;

            var autoTransitioned = false;

            if (request.StatusTypeName == WithdrawalStatusNames.PendingRTGSVerification
                && !string.IsNullOrWhiteSpace(normalizedRtgsCode))
            {
                var oldStatusId   = request.StatusTypeId;
                var oldStatusName = request.StatusTypeName;
                var newStatusId   = await _statusResolver.GetStatusIdAsync(WithdrawalStatusNames.PendingWithdrawalConfirmation);
                var username      = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";

                request.StatusTypeId   = newStatusId;
                request.StatusTypeName = WithdrawalStatusNames.PendingWithdrawalConfirmation;

                await AddStatusHistoryAsync(
                    request.Id,
                    oldStatusId, oldStatusName,
                    newStatusId, WithdrawalStatusNames.PendingWithdrawalConfirmation,
                    GetUserId(),
                    $"RTGS code {normalizedRtgsCode} entered — automatically advanced to Pending RTGS Confirmation.",
                    ct, changedBy: username);

                autoTransitioned = true;
            }

            try
            {
                await _uow.SaveAsync(ct);
            }
            catch (DbUpdateException ex) when (IsRtgsCodeUniqueViolation(ex))
            {
                TempData["Error"] = RtgsCodeConflictMessage(normalizedRtgsCode!);
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["Success"] = autoTransitioned
                ? "RTGS details saved. Request advanced to Pending RTGS Confirmation."
                : "RTGS details updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── Helpers ───────────────────────────────


        //private async Task<WithdrawalRequest> GetOrCreateRequestAsync(SaveWithdrawalRequestViewModel vm,
        //string userId,
        //CancellationToken ct)
        //{
        //    WithdrawalRequest request;


        //    if (vm.Id.HasValue)
        //    {
        //        request = await _uow.Requests.FindById(vm.Id.Value);
        //        //if (!await CanAccessRequestAsync(request.Id))
        //        //    throw new UnauthorizedAccessException();
        //    }
        //    else
        //    {
        //        request = new WithdrawalRequest
        //        {
        //            CbId = GetCbIdFromClaims(),
        //            StatusTypeId = WithdrawalStatusIds.Draft,
        //            CreatedBy = userId,
        //            CreatedDate = DateTime.Now,
        //        };
        //        await _uow.Requests.Create(request);
        //    }
        //    request.RequiresSpecialApproval = vm.RtgsDate.HasValue && vm.CollectionDate == vm.RtgsDate.Value;
        //    request.SpecialApprovalReason = vm.SpecialApprovalReason;

        //    request.CbCode = vm.CbCode;
        //    request.BouBranchId = vm.BouBranchId;
        //    request.BouBranchCode = vm.BouBranchCode;
        //    request.CollectionDate = vm.CollectionDate;
        //    request.RtgsDate = vm.RtgsDate;
        //    request.RtgsCode = vm.RtgsCode;

        //    // Recompute totals
        //    request.TotalAmount = vm.Denominations.Sum(d => d.Amount);
        //    request.TotalNotesAmount = vm.Denominations
        //        .Where(d => d.Bundles.HasValue)
        //        .Sum(d => d.Amount);
        //    request.TotalCoinsAmount = vm.Denominations
        //        .Where(d => d.Boxes.HasValue)
        //        .Sum(d => d.Amount);

        //    request.TransactionRefNumber = await _txnrefNoGenerator.GenerateAsync(request.BouBranchId, request.BouBranchCode, request.CbId, request.CbCode, request.CollectionDate);

        //    return request;
        //}

        // CB-facing progress: Maker -> Approver -> RTGS Verification -> everything
        // downstream of that (BOU's internal verification/approval workflow) is shown
        // as a single "BOU Processing" step, since CB users don't track BOU's internal
        // stages individually — see GetCurrentStepKeyBOU for the BOU-facing view.
        private static string GetCurrentStepKey(string statusName) => statusName switch
        {
            WithdrawalStatusNames.Draft or
            WithdrawalStatusNames.ReturnedForRevision => "CbMaker",
            WithdrawalStatusNames.PendingInternalApproval => "CbApprover",
            WithdrawalStatusNames.PendingRTGSVerification => "RtgsVerification",
            _ => "BouProcessing",
        };

        private static string GetCurrentStepKeyBOU(string statusName) => statusName switch
        {
            WithdrawalStatusNames.Draft or
            WithdrawalStatusNames.ReturnedForRevision or
            WithdrawalStatusNames.PendingInternalApproval => "CB",
            WithdrawalStatusNames.PendingExceptionalApproval or
            WithdrawalStatusNames.PendingWithdrawalConfirmation => "Remittance",
            WithdrawalStatusNames.PendingVerification => "PanelB",
            WithdrawalStatusNames.PendingApproval or
            WithdrawalStatusNames.Approved or
            WithdrawalStatusNames.RejectExceptionalApproval => "PanelA",
            WithdrawalStatusNames.RecallRequested or
            WithdrawalStatusNames.RecallApproved => "Remittance",
            _ => "CB",
        };

        private WithdrawalStepViewModel BuildStepViewModel(string statusName, bool isApproved, bool requiresSpecialApproval)
            => WithdrawalStepViewModelFactory.Build(
                currentStepKey: GetCurrentStepKey(statusName),
                isRequestApproved: isApproved,
                requiresSpecialApproval: requiresSpecialApproval,
                user: User);

        private string GetUserId()
            => User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        // Create/Edit/SaveDraft/Delete mutate request data — a CB Approver must not be able to
        // bypass their own downstream review by editing/deleting the draft directly (mirrors the
        // Details page's canEdit flag, which already hides these buttons from CB Approver;
        // RequirePermission alone can't enforce this server-side, since the whole controller
        // shares one "Details" permission covering both CB roles).
        private bool IsCbMaker() => User.GetAppRoleNames().Contains(RolePolicy.CbMaker);
        private bool IsCbApprover() => User.GetAppRoleNames().Contains(RolePolicy.CbApprover);

        // Every action left on this controller is CB-only: CbOwnershipAuthorizationHandler's "any
        // BOU role bypasses ownership" rule is blanket across every action that calls it, so
        // without this check a BOU user who knows a request's GUID could edit its RTGS code,
        // execute CB-side workflow transitions, view its internal review history, or set the CB
        // Approver's exceptional-approval reason. Account-type check (cbId claim presence), not a
        // role check — see VMSService.Core.Extensions.ClaimsPrincipalExtensions.IsCbUser.
        private bool IsCbUser() => User.IsCbUser();

        // Stuck pre-submission (Draft/Pending Internal Approval/Returned for Internal Revision)
        // with a collection date that's already passed. A hard block, not an auto-expiry: the
        // record just sits inert until the CB Maker fixes the date via Edit — every other action,
        // for both CB Maker and CB Approver, is rejected while this is true.
        private static bool IsStalePreSubmission(WithdrawalRequest request)
            => WithdrawalRequestRepository.IsStalePreSubmission(
                request.StatusTypeName, request.CollectionDate, DateOnly.FromDateTime(DateTime.Today));

        private static readonly TimeZoneInfo EatZone = TimeZoneInfo.FindSystemTimeZoneById("E. Africa Standard Time");

        // Mirrors collection-date-rules.js's getGracePeriodReason, server-side, for display on
        // the Details page. Null unless the grace period is active right now for this request.
        private static string? GetGracePeriodReason(DateOnly collectionDate, PublicHoliday? dayBeforeHoliday)
        {
            var nowEat = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, EatZone);

            if (collectionDate != DateOnly.FromDateTime(nowEat)) return null;
            if (nowEat.TimeOfDay >= TimeSpan.FromHours(11)) return null;

            if (dayBeforeHoliday is not null) return dayBeforeHoliday.Name;

            var dayBefore = collectionDate.AddDays(-1);
            if (dayBefore.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                return "the previous day was a weekend";

            return null;
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

        private List<int> GetUserRoles()
        {
            return User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                    .Select(c => int.TryParse(c.Value.Split(":")[1], out var appRoleId) ? appRoleId : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList();
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
                    DenominationValue = line.DenominationValue,
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

        private async Task CopyDenominationsToBouAsync(
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
                    DenominationValue = line.DenominationValue,
                    DenominationTypeId = line.DenominationTypeId,
                    Bundles = line.Bundles,
                    Boxes = line.Boxes,
                    Amount = line.Amount,
                    Origin = "BOU",
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
            CancellationToken ct,
            string? changedBy = null)
        {
            await _uow.StatusHistory.Create(new WithdrawalRequestStatusHistory
            {
                WithdrawalRequestId = requestId,
                OldStatusId = oldStatusId,
                OldStatusName = oldStatusName,
                NewStatusId = newStatusId,
                NewStatusName = newStatusName,
                // ChangedBy is shown in the Comments timeline, so store the readable
                // username (matching workflow-driven entries). CreatedBy keeps the id for audit.
                ChangedBy = changedBy ?? userId,
                ChangedAt = DateTime.UtcNow,
                Remarks = remarks,
                CreatedBy = userId,
                CreatedDate = DateTime.Now,
            });
        }
        private int GetUserRole()
            => User.FindAll("appRoleId")
            .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
            .Select(c => int.TryParse(c.Value.Split(":")[1], out var roleId) ? roleId : 0)
            .FirstOrDefault(r => r > 0);

        private int GetCbIdFromClaims()
            => int.TryParse(User.Claims
                .FirstOrDefault(c => c.Type == "cbId")?.Value, out var id) ? id : 0;
    }
}

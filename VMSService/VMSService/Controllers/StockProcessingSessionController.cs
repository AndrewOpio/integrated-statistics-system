using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.BoardSessionDifferentDenoms;
using VMSService.Core.Models.BoardSessionSurplusShortages;
using VMSService.Core.Models.StockProcessingResults;
using VMSService.Core.Models.StockProcessingSessions;
using VMSService.Core.Models.StockProcessingSessionStatusHistories;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.CfbBoxes;
using VMSService.Core.Repositories.CommercialBanks;
using VMSService.Core.Repositories.NoteExaminers;
using VMSService.Core.Repositories.StockProcessingResults;
using VMSService.Core.Repositories.StockProcessingSessions;
using VMSService.Core.Repositories.StockProcessingSessionStatusHistories;
using VMSService.Core.Services.Identity;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class StockProcessingSessionController(
        IStockProcessingSessionRepository _sessionRepository,
        IStockProcessingSessionStatusHistoryRepository _sessionStatusHistoryRepository,
        IStockProcessingResultRepository _resultRepository,
        ICommercialBankRepository _commercialBankRepository,
        IAttachmentRepository _attachmentRepository,
        ICfbBoxRepository _cfbBoxRepository,
        INoteExaminerRepository _noteExaminerRepository,
        IConfigurationsServiceClient _configServiceClient,
        IStockService _stockService,
        IIdentityService _identityService,
        IWorkflowService _workflowService,
        IWorkflowServiceClient _workflowServiceClient,
        VMSServiceDbContext _context,
        IWebHostEnvironment _environment,
        ILogger<StockProcessingSessionController> _logger) : Controller
    {
        [HttpGet]
        [Breadcrumb("My Sessions")]
        public async Task<IActionResult> Index(bool showClosed = false)
        {
            try
            {
                ViewBag.ShowClosed = showClosed;

                var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return View(new List<StockProcessingSessionIndexItemViewModel>());

                // Find counting rooms where this user is the assigned board supervisor
                var supervisorRoomPlans = await _context.CountingRoomSessions
                    .Where(crs => crs.BoardSupervisorUserId == username && !crs.IsDeleted)
                    .Select(crs => new { crs.CountingRoomId, crs.StockProcessingPlanId })
                    .ToListAsync();

                if (!supervisorRoomPlans.Any())
                    return View(new List<StockProcessingSessionIndexItemViewModel>());

                var allRoomIds = supervisorRoomPlans.Select(rs => rs.CountingRoomId).Distinct().ToList();
                var allPlanIds = supervisorRoomPlans.Select(rs => rs.StockProcessingPlanId).Distinct().ToList();

                // Map processing boards to their counting rooms
                var boardToRoom = await _context.ProcessingBoards
                    .Where(pb => allRoomIds.Contains(pb.CountingRoomId) && !pb.IsDeleted)
                    .Select(pb => new { pb.Id, pb.CountingRoomId })
                    .ToListAsync();

                var allBoardIds = boardToRoom.Select(b => b.Id).Distinct().ToList();

                // Load sessions assigned to those boards
                var sessions = await _context.StockProcessingSessions
                    .Include(s => s.ProcessingBoard)
                        .ThenInclude(b => b!.BPSMachine)
                    .Where(s => !s.IsDeleted
                                && s.ProcessingBoardId.HasValue
                                && allBoardIds.Contains(s.ProcessingBoardId!.Value)
                                && allPlanIds.Contains(s.StockProcessingPlanId))
                    .ToListAsync();

                // Filter to valid (boardId, planId) pairs so cross-plan contamination is impossible
                var validPairs = new HashSet<(int, int)>(
                    supervisorRoomPlans.SelectMany(rp =>
                        boardToRoom
                            .Where(b => b.CountingRoomId == rp.CountingRoomId)
                            .Select(b => (b.Id, rp.StockProcessingPlanId))));

                sessions = sessions
                    .Where(s => s.ProcessingBoardId.HasValue &&
                                validPairs.Contains((s.ProcessingBoardId.Value, s.StockProcessingPlanId)))
                    .ToList();

                if (!sessions.Any())
                    return View(new List<StockProcessingSessionIndexItemViewModel>());

                // Enrich with lookup data
                var allBanks = await _commercialBankRepository.FindAll();
                var bankDict = allBanks.ToDictionary(b => b.Id, b => b.Name);

                var allDenominations = await _stockService.GetAllDenominations();
                var denomDict = allDenominations.ToDictionary(d => d.Id);

                var planDict = await _context.StockProcessingPlans
                    .Where(p => allPlanIds.Contains(p.Id) && !p.IsDeleted)
                    .Select(p => new { p.Id, p.ProcessingDate, p.ReferenceNumber })
                    .ToDictionaryAsync(p => p.Id);

                var roomIdList = boardToRoom.Select(b => b.CountingRoomId).Distinct().ToList();
                var roomDict = await _context.CountingRooms
                    .Where(cr => roomIdList.Contains(cr.Id) && !cr.IsDeleted)
                    .Select(cr => new { cr.Id, cr.Name })
                    .ToDictionaryAsync(cr => cr.Id, cr => cr.Name);

                var boardRoomDict = boardToRoom.ToDictionary(b => b.Id, b => b.CountingRoomId);

                // Resolve status names (batched by distinct status IDs)
                var statusIds = sessions.Select(s => s.CurrentStatusId).Distinct().ToList();
                var statusNameDict = new Dictionary<int, string?>();
                foreach (var statusId in statusIds)
                {
                    var status = await _workflowServiceClient.FindStatusById(statusId);
                    statusNameDict[statusId] = status?.Name;
                }

                var viewModel = sessions
                    .OrderByDescending(s => planDict.TryGetValue(s.StockProcessingPlanId, out var p) ? p.ProcessingDate : DateTime.MinValue)
                    .ThenBy(s => s.ReferenceNumber)
                    .Select(s =>
                    {
                        var denom = s.DenominationId > 0 ? denomDict.GetValueOrDefault(s.DenominationId) : null;
                        var boardRoomId = s.ProcessingBoardId.HasValue ? boardRoomDict.GetValueOrDefault(s.ProcessingBoardId.Value) : 0;
                        var plan = planDict.GetValueOrDefault(s.StockProcessingPlanId);
                        return new StockProcessingSessionIndexItemViewModel
                        {
                            Id = s.Id,
                            ReferenceNumber = s.ReferenceNumber,
                            StockProcessingPlanId = s.StockProcessingPlanId,
                            PlanReferenceNumber = plan?.ReferenceNumber ?? string.Empty,
                            ProcessingDate = plan?.ProcessingDate ?? s.CreatedDate,
                            ProcessingBoardName = s.ProcessingBoard?.Name,
                            MachineName = s.ProcessingBoard?.BPSMachine?.Name,
                            CountingRoomName = boardRoomId > 0 ? roomDict.GetValueOrDefault(boardRoomId) : null,
                            CommercialBankName = bankDict.GetValueOrDefault(s.CommercialBankId),
                            DenominationValue = denom?.Value ?? 0m,
                            DenominationName = denom?.Name,
                            CurrentStatusName = statusNameDict.GetValueOrDefault(s.CurrentStatusId) ?? string.Empty,
                            TotalIssuedValue = s.TotalIssuedValue,
                            IsManual = s.IsManual,
                            IsCompleted = s.IsCompleted,
                            CreatedDate = s.CreatedDate
                        };
                    }).ToList();

                viewModel = showClosed
                    ? viewModel.Where(s => s.CurrentStatusName == StatusConstants.StockProcessingSession.Closed).ToList()
                    : viewModel.Where(s => s.CurrentStatusName != StatusConstants.StockProcessingSession.Closed).ToList();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Board Supervisor sessions for user {Username}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value);
                TempData["Error"] = "An error occurred while loading your sessions.";
                return View(new List<StockProcessingSessionIndexItemViewModel>());
            }
        }

        [HttpGet]
        [Breadcrumb("Session Details", FromController = typeof(HomeController))]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var session = await _sessionRepository.FindById(id);
                if (session == null) return NotFound();

                var statusType = await _workflowServiceClient.FindStatusById(session.CurrentStatusId);

                var allBanks = await _commercialBankRepository.FindAll();
                var bankDict = allBanks.ToDictionary(b => b.Id, b => b.Name);

                var allDenominations = await _stockService.GetAllDenominations();
                var denomDict = allDenominations.ToDictionary(d => d.Id);

                var allSubCategories = await _stockService.GetAllSubCategories();
                var subCatDict = allSubCategories.ToDictionary(sc => sc.Id, sc => sc.Name);

                var bagIds = session.ProcessingSessionBags
                    .Where(psb => !psb.IsDeleted)
                    .Select(psb => psb.BagId)
                    .ToList();

                // Denomination data per bag (for value / bundle counts)
                var bagDenomData = await _context.BagDenominations
                    .Where(bd => bagIds.Contains(bd.BagId) && !bd.IsDeleted)
                    .Select(bd => new
                    {
                        bd.BagId,
                        bd.DepositRequestDenomination.DenominationValue,
                        bd.NumberOfBundlesOrBoxes
                    })
                    .ToListAsync();

                var denomDataByBagId = bagDenomData
                    .GroupBy(x => x.BagId)
                    .ToDictionary(g => g.Key, g => new
                    {
                        DenominationValue = g.Select(x => x.DenominationValue).FirstOrDefault(),
                        TotalBundles = g.Sum(x => x.NumberOfBundlesOrBoxes),
                        TotalAmount = g.Sum(x => x.DenominationValue * 1000 * x.NumberOfBundlesOrBoxes)
                    });

                // SubCategory per bag (own, or fall back to parent bag)
                var bagSubCatById = await _context.Bags
                    .Where(b => bagIds.Contains(b.Id) && !b.IsDeleted)
                    .Select(b => new { b.Id, b.SubCategoryId })
                    .ToDictionaryAsync(b => b.Id, b => b.SubCategoryId);

                var bagsWithNoSubCat = bagSubCatById
                    .Where(kvp => !kvp.Value.HasValue).Select(kvp => kvp.Key).ToList();
                if (bagsWithNoSubCat.Count > 0)
                {
                    var parentSubCats = await _context.BaggedBags
                        .Where(bb => bagsWithNoSubCat.Contains(bb.ChildBagId) && !bb.IsDeleted)
                        .Join(_context.Bags, bb => bb.ParentBagId, b => b.Id,
                              (bb, b) => new { bb.ChildBagId, b.SubCategoryId })
                        .ToListAsync();
                    foreach (var item in parentSubCats.Where(x => x.SubCategoryId.HasValue))
                        bagSubCatById[item.ChildBagId] = item.SubCategoryId;
                }

                var bags = session.ProcessingSessionBags
                    .Where(psb => !psb.IsDeleted)
                    .Select(psb =>
                    {
                        var data = denomDataByBagId.GetValueOrDefault(psb.BagId);
                        var subCatId = bagSubCatById.GetValueOrDefault(psb.BagId);
                        return new SessionBagDetailsInfo
                        {
                            BagId = psb.BagId,
                            ReferenceNumber = psb.Bag?.ReferenceNumber ?? psb.BagId.ToString(),
                            SubCategoryName = subCatId.HasValue ? subCatDict.GetValueOrDefault(subCatId.Value) : null,
                            DenominationValue = data?.DenominationValue ?? 0m,
                            TotalBundles = data?.TotalBundles ?? 0,
                            TotalAmount = data?.TotalAmount ?? 0m
                        };
                    }).ToList();

                // Available workflow transitions for this user
                var userRoleIds = User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                    .Select(c => int.TryParse(c.Value.Split(":")[1], out var rid) ? rid : (int?)null)
                    .Where(r => r.HasValue).Select(r => r!.Value).ToList();

                var availableTransitions = new List<WorkflowTransitionDto>();
                foreach (var roleId in userRoleIds)
                {
                    var transitions = await _workflowServiceClient.GetAvailableTransitions(new GetAvailableTransitionsRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName = StatusConstants.Entity.StockProcessingSession,
                        CurrentStatusId = session.CurrentStatusId,
                        AppRole = roleId,
                    });
                    if (transitions != null) availableTransitions.AddRange(transitions);
                }

                // Status history
                var historyEntities = await _sessionStatusHistoryRepository.FindByStockProcessingSessionId(id);
                var statusHistory = new List<StockProcessingSessionStatusHistoryViewModel>();
                foreach (var h in historyEntities)
                {
                    var oldStatus = await _workflowServiceClient.FindStatusById(h.OldStatusId);
                    var newStatus = await _workflowServiceClient.FindStatusById(h.NewStatusId);
                    var historyVm = new StockProcessingSessionStatusHistoryViewModel
                    {
                        Id = h.Id,
                        StockProcessingSessionId = h.StockProcessingSessionId,
                        OldStatusId = h.OldStatusId,
                        OldStatusName = oldStatus?.Name ?? h.OldStatusId.ToString(),
                        NewStatusId = h.NewStatusId,
                        NewStatusName = newStatus?.Name ?? h.NewStatusId.ToString(),
                        ChangedBy = h.ChangedBy,
                        ChangedAt = h.ChangedAt,
                        Remarks = h.Remarks,
                        Attachments = h.Attachments
                    };

                    if (!string.IsNullOrEmpty(h.Attachments))
                    {
                        var attachmentIds = h.Attachments.Split(',')
                            .Where(a => int.TryParse(a.Trim(), out _))
                            .Select(a => int.Parse(a.Trim())).ToList();

                        var attachmentFiles = new List<AttachmentViewModel>();
                        foreach (var attachmentId in attachmentIds)
                        {
                            var attachment = await _attachmentRepository.FindById(attachmentId);
                            if (attachment != null && !attachment.IsDeleted)
                            {
                                attachmentFiles.Add(new AttachmentViewModel
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
                        historyVm.AttachmentFiles = attachmentFiles;
                    }

                    statusHistory.Add(historyVm);
                }

                // All entity attachments
                var allAttachmentEntities = await _attachmentRepository.FindByEntityId(id.ToString());
                var allAttachments = allAttachmentEntities
                    .Where(a => !a.IsDeleted && a.EntityType == StatusConstants.Entity.StockProcessingSession)
                    .Select(a => new AttachmentViewModel
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        FilePath = a.FilePath,
                        FileType = a.FileType,
                        FileSize = a.FileSize,
                        UploadedDate = a.CreatedDate
                    }).ToList();

                var denom = denomDict.GetValueOrDefault(session.DenominationId);

                // Counting room and supervisor for the processing board
                string? countingRoomName = null;
                string? supervisorName = null;
                string? supervisorId = null;
                if (session.ProcessingBoard != null)
                {
                    var countingRoomId = session.ProcessingBoard.CountingRoomId;

                    var countingRoom = await _context.CountingRooms
                        .FirstOrDefaultAsync(cr => cr.Id == countingRoomId && !cr.IsDeleted);
                    countingRoomName = countingRoom?.Name;

                    var roomSession = await _context.CountingRoomSessions
                        .FirstOrDefaultAsync(crs => crs.StockProcessingPlanId == session.StockProcessingPlanId
                            && crs.CountingRoomId == countingRoomId
                            && !crs.IsDeleted);

                    if (!string.IsNullOrEmpty(roomSession?.BoardSupervisorUserId))
                    {
                        try
                        {
                            supervisorId = roomSession.BoardSupervisorUserId;
                            var user = await _identityService.GetUserByUsername(roomSession.BoardSupervisorUserId);
                            supervisorName = user != null
                                ? $"{user.FirstName} {user.LastName}".Trim()
                                : roomSession.BoardSupervisorUserId;
                        }
                        catch
                        {
                            supervisorName = roomSession.BoardSupervisorUserId;
                        }
                    }
                }

                // Note Examiners captured for this session (manual boards)
                var branchId = session.ProcessingBoard?.BranchId ?? 0;

                var noteExaminerEntities = await _context.StockProcessingSessionNoteExaminers
                    .Include(ne => ne.NoteExaminer)
                    .Where(ne => ne.StockProcessingSessionId == id && !ne.IsDeleted)
                    .OrderBy(ne => ne.CreatedDate)
                    .ToListAsync();

                var noteExaminers = noteExaminerEntities.Select(ne => new SessionNoteExaminerViewModel
                {
                    Id = ne.Id,
                    NoteExaminerId = ne.NoteExaminerId,
                    NoteExaminerName = ne.NoteExaminer != null
                        ? $"{ne.NoteExaminer.FirstName} {ne.NoteExaminer.LastName}".Trim()
                        : $"Examiner {ne.NoteExaminerId}",
                    NumberOfBundles = ne.NumberOfBundles,
                    CreatedDate = ne.CreatedDate
                }).ToList();

                var availableNoteExaminers = (await _noteExaminerRepository.FindAll())
                    .Where(ne => ne.BranchId == branchId && ne.IsActive)
                    .OrderBy(ne => ne.FirstName).ThenBy(ne => ne.LastName)
                    .Select(ne => new NoteExaminerOptionViewModel
                    {
                        Id = ne.Id,
                        Name = string.IsNullOrWhiteSpace(ne.MiddleName)
                            ? $"{ne.FirstName} {ne.LastName}"
                            : $"{ne.FirstName} {ne.MiddleName} {ne.LastName}"
                    })
                    .ToList();

                // Existing processing results for this session
                var resultEntities = await _resultRepository.FindBySessionIdAsync(id);
                var results = resultEntities.Select(r => new StockProcessingResultViewModel
                {
                    Id = r.Id,
                    StockProcessingSessionId = r.StockProcessingSessionId,
                    Count = r.Count,
                    FitShredValue = r.FitShredValue,
                    CanceledValue = r.CanceledValue,
                    ReissueValue = r.ReissueValue,
                    UnProcessedValue = r.UnProcessedValue,
                    IsVerified = r.IsVerified,
                    IsDeleted = r.IsDeleted,
                    CreatedDate = r.CreatedDate,
                    CreatedBy = r.CreatedBy
                }).ToList();

                // Different denominations found during processing
                var differentDenomEntities = await _context.BoardSessionDifferentDenoms
                    .Where(d => d.StockProcessingSessionId == id)
                    .ToListAsync();

                var differentDenoms = differentDenomEntities.Select(d =>
                {
                    var dd = denomDict.GetValueOrDefault(d.DenominationId);
                    return new BoardSessionDifferentDenomViewModel
                    {
                        Id = d.Id,
                        DenominationId = d.DenominationId,
                        DenominationName = dd?.Name,
                        DenominationValue = dd?.Value ?? 0m,
                        NumberOfPieces = d.NumberOfPieces,
                        Amount = d.Amount
                    };
                }).ToList();

                // Surplus/shortage records for this session
                var surplusShortageEntities = await _context.BoardSessionSurplusShortages
                    .Where(s => s.StockProcessingSessionId == id)
                    .OrderByDescending(s => s.CreatedDate)
                    .ToListAsync();

                var surplusShortageStatusIds = surplusShortageEntities.Select(s => s.CurrentStatusId).Distinct().ToList();
                var surplusShortageStatusNameDict = new Dictionary<int, string?>();
                foreach (var statusId in surplusShortageStatusIds)
                {
                    var status = await _workflowServiceClient.FindStatusById(statusId);
                    surplusShortageStatusNameDict[statusId] = status?.Name;
                }

                var surplusShortages = surplusShortageEntities.Select(s =>
                {
                    var sd = denomDict.GetValueOrDefault(s.DenominationId);
                    return new BoardSessionSurplusShortageViewModel
                    {
                        Id = s.Id,
                        Amount = s.Amount,
                        IsSurplus = s.IsSurplus,
                        DenominationId = s.DenominationId,
                        DenominationName = sd?.Name,
                        DenominationValue = sd?.Value ?? 0m,
                        CommercialBankId = s.CommercialBankId,
                        CommercialBankName = bankDict.GetValueOrDefault(s.CommercialBankId),
                        CurrentStatusId = s.CurrentStatusId,
                        CurrentStatusName = surplusShortageStatusNameDict.GetValueOrDefault(s.CurrentStatusId),
                        CreatedDate = s.CreatedDate
                    };
                }).ToList();

                ViewBag.Denominations = allDenominations
                    .OrderBy(d => d.DenominationTypeId)
                    .ThenBy(d => d.Value)
                    .ToList();

                var model = new StockProcessingSessionDetailsViewModel
                {
                    Id = session.Id,
                    ReferenceNumber = session.ReferenceNumber,
                    StockProcessingPlanId = session.StockProcessingPlanId,
                    CommercialBankId = session.CommercialBankId,
                    CommercialBankName = bankDict.GetValueOrDefault(session.CommercialBankId),
                    DenominationId = session.DenominationId,
                    DenominationName = denom?.Name,
                    DenominationValue = denom?.Value ?? 0m,
                    ProcessingBoardId = session.ProcessingBoardId,
                    ProcessingBoardName = session.ProcessingBoard?.Name,
                    MachineName = session.ProcessingBoard?.BPSMachine?.Name,
                    CountingRoomName = countingRoomName,
                    SupervisorName = supervisorName,
                    CurrentStatusId = session.CurrentStatusId,
                    CurrentStatusName = statusType?.Name ?? string.Empty,
                    TotalIssuedValue = session.TotalIssuedValue,
                    IsManual = session.IsManual,
                    IsCompleted = session.IsCompleted,
                    IsDeleted = session.IsDeleted,
                    CreatedDate = session.CreatedDate,
                    CreatedBy = session.CreatedBy,
                    Bags = bags,
                    Results = results,
                    DifferentDenoms = differentDenoms,
                    SurplusShortages = surplusShortages,
                    StatusHistory = statusHistory,
                    AvailableTransitions = availableTransitions.Distinct().ToList(),
                    AllAttachments = allAttachments,
                    BoardSupervisorUserId = supervisorId ?? "",
                    NoteExaminers = noteExaminers,
                    AvailableNoteExaminers = availableNoteExaminers
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock processing session details for session {Id}", id);
                TempData["Error"] = "An error occurred while retrieving session details.";
                return RedirectToAction(nameof(Index), "StockProcessingPlan");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(int id, string actionName, string remarks)
        {
            try
            {
                var session = await _sessionRepository.FindById(id);
                if (session == null)
                {
                    TempData["Error"] = "Processing Session not found.";
                    return RedirectToAction(nameof(Index), "StockProcessingPlan");
                }

                if (actionName == "SubmitResults")
                {
                    var existingResults = await _resultRepository.FindBySessionIdAsync(id);
                    if (!existingResults.Any(r => !r.IsVerified))
                    {
                        TempData["Error"] = "No processing results have been saved for this session. Please save results before submitting.";
                        return RedirectToAction(nameof(Details), new { id });
                    }

                    if (session.IsManual)
                    {
                        var hasNoteExaminers = await _context.StockProcessingSessionNoteExaminers
                            .AnyAsync(ne => ne.StockProcessingSessionId == id && !ne.IsDeleted);
                        if (!hasNoteExaminers)
                        {
                            TempData["Error"] = "Please capture the Note Examiners who worked this board, and the bundles each processed, before submitting results.";
                            return RedirectToAction(nameof(Details), new { id });
                        }
                    }
                }

                var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "System";
                var userRoles = User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                    .Select(c => int.TryParse(c.Value.Split(":")[1], out var rid) ? rid : (int?)null)
                    .Where(r => r.HasValue).Select(r => r!.Value).ToList();

                var transition = await _workflowServiceClient.FindByTransitionKey(new GetTransitionByKeyRequestDto
                {
                    EntityTypeName = StatusConstants.Entity.StockProcessingSession,
                    FromStatusId = session.CurrentStatusId,
                    ActionName = actionName,
                });

                List<long>? uploadedAttachmentIds = null;
                if (transition != null && transition.RequiresAttachments && transition.AttachmentRequirements.Any())
                {
                    uploadedAttachmentIds = await HandleAttachmentUploads(
                        transition.AttachmentRequirements.ToList(),
                        transition.Id,
                        id,
                        StatusConstants.Entity.StockProcessingSession,
                        username);

                    var requiredAttachments = transition.AttachmentRequirements.Where(r => r.IsRequired).ToList();
                    if (requiredAttachments.Any())
                    {
                        var uploadedRequirementIds = uploadedAttachmentIds?
                            .Select(_ => transition.AttachmentRequirements
                                .FirstOrDefault(r => Request.Form.Files[$"attachments_{r.Id}"]?.Length > 0)?.Id ?? 0)
                            .Where(rid => rid > 0).ToList();

                        var missing = requiredAttachments
                            .Where(r => !(uploadedRequirementIds ?? []).Contains(r.Id)).ToList();

                        if (missing.Any())
                        {
                            TempData["Error"] = $"Missing required attachments: {string.Join(", ", missing.Select(r => r.AttachmentName))}";
                            return RedirectToAction(nameof(Details), new { id });
                        }
                    }
                }

                bool isExecuted = await _workflowService.ExecuteTransition(
                    StatusConstants.Entity.StockProcessingSession,
                    id,
                    session.CurrentStatusId,
                    actionName,
                    username,
                    userRoles,
                    remarks,
                    uploadedAttachmentIds);

                if (isExecuted)
                {
                    var attachmentMessage = uploadedAttachmentIds?.Any() == true
                        ? $" {uploadedAttachmentIds.Count} attachment(s) uploaded."
                        : "";
                    TempData["Success"] = $"Action completed successfully.{attachmentMessage}";

                    if (actionName == "SubmitResults")
                    {
                        var sessionResults = await _resultRepository.FindBySessionIdAsync(id);
                        if (sessionResults.Count > 0 && sessionResults.OrderByDescending(r => r.CreatedDate)?.FirstOrDefault()?.UnProcessedValue == 0)
                        {
                            session.IsCompleted = true;
                            await _context.SaveChangesAsync();
                        }

                        try
                        {
                            var closedStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                            {
                                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                                EntityTypeName = StatusConstants.Entity.StockProcessingSession,
                                StatusName = StatusConstants.StockProcessingSession.Closed
                            });

                            if (closedStatus != null)
                            {
                                var allPlanSessions = await _context.StockProcessingSessions
                                    .Where(s => s.StockProcessingPlanId == session.StockProcessingPlanId && !s.IsDeleted)
                                    .ToListAsync();

                                if (allPlanSessions.Any() && allPlanSessions.All(s => s.CurrentStatusId == closedStatus.Id))
                                {
                                    var targetStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                                    {
                                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                                        EntityTypeName = StatusConstants.Entity.StockProcessingPlan,
                                        StatusName = StatusConstants.StockProcessingPlan.ResultsPendingVerification
                                    });

                                    if (targetStatus != null)
                                    {
                                        var plan = await _context.StockProcessingPlans
                                            .FirstOrDefaultAsync(p => p.Id == session.StockProcessingPlanId && !p.IsDeleted);

                                        if (plan != null)
                                        {
                                            plan.CurrentStatusId = targetStatus.Id;
                                            await _context.SaveChangesAsync();
                                            _logger.LogInformation(
                                                "Stock Processing Plan {PlanId} advanced to '{Status}' — all sessions closed.",
                                                plan.Id, StatusConstants.StockProcessingPlan.ResultsPendingVerification);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error advancing plan status after SubmitResults for session {Id}", id);
                        }
                    }
                }
                else
                {
                    TempData["Error"] = $"Failed to execute {actionName} action. You may not have permission or the action is not valid for the current status.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing action {Action} on Processing Session {Id}", actionName, id);
                TempData["Error"] = "An error occurred while executing the action.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveResult(SubmitProcessingResultViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .Where(m => !string.IsNullOrEmpty(m))
                        .ToList();
                    TempData["ResultErrors"] = System.Text.Json.JsonSerializer.Serialize(errors);
                    return RedirectToAction(nameof(Details), new { id = model.SessionId });
                }

                var session = await _sessionRepository.FindById(model.SessionId);
                if (session == null)
                {
                    TempData["Error"] = "Processing Session not found.";
                    return RedirectToAction(nameof(Index));
                }

                var allDenominationsForResult = await _stockService.GetAllDenominations();
                var denomDictForResult = allDenominationsForResult.ToDictionary(d => d.Id);
                var denominationValue = denomDictForResult.GetValueOrDefault(session.DenominationId)?.Value ?? 0m;

                var fitShredValue = session.IsManual ? 0m : model.FitShredValue;
                decimal processedTotal = model.CanceledValue + model.ReissueValue + fitShredValue;
                decimal unProcessedValue = session.TotalIssuedValue - processedTotal;

                if (unProcessedValue < 0)
                {
                    TempData["ResultErrors"] = System.Text.Json.JsonSerializer.Serialize(new List<string>
                    {
                        "Reissue, Canceled" + (session.IsManual ? "" : " and Fit/Shred") +
                        " values cannot exceed the Total Issued Value — Unprocessed Value cannot be less than 0."
                    });
                    return RedirectToAction(nameof(Details), new { id = model.SessionId });
                }

                var result = new StockProcessingResult
                {
                    StockProcessingSessionId = model.SessionId,
                    Count = denominationValue > 0 ? (int)Math.Round(processedTotal / denominationValue) : 0,
                    UnProcessedValue = unProcessedValue,
                    FitShredValue = fitShredValue,
                    CanceledValue = model.CanceledValue,
                    ReissueValue = model.ReissueValue,
                };

                await _resultRepository.Create(result);

                // Stage denom entries
                var validDenomEntries = model.DifferentDenomEntries
                    .Where(e => e.DenominationId > 0 && e.NumberOfPieces > 0)
                    .ToList();

                if (validDenomEntries.Count > 0)
                {
                    foreach (var entry in validDenomEntries)
                    {
                        denomDictForResult.TryGetValue(entry.DenominationId, out var denom);
                        _context.BoardSessionDifferentDenoms.Add(new BoardSessionDifferentDenom
                        {
                            StockProcessingSessionId = model.SessionId,
                            DenominationId = entry.DenominationId,
                            NumberOfPieces = entry.NumberOfPieces,
                            Amount = (denom?.Value ?? 0m) * entry.NumberOfPieces
                        });
                    }

                    await _context.SaveChangesAsync();
                }

                // Stage note examiner entries (manual boards only)
                if (session.IsManual)
                {
                    var validNoteExaminerEntries = model.NoteExaminerEntries
                        .Where(e => e.NoteExaminerId > 0 && e.NumberOfBundles > 0)
                        .ToList();

                    if (validNoteExaminerEntries.Count > 0)
                    {
                        foreach (var entry in validNoteExaminerEntries)
                        {
                            _context.StockProcessingSessionNoteExaminers.Add(new StockProcessingSessionNoteExaminer
                            {
                                StockProcessingSessionId = model.SessionId,
                                NoteExaminerId = entry.NoteExaminerId,
                                NumberOfBundles = entry.NumberOfBundles,
                                IsDeleted = false
                            });
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                TempData["ResultSuccess"] = "Processing result saved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving result for Processing Session {Id}", model.SessionId);
                TempData["Error"] = "An error occurred while saving the result.";
            }

            return RedirectToAction(nameof(Details), new { id = model.SessionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditResult(SubmitProcessingResultViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .Where(m => !string.IsNullOrEmpty(m))
                        .ToList();
                    TempData["ResultErrors"] = System.Text.Json.JsonSerializer.Serialize(errors);
                    return RedirectToAction(nameof(Details), new { id = model.SessionId });
                }

                var result = await _resultRepository.FindById(model.Id);
                if (result == null)
                {
                    TempData["Error"] = "Processing result not found.";
                    return RedirectToAction(nameof(Details), new { id = model.SessionId });
                }

                if (result.IsVerified)
                {
                    TempData["Error"] = "Verified results cannot be edited.";
                    return RedirectToAction(nameof(Details), new { id = model.SessionId });
                }

                var session = await _sessionRepository.FindById(model.SessionId);
                if (session == null)
                {
                    TempData["Error"] = "Processing Session not found.";
                    return RedirectToAction(nameof(Index));
                }

                var allDenominationsForEdit = await _stockService.GetAllDenominations();
                var denomDictForEdit = allDenominationsForEdit.ToDictionary(d => d.Id);
                var denominationValue = denomDictForEdit.GetValueOrDefault(session.DenominationId)?.Value ?? 0m;

                var fitShredValue = session.IsManual ? 0m : model.FitShredValue;
                decimal processedTotal = model.CanceledValue + model.ReissueValue + fitShredValue;
                decimal unProcessedValue = session.TotalIssuedValue - processedTotal;

                if (unProcessedValue < 0)
                {
                    TempData["ResultErrors"] = System.Text.Json.JsonSerializer.Serialize(new List<string>
                    {
                        "Reissue, Canceled" + (session.IsManual ? "" : " and Fit/Shred") +
                        " values cannot exceed the Total Issued Value — Unprocessed Value cannot be less than 0."
                    });
                    return RedirectToAction(nameof(Details), new { id = model.SessionId });
                }

                result.Count = denominationValue > 0 ? (int)Math.Round(processedTotal / denominationValue) : 0;
                result.UnProcessedValue = unProcessedValue;
                result.FitShredValue = fitShredValue;
                result.CanceledValue = model.CanceledValue;
                result.ReissueValue = model.ReissueValue;

                await _resultRepository.Update(result);

                // Replace denom entries
                var existingDenoms = await _context.BoardSessionDifferentDenoms
                    .Where(d => d.StockProcessingSessionId == model.SessionId)
                    .ToListAsync();
                _context.BoardSessionDifferentDenoms.RemoveRange(existingDenoms);

                var validDenomEntries = model.DifferentDenomEntries
                    .Where(e => e.DenominationId > 0 && e.NumberOfPieces > 0)
                    .ToList();

                if (validDenomEntries.Count > 0)
                {
                    foreach (var entry in validDenomEntries)
                    {
                        denomDictForEdit.TryGetValue(entry.DenominationId, out var denom);
                        _context.BoardSessionDifferentDenoms.Add(new BoardSessionDifferentDenom
                        {
                            StockProcessingSessionId = model.SessionId,
                            DenominationId = entry.DenominationId,
                            NumberOfPieces = entry.NumberOfPieces,
                            Amount = (denom?.Value ?? 0m) * entry.NumberOfPieces
                        });
                    }
                }

                await _context.SaveChangesAsync();
                TempData["ResultSuccess"] = "Processing result updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing result {ResultId} for Processing Session {Id}", model.Id, model.SessionId);
                TempData["Error"] = "An error occurred while updating the result.";
            }

            return RedirectToAction(nameof(Details), new { id = model.SessionId });
        }

        [HttpGet]
        public async Task<IActionResult> SendCfbRequest(int sessionId)
        {
            var session = await _sessionRepository.FindById(sessionId);
            if (session == null)
            {
                TempData["Error"] = "Processing Session not found.";
                return RedirectToAction(nameof(Index));
            }

            if (!await CanSendCfbRequestAsync(session))
            {
                TempData["Error"] = "A CFB request can only be sent while the session is Open and only if one hasn't already been sent.";
                return RedirectToAction(nameof(Details), new { id = sessionId });
            }

            var branchId = session.ProcessingBoard?.BranchId ?? 0;

            var allDenominations = await _stockService.GetAllDenominations();
            var denomination = allDenominations.FirstOrDefault(d => d.Id == session.DenominationId);

            var allBanks = await _commercialBankRepository.FindAll();
            var bank = allBanks.FirstOrDefault(b => b.Id == session.CommercialBankId);

            var cfbBox = await FindCfbBoxForSessionAsync(branchId, denomination?.DenominationType?.Name);
            if (cfbBox == null)
            {
                TempData["Error"] = $"No CFB box is configured for this branch{(denomination?.DenominationType?.Name != null ? $" ({denomination.DenominationType.Name})" : "")}. Please contact CFB administration.";
                return RedirectToAction(nameof(Details), new { id = sessionId });
            }

            var pendingCfbStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                EntityTypeName = StatusConstants.Entity.BoardSessionSurplusShortage,
                StatusName = StatusConstants.BoardSessionSurplusShortage.PendingCFBSupervisor
            });

            if (pendingCfbStatus == null)
            {
                TempData["Error"] = "CFB request workflow status is not configured. Please contact an administrator.";
                return RedirectToAction(nameof(Details), new { id = sessionId });
            }

            string? branchName = null;
            try
            {
                var branch = await _configServiceClient.GetBouBranchByIdAsync(branchId);
                branchName = branch?.Name;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not resolve branch name {BranchId} for CFB request form", branchId);
            }

            var model = new CfbRequestViewModel
            {
                StockProcessingSessionId = session.Id,
                SessionReferenceNumber = session.ReferenceNumber,
                CommercialBankId = session.CommercialBankId,
                CommercialBankName = bank?.Name,
                BranchId = branchId,
                BranchName = branchName,
                DenominationId = session.DenominationId,
                DenominationName = denomination?.Name,
                DenominationValue = denomination?.Value ?? 0m,
                TotalIssuedValue = session.TotalIssuedValue,
                CfbBoxId = cfbBox.Id,
                CurrentStatusId = pendingCfbStatus.Id,
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCfbRequest(CfbRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please correct the errors and try again.";
                return RedirectToAction(nameof(SendCfbRequest), new { sessionId = model.StockProcessingSessionId });
            }

            try
            {
                var session = await _sessionRepository.FindById(model.StockProcessingSessionId);
                if (session == null)
                {
                    TempData["Error"] = "Processing Session not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (!await CanSendCfbRequestAsync(session))
                {
                    TempData["Error"] = "A CFB request can only be sent while the session is Open and only if one hasn't already been sent.";
                    return RedirectToAction(nameof(Details), new { id = model.StockProcessingSessionId });
                }

                // Re-derive the autopopulated fields server-side rather than trusting the posted hidden values.
                var branchId = session.ProcessingBoard?.BranchId ?? 0;

                var allDenominations = await _stockService.GetAllDenominations();
                var denomination = allDenominations.FirstOrDefault(d => d.Id == session.DenominationId);

                var cfbBox = await FindCfbBoxForSessionAsync(branchId, denomination?.DenominationType?.Name);
                if (cfbBox == null)
                {
                    TempData["Error"] = "No CFB box is configured for this branch. Please contact CFB administration.";
                    return RedirectToAction(nameof(Details), new { id = model.StockProcessingSessionId });
                }

                var pendingCfbStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    EntityTypeName = StatusConstants.Entity.BoardSessionSurplusShortage,
                    StatusName = StatusConstants.BoardSessionSurplusShortage.PendingCFBSupervisor
                });

                if (pendingCfbStatus == null)
                {
                    TempData["Error"] = "CFB request workflow status is not configured. Please contact an administrator.";
                    return RedirectToAction(nameof(Details), new { id = model.StockProcessingSessionId });
                }

                var entity = new BoardSessionSurplusShortage
                {
                    StockProcessingSessionId = session.Id,
                    CommercialBankId = session.CommercialBankId,
                    BranchId = branchId,
                    DenominationId = session.DenominationId,
                    CfbBoxId = cfbBox.Id,
                    CurrentStatusId = pendingCfbStatus.Id,
                    Amount = model.Amount,
                    IsSurplus = model.IsSurplus,
                    IsDifferentDenomination = model.IsDifferentDenomination,
                    IsProcessedOnRtgs = false,
                    IsDeleted = false,
                };

                _context.BoardSessionSurplusShortages.Add(entity);
                await _context.SaveChangesAsync();

                TempData["Success"] = "CFB request sent successfully.";
                return RedirectToAction(nameof(Details), new { id = session.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending CFB request for Processing Session {Id}", model.StockProcessingSessionId);
                TempData["Error"] = "An error occurred while sending the CFB request.";
                return RedirectToAction(nameof(Details), new { id = model.StockProcessingSessionId });
            }
        }

        private async Task<CfbBox?> FindCfbBoxForSessionAsync(int branchId, string? boxType)
        {
            var branchBoxes = await _cfbBoxRepository.FindByBranchId(branchId);
            return branchBoxes.FirstOrDefault(b => b.BoxType == boxType);
        }

        private async Task<bool> CanSendCfbRequestAsync(StockProcessingSession session)
        {
            var statusType = await _workflowServiceClient.FindStatusById(session.CurrentStatusId);
            if (statusType?.Name != StatusConstants.StockProcessingSession.Open)
                return false;

            var hasExistingRequest = await _context.BoardSessionSurplusShortages
                .AnyAsync(s => s.StockProcessingSessionId == session.Id);

            return !hasExistingRequest;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDifferentDenoms(SaveDifferentDenomsViewModel model)
        {
            try
            {
                var validEntries = model.Entries
                    .Where(e => e.DenominationId > 0 && e.NumberOfPieces > 0)
                    .ToList();

                if (validEntries.Count == 0)
                {
                    TempData["Error"] = "Please add at least one denomination entry with a valid denomination and piece count.";
                    return RedirectToAction(nameof(Details), new { id = model.SessionId });
                }

                var allDenominations = await _stockService.GetAllDenominations();
                var denomDict = allDenominations.ToDictionary(d => d.Id);

                foreach (var entry in validEntries)
                {
                    denomDict.TryGetValue(entry.DenominationId, out var denom);
                    _context.BoardSessionDifferentDenoms.Add(new BoardSessionDifferentDenom
                    {
                        StockProcessingSessionId = model.SessionId,
                        DenominationId = entry.DenominationId,
                        NumberOfPieces = entry.NumberOfPieces,
                        Amount = (denom?.Value ?? 0m) * entry.NumberOfPieces
                    });
                }

                await _context.SaveChangesAsync();
                TempData["DifferentDenomSuccess"] = $"{validEntries.Count} different denomination entr{(validEntries.Count == 1 ? "y" : "ies")} saved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving different denoms for session {Id}", model.SessionId);
                TempData["Error"] = "An error occurred while saving the denomination entries.";
            }

            return RedirectToAction(nameof(Details), new { id = model.SessionId });
        }

        private async Task<List<long>> HandleAttachmentUploads(
            List<TransitionAttachmentRequirementDto> requirements,
            int workflowTransitionId,
            int entityId,
            string entityType,
            string username)
        {
            var uploadedAttachmentIds = new List<long>();
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "stockprocessingsession", entityId.ToString());
            Directory.CreateDirectory(uploadsPath);

            foreach (var requirement in requirements)
            {
                var file = Request.Form.Files[$"attachments_{requirement.Id}"];
                if (file == null || file.Length == 0) continue;

                try
                {
                    var fileExtension = Path.GetExtension(file.FileName).TrimStart('.').ToLower();
                    var allowedFormats = requirement.AllowedFormats.Split(',')
                        .Select(f => f.Trim().ToLower()).ToList();

                    var formatMap = new Dictionary<string, List<string>>
                    {
                        { "excel", ["xls", "xlsx"] },
                        { "word",  ["doc", "docx"] },
                        { "docx",  ["doc", "docx"] },
                        { "jpg",   ["jpg", "jpeg"] },
                        { "jpeg",  ["jpg", "jpeg"] }
                    };

                    bool isValid = allowedFormats.Any(fmt =>
                        formatMap.TryGetValue(fmt, out var exts) ? exts.Contains(fileExtension) : fmt == fileExtension);

                    if (!isValid)
                    {
                        _logger.LogWarning("Invalid file format {Ext} for requirement {Id}", fileExtension, requirement.Id);
                        continue;
                    }

                    var uniqueName = $"{requirement.Id}_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileNameWithoutExtension(file.FileName)}.{fileExtension}";
                    var filePath = Path.Combine(uploadsPath, uniqueName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await file.CopyToAsync(stream);

                    var attachment = new Attachment
                    {
                        FileName = file.FileName,
                        FilePath = Path.Combine("uploads", "stockprocessingsession", entityId.ToString(), uniqueName),
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
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading attachment for requirement {Id}", requirement.Id);
                }
            }

            return uploadedAttachmentIds;
        }
    }
}

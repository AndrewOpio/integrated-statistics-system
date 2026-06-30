using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.Bags;
using VMSService.Core.Models.StockProcessingPlans;
using VMSService.Core.Repositories.Bags;
using VMSService.Core.Repositories.CountingRooms;
using VMSService.Core.Repositories.ProcessingBoards;
using VMSService.Core.Repositories.CommercialBanks;
using VMSService.Core.Repositories.StockProcessingPlans;
using VMSService.Core.Repositories.StockProcessingSessions;
using VMSService.Core.Static;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class StockProcessingPlanController(
        IStockProcessingPlanRepository _planRepository,
        IConfigurationsServiceClient _configServiceClient,
        ICountingRoomRepository _countingRoomRepository,
        IProcessingBoardRepository _processingBoardRepository,
        IBagRepository _bagRepository,
        IStockProcessingSessionRepository _sessionRepository,
        ICommercialBankRepository _commercialBankRepository,
        VMSServiceDbContext _context,
        IMapper _mapper,
        ILogger<StockProcessingPlanController> _logger,
        IWorkflowServiceClient _workflowServiceClient) : Controller
    {
        [Breadcrumb("Processing Plans", FromController = typeof(HomeController), FromAction = nameof(Index))]
        public async Task<IActionResult> Index(int? branchId = null)
        {
            try
            {
                var allBouBranches = await _configServiceClient.GetAllBouBranchesAsync();

                var userRoles = User.FindAll("appRole")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                    .Select(c => c.Value.Split(":")[1])
                    .ToList();

                List<BranchDto> userBranches = allBouBranches
                    .Where(branch => userRoles.Any(role => role.Contains(branch.Name)))
                    .ToList();

                if (branchId == null)
                    branchId = userBranches.FirstOrDefault()?.Id ?? 0;

                var plans = await _planRepository.FindAll();
                var filtered = plans
                    .Where(p => branchId == 0 || p.BranchId == branchId)
                    .ToList();

                var model = _mapper.Map<List<StockProcessingPlanViewModel>>(filtered);

                var branchDict = allBouBranches.ToDictionary(b => b.Id, b => b.Name);
                foreach (var item in model)
                {
                    if (branchDict.TryGetValue(item.BranchId, out var name))
                        item.BranchName = name;

                    var statusType = await _workflowServiceClient.FindStatusById(item.CurrentStatusId);
                    item.CurrentStatusName = statusType.Name;
                }

                ViewBag.Branches = userBranches;
                ViewBag.SelectedBranchId = branchId;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock processing plans");
                TempData["Error"] = "An error occurred while retrieving processing plans. Please try again.";
                return View(new List<StockProcessingPlanViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUsedDates(int branchId)
        {
            var plans = await _planRepository.FindAll();
            var usedDates = plans
                .Where(p => p.BranchId == branchId)
                .Select(p => p.ProcessingDate.Date.ToString("yyyy-MM-dd"))
                .Distinct()
                .ToList();
            return Json(usedDates);
        }

        [HttpGet]
        [Breadcrumb("Create Plan", FromController = typeof(StockProcessingPlanController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(DateTime processingDate, int branchId)
        {
            try
            {
                var allBouBranches = await _configServiceClient.GetAllBouBranchesAsync();
                var branch = allBouBranches.FirstOrDefault(b => b.Id == branchId);

                var countingRooms = await _countingRoomRepository.FindAll();
                var branchRooms = countingRooms.Where(r => r.BranchId == branchId && !r.IsDeleted).ToList();

                var allBoards = await _processingBoardRepository.FindAll();
                var branchBoards = allBoards.Where(b => b.BranchId == branchId && !b.IsDeleted).ToList();

                var bags = await _bagRepository.GetAllBagsAsync(branchId);

                var model = new StockProcessingPlanCreateViewModel
                {
                    ProcessingDate = processingDate,
                    BranchId = branchId,
                    BranchName = branch?.Name,
                    CountingRooms = branchRooms.Select(room => new CountingRoomBoardsViewModel
                    {
                        Id = room.Id,
                        Name = room.Name,
                        Boards = branchBoards
                            .Where(b => b.CountingRoomId == room.Id)
                            .Select(b => new ProcessingBoardSimpleViewModel
                            {
                                Id = b.Id,
                                Name = b.Name,
                                MachineName = b.BPSMachine?.Name
                            })
                            .ToList()
                    }).ToList(),
                    AvailableBags = bags
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create processing plan view");
                TempData["Error"] = "An error occurred while loading the create form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DateTime processingDate, int branchId, string? assignmentsJson)
        {
            try
            {
                var draftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    StatusName = StatusConstants.StockProcessingPlan.Draft,
                    EntityTypeName = StatusConstants.Entity.StockProcessingPlan
                });

                var plan = new StockProcessingPlan
                {
                    ProcessingDate = processingDate,
                    BranchId = branchId,
                    CurrentStatusId = draftStatus.Id,
                    IsDeleted = false
                };

                await _planRepository.Create(plan);

                if (!string.IsNullOrEmpty(assignmentsJson) && assignmentsJson != "[]")
                {
                    var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var assignments = System.Text.Json.JsonSerializer.Deserialize<List<BoardBagAssignment>>(assignmentsJson, options);
                    if (assignments != null)
                    {
                        // Step 1: save all SessionBag records
                        foreach (var assignment in assignments)
                        {
                            foreach (var bagId in assignment.BagIds)
                            {
                                _context.SessionBags.Add(new SessionBag
                                {
                                    BagId = bagId,
                                    StockProcessingPlanId = plan.Id,
                                    ProcessingBoardId = assignment.ProcessingBoardId,
                                    IsDeleted = false
                                });
                            }
                        }
                        await _context.SaveChangesAsync();

                        // Step 2: create sessions grouped by denomination + commercial bank per board
                        var sessionDraftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                        {
                            ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                            StatusName = StatusConstants.StockProcessingSession.Draft,
                            EntityTypeName = StatusConstants.Entity.StockProcessingSession
                        });

                        int sessionCounter = 0;
                        foreach (var assignment in assignments)
                        {
                            if (assignment.BagIds.Count == 0) continue;

                            // Load parent→child mappings for bags on this board
                            var baggedBags = await _context.BaggedBags
                                .Where(bb => assignment.BagIds.Contains(bb.ParentBagId) && !bb.IsDeleted)
                                .ToListAsync();

                            var allBagIds = assignment.BagIds
                                .Concat(baggedBags.Select(bb => bb.ChildBagId))
                                .ToHashSet();

                            // Load denominations for own + child bags in one query
                            var denominations = await _context.BagDenominations
                                .Include(bd => bd.DepositRequestDenomination)
                                    .ThenInclude(drd => drd.DepositRequest)
                                .Where(bd => allBagIds.Contains(bd.BagId) && !bd.IsDeleted)
                                .ToListAsync();

                            var denomsByBagId = denominations.ToLookup(bd => bd.BagId);

                            // Load CategoryId for each assigned bag
                            var bagCategories = await _context.Bags
                                .Where(b => assignment.BagIds.Contains(b.Id))
                                .Select(b => new { b.Id, b.CategoryId })
                                .ToDictionaryAsync(b => b.Id, b => b.CategoryId);

                            // Resolve (DenomValue, CbId) for each assigned bag:
                            // prefer own denominations; fall back to child bag denominations
                            var bagKeys = assignment.BagIds.Select(bagId =>
                            {
                                var primary = denomsByBagId[bagId].FirstOrDefault()
                                    ?? baggedBags
                                        .Where(bb => bb.ParentBagId == bagId)
                                        .SelectMany(bb => denomsByBagId[bb.ChildBagId])
                                        .FirstOrDefault();

                                return new
                                {
                                    BagId    = bagId,
                                    DenomValue = primary?.DepositRequestDenomination?.DenominationValue ?? 0m,
                                    CbId     = primary?.DepositRequestDenomination?.DepositRequest?.CbId ?? 0,
                                    CategoryId = bagCategories.GetValueOrDefault(bagId) ?? 0
                                };
                            });

                            // One StockProcessingSession per (DenominationValue, CommercialBank) group
                            foreach (var group in bagKeys.GroupBy(x => (x.DenomValue, x.CbId)))
                            {
                                sessionCounter++;
                                var session = new StockProcessingSession
                                {
                                    ReferenceNumber = $"SPS-{plan.Id:D4}-{sessionCounter:D3}",
                                    StockProcessingPlanId = plan.Id,
                                    ProcessingBoardId = assignment.ProcessingBoardId,
                                    CommercialBankId = group.Key.CbId,
                                    CategoryId = group.First().CategoryId,
                                    CurrentStatusId = sessionDraftStatus.Id,
                                    IsManual = false,
                                    IsDeleted = false
                                };

                                await _sessionRepository.Create(session);

                                foreach (var bagInfo in group)
                                {
                                    _context.ProcessingSessionBags.Add(new ProcessingSessionBag
                                    {
                                        BagId = bagInfo.BagId,
                                        StockProcessingSessionId = session.Id,
                                        IsDeleted = false
                                    });
                                }
                            }
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["Success"] = "Processing plan created successfully.";
                return RedirectToAction(nameof(Details), new { id = plan.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating stock processing plan");
                TempData["Error"] = "An error occurred while creating the plan.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Breadcrumb("Plan Details", FromController = typeof(StockProcessingPlanController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var plan = await _planRepository.FindById(id);
                if (plan == null) return NotFound();

                var allBouBranches = await _configServiceClient.GetAllBouBranchesAsync();
                var branch = allBouBranches.FirstOrDefault(b => b.Id == plan.BranchId);
                var statusType = await _workflowServiceClient.FindStatusById(plan.CurrentStatusId);

                var sessionBags = await _context.SessionBags
                    .Include(sb => sb.Bag)
                        .ThenInclude(b => b.BagDenominations.Where(bd => !bd.IsDeleted))
                            .ThenInclude(bd => bd.DepositRequestDenomination)
                    .Where(sb => sb.StockProcessingPlanId == id && !sb.IsDeleted)
                    .ToListAsync();

                var countingRooms = await _countingRoomRepository.FindAll();
                var branchRooms = countingRooms.Where(r => r.BranchId == plan.BranchId && !r.IsDeleted).ToList();

                var allBoards = await _processingBoardRepository.FindAll();
                var branchBoards = allBoards.Where(b => b.BranchId == plan.BranchId && !b.IsDeleted).ToList();

                var planSessions = await _sessionRepository.GetByPlanIdAsync(id);

                // Resolve unique status names for sessions in one pass
                var sessionStatusIds = planSessions.Select(s => s.CurrentStatusId).Distinct().ToList();
                var sessionStatusNames = new Dictionary<int, string>();
                foreach (var statusId in sessionStatusIds)
                {
                    var st = await _workflowServiceClient.FindStatusById(statusId);
                    sessionStatusNames[statusId] = st.Name;
                }

                var allBanks = await _commercialBankRepository.FindAll();
                var bankDict = allBanks.ToDictionary(b => b.Id, b => b.Name);

                var model = new StockProcessingPlanDetailsViewModel
                {
                    Id = plan.Id,
                    ProcessingDate = plan.ProcessingDate,
                    BranchId = plan.BranchId,
                    BranchName = branch?.Name,
                    CurrentStatusName = statusType.Name,
                    CountingRooms = branchRooms.Select(room => new CountingRoomWithBagsViewModel
                    {
                        Id = room.Id,
                        Name = room.Name,
                        Boards = branchBoards
                            .Where(b => b.CountingRoomId == room.Id)
                            .Select(b => new ProcessingBoardWithBagsViewModel
                            {
                                Id = b.Id,
                                Name = b.Name,
                                MachineName = b.BPSMachine?.Name,
                                Bags = sessionBags
                                    .Where(sb => sb.ProcessingBoardId == b.Id)
                                    .Select(sb => new BagViewModel
                                    {
                                        Id = sb.Bag.Id,
                                        ReferenceNumber = sb.Bag.ReferenceNumber,
                                        BranchId = sb.Bag.BranchId,
                                        DenominationValue = sb.Bag.BagDenominations
                                            .Select(bd => bd.DepositRequestDenomination.DenominationValue)
                                            .FirstOrDefault(),
                                        TotalBundles = sb.Bag.BagDenominations
                                            .Sum(bd => bd.NumberOfBundlesOrBoxes),
                                        TotalAmount = sb.Bag.BagDenominations
                                            .Sum(bd => bd.DepositRequestDenomination.DenominationValue * bd.NumberOfBundlesOrBoxes)
                                    })
                                    .ToList()
                            })
                            .ToList()
                    }).ToList(),
                    Sessions = planSessions.Select(s => new ProcessingSessionViewModel
                    {
                        Id = s.Id,
                        ReferenceNumber = s.ReferenceNumber,
                        BoardName = s.ProcessingBoard?.Name,
                        MachineName = s.ProcessingBoard?.BPSMachine?.Name,
                        CommercialBankName = bankDict.GetValueOrDefault(s.CommercialBankId),
                        CurrentStatusName = sessionStatusNames.GetValueOrDefault(s.CurrentStatusId, string.Empty),
                        BagReferenceNumbers = s.ProcessingSessionBags
                            .Where(psb => !psb.IsDeleted)
                            .Select(psb => psb.Bag.ReferenceNumber)
                            .ToList()
                    }).ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving processing plan details");
                TempData["Error"] = "An error occurred while retrieving plan details.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}

using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.Bags;
using VMSService.Core.Models.MIandFits;
using VMSService.Core.Models.StockProcessingPlans;
using VMSService.Core.Models.StockProcessingPlanStatusHistories;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.BagMovements;
using VMSService.Core.Repositories.Bags;
using VMSService.Core.Repositories.Categories;
using VMSService.Core.Repositories.CommercialBanks;
using VMSService.Core.Repositories.CounterPartyTypes;
using VMSService.Core.Repositories.CountingRooms;
using VMSService.Core.Repositories.CountingRoomSessions;
using VMSService.Core.Repositories.DepositUnitOfWorks;
using VMSService.Core.Repositories.MIandFits;
using VMSService.Core.Repositories.ProcessingBoards;
using VMSService.Core.Repositories.StockMovements;
using VMSService.Core.Repositories.StockProcessingPlans;
using VMSService.Core.Repositories.StockProcessingPlanStatusHistories;
using VMSService.Core.Repositories.StockProcessingResults;
using VMSService.Core.Repositories.StockProcessingSessions;
using VMSService.Core.Repositories.Stocks;
using VMSService.Core.Services.Identity;
using VMSService.Core.Services.StockPositions;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;
using VMSService.Web.Helpers;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class StockProcessingPlanController(
        IStockProcessingPlanRepository _planRepository,
        IConfigurationsServiceClient _configServiceClient,
        ICountingRoomRepository _countingRoomRepository,
        ICountingRoomSessionRepository _countingRoomSessionRepository,
        IProcessingBoardRepository _processingBoardRepository,
        IBagRepository _bagRepository,
        IBagMovementRepository _bagMovementRepository,
        IStockProcessingSessionRepository _sessionRepository,
        IStockProcessingResultRepository _resultRepository,
        ICommercialBankRepository _commercialBankRepository,
        IStockService _stockService,
        IIdentityService _identityService,
        IWorkflowService _workflowService,
        IStockProcessingPlanStatusHistoryRepository _planStatusHistoryRepository,
        IAttachmentRepository _attachmentRepository,
        IWebHostEnvironment _environment,
        VMSServiceDbContext _context,
        IMapper _mapper,
        ILogger<StockProcessingPlanController> _logger,
        IWorkflowServiceClient _workflowServiceClient,
        IStockLedgerRepository _stockLedgerRepository,
        ICounterPartyTypeRepository _counterPartyTypeRepository,
        IStockMovementRepository _stockMovementRepository,
        ICategoryRepository _categoryRepository,
        IDepositTransactionRefNumberGenerator _depositTxnRefGenerator,
        IMIandFitRepository _miAndFitRepository,
        IStockPositionService _stockPositionService) : Controller
    {
        // Category names whose current stock position seeds the MI & Fit issued values.
        private const string FitCategoryName = "Fit";
        private const string MiCategoryName = "MI";

        /// <summary>
        /// Current "Fit" and "MI" stock positions for a branch, keyed by denomination id.
        /// These are the values issued to the boards for MI &amp; Fit — the maker never types them in.
        /// </summary>
        private async Task<Dictionary<int, (decimal Fit, decimal Mi)>> GetIssuedMIandFitPositionsAsync(
            int branchId, List<int> denominationIds)
        {
            var positions = denominationIds.ToDictionary(id => id, _ => (Fit: 0m, Mi: 0m));

            try
            {
                if (denominationIds.Count == 0) return positions;

                var allCategories = await _categoryRepository.FindAll();
                var fitCategory = allCategories.FirstOrDefault(c =>
                    c.Name.Trim().Equals(FitCategoryName, StringComparison.OrdinalIgnoreCase));
                var miCategory = allCategories.FirstOrDefault(c =>
                    c.Name.Trim().Equals(MiCategoryName, StringComparison.OrdinalIgnoreCase));

                if (fitCategory == null || miCategory == null)
                {
                    _logger.LogWarning(
                        "MI & Fit issued values default to 0 — category '{Fit}' or '{Mi}' is not configured.",
                        FitCategoryName, MiCategoryName);
                    return positions;
                }

                var matrix = await _stockPositionService.GetStockMatrixAsync(
                    branchId, [fitCategory.Id, miCategory.Id], denominationIds);

                foreach (var denominationId in denominationIds)
                {
                    if (!matrix.TryGetValue(denominationId, out var byCategory)) continue;

                    positions[denominationId] = (
                        Fit: byCategory.GetValueOrDefault(fitCategory.Id),
                        Mi: byCategory.GetValueOrDefault(miCategory.Id));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving MI & Fit stock positions for branch {BranchId}", branchId);
            }

            return positions;
        }

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
        public async Task<IActionResult> GetBranchesJson()
        {
            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            return Json(branches.Select(b => new { b.Id, b.Name }).OrderBy(b => b.Name));
        }

        // Legacy "N/A" subcategory id — matches the convention hardcoded across the deposit controllers
        // (DepositRequestDenomination.SubCategoryId: 1 - "FIT", 2 - "UNFIT", 3 - "N/A").
        private const int LegacyNaSubCategoryId = 3;

        [HttpPost]
        [ValidateAntiForgeryTokenFromHeader]
        public async Task<IActionResult> CreateBag([FromBody] CreateLegacyBagRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { error = "Invalid request." });

                if (string.IsNullOrWhiteSpace(request.CommercialBankName))
                    return BadRequest(new { error = "Commercial Bank is required." });

                if (request.SourceDepositBranchId <= 0)
                    return BadRequest(new { error = "Source Deposit Branch is required." });

                if (request.Value <= 0)
                    return BadRequest(new { error = "A valid Value is required." });

                var allBanks = await _commercialBankRepository.FindAll();
                var bank = allBanks.FirstOrDefault(b =>
                    string.Equals(b.Name, request.CommercialBankName, StringComparison.OrdinalIgnoreCase));
                if (bank == null)
                    return BadRequest(new { error = "Commercial Bank not found." });

                var denomination = await _stockService.GetDenominationById(request.DenominationId);
                if (denomination == null)
                    return BadRequest(new { error = "Denomination not found." });

                if (denomination.Value <= 0)
                    return BadRequest(new { error = "Selected denomination has an invalid value." });

                if (request.Value % denomination.Value != 0)
                    return BadRequest(new { error = $"Value must be an exact multiple of the denomination value (UGX {denomination.Value:N0})." });

                var maxBagValue = denomination.Value * 1000 * 50;
                if (request.Value > maxBagValue)
                    return BadRequest(new { error = $"Value cannot exceed UGX {maxBagValue:N0} (a full bag of 50 bundles/boxes for this denomination)." });

                var completedStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    EntityTypeName = StatusConstants.Entity.DepositRequest,
                    StatusName = DepositStatuses.DepositStatusNames.Completed
                });

                if (completedStatus == null)
                    return BadRequest(new { error = "Deposit workflow is not configured. Please contact an administrator." });

                var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "System";

                // Prefixed with "LEG" to flag this as a legacy-bag-backfilled deposit, while still going through
                // the shared generator for its branch/CB/date sequence uniqueness.
                var transactionRefNumber = "LEG" + await _depositTxnRefGenerator.GenerateAsync(
                    request.SourceDepositBranchId, bank.Id, request.DepositDate);

                bool isNote = denomination.DenominationType?.Name == StockConstants.DenominationTypes.Note;

                // Bundles/Boxes back-calculated from the entered value: Amount = units × 1,000 × DenominationValue.
                int units = denomination.Value > 0
                    ? (int)Math.Ceiling(request.Value / (1000m * denomination.Value))
                    : 0;

                if (units <= 0)
                    return BadRequest(new { error = "Unable to determine bundles/boxes for the entered value." });

                Category paids = (await _categoryRepository.FindAll()).FirstOrDefault(c => c.Name == StockConstants.StoreTypes.Paids);

                var depositRequest = new DepositRequest
                {
                    IsLegacyDeposit = true,
                    CbId = bank.Id,
                    CbCode = bank.CBCode,
                    InitiatorUserId = username,
                    BouBranchId = request.SourceDepositBranchId,
                    DepositDate = request.DepositDate,
                    StatusTypeId = completedStatus.Id,
                    TransactionRefNumber = transactionRefNumber,
                    TotalNotesAmount = isNote ? request.Value : 0,
                    TotalCoinsAmount = isNote ? 0 : request.Value,
                    TotalAmount = request.Value
                };

                var depositRequestDenomination = new DepositRequestDenomination
                {
                    DepositRequestId = depositRequest.Id,
                    DenominationId = request.DenominationId,
                    DenominationValue = denomination.Value,
                    DenominationTypeId = denomination.DenominationTypeId,
                    Bundles = isNote ? units : null,
                    Boxes = isNote ? null : units,
                    Amount = request.Value,
                    Origin = DepositStatuses.DepositOrigin.Bou,
                    SubCategoryId = LegacyNaSubCategoryId
                };
                depositRequest.DepositRequestDenominations.Add(depositRequestDenomination);

                var denomShortForm = GetDenominationShortForm(denomination.Value);
                // Use the CbCode-scoped overload since this bag carries a real CbCode (unlike BaggingController's
                // manually-created empty bags, which leave CbCode null and use the 2-arg overload instead).
                var bagSequence = await _bagRepository.BagSequence(DateTime.UtcNow, request.DenominationId, bank.Code);
                var referenceNumber = $"LEG-{DateTime.UtcNow:ddMMyy}-{denomShortForm}-{bagSequence}";

                // Newly-created legacy bags go under the "NEW LEGACY BAGS" section of the "BAGS WITH BOARDS"
                // vault — kept separate from the BAGS WITH BOARDS section itself so these bags still show up
                // in the Available Bags pool (that section is excluded there as already-issued-to-boards).
                var boardsVault = await _context.Vaults
                    .FirstOrDefaultAsync(v => v.BranchId == request.BranchId
                        && v.Name == VaultConstants.BagsWithBoardsSectionName
                        && !v.IsDeleted);

                var legacyBagsSection = boardsVault != null
                    ? await _context.VaultSections
                        .FirstOrDefaultAsync(vs => vs.VaultId == boardsVault.Id
                            && vs.Name == VaultConstants.NewLegacyBagsSectionName
                            && !vs.IsDeleted)
                    : null;

                if (legacyBagsSection == null)
                {
                    _logger.LogWarning(
                        "NEW LEGACY BAGS vault section not found for branch {BranchId}; new legacy bag will be created without a vault section.",
                        request.BranchId);
                }

                var bag = new Bag
                {
                    ReferenceNumber = referenceNumber,
                    DenominationId = request.DenominationId,
                    BranchId = request.BranchId,
                    CbCode = bank.CBCode,
                    IsLegacyBag = true,
                    CategoryId = paids?.Id,
                    SubCategoryId = LegacyNaSubCategoryId,
                    VaultSectionId = legacyBagsSection?.Id,
                    IsDeleted = false,
                    IsProcessed = false
                };

                var bagDenomination = new BagDenomination
                {
                    Bag = bag,
                    DepositRequestDenominationId = depositRequestDenomination.Id,
                    NumberOfBundlesOrBoxes = units,
                    IsDeleted = false
                };
                bag.BagDenominations.Add(bagDenomination);

                _context.DepositRequests.Add(depositRequest);
                _context.Bags.Add(bag);

                await _context.SaveChangesAsync();

                return Json(new { id = bag.Id, referenceNumber = bag.ReferenceNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating legacy bag");
                return BadRequest(new { error = "Failed to save the bag. Please try again." });
            }
        }

        /// <summary>
        /// Converts denomination value to short form for bag reference numbers.
        /// Examples: 1000 -> 1K, 2000 -> 2K, 50000 -> 50K, 500 -> 500, 100 -> 100
        /// </summary>
        private string GetDenominationShortForm(decimal denominationValue)
        {
            if (denominationValue >= 1000)
            {
                int thousands = (int)(denominationValue / 1000);
                return $"{thousands}K";
            }

            return denominationValue.ToString("0");
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

                var allBags = await _bagRepository.GetAllBagsAsync(branchId);

                // Enrich with denomination names/values (sourced from the bag's own DenominationId,
                // since merging only allows bags of the same denomination, so this also covers
                // parent/container bags whose own BagDenominations are empty)
                var denominations = await _stockService.GetAllDenominations();
                var denomMap = denominations.ToDictionary(d => d.Id);
                foreach (var bag in allBags)
                {
                    if (denomMap.TryGetValue(bag.DenominationId, out var denom))
                    {
                        bag.DenominationName = denom.Name;
                        bag.DenominationValue = denom.Value;
                    }
                }

                var allBanks = await _commercialBankRepository.FindAll();
                var bankDict = allBanks.ToDictionary(b => b.Id, b => b.Name);
                foreach (var bag in allBags)
                {
                    bag.CommercialBankNames = bag.CommercialBankIds
                        .Where(id => bankDict.ContainsKey(id))
                        .Select(id => bankDict[id])
                        .Distinct()
                        .ToList();
                }

                var allSubCategories = await _stockService.GetAllSubCategories();
                var subCategoryDict = allSubCategories.ToDictionary(sc => sc.Id, sc => sc.Name);
                foreach (var bag in allBags)
                {
                    if (bag.SubCategoryId.HasValue)
                        bag.SubCategoryName = subCategoryDict.GetValueOrDefault(bag.SubCategoryId.Value);
                }

                // Exclude only bags actually issued to boards (BAGS WITH BOARDS section) — checked by section,
                // not vault, so bags in the NEW LEGACY BAGS section (same vault) still show up here.
                var bags = allBags.Where(b => b.VaultSectionId.HasValue
                    && !string.Equals(b.VaultSectionName, VaultConstants.BagsWithBoardsSectionName, StringComparison.OrdinalIgnoreCase)).ToList();

                var boardSupervisors = await _identityService.GetUsersByRoleName(SystemPolicy.Roles.BoardSupervisor);

                var activeDenominations = denominations.Where(d => !d.IsDeleted).OrderBy(d => d.Value).ToList();
                var issuedPositions = await GetIssuedMIandFitPositionsAsync(
                    branchId, activeDenominations.Select(d => d.Id).ToList());

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
                    AvailableBags = bags,
                    BoardSupervisors = boardSupervisors.ToList(),
                    MIandFits = activeDenominations
                        .Select(d =>
                        {
                            var position = issuedPositions.GetValueOrDefault(d.Id);
                            return new MIandFitViewModel
                            {
                                DenominationId = d.Id,
                                DenominationName = d.Name,
                                DenominationValue = d.Value,
                                IssuedFitValue = position.Fit,
                                IssuedUnFitValue = position.Mi
                            };
                        })
                        .ToList()
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
        public async Task<IActionResult> Create(DateTime processingDate, int branchId, string? assignmentsJson, string? roomSupervisorsJson, string? miAndFitsJson)
        {
            try
            {
                var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var newAssignments = string.IsNullOrEmpty(assignmentsJson) || assignmentsJson == "[]"
                    ? []
                    : System.Text.Json.JsonSerializer.Deserialize<List<BoardBagAssignment>>(assignmentsJson, jsonOptions) ?? [];

                var newMiFits = string.IsNullOrEmpty(miAndFitsJson) || miAndFitsJson == "[]"
                    ? []
                    : System.Text.Json.JsonSerializer.Deserialize<List<MIandFitAssignment>>(miAndFitsJson, jsonOptions) ?? [];

                // MI & Fit may only be declared for denominations actually present among the submitted
                // bag assignments — validated up front, before the plan is created.
                if (newMiFits.Count > 0)
                {
                    var assignedBagIdsForMiFit = newAssignments.SelectMany(a => a.BagIds).Distinct().ToList();
                    var allowedDenominationIds = assignedBagIdsForMiFit.Count > 0
                        ? (await _context.Bags
                            .Where(b => assignedBagIdsForMiFit.Contains(b.Id))
                            .Select(b => b.DenominationId)
                            .Distinct()
                            .ToListAsync()).ToHashSet()
                        : new HashSet<int>();

                    if (newMiFits.Any(mf => !allowedDenominationIds.Contains(mf.DenominationId)))
                    {
                        TempData["Error"] = "MI & Fit was submitted for a denomination that has no bags assigned to a processing board.";
                        return RedirectToAction(nameof(Create), new { processingDate = processingDate.ToString("yyyy-MM-dd"), branchId });
                    }
                }

                var draftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    StatusName = StatusConstants.StockProcessingPlan.Draft,
                    EntityTypeName = StatusConstants.Entity.StockProcessingPlan
                });

                var branch = await _configServiceClient.GetBouBranchByIdAsync(branchId);
                var branchCode = branch?.BranchCode ?? branchId.ToString();

                var planNumber = await _context.StockProcessingPlans
                    .CountAsync(p => p.BranchId == branchId
                        && p.ProcessingDate.Date == processingDate.Date
                        && !p.IsDeleted) + 1;

                var plan = new StockProcessingPlan
                {
                    ProcessingDate = processingDate,
                    BranchId = branchId,
                    ReferenceNumber = $"SPP-{processingDate:ddMMyyyy}-{branchCode}-{planNumber:D2}",
                    CurrentStatusId = draftStatus.Id,
                    IsDeleted = false
                };

                await _planRepository.Create(plan);

                if (!string.IsNullOrEmpty(roomSupervisorsJson) && roomSupervisorsJson != "[]")
                {
                    var supervisorOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var roomSupervisors = System.Text.Json.JsonSerializer.Deserialize<List<RoomSupervisorAssignment>>(roomSupervisorsJson, supervisorOptions);
                    if (roomSupervisors != null)
                    {
                        foreach (var rs in roomSupervisors.Where(rs => !string.IsNullOrEmpty(rs.BoardSupervisorUserId)))
                        {
                            await _countingRoomSessionRepository.Create(new CountingRoomSession
                            {
                                CountingRoomId = rs.CountingRoomId,
                                StockProcessingPlanId = plan.Id,
                                BoardSupervisorUserId = rs.BoardSupervisorUserId,
                                IsDeleted = false
                            });
                        }
                    }
                }

                if (newMiFits.Count > 0)
                {
                    // Issued values are the branch's current Fit/MI stock positions — re-derived here so
                    // they reflect the position at save time and can't be set from the browser.
                    var issuedPositions = await GetIssuedMIandFitPositionsAsync(
                        branchId, newMiFits.Select(mf => mf.DenominationId).Distinct().ToList());

                    foreach (var mf in newMiFits)
                    {
                        var position = issuedPositions.GetValueOrDefault(mf.DenominationId);
                        await _miAndFitRepository.Create(new MIandFit
                        {
                            StockProcessingPlanId = plan.Id,
                            DenominationId = mf.DenominationId,
                            IssuedFitValue = position.Fit,
                            IssuedUnFitValue = position.Mi,
                            ReturnedFitValue = 0,
                            ReturnedUnFitValue = 0,
                            BoardSupervisorUserId = mf.BoardSupervisorUserId,
                            IsDeleted = false
                        });
                    }
                }

                if (newAssignments.Count > 0)
                {
                    var assignments = newAssignments;

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

                        var boardBpsMachineIds = await _context.ProcessingBoards
                            .Where(b => assignments.Select(a => a.ProcessingBoardId).Contains(b.Id))
                            .Select(b => new { b.Id, b.BPSMachineId })
                            .ToDictionaryAsync(b => b.Id, b => b.BPSMachineId);

                        int sessionCounter = 0;
                        foreach (var assignment in assignments)
                        {
                            if (assignment.BagIds.Count == 0) continue;

                            // BFS through BaggedBags to resolve every bag assigned to this board down
                            // to its actual leaf child bags (a bag with no children is its own leaf).
                            var childrenByParent = new Dictionary<int, List<int>>();
                            var frontier = assignment.BagIds.ToHashSet();
                            var visited = new HashSet<int>(frontier);
                            while (frontier.Count > 0)
                            {
                                var links = await _context.BaggedBags
                                    .Where(bb => frontier.Contains(bb.ParentBagId) && !bb.IsDeleted)
                                    .Select(bb => new { bb.ParentBagId, bb.ChildBagId })
                                    .ToListAsync();

                                var nextFrontier = new HashSet<int>();
                                foreach (var link in links)
                                {
                                    if (!childrenByParent.TryGetValue(link.ParentBagId, out var list))
                                        childrenByParent[link.ParentBagId] = list = [];
                                    list.Add(link.ChildBagId);

                                    if (visited.Add(link.ChildBagId))
                                        nextFrontier.Add(link.ChildBagId);
                                }
                                frontier = nextFrontier;
                            }

                            List<int> ResolveLeafBagIds(int bagId) =>
                                childrenByParent.TryGetValue(bagId, out var children) && children.Count > 0
                                    ? children.SelectMany(ResolveLeafBagIds).ToList()
                                    : [bagId];

                            var leafBagIds = assignment.BagIds.SelectMany(ResolveLeafBagIds).Distinct().ToList();
                            if (leafBagIds.Count == 0) continue;

                            // Load denominations for the leaf (child) bags only
                            var denominations = await _context.BagDenominations
                                .Include(bd => bd.DepositRequestDenomination)
                                    .ThenInclude(drd => drd.DepositRequest)
                                .Where(bd => leafBagIds.Contains(bd.BagId) && !bd.IsDeleted)
                                .ToListAsync();

                            var denomsByBagId = denominations.ToLookup(bd => bd.BagId);

                            // Load CategoryId and DenominationId for each leaf (child) bag
                            var bagMeta = await _context.Bags
                                .Where(b => leafBagIds.Contains(b.Id))
                                .Select(b => new { b.Id, b.CategoryId, b.DenominationId })
                                .ToDictionaryAsync(b => b.Id, b => new { b.CategoryId, b.DenominationId });

                            // SubCategoryId per leaf bag (own, or fall back to parent bag)
                            var subCatByBagId = await ResolveSubCategoryIdsAsync(leafBagIds);

                            // Resolve (DenomValue, CbId, SubCategoryId) for each leaf (child) bag from its own denominations
                            var bagKeys = leafBagIds.Select(bagId =>
                            {
                                var primary = denomsByBagId[bagId].FirstOrDefault();
                                var meta = bagMeta.GetValueOrDefault(bagId);
                                return new
                                {
                                    BagId          = bagId,
                                    DenomValue     = primary?.DepositRequestDenomination?.DenominationValue ?? 0m,
                                    CbId           = primary?.DepositRequestDenomination?.DepositRequest?.CbId ?? 0,
                                    CategoryId     = meta?.CategoryId ?? 0,
                                    DenominationId = meta?.DenominationId ?? 0,
                                    SubCategoryId  = subCatByBagId.GetValueOrDefault(bagId),
                                    TotalValue     = denomsByBagId[bagId].Sum(bd =>
                                        (bd.DepositRequestDenomination?.DenominationValue ?? 0m) * 1000 * bd.NumberOfBundlesOrBoxes)
                                };
                            });

                            // One StockProcessingSession per (DenominationValue, CommercialBank, SubCategory) group, built from child bags
                            foreach (var group in bagKeys.GroupBy(x => (x.DenomValue, x.CbId, x.SubCategoryId)))
                            {
                                sessionCounter++;
                                var session = new StockProcessingSession
                                {
                                    ReferenceNumber = $"SPS-{plan.ProcessingDate:ddMMyyyy}-{branchCode}-{sessionCounter:D2}",
                                    StockProcessingPlanId = plan.Id,
                                    ProcessingBoardId = assignment.ProcessingBoardId,
                                    CommercialBankId = group.Key.CbId,
                                    CategoryId = group.First().CategoryId,
                                    DenominationId = group.First().DenominationId,
                                    CurrentStatusId = sessionDraftStatus.Id,
                                    TotalIssuedValue = group.Sum(x => x.TotalValue),
                                    IsManual = !boardBpsMachineIds.TryGetValue(assignment.ProcessingBoardId, out var bpsMachineId) || !bpsMachineId.HasValue,
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
        [Breadcrumb("Edit Plan", FromController = typeof(StockProcessingPlanController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var plan = await _planRepository.FindById(id);
                if (plan == null) return NotFound();

                var statusType = await _workflowServiceClient.FindStatusById(plan.CurrentStatusId);
                if (statusType.Name != StatusConstants.StockProcessingPlan.Draft &&
                    statusType.Name != StatusConstants.StockProcessingPlan.PendingVerification)
                {
                    TempData["Error"] = "This plan cannot be edited in its current status.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var allBouBranches = await _configServiceClient.GetAllBouBranchesAsync();
                var branch = allBouBranches.FirstOrDefault(b => b.Id == plan.BranchId);

                var countingRooms = await _countingRoomRepository.FindAll();
                var branchRooms = countingRooms.Where(r => r.BranchId == plan.BranchId && !r.IsDeleted).ToList();

                var allBoards = await _processingBoardRepository.FindAll();
                var branchBoards = allBoards.Where(b => b.BranchId == plan.BranchId && !b.IsDeleted).ToList();

                var currentSessionBags = await _context.SessionBags
                    .Where(sb => sb.StockProcessingPlanId == id && !sb.IsDeleted)
                    .ToListAsync();

                var assignedBagIdsByBoard = currentSessionBags
                    .GroupBy(sb => sb.ProcessingBoardId)
                    .ToDictionary(g => g.Key, g => g.Select(sb => sb.BagId).ToList());

                var allBags = await _bagRepository.GetAllBagsAsync(plan.BranchId);

                var denominations = await _stockService.GetAllDenominations();
                var denomMap = denominations.ToDictionary(d => d.Id);
                foreach (var bag in allBags)
                {
                    if (denomMap.TryGetValue(bag.DenominationId, out var denom))
                    {
                        bag.DenominationName = denom.Name;
                        bag.DenominationValue = denom.Value;
                    }
                }

                var allBanks = await _commercialBankRepository.FindAll();
                var bankDict = allBanks.ToDictionary(b => b.Id, b => b.Name);
                foreach (var bag in allBags)
                {
                    bag.CommercialBankNames = bag.CommercialBankIds
                        .Where(bid => bankDict.ContainsKey(bid))
                        .Select(bid => bankDict[bid])
                        .Distinct()
                        .ToList();
                }

                var allSubCategories = await _stockService.GetAllSubCategories();
                var subCategoryDict = allSubCategories.ToDictionary(sc => sc.Id, sc => sc.Name);
                foreach (var bag in allBags)
                {
                    if (bag.SubCategoryId.HasValue)
                        bag.SubCategoryName = subCategoryDict.GetValueOrDefault(bag.SubCategoryId.Value);
                }

                var bags = allBags.Where(b => b.VaultSectionId.HasValue).ToList();

                var boardSupervisors = await _identityService.GetUsersByRoleName(SystemPolicy.Roles.BoardSupervisor);

                var roomSessions = await _countingRoomSessionRepository.FindByStockProcessingPlanId(id);
                var supervisorByRoomId = roomSessions
                    .Where(rs => !string.IsNullOrEmpty(rs.BoardSupervisorUserId))
                    .ToDictionary(rs => rs.CountingRoomId, rs => rs.BoardSupervisorUserId);

                // MI & Fit: one row per active denomination, merged with any values already declared for this plan.
                // Issued values always show the branch's live Fit/MI stock position rather than what was
                // stored earlier, so the maker sees what will actually be saved.
                var existingMiFits = await _miAndFitRepository.FindByPlanId(id);
                var miFitByDenomId = existingMiFits.ToDictionary(m => m.DenominationId);
                var activeDenominations = denominations.Where(d => !d.IsDeleted).OrderBy(d => d.Value).ToList();
                var issuedPositions = await GetIssuedMIandFitPositionsAsync(
                    plan.BranchId, activeDenominations.Select(d => d.Id).ToList());

                var miAndFitRows = activeDenominations
                    .Select(d =>
                    {
                        miFitByDenomId.TryGetValue(d.Id, out var existing);
                        var position = issuedPositions.GetValueOrDefault(d.Id);
                        return new MIandFitViewModel
                        {
                            Id = existing?.Id ?? 0,
                            StockProcessingPlanId = id,
                            DenominationId = d.Id,
                            DenominationName = d.Name,
                            DenominationValue = d.Value,
                            IssuedFitValue = position.Fit,
                            IssuedUnFitValue = position.Mi,
                            ReturnedFitValue = existing?.ReturnedFitValue ?? 0,
                            ReturnedUnFitValue = existing?.ReturnedUnFitValue ?? 0,
                            BoardSupervisorUserId = existing?.BoardSupervisorUserId
                        };
                    })
                    .ToList();

                var bagDict = bags.ToDictionary(b => b.Id);

                // Once the plan is Pending Verification, only bags already attached to a board in
                // this plan may be reassigned between boards — no new bags may be pulled in.
                var availableBags = bags;
                if (statusType.Name == StatusConstants.StockProcessingPlan.PendingVerification)
                {
                    var assignedBagIds = assignedBagIdsByBoard.Values.SelectMany(ids => ids).ToHashSet();
                    availableBags = bags.Where(b => assignedBagIds.Contains(b.Id)).ToList();
                }

                var model = new StockProcessingPlanEditViewModel
                {
                    Id = plan.Id,
                    ProcessingDate = plan.ProcessingDate,
                    BranchId = plan.BranchId,
                    BranchName = branch?.Name,
                    CurrentStatusName = statusType.Name,
                    CountingRooms = branchRooms.Select(room => new CountingRoomEditViewModel
                    {
                        Id = room.Id,
                        Name = room.Name,
                        CurrentBoardSupervisorUserId = supervisorByRoomId.GetValueOrDefault(room.Id),
                        Boards = branchBoards
                            .Where(b => b.CountingRoomId == room.Id)
                            .Select(b => new ProcessingBoardEditViewModel
                            {
                                Id = b.Id,
                                Name = b.Name,
                                MachineName = b.BPSMachine?.Name,
                                AssignedBags = assignedBagIdsByBoard.TryGetValue(b.Id, out var sbs)
                                    ? sbs.Select(bagId => bagDict.GetValueOrDefault(bagId))
                                         .Where(bvm => bvm != null).ToList()!
                                    : []
                            })
                            .ToList()
                    }).ToList(),
                    AvailableBags = availableBags,
                    BoardSupervisors = boardSupervisors.ToList(),
                    MIandFits = miAndFitRows
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit processing plan view for plan {Id}", id);
                TempData["Error"] = "An error occurred while loading the edit form.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string? assignmentsJson, string? roomSupervisorsJson, string? miAndFitsJson)
        {
            try
            {
                var plan = await _planRepository.FindById(id);
                if (plan == null) return NotFound();

                var statusType = await _workflowServiceClient.FindStatusById(plan.CurrentStatusId);
                if (statusType.Name != StatusConstants.StockProcessingPlan.Draft &&
                    statusType.Name != StatusConstants.StockProcessingPlan.PendingVerification)
                {
                    TempData["Error"] = "This plan cannot be edited in its current status.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var newAssignments = string.IsNullOrEmpty(assignmentsJson) || assignmentsJson == "[]"
                    ? []
                    : System.Text.Json.JsonSerializer.Deserialize<List<BoardBagAssignment>>(assignmentsJson, jsonOptions) ?? [];

                var newMiFits = string.IsNullOrEmpty(miAndFitsJson) || miAndFitsJson == "[]"
                    ? []
                    : System.Text.Json.JsonSerializer.Deserialize<List<MIandFitAssignment>>(miAndFitsJson, jsonOptions) ?? [];

                // MI & Fit may only be declared for denominations actually present among the newly-submitted
                // bag assignments — validated up front, before any writes, so a rejection leaves nothing changed.
                if (newMiFits.Count > 0)
                {
                    var assignedBagIdsForMiFit = newAssignments.SelectMany(a => a.BagIds).Distinct().ToList();
                    var allowedDenominationIds = assignedBagIdsForMiFit.Count > 0
                        ? (await _context.Bags
                            .Where(b => assignedBagIdsForMiFit.Contains(b.Id))
                            .Select(b => b.DenominationId)
                            .Distinct()
                            .ToListAsync()).ToHashSet()
                        : new HashSet<int>();

                    if (newMiFits.Any(mf => !allowedDenominationIds.Contains(mf.DenominationId)))
                    {
                        TempData["Error"] = "MI & Fit was submitted for a denomination that has no bags assigned to a processing board.";
                        return RedirectToAction(nameof(Edit), new { id });
                    }
                }

                // Determine which boards have changed
                var currentSessionBags = await _context.SessionBags
                    .Where(sb => sb.StockProcessingPlanId == id && !sb.IsDeleted)
                    .ToListAsync();

                // While Pending Verification, bags already attached to this plan may only be
                // reassigned between boards — no new bags may be introduced, and none may be
                // left unassigned.
                if (statusType.Name == StatusConstants.StockProcessingPlan.PendingVerification)
                {
                    var currentPlanBagIds = currentSessionBags.Select(sb => sb.BagId).ToHashSet();
                    var newPlanBagIds = newAssignments.SelectMany(a => a.BagIds).ToHashSet();

                    if (!newPlanBagIds.IsSubsetOf(currentPlanBagIds))
                    {
                        TempData["Error"] = "Only bags already attached to this plan may be reassigned while it is Pending Verification.";
                        return RedirectToAction(nameof(Edit), new { id });
                    }

                    if (!currentPlanBagIds.IsSubsetOf(newPlanBagIds))
                    {
                        TempData["Error"] = "All bags must be assigned to a processing board while this plan is Pending Verification.";
                        return RedirectToAction(nameof(Edit), new { id });
                    }
                }

                var currentBagsByBoard = currentSessionBags
                    .GroupBy(sb => sb.ProcessingBoardId)
                    .ToDictionary(g => g.Key, g => g.Select(sb => sb.BagId).ToHashSet());

                var newBagsByBoard = newAssignments
                    .ToDictionary(a => a.ProcessingBoardId, a => a.BagIds.ToHashSet());

                var allBoardIds = currentBagsByBoard.Keys.Union(newBagsByBoard.Keys).ToHashSet();
                var changedBoardIds = allBoardIds
                    .Where(bid =>
                    {
                        var oldBags = currentBagsByBoard.GetValueOrDefault(bid) ?? [];
                        var newBags = newBagsByBoard.GetValueOrDefault(bid) ?? [];
                        return !oldBags.SetEquals(newBags);
                    })
                    .ToHashSet();

                if (changedBoardIds.Count > 0)
                {
                    // Soft-delete existing SessionBags and sessions for changed boards
                    var sessionsToDelete = await _context.StockProcessingSessions
                        .Include(s => s.ProcessingSessionBags)
                        .Where(s => s.StockProcessingPlanId == id && !s.IsDeleted
                                 && changedBoardIds.Contains(s.ProcessingBoardId ?? 0))
                        .ToListAsync();

                    foreach (var sb in currentSessionBags.Where(sb => changedBoardIds.Contains(sb.ProcessingBoardId)))
                        sb.IsDeleted = true;

                    foreach (var session in sessionsToDelete)
                    {
                        foreach (var psb in session.ProcessingSessionBags)
                            psb.IsDeleted = true;
                        session.IsDeleted = true;
                    }
                    await _context.SaveChangesAsync();

                    // Create new SessionBags for changed boards
                    foreach (var assignment in newAssignments.Where(a => changedBoardIds.Contains(a.ProcessingBoardId)))
                    {
                        foreach (var bagId in assignment.BagIds)
                        {
                            _context.SessionBags.Add(new SessionBag
                            {
                                BagId = bagId,
                                StockProcessingPlanId = id,
                                ProcessingBoardId = assignment.ProcessingBoardId,
                                IsDeleted = false
                            });
                        }
                    }
                    await _context.SaveChangesAsync();

                    // Re-create sessions for changed boards (BFS + denomination/CB grouping)
                    var sessionDraftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        StatusName = StatusConstants.StockProcessingSession.Draft,
                        EntityTypeName = StatusConstants.Entity.StockProcessingSession
                    });

                    var editBranch = await _configServiceClient.GetBouBranchByIdAsync(plan.BranchId);
                    var editBranchCode = editBranch?.BranchCode ?? plan.BranchId.ToString();

                    int sessionCounter = await _context.StockProcessingSessions
                        .CountAsync(s => s.StockProcessingPlanId == id && !s.IsDeleted);

                    var boardBpsMachineIds = await _context.ProcessingBoards
                        .Where(b => newAssignments.Select(a => a.ProcessingBoardId).Contains(b.Id))
                        .Select(b => new { b.Id, b.BPSMachineId })
                        .ToDictionaryAsync(b => b.Id, b => b.BPSMachineId);

                    foreach (var assignment in newAssignments.Where(a => changedBoardIds.Contains(a.ProcessingBoardId)))
                    {
                        if (assignment.BagIds.Count == 0) continue;

                        var childrenByParent = new Dictionary<int, List<int>>();
                        var frontier = assignment.BagIds.ToHashSet();
                        var visited = new HashSet<int>(frontier);
                        while (frontier.Count > 0)
                        {
                            var links = await _context.BaggedBags
                                .Where(bb => frontier.Contains(bb.ParentBagId) && !bb.IsDeleted)
                                .Select(bb => new { bb.ParentBagId, bb.ChildBagId })
                                .ToListAsync();

                            var nextFrontier = new HashSet<int>();
                            foreach (var link in links)
                            {
                                if (!childrenByParent.TryGetValue(link.ParentBagId, out var list))
                                    childrenByParent[link.ParentBagId] = list = [];
                                list.Add(link.ChildBagId);
                                if (visited.Add(link.ChildBagId))
                                    nextFrontier.Add(link.ChildBagId);
                            }
                            frontier = nextFrontier;
                        }

                        List<int> ResolveLeafBagIds(int bagId) =>
                            childrenByParent.TryGetValue(bagId, out var children) && children.Count > 0
                                ? children.SelectMany(ResolveLeafBagIds).ToList()
                                : [bagId];

                        var leafBagIds = assignment.BagIds.SelectMany(ResolveLeafBagIds).Distinct().ToList();
                        if (leafBagIds.Count == 0) continue;

                        var denomBagData = await _context.BagDenominations
                            .Include(bd => bd.DepositRequestDenomination)
                                .ThenInclude(drd => drd.DepositRequest)
                            .Where(bd => leafBagIds.Contains(bd.BagId) && !bd.IsDeleted)
                            .ToListAsync();

                        var denomsByBagId = denomBagData.ToLookup(bd => bd.BagId);

                        var bagMeta = await _context.Bags
                            .Where(b => leafBagIds.Contains(b.Id))
                            .Select(b => new { b.Id, b.CategoryId, b.DenominationId })
                            .ToDictionaryAsync(b => b.Id, b => new { b.CategoryId, b.DenominationId });

                        // SubCategoryId per leaf bag (own, or fall back to parent bag)
                        var subCatByBagId = await ResolveSubCategoryIdsAsync(leafBagIds);

                        var bagKeys = leafBagIds.Select(bagId =>
                        {
                            var primary = denomsByBagId[bagId].FirstOrDefault();
                            var meta = bagMeta.GetValueOrDefault(bagId);
                            return new
                            {
                                BagId          = bagId,
                                DenomValue     = primary?.DepositRequestDenomination?.DenominationValue ?? 0m,
                                CbId           = primary?.DepositRequestDenomination?.DepositRequest?.CbId ?? 0,
                                CategoryId     = meta?.CategoryId ?? 0,
                                DenominationId = meta?.DenominationId ?? 0,
                                SubCategoryId  = subCatByBagId.GetValueOrDefault(bagId),
                                TotalValue     = denomsByBagId[bagId].Sum(bd =>
                                    (bd.DepositRequestDenomination?.DenominationValue ?? 0m) * 1000 * bd.NumberOfBundlesOrBoxes)
                            };
                        });

                        // One StockProcessingSession per (DenominationValue, CommercialBank, SubCategory) group, built from child bags
                        foreach (var group in bagKeys.GroupBy(x => (x.DenomValue, x.CbId, x.SubCategoryId)))
                        {
                            sessionCounter++;
                            var session = new StockProcessingSession
                            {
                                ReferenceNumber = $"SPS-{plan.ProcessingDate:ddMMyyyy}-{editBranchCode}-{sessionCounter:D2}",
                                StockProcessingPlanId = id,
                                ProcessingBoardId = assignment.ProcessingBoardId,
                                CommercialBankId = group.Key.CbId,
                                CategoryId = group.First().CategoryId,
                                DenominationId = group.First().DenominationId,
                                CurrentStatusId = sessionDraftStatus.Id,
                                TotalIssuedValue = group.Sum(x => x.TotalValue),
                                IsManual = !boardBpsMachineIds.TryGetValue(assignment.ProcessingBoardId, out var bpsMachineId) || !bpsMachineId.HasValue,
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

                // Update counting room supervisor assignments
                if (!string.IsNullOrEmpty(roomSupervisorsJson))
                {
                    var newSupervisors = roomSupervisorsJson == "[]"
                        ? []
                        : System.Text.Json.JsonSerializer.Deserialize<List<RoomSupervisorAssignment>>(roomSupervisorsJson, jsonOptions) ?? [];

                    var existingRoomSessions = await _countingRoomSessionRepository.FindByStockProcessingPlanId(id);
                    var existingByRoomId = existingRoomSessions.ToDictionary(rs => rs.CountingRoomId);

                    var newSupervisorByRoomId = newSupervisors
                        .Where(ns => !string.IsNullOrEmpty(ns.BoardSupervisorUserId))
                        .ToDictionary(ns => ns.CountingRoomId, ns => ns.BoardSupervisorUserId!);

                    foreach (var (roomId, userId) in newSupervisorByRoomId)
                    {
                        if (existingByRoomId.TryGetValue(roomId, out var existing))
                        {
                            if (existing.BoardSupervisorUserId != userId)
                            {
                                existing.BoardSupervisorUserId = userId;
                                await _countingRoomSessionRepository.Update(existing);
                            }
                        }
                        else
                        {
                            await _countingRoomSessionRepository.Create(new CountingRoomSession
                            {
                                CountingRoomId = roomId,
                                StockProcessingPlanId = id,
                                BoardSupervisorUserId = userId,
                                IsDeleted = false
                            });
                        }
                    }

                    foreach (var (roomId, existing) in existingByRoomId)
                    {
                        if (!newSupervisorByRoomId.ContainsKey(roomId))
                        {
                            existing.IsDeleted = true;
                            await _countingRoomSessionRepository.Update(existing);
                        }
                    }
                }

                // Update MI & Fit declarations
                if (!string.IsNullOrEmpty(miAndFitsJson))
                {
                    var existingMiFits = await _miAndFitRepository.FindByPlanId(id);
                    var existingByDenomId = existingMiFits.ToDictionary(m => m.DenominationId);
                    var newByDenomId = newMiFits.ToDictionary(m => m.DenominationId);

                    // Issued values are the branch's current Fit/MI stock positions — re-derived here so
                    // they reflect the position at save time and can't be set from the browser.
                    var issuedPositions = await GetIssuedMIandFitPositionsAsync(
                        plan.BranchId, newByDenomId.Keys.ToList());

                    foreach (var (denomId, mf) in newByDenomId)
                    {
                        var position = issuedPositions.GetValueOrDefault(denomId);

                        if (existingByDenomId.TryGetValue(denomId, out var existing))
                        {
                            // Returned* values are left untouched here — they belong to the assigned
                            // Board Supervisor's later declaration, not the maker's Create/Edit form.
                            existing.IssuedFitValue = position.Fit;
                            existing.IssuedUnFitValue = position.Mi;
                            existing.BoardSupervisorUserId = mf.BoardSupervisorUserId;
                            await _miAndFitRepository.Update(existing);
                        }
                        else
                        {
                            await _miAndFitRepository.Create(new MIandFit
                            {
                                StockProcessingPlanId = id,
                                DenominationId = denomId,
                                IssuedFitValue = position.Fit,
                                IssuedUnFitValue = position.Mi,
                                ReturnedFitValue = 0,
                                ReturnedUnFitValue = 0,
                                BoardSupervisorUserId = mf.BoardSupervisorUserId,
                                IsDeleted = false
                            });
                        }
                    }

                    foreach (var (denomId, existing) in existingByDenomId)
                    {
                        if (!newByDenomId.ContainsKey(denomId))
                        {
                            existing.IsDeleted = true;
                            await _miAndFitRepository.Update(existing);
                        }
                    }
                }

                TempData["Success"] = "Processing plan updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing processing plan {Id}", id);
                TempData["Error"] = "An error occurred while updating the plan.";
                return RedirectToAction(nameof(Details), new { id });
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

                // Load SubCategoryId for each bag that appears in a session
                var sessionBagIdList = planSessions
                    .SelectMany(s => s.ProcessingSessionBags)
                    .Select(psb => psb.BagId)
                    .Distinct().ToList();

                var sessionBagDenoms = await _context.BagDenominations
                    .Where(bd => sessionBagIdList.Contains(bd.BagId) && !bd.IsDeleted)
                    .Select(bd => new
                    {
                        bd.BagId,
                        bd.DepositRequestDenomination.DenominationValue,
                        bd.NumberOfBundlesOrBoxes
                    })
                    .ToListAsync();

                var denomDataByBagId = sessionBagDenoms
                    .GroupBy(x => x.BagId)
                    .ToDictionary(g => g.Key, g => new
                    {
                        DenominationValue = g.Select(x => x.DenominationValue).FirstOrDefault(),
                        TotalBundles = g.Sum(x => x.NumberOfBundlesOrBoxes)
                    });

                // SubCategoryId from Bag entity (set by AddBagToBagAsync for parent bags)
                var bagSubCatById = await _context.Bags
                    .Where(b => sessionBagIdList.Contains(b.Id) && !b.IsDeleted)
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

                var allSubCategories = await _stockService.GetAllSubCategories();
                var subCatDict = allSubCategories.ToDictionary(sc => sc.Id, sc => sc.Name);

                var allDenominations = await _stockService.GetAllDenominations();
                var denomValueById = allDenominations.ToDictionary(d => d.Id, d => d.Value);

                // Results summary per session (only sessions with at least one submitted result).
                // Each StockProcessingResult row is a cumulative snapshot of its session (see
                // StockProcessingSessionController.SaveResult), not an incremental delta, so only the
                // latest non-deleted row per session is used — summing rows would double-count
                // sessions whose results were saved more than once. Verified rows are preferred.
                var planSessionIds = planSessions.Select(s => s.Id).ToList();
                var planResults = await _resultRepository.FindBySessionIdsAsync(planSessionIds);
                var resultSummaries = planResults
                    .GroupBy(r => r.StockProcessingSessionId)
                    .Select(g =>
                    {
                        var session = planSessions.First(s => s.Id == g.Key);
                        var verified = g.Where(r => r.IsVerified).ToList();
                        var pool = verified.Count > 0 ? verified : g.ToList();
                        var latest = pool
                            .OrderByDescending(r => r.CreatedDate)
                            .ThenByDescending(r => r.Id)
                            .First();
                        return new SessionResultSummaryViewModel
                        {
                            SessionId = session.Id,
                            SessionReferenceNumber = session.ReferenceNumber,
                            DenominationValue = denomValueById.GetValueOrDefault(session.DenominationId),
                            TotalIssuedValue = session.TotalIssuedValue,
                            ReissueValue = latest.ReissueValue,
                            CanceledValue = latest.CanceledValue,
                            FitShredValue = latest.FitShredValue,
                            UnProcessedValue = latest.UnProcessedValue
                        };
                    })
                    .ToList();

                // Board-card bag chips: aggregate child bag bundles/amounts for parent bags
                var boardBagIds = sessionBags.Select(sb => sb.Bag.Id).Distinct().ToList();
                var boardChildBundlesByParent = new Dictionary<int, int>();
                var boardChildAmountByParent = new Dictionary<int, decimal>();

                if (boardBagIds.Count > 0)
                {
                    var boardChildMaps = await _context.BaggedBags
                        .Where(bb => boardBagIds.Contains(bb.ParentBagId) && !bb.IsDeleted)
                        .Select(bb => new { bb.ParentBagId, bb.ChildBagId })
                        .ToListAsync();

                    if (boardChildMaps.Count > 0)
                    {
                        var boardChildIds = boardChildMaps.Select(x => x.ChildBagId).Distinct().ToList();
                        var boardChildDenoms = await _context.BagDenominations
                            .Where(bd => boardChildIds.Contains(bd.BagId) && !bd.IsDeleted)
                            .Select(bd => new { bd.BagId, bd.DepositRequestDenomination.DenominationValue, bd.NumberOfBundlesOrBoxes })
                            .ToListAsync();

                        var boardChildDataById = boardChildDenoms
                            .GroupBy(x => x.BagId)
                            .ToDictionary(g => g.Key, g => new
                            {
                                TotalBundles = g.Sum(x => x.NumberOfBundlesOrBoxes),
                                TotalAmount = g.Sum(x => x.DenominationValue * 1000 * x.NumberOfBundlesOrBoxes)
                            });

                        foreach (var map in boardChildMaps)
                        {
                            if (!boardChildDataById.TryGetValue(map.ChildBagId, out var d)) continue;
                            boardChildBundlesByParent.TryGetValue(map.ParentBagId, out var eb);
                            boardChildBundlesByParent[map.ParentBagId] = eb + d.TotalBundles;
                            boardChildAmountByParent.TryGetValue(map.ParentBagId, out var ea);
                            boardChildAmountByParent[map.ParentBagId] = ea + d.TotalAmount;
                        }
                    }
                }

                // SubCategoryId for board-card bags (own or parent's)
                var boardBagSubCatById = await _context.Bags
                    .Where(b => boardBagIds.Contains(b.Id) && !b.IsDeleted)
                    .Select(b => new { b.Id, b.SubCategoryId })
                    .ToDictionaryAsync(b => b.Id, b => b.SubCategoryId);

                var boardBagsNoSubCat = boardBagSubCatById
                    .Where(kvp => !kvp.Value.HasValue).Select(kvp => kvp.Key).ToList();
                if (boardBagsNoSubCat.Count > 0)
                {
                    var boardParentSubCats = await _context.BaggedBags
                        .Where(bb => boardBagsNoSubCat.Contains(bb.ChildBagId) && !bb.IsDeleted)
                        .Join(_context.Bags, bb => bb.ParentBagId, b => b.Id,
                              (bb, b) => new { bb.ChildBagId, b.SubCategoryId })
                        .ToListAsync();
                    foreach (var item in boardParentSubCats.Where(x => x.SubCategoryId.HasValue))
                        boardBagSubCatById[item.ChildBagId] = item.SubCategoryId;
                }

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
                        EntityTypeName = StatusConstants.Entity.StockProcessingPlan,
                        CurrentStatusId = plan.CurrentStatusId,
                        AppRole = roleId,
                    });
                    if (transitions != null) availableTransitions.AddRange(transitions);
                }

                // Status history
                var historyEntities = await _planStatusHistoryRepository.FindByStockProcessingPlanId(id);
                var statusHistory = new List<StockProcessingPlanStatusHistoryViewModel>();
                foreach (var h in historyEntities)
                {
                    var oldStatus = await _workflowServiceClient.FindStatusById(h.OldStatusId);
                    var newStatus = await _workflowServiceClient.FindStatusById(h.NewStatusId);
                    var historyVm = new StockProcessingPlanStatusHistoryViewModel
                    {
                        Id = h.Id,
                        StockProcessingPlanId = h.StockProcessingPlanId,
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
                    .Where(a => !a.IsDeleted && a.EntityType == StatusConstants.Entity.StockProcessingPlan)
                    .Select(a => new AttachmentViewModel
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        FilePath = a.FilePath,
                        FileType = a.FileType,
                        FileSize = a.FileSize,
                        UploadedDate = a.CreatedDate
                    }).ToList();

                // Board supervisors per counting room for this plan
                var roomSessions = await _countingRoomSessionRepository.FindByStockProcessingPlanId(id);
                var supervisorNameByRoomId = new Dictionary<int, string?>();
                foreach (var rs in roomSessions.Where(rs => !string.IsNullOrEmpty(rs.BoardSupervisorUserId)))
                {
                    try
                    {
                        var user = await _identityService.GetUserByUsername(rs.BoardSupervisorUserId!);
                        supervisorNameByRoomId[rs.CountingRoomId] = user != null
                            ? $"{user.FirstName} {user.LastName}".Trim()
                            : rs.BoardSupervisorUserId;
                    }
                    catch
                    {
                        supervisorNameByRoomId[rs.CountingRoomId] = rs.BoardSupervisorUserId;
                    }
                }

                // MI & Fit — read-only, only denominations actually declared for this plan
                var planMiFits = await _miAndFitRepository.FindByPlanId(id);
                var miFitDenomDict = allDenominations.ToDictionary(d => d.Id);
                var miFitSupervisorNames = new Dictionary<string, string?>();
                foreach (var supervisorUserId in planMiFits
                    .Where(m => !string.IsNullOrEmpty(m.BoardSupervisorUserId))
                    .Select(m => m.BoardSupervisorUserId!)
                    .Distinct())
                {
                    try
                    {
                        var user = await _identityService.GetUserByUsername(supervisorUserId);
                        miFitSupervisorNames[supervisorUserId] = user != null
                            ? $"{user.FirstName} {user.LastName}".Trim()
                            : supervisorUserId;
                    }
                    catch
                    {
                        miFitSupervisorNames[supervisorUserId] = supervisorUserId;
                    }
                }

                var miAndFitViewRows = planMiFits
                    .Select(m =>
                    {
                        var denom = miFitDenomDict.GetValueOrDefault(m.DenominationId);
                        return new MIandFitViewModel
                        {
                            Id = m.Id,
                            StockProcessingPlanId = m.StockProcessingPlanId,
                            DenominationId = m.DenominationId,
                            DenominationName = denom?.Name,
                            DenominationValue = denom?.Value ?? 0,
                            IssuedFitValue = m.IssuedFitValue,
                            IssuedUnFitValue = m.IssuedUnFitValue,
                            ReturnedFitValue = m.ReturnedFitValue,
                            ReturnedUnFitValue = m.ReturnedUnFitValue,
                            BoardSupervisorUserId = m.BoardSupervisorUserId,
                            BoardSupervisorName = !string.IsNullOrEmpty(m.BoardSupervisorUserId)
                                ? miFitSupervisorNames.GetValueOrDefault(m.BoardSupervisorUserId)
                                : null
                        };
                    })
                    .OrderBy(m => m.DenominationValue)
                    .ToList();

                var model = new StockProcessingPlanDetailsViewModel
                {
                    Id = plan.Id,
                    ReferenceNumber = plan.ReferenceNumber,
                    ProcessingDate = plan.ProcessingDate,
                    BranchId = plan.BranchId,
                    BranchName = branch?.Name,
                    CurrentStatusId = plan.CurrentStatusId,
                    CurrentStatusName = statusType.Name,
                    CountingRooms = branchRooms.Select(room => new CountingRoomWithBagsViewModel
                    {
                        Id = room.Id,
                        Name = room.Name,
                        BoardSupervisorName = supervisorNameByRoomId.GetValueOrDefault(room.Id),
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
                                        TotalBundles = sb.Bag.BagDenominations.Sum(bd => bd.NumberOfBundlesOrBoxes)
                                            + boardChildBundlesByParent.GetValueOrDefault(sb.Bag.Id),
                                        TotalAmount = sb.Bag.BagDenominations
                                            .Sum(bd => bd.DepositRequestDenomination.DenominationValue * 1000 * bd.NumberOfBundlesOrBoxes)
                                            + boardChildAmountByParent.GetValueOrDefault(sb.Bag.Id),
                                        SubCategoryName = boardBagSubCatById.TryGetValue(sb.Bag.Id, out var scId) && scId.HasValue
                                            ? subCatDict.GetValueOrDefault(scId.Value)
                                            : null
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
                        DenominationValue = denomValueById.GetValueOrDefault(s.DenominationId),
                        CurrentStatusName = sessionStatusNames.GetValueOrDefault(s.CurrentStatusId, string.Empty),
                        Bags = s.ProcessingSessionBags
                            .Select(psb =>
                            {
                                var data = denomDataByBagId.GetValueOrDefault(psb.BagId);
                                var bagSubCatId = bagSubCatById.GetValueOrDefault(psb.BagId);
                                var subCatName = bagSubCatId.HasValue
                                    ? subCatDict.GetValueOrDefault(bagSubCatId.Value)
                                    : null;
                                var denomVal = data?.DenominationValue ?? 0m;
                                var bundles = data?.TotalBundles ?? 0;
                                return new SessionBagInfo
                                {
                                    ReferenceNumber = psb.Bag.ReferenceNumber,
                                    SubCategoryName = string.IsNullOrEmpty(subCatName) ? null : subCatName,
                                    DenominationValue = denomVal,
                                    TotalBundles = bundles,
                                    TotalAmount = denomVal * 1000 * bundles
                                };
                            })
                            .ToList()
                    }).ToList(),
                    ResultSummaries = resultSummaries,
                    MIandFits = miAndFitViewRows,
                    StatusHistory = statusHistory,
                    AvailableTransitions = availableTransitions.Distinct().ToList(),
                    AllAttachments = allAttachments
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

        [HttpGet]
        public async Task<IActionResult> PrintProcessingSessions(int id)
        {
            try
            {
                var plan = await _planRepository.FindById(id);
                if (plan == null) return NotFound();

                var allBouBranches = await _configServiceClient.GetAllBouBranchesAsync();
                var branch = allBouBranches.FirstOrDefault(b => b.Id == plan.BranchId);

                var planSessions = await _sessionRepository.GetByPlanIdAsync(id);
                if (planSessions.Count == 0)
                {
                    TempData["Error"] = "This plan has no processing sessions to print.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var allBanks = await _commercialBankRepository.FindAll();
                var bankDict = allBanks.ToDictionary(b => b.Id, b => b.Name);

                var sessionBagIds = planSessions
                    .SelectMany(s => s.ProcessingSessionBags)
                    .Select(psb => psb.BagId)
                    .Distinct()
                    .ToList();

                // Amount per (bag, denomination) — a bag can in principle carry more than one denomination.
                var bagDenomData = await _context.BagDenominations
                    .Where(bd => sessionBagIds.Contains(bd.BagId) && !bd.IsDeleted)
                    .Select(bd => new
                    {
                        bd.BagId,
                        bd.DepositRequestDenomination.DenominationId,
                        bd.DepositRequestDenomination.DenominationValue,
                        bd.NumberOfBundlesOrBoxes
                    })
                    .ToListAsync();

                var amountByBagAndDenom = bagDenomData
                    .GroupBy(x => (x.BagId, x.DenominationId))
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.DenominationValue * 1000 * x.NumberOfBundlesOrBoxes));

                // Distinct denominations found across the plan's bags, ascending by value.
                var denominationColumns = bagDenomData
                    .GroupBy(x => x.DenominationId)
                    .Select(g => new PrintDenominationColumn { DenominationId = g.Key, Value = g.First().DenominationValue })
                    .OrderBy(c => c.Value)
                    .ToList();

                // SubCategoryId per bag (own, or fall back to parent bag) — same fallback used in Details.
                var bagSubCatById = await _context.Bags
                    .Where(b => sessionBagIds.Contains(b.Id) && !b.IsDeleted)
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

                var allSubCategories = await _stockService.GetAllSubCategories();
                var subCatDict = allSubCategories.ToDictionary(sc => sc.Id, sc => sc.Name);

                // Board supervisor per counting room for this plan
                var roomSessions = await _countingRoomSessionRepository.FindByStockProcessingPlanId(id);
                var supervisorNameByRoomId = new Dictionary<int, string?>();
                foreach (var rs in roomSessions.Where(rs => !string.IsNullOrEmpty(rs.BoardSupervisorUserId)))
                {
                    try
                    {
                        var user = await _identityService.GetUserByUsername(rs.BoardSupervisorUserId!);
                        supervisorNameByRoomId[rs.CountingRoomId] = user != null
                            ? $"{user.FirstName} {user.LastName}".Trim()
                            : rs.BoardSupervisorUserId;
                    }
                    catch
                    {
                        supervisorNameByRoomId[rs.CountingRoomId] = rs.BoardSupervisorUserId;
                    }
                }

                var rows = new List<ProcessingSessionPrintRowViewModel>();
                foreach (var session in planSessions)
                {
                    var bagIdsForSession = session.ProcessingSessionBags
                        .Select(psb => psb.BagId)
                        .ToList();

                    var subCatId = bagIdsForSession
                        .Select(bagId => bagSubCatById.GetValueOrDefault(bagId))
                        .FirstOrDefault(sc => sc.HasValue);
                    var subCatName = subCatId.HasValue ? subCatDict.GetValueOrDefault(subCatId.Value) : null;

                    var amountByDenom = new Dictionary<int, decimal>();
                    decimal rowTotal = 0m;
                    foreach (var col in denominationColumns)
                    {
                        var amount = bagIdsForSession.Sum(bagId => amountByBagAndDenom.GetValueOrDefault((bagId, col.DenominationId)));
                        amountByDenom[col.DenominationId] = amount;
                        rowTotal += amount;
                    }

                    var supervisorName = session.ProcessingBoard != null
                        ? supervisorNameByRoomId.GetValueOrDefault(session.ProcessingBoard.CountingRoomId)
                        : null;

                    rows.Add(new ProcessingSessionPrintRowViewModel
                    {
                        SessionId = session.Id,
                        WasteNumber = session.ReferenceNumber,
                        CommercialBankName = bankDict.GetValueOrDefault(session.CommercialBankId),
                        SubCategoryName = subCatName,
                        BoardSupervisorName = supervisorName,
                        AmountByDenominationId = amountByDenom,
                        Total = rowTotal
                    });
                }

                var model = new ProcessingSessionsPrintViewModel
                {
                    PlanId = plan.Id,
                    PlanReferenceNumber = plan.ReferenceNumber,
                    ProcessingDate = plan.ProcessingDate,
                    BranchName = branch?.Name,
                    DenominationColumns = denominationColumns,
                    Rows = rows
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating processing sessions print report for plan {Id}", id);
                TempData["Error"] = "An error occurred while generating the print report.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(int id, string actionName, string remarks)
        {
            try
            {
                var plan = await _planRepository.FindById(id);
                if (plan == null)
                {
                    TempData["Error"] = "Processing Plan not found.";
                    return RedirectToAction(nameof(Index));
                }

                var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "System";
                var userRoles = User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                    .Select(c => int.TryParse(c.Value.Split(":")[1], out var rid) ? rid : (int?)null)
                    .Where(r => r.HasValue).Select(r => r!.Value).ToList();

                var transition = await _workflowServiceClient.FindByTransitionKey(new GetTransitionByKeyRequestDto
                {
                    EntityTypeName = StatusConstants.Entity.StockProcessingPlan,
                    FromStatusId = plan.CurrentStatusId,
                    ActionName = actionName,
                });

                List<long>? uploadedAttachmentIds = null;
                if (transition != null && transition.RequiresAttachments && transition.AttachmentRequirements.Any())
                {
                    uploadedAttachmentIds = await HandleAttachmentUploads(
                        transition.AttachmentRequirements.ToList(),
                        transition.Id,
                        id,
                        StatusConstants.Entity.StockProcessingPlan,
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

                if (actionName.Equals("IssueToBoards"))
                {
                    // Validate that all counting rooms involved in this plan have a board supervisor assigned
                    var planBoardIds = await _context.SessionBags
                        .Where(sb => sb.StockProcessingPlanId == id && !sb.IsDeleted)
                        .Select(sb => sb.ProcessingBoardId)
                        .Distinct()
                        .ToListAsync();

                    var involvedRoomIds = await _context.ProcessingBoards
                        .Where(pb => planBoardIds.Contains(pb.Id) && !pb.IsDeleted)
                        .Select(pb => pb.CountingRoomId)
                        .Distinct()
                        .ToListAsync();

                    var assignedRoomIds = await _context.CountingRoomSessions
                        .Where(crs => crs.StockProcessingPlanId == id
                            && !crs.IsDeleted
                            && !string.IsNullOrEmpty(crs.BoardSupervisorUserId))
                        .Select(crs => crs.CountingRoomId)
                        .Distinct()
                        .ToListAsync();

                    var unsupervised = involvedRoomIds.Except(assignedRoomIds).ToList();
                    if (unsupervised.Count > 0)
                    {
                        var roomNames = await _context.CountingRooms
                            .Where(cr => unsupervised.Contains(cr.Id))
                            .Select(cr => cr.Name)
                            .ToListAsync();

                        TempData["Error"] = $"Cannot issue to boards — the following counting room(s) have no Board Supervisor assigned: {string.Join(", ", roomNames)}.";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                }

                bool isExecuted = await _workflowService.ExecuteTransition(
                    StatusConstants.Entity.StockProcessingPlan,
                    id,
                    plan.CurrentStatusId,
                    actionName,
                    username,
                    userRoles,
                    remarks,
                    uploadedAttachmentIds);

                if (isExecuted)
                {
                    if (actionName.Equals("IssueToBoards"))
                    {
                        var issuedStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                        {
                            ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                            EntityTypeName = StatusConstants.Entity.StockProcessingSession,
                            StatusName = StatusConstants.StockProcessingSession.IssuedToBoard
                        });

                        if (issuedStatus != null)
                        {
                            var sessions = await _sessionRepository.GetByPlanIdAsync(id);
                            foreach (var session in sessions.Where(s => !s.IsDeleted))
                            {
                                session.CurrentStatusId = issuedStatus.Id;
                                await _sessionRepository.Update(session);
                            }
                        }

                        var targetVault = await _context.Vaults
                            .FirstOrDefaultAsync(v => v.BranchId == plan.BranchId
                                && v.Name == VaultConstants.BagsWithBoardsVaultName
                                && !v.IsDeleted);

                        if (targetVault != null)
                        {
                            var targetSection = await _context.VaultSections
                                .FirstOrDefaultAsync(vs => vs.VaultId == targetVault.Id
                                    && vs.Name == VaultConstants.BagsWithBoardsSectionName
                                    && !vs.IsDeleted);

                            if (targetSection != null)
                            {
                                var planBagIds = await _context.SessionBags
                                    .Where(sb => sb.StockProcessingPlanId == id && !sb.IsDeleted)
                                    .Select(sb => sb.BagId)
                                    .Distinct()
                                    .ToListAsync();

                                var planBags = await _context.Bags
                                    .Where(b => planBagIds.Contains(b.Id) && !b.IsDeleted && b.VaultSectionId.HasValue)
                                    .ToListAsync();

                                foreach (var bag in planBags)
                                {
                                    await _bagMovementRepository.Create(new Data.Entities.BagMovement
                                    {
                                        BagId = bag.Id,
                                        OldVaultSectionId = bag.VaultSectionId!.Value,
                                        NewVaultSectionId = targetSection.Id,
                                        CreatedBy = username,
                                        ModifiedBy = username
                                    });

                                    bag.VaultSectionId = targetSection.Id;
                                }

                                await _context.SaveChangesAsync();
                            }
                        }


                        // Create StockMovementRecord and Update Stock Position for Money With Boards
                        PlanBagSummaryResult planBagSummary = await _planRepository.GetBagSummaryAsync(id);

                        var allCategoriesIssue = await _categoryRepository.FindAll();
                        Category paids = allCategoriesIssue.FirstOrDefault(c => c.Name == StockConstants.StoreTypes.Paids);
                        Category moneyWithBoards = allCategoriesIssue.FirstOrDefault(c => c.Name == StockConstants.StoreTypes.MWB);

                        var counterPartyType = await _counterPartyTypeRepository.FindByName(StockConstants.StockCounterPartyType.Category);

                        if (paids == null || moneyWithBoards == null || counterPartyType == null)
                        {
                            var missing = new List<string>();
                            if (paids == null) missing.Add(StockConstants.StoreTypes.Paids);
                            if (moneyWithBoards == null) missing.Add(StockConstants.StoreTypes.MWB);
                            if (counterPartyType == null) missing.Add(StockConstants.StockCounterPartyType.Category);
                            TempData["Error"] = $"Cannot update stock position — missing configuration: {string.Join(", ", missing)}.";
                            return RedirectToAction(nameof(Details), new { id });
                        }

                        StatusTypeDto receivedStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                        {
                            ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                            StatusName = StatusConstants.StockMovement.Received,
                            EntityTypeName = StatusConstants.Entity.StockMovement,
                        });

                        var movement = new StockMovement
                        {
                            MovementType = StockConstants.StockMovementTypes.PaidsToMWB,
                            Date = DateTime.Now,
                            ReferenceNumber = plan.ReferenceNumber,
                            DispatchingBranchId = null,
                            ReceivingBranchId = null,
                            TotalBagsBoxes = null,
                            GrandTotalValue = planBagSummary.GrandTotalValue,
                            CurrentStatusId = receivedStatus.Id,
                            CurrentStatusName = receivedStatus.Name,
                            Denominations = planBagSummary.Denominations
                            //Denominations = [.. cbDenoms.Select(x => {
                            //    return new StockMovementDenomination
                            //    {
                            //        DenominationId = x.DenominationId,
                            //        NumBagsBoxes = x.Boxes,
                            //        NumBundles = x.Bundles,
                            //        CategoryId = paids?.Id,
                            //        ContainerType = x.Boxes == null ? StockConstants.ContainerTypes.Bag : StockConstants.ContainerTypes.Box,
                            //        SubTotalValue = x.Amount,
                            //        Categorization = paids?.Name
                            //    };
                            //})]
                        };

                        StockMovement newStockMovement = await _stockMovementRepository.CreateAsync(movement);

                        List<StockLedgerDetail> stockLedgerDetails = new();

                        foreach (var denom in planBagSummary.Denominations)
                        {
                            StockLedgerDetail stockLedgerDetail = new()
                            {
                                CreatedDate = DateTime.Now,
                                ModifiedDate = DateTime.Now,
                                CreatedBy = username ?? "unknown",
                                ModifiedBy = username ?? "unknown",
                                DenominationId = denom.DenominationId,
                                NumberOfBoxes = denom.NumBagsBoxes ?? 0,
                                NumberOfBundles = denom.NumBundles,
                                NumberOfPieces = ((denom.NumBundles ?? 0) * 1000) + ((denom.NumBagsBoxes ?? 0) * 1000),
                                Value = denom.SubTotalValue,
                                CategoryId = paids.Id,
                                //SubCategoryId = denom.SubCategoryId
                            };
                            stockLedgerDetails.Add(stockLedgerDetail);
                        }

                        // Create StockLedger entry for Removing from Paids
                        StockLedger paidsEntry = new()
                        {
                            ReferenceCode = plan.ReferenceNumber,
                            StoreId = plan.BranchId, //Branch
                            IsIn = false,
                            CounterPartyId = moneyWithBoards.Id,
                            CounterPartyTypeId = counterPartyType.Id, //Currency Category
                            Notes = remarks,
                            CreatedBy = username ?? "unknown",
                            CreatedDate = DateTime.Now,
                            ModifiedBy = username ?? "unknown",
                            ModifiedDate = DateTime.Now,
                            StockLedgerDetails = stockLedgerDetails,
                            StockMovementId = newStockMovement.Id
                        };

                        bool createdLedgerRecord = await _stockLedgerRepository.Create(paidsEntry);
                        if (!createdLedgerRecord)
                        {
                            TempData["Error"] = "Error Updating Stock Position";
                        }

                        // Create StockLedger entry for Adding to MWB
                        foreach (var denom in stockLedgerDetails)
                        {
                            denom.CategoryId = moneyWithBoards.Id;
                        }

                        StockLedger moneyWithBoardsEntry = new()
                        {
                            ReferenceCode = plan.ReferenceNumber,
                            StoreId = plan.BranchId, //Branch
                            IsIn = true,
                            CounterPartyId = paids.Id,
                            CounterPartyTypeId = counterPartyType.Id, //Currency Category
                            Notes = remarks,
                            CreatedBy = username ?? "unknown",
                            CreatedDate = DateTime.Now,
                            ModifiedBy = username ?? "unknown",
                            ModifiedDate = DateTime.Now,
                            StockLedgerDetails = stockLedgerDetails,
                            StockMovementId = newStockMovement.Id
                        };

                        bool createdLedgerRecordMWB = await _stockLedgerRepository.Create(moneyWithBoardsEntry);
                        if (!createdLedgerRecordMWB)
                        {
                            TempData["Error"] = "Error Updating Stock Position";
                        }

                        TempData["Success"] = "Stocks Issued Successfully. Stock position Updated";

                    }

                    if (actionName.Equals("VerifyResults"))
                    {
                        var sessionIds = await _context.StockProcessingSessions
                            .Where(s => s.StockProcessingPlanId == id && !s.IsDeleted)
                            .Select(s => s.Id)
                            .ToListAsync();

                        var results = await _context.StockProcessingResults
                            .Where(r => sessionIds.Contains(r.StockProcessingSessionId))
                            .ToListAsync();

                        foreach (var result in results)
                            result.IsVerified = true;

                        await _context.SaveChangesAsync();

                        TempData["Success"] = "Results verified successfully.";
                    }

                    if (actionName.Equals("ConfirmResults"))
                    {
                        // At ConfirmResults: Reissue increases by the reissue value, Cancelled increases
                        // by the cancelled value, and MWB decreases by reissue + cancelled + shredded —
                        // so MWB's ending balance equals the session results' UnProcessedValue (the money
                        // still with boards). Shredded money is destroyed: it only reduces MWB, with no
                        // offsetting category increase.
                        var allCategoriesConfirm = await _categoryRepository.FindAll();
                        Category reIssue = allCategoriesConfirm.FirstOrDefault(c => c.Name == StockConstants.StoreTypes.Reissue);
                        Category cancelled = allCategoriesConfirm.FirstOrDefault(c => c.Name == StockConstants.StoreTypes.Cancelled);
                        Category moneyWithBoards = allCategoriesConfirm.FirstOrDefault(c => c.Name == StockConstants.StoreTypes.MWB);

                        var counterPartyType = await _counterPartyTypeRepository.FindByName(StockConstants.StockCounterPartyType.Category);

                        if (reIssue == null || cancelled == null || moneyWithBoards == null || counterPartyType == null)
                        {
                            var missing = new List<string>();
                            if (reIssue == null) missing.Add(StockConstants.StoreTypes.Reissue);
                            if (cancelled == null) missing.Add(StockConstants.StoreTypes.Cancelled);
                            if (moneyWithBoards == null) missing.Add(StockConstants.StoreTypes.MWB);
                            if (counterPartyType == null) missing.Add(StockConstants.StockCounterPartyType.Category);
                            TempData["Error"] = $"Cannot update stock position — missing configuration: {string.Join(", ", missing)}.";
                            return RedirectToAction(nameof(Details), new { id });
                        }

                        StatusTypeDto receivedStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                        {
                            ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                            StatusName = StatusConstants.StockMovement.Received,
                            EntityTypeName = StatusConstants.Entity.StockMovement,
                        });

                        // Latest (not summed) result row per session — see GetConfirmedResultsSummaryAsync.
                        var confirmedResults = await _planRepository.GetConfirmedResultsSummaryAsync(id);

                        var allDenominationsConfirm = await _stockService.GetAllDenominations();
                        var denomValueByIdConfirm = allDenominationsConfirm.ToDictionary(d => d.Id, d => d.Value);

                        // Builds one StockMovementDenomination per denomination with a non-zero amount,
                        // deriving bundle/piece counts from the denomination value (value = denomValue * 1000 * bundles,
                        // matching the convention used elsewhere when sessions are built from bags).
                        List<StockMovementDenomination> BuildMovementDenoms(Func<SessionResultDenominationSummary, decimal> amountSelector)
                        {
                            var list = new List<StockMovementDenomination>();
                            foreach (var d in confirmedResults.Denominations)
                            {
                                var amount = amountSelector(d);
                                if (amount == 0) continue;

                                var denomValue = denomValueByIdConfirm.GetValueOrDefault(d.DenominationId);
                                var numBundles = denomValue > 0 ? (int)Math.Round(amount / (denomValue * 1000)) : 0;

                                list.Add(new StockMovementDenomination
                                {
                                    DenominationId = d.DenominationId,
                                    NumBundles = numBundles,
                                    SubTotalValue = amount
                                });
                            }
                            return list;
                        }

                        // Independent StockLedgerDetail list for one ledger leg — never share/mutate a
                        // list between two StockLedger entries.
                        List<StockLedgerDetail> BuildLedgerDetails(List<StockMovementDenomination> denoms, int categoryId) =>
                            denoms.Select(denom => new StockLedgerDetail
                            {
                                CreatedDate = DateTime.Now,
                                ModifiedDate = DateTime.Now,
                                CreatedBy = username ?? "unknown",
                                ModifiedBy = username ?? "unknown",
                                DenominationId = denom.DenominationId,
                                NumberOfBoxes = denom.NumBagsBoxes ?? 0,
                                NumberOfBundles = denom.NumBundles,
                                NumberOfPieces = ((denom.NumBundles ?? 0) * 1000) + ((denom.NumBagsBoxes ?? 0) * 1000),
                                Value = denom.SubTotalValue,
                                CategoryId = categoryId
                            }).ToList();

                        // Posts a movement that increases targetCategory and reduces MWB by the same amount.
                        // No-op when there's nothing to post for this leg.
                        async Task PostIncreaseFromMwbAsync(string movementType, List<StockMovementDenomination> movementDenoms, Category targetCategory)
                        {
                            if (movementDenoms.Count == 0) return;

                            var movement = new StockMovement
                            {
                                MovementType = movementType,
                                Date = DateTime.Now,
                                ReferenceNumber = plan.ReferenceNumber,
                                DispatchingBranchId = null,
                                ReceivingBranchId = null,
                                TotalBagsBoxes = null,
                                GrandTotalValue = movementDenoms.Sum(d => d.SubTotalValue),
                                CurrentStatusId = receivedStatus.Id,
                                CurrentStatusName = receivedStatus.Name,
                                Denominations = movementDenoms
                            };
                            var newMovement = await _stockMovementRepository.CreateAsync(movement);

                            var increaseEntry = new StockLedger
                            {
                                ReferenceCode = plan.ReferenceNumber,
                                StoreId = plan.BranchId, //Branch
                                IsIn = true,
                                CounterPartyId = moneyWithBoards.Id,
                                CounterPartyTypeId = counterPartyType.Id, //Currency Category
                                Notes = remarks,
                                CreatedBy = username ?? "unknown",
                                CreatedDate = DateTime.Now,
                                ModifiedBy = username ?? "unknown",
                                ModifiedDate = DateTime.Now,
                                StockLedgerDetails = BuildLedgerDetails(movementDenoms, targetCategory.Id),
                                StockMovementId = newMovement.Id
                            };
                            if (!await _stockLedgerRepository.Create(increaseEntry))
                                TempData["Error"] = "Error Updating Stock Position";

                            var mwbDecreaseEntry = new StockLedger
                            {
                                ReferenceCode = plan.ReferenceNumber,
                                StoreId = plan.BranchId, //Branch
                                IsIn = false,
                                CounterPartyId = targetCategory.Id,
                                CounterPartyTypeId = counterPartyType.Id, //Currency Category
                                Notes = remarks,
                                CreatedBy = username ?? "unknown",
                                CreatedDate = DateTime.Now,
                                ModifiedBy = username ?? "unknown",
                                ModifiedDate = DateTime.Now,
                                StockLedgerDetails = BuildLedgerDetails(movementDenoms, moneyWithBoards.Id),
                                StockMovementId = newMovement.Id
                            };
                            if (!await _stockLedgerRepository.Create(mwbDecreaseEntry))
                                TempData["Error"] = "Error Updating Stock Position";
                        }

                        // Posts a movement that only reduces MWB — for shredded money, which is destroyed
                        // rather than moved into another category.
                        async Task PostShredFromMwbAsync(List<StockMovementDenomination> movementDenoms)
                        {
                            if (movementDenoms.Count == 0) return;

                            var movement = new StockMovement
                            {
                                MovementType = StockConstants.StockMovementTypes.MWBToShredded,
                                Date = DateTime.Now,
                                ReferenceNumber = plan.ReferenceNumber,
                                DispatchingBranchId = null,
                                ReceivingBranchId = null,
                                TotalBagsBoxes = null,
                                GrandTotalValue = movementDenoms.Sum(d => d.SubTotalValue),
                                CurrentStatusId = receivedStatus.Id,
                                CurrentStatusName = receivedStatus.Name,
                                Denominations = movementDenoms
                            };
                            var newMovement = await _stockMovementRepository.CreateAsync(movement);

                            var mwbDecreaseEntry = new StockLedger
                            {
                                ReferenceCode = plan.ReferenceNumber,
                                StoreId = plan.BranchId, //Branch
                                IsIn = false,
                                CounterPartyId = moneyWithBoards.Id,
                                CounterPartyTypeId = counterPartyType.Id, //Currency Category
                                Notes = remarks,
                                CreatedBy = username ?? "unknown",
                                CreatedDate = DateTime.Now,
                                ModifiedBy = username ?? "unknown",
                                ModifiedDate = DateTime.Now,
                                StockLedgerDetails = BuildLedgerDetails(movementDenoms, moneyWithBoards.Id),
                                StockMovementId = newMovement.Id
                            };
                            if (!await _stockLedgerRepository.Create(mwbDecreaseEntry))
                                TempData["Error"] = "Error Updating Stock Position";
                        }

                        await PostIncreaseFromMwbAsync(StockConstants.StockMovementTypes.MWBToReIssue,
                            BuildMovementDenoms(d => d.ReissueValue), reIssue);

                        await PostIncreaseFromMwbAsync(StockConstants.StockMovementTypes.MWBToCancelled,
                            BuildMovementDenoms(d => d.CanceledValue), cancelled);

                        await PostShredFromMwbAsync(BuildMovementDenoms(d => d.FitShredValue));

                        var planBagIds = await _context.SessionBags
                            .Where(sb => sb.StockProcessingPlanId == id && !sb.IsDeleted)
                            .Select(sb => sb.BagId)
                            .Distinct()
                            .ToListAsync();

                        var planBags = await _context.Bags
                            .Where(b => planBagIds.Contains(b.Id) && !b.IsDeleted)
                            .ToListAsync();

                        foreach (var bag in planBags)
                            bag.VaultSectionId = null;

                        await _context.SaveChangesAsync();

                        TempData["Success"] = "Stocks Confirmed Successfully. Stock position Updated";
                    }
                }
                else
                {
                    TempData["Error"] = $"Failed to execute {actionName} action. You may not have permission or the action is not valid for the current status.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing action {Action} on Processing Plan {Id}", actionName, id);
                TempData["Error"] = "An error occurred while executing the action.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Resolves each bag's own SubCategoryId, falling back to its parent bag's SubCategoryId
        /// when the bag itself doesn't have one set (same fallback used for display in Details).
        /// </summary>
        private async Task<Dictionary<int, int?>> ResolveSubCategoryIdsAsync(List<int> bagIds)
        {
            var subCatByBagId = await _context.Bags
                .Where(b => bagIds.Contains(b.Id) && !b.IsDeleted)
                .Select(b => new { b.Id, b.SubCategoryId })
                .ToDictionaryAsync(b => b.Id, b => b.SubCategoryId);

            var bagsWithNoSubCat = subCatByBagId
                .Where(kvp => !kvp.Value.HasValue).Select(kvp => kvp.Key).ToList();
            if (bagsWithNoSubCat.Count > 0)
            {
                var parentSubCats = await _context.BaggedBags
                    .Where(bb => bagsWithNoSubCat.Contains(bb.ChildBagId) && !bb.IsDeleted)
                    .Join(_context.Bags, bb => bb.ParentBagId, b => b.Id,
                          (bb, b) => new { bb.ChildBagId, b.SubCategoryId })
                    .ToListAsync();
                foreach (var item in parentSubCats.Where(x => x.SubCategoryId.HasValue))
                    subCatByBagId[item.ChildBagId] = item.SubCategoryId;
            }

            return subCatByBagId;
        }

        private async Task<List<long>> HandleAttachmentUploads(
            List<TransitionAttachmentRequirementDto> requirements,
            int workflowTransitionId,
            int entityId,
            string entityType,
            string username)
        {
            var uploadedAttachmentIds = new List<long>();
            var uploadsPath = Path.Combine(FileStorageConstants.UploadsRoot, "stockprocessingplan", entityId.ToString());
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
                        FilePath = Path.Combine("uploads", "stockprocessingplan", entityId.ToString(), uniqueName),
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

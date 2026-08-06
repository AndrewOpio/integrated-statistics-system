using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Core.Models.StockLedgers;
using VMSService.Core.Repositories.CommercialBanks;
using VMSService.Core.Repositories.CounterPartyTypes;
using VMSService.Core.Repositories.CurrencyVendors;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Core.Static;

namespace VMSService.Controllers
{
    [Authorize]
    public class StockLedgerController : Controller
    {
        private readonly IStockService _stockService;
        private readonly IConfigurationsServiceClient _configServiceClient;
        private readonly ICounterPartyTypeRepository _counterPartyTypeRepository;
        private readonly ICommercialBankRepository _commercialBankRepository;
        private readonly ICurrencyVendorRepository _currencyVendorRepository;
        private readonly ILogger<StockLedgerController> _logger;

        public StockLedgerController(
            IStockService stockService,
            IConfigurationsServiceClient configServiceClient,
            ICounterPartyTypeRepository counterPartyTypeRepository,
            ICommercialBankRepository commercialBankRepository,
            ICurrencyVendorRepository currencyVendorRepository,
            ILogger<StockLedgerController> logger)
        {
            _stockService = stockService;
            _configServiceClient = configServiceClient;
            _counterPartyTypeRepository = counterPartyTypeRepository;
            _commercialBankRepository = commercialBankRepository;
            _currencyVendorRepository = currencyVendorRepository;
            _logger = logger;
        }

        // GET: StockLedger
        [Breadcrumb("Stock Ledger", FromController = typeof(HomeController), FromAction = nameof(Index))]
        public async Task<IActionResult> Index(int? branchId = null, bool allBranches = false)
        {
            var allBouBranches = await _configServiceClient.GetAllBouBranchesAsync();

            // Get user roles
            var userRoles = User.FindAll("appRole")
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();

            // Filter branches based on role names matching branch names
            List<BranchDto> userBranches = allBouBranches
                .Where(branch => userRoles.Any(role => role.Contains(branch.Name)))
                .ToList();

            // Default to the user's own branch; fall back to all branches when they have none
            if (!allBranches && branchId == null)
            {
                branchId = userBranches.FirstOrDefault()?.Id;
                allBranches = branchId == null;
            }

            // A null branchId makes the API return every branch
            var entries = await _stockService.GetStockLedgerDetailsByBranch(allBranches ? null : branchId);

            ViewBag.Branches = allBouBranches;
            ViewBag.SelectedBranchId = allBranches ? null : branchId;
            ViewBag.AllBranches = allBranches;
            ViewBag.BranchNames = allBouBranches.ToDictionary(b => b.Id, b => b.Name);

            return View(entries.ToList());
        }

        // GET: StockLedger/Create
        [Breadcrumb("Add To Ledger", FromController = typeof(StockLedgerController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            var model = new StockLedgerCreateViewModel
            {
                ReferenceCode = GenerateReferenceCode()
            };

            // Default the store to the user's own branch when it can be inferred from their roles.
            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            var userRoles = User.FindAll("appRole")
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();
            model.StoreId = branches
                .FirstOrDefault(b => userRoles.Any(role => role.Contains(b.Name)))?.Id ?? 0;

            await PopulateLookups(model);
            return View(model);
        }

        // POST: StockLedger/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StockLedgerCreateViewModel model)
        {
            var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            List<StockLedgerDetailEntry> details;
            try
            {
                details = string.IsNullOrWhiteSpace(model.DetailsJson) || model.DetailsJson == "[]"
                    ? []
                    : System.Text.Json.JsonSerializer.Deserialize<List<StockLedgerDetailEntry>>(model.DetailsJson, jsonOptions) ?? [];
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogWarning(ex, "Malformed stock ledger details payload");
                ModelState.AddModelError(string.Empty, "The captured ledger lines could not be read. Please re-enter them.");
                details = [];
            }

            if (details.Count == 0)
                ModelState.AddModelError(string.Empty, "Add at least one ledger line before saving.");

            // Validate the lines against the real lookups so a hand-crafted post cannot slip
            // through with an unknown denomination, category, or mismatched sub-category.
            var denominations = (await _stockService.GetAllDenominations()).Where(d => !d.IsDeleted).ToList();
            var categories = (await _stockService.GetAllCategories()).Where(c => !c.IsDeleted).ToList();
            var subCategories = (await _stockService.GetAllSubCategories()).Where(sc => !sc.IsDeleted).ToList();

            var denomIds = denominations.Select(d => d.Id).ToHashSet();
            var categoryIds = categories.Select(c => c.Id).ToHashSet();
            var subCategoryById = subCategories.ToDictionary(sc => sc.Id);

            for (var i = 0; i < details.Count; i++)
            {
                var line = details[i];
                var label = $"Line {i + 1}";

                if (!denomIds.Contains(line.DenominationId))
                    ModelState.AddModelError(string.Empty, $"{label}: select a valid denomination.");

                if (!categoryIds.Contains(line.CategoryId))
                    ModelState.AddModelError(string.Empty, $"{label}: select a valid category.");

                if (line.SubCategoryId is > 0)
                {
                    if (!subCategoryById.TryGetValue(line.SubCategoryId.Value, out var subCategory))
                        ModelState.AddModelError(string.Empty, $"{label}: select a valid sub-category.");
                    else if (subCategory.CategoryId != line.CategoryId)
                        ModelState.AddModelError(string.Empty, $"{label}: the sub-category does not belong to the selected category.");
                }

                if (line.NumberOfPieces < 0 || line.NumberOfBoxes < 0 || line.NumberOfBundles < 0)
                    ModelState.AddModelError(string.Empty, $"{label}: quantities cannot be negative.");

                if (line.Value <= 0)
                    ModelState.AddModelError(string.Empty, $"{label}: value must be greater than zero.");
            }

            if (!ModelState.IsValid)
            {
                model.Details = details;
                await PopulateLookups(model);
                return View(model);
            }

            var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "System";
            var now = DateTime.Now;

            var ledger = new StockLedger
            {
                ReferenceCode = model.ReferenceCode.Trim(),
                // Manual ledger entries are not raised off a stock movement. StockMovementId is a
                // loose reference on the StockService side (no FK), so 0 marks "no movement".
                StockMovementId = 0,
                IsIn = model.IsIn,
                CounterPartyTypeId = model.CounterPartyTypeId,
                CounterPartyId = model.CounterPartyId,
                StoreId = model.StoreId,
                Notes = model.Notes ?? string.Empty,
                CreatedBy = username,
                CreatedDate = now,
                ModifiedBy = username,
                ModifiedDate = now,
                StockLedgerDetails = details.Select(d => new StockLedgerDetail
                {
                    DenominationId = d.DenominationId,
                    CategoryId = d.CategoryId,
                    SubCategoryId = d.SubCategoryId ?? 0,
                    NumberOfPieces = d.NumberOfPieces,
                    NumberOfBundles = d.NumberOfBundles,
                    NumberOfBoxes = d.NumberOfBoxes,
                    Value = d.Value,
                    CreatedBy = username,
                    CreatedDate = now,
                    ModifiedBy = username,
                    ModifiedDate = now
                }).ToList()
            };

            try
            {
                // The StockService create endpoint persists the header and its details together,
                // so the lines are saved in the same call rather than looped separately.
                var created = await _stockService.CreateStockLedger(ledger);
                if (!created)
                {
                    ModelState.AddModelError(string.Empty, "The ledger entry could not be saved. Please try again or contact an administrator.");
                    model.Details = details;
                    await PopulateLookups(model);
                    return View(model);
                }

                _logger.LogInformation(
                    "Manual stock ledger {ReferenceCode} created by {User} with {LineCount} line(s).",
                    ledger.ReferenceCode, username, ledger.StockLedgerDetails.Count);

                TempData["Success"] = $"Ledger entry {ledger.ReferenceCode} saved with {ledger.StockLedgerDetails.Count} line(s).";
                return RedirectToAction(nameof(Index), new { branchId = model.StoreId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating manual stock ledger {ReferenceCode}", model.ReferenceCode);
                ModelState.AddModelError(string.Empty, "An error occurred while saving the ledger entry.");
                model.Details = details;
                await PopulateLookups(model);
                return View(model);
            }
        }

        private static string GenerateReferenceCode() =>
            $"MAN-{DateTime.Now:yyyyMMdd-HHmmss}";

        /// <summary>
        /// Loads every dropdown the form needs. Sub-categories and counter parties are handed to the
        /// view in full and filtered client-side, so the cascades need no extra round-trips.
        /// </summary>
        private async Task PopulateLookups(StockLedgerCreateViewModel model)
        {
            var branches = (await _configServiceClient.GetAllBouBranchesAsync())
                .OrderBy(b => b.Name)
                .ToList();

            var denominations = (await _stockService.GetAllDenominations())
                .Where(d => !d.IsDeleted)
                .OrderBy(d => d.DenominationTypeId)
                .ThenBy(d => d.Value)
                .ToList();

            var categories = (await _stockService.GetAllCategories())
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Name)
                .ToList();

            var subCategories = (await _stockService.GetAllSubCategories())
                .Where(sc => !sc.IsDeleted)
                .OrderBy(sc => sc.Name)
                .Select(sc => new SubCategoryOption
                {
                    Id = sc.Id,
                    CategoryId = sc.CategoryId,
                    Name = sc.Name
                })
                .ToList();

            var counterPartyTypes = (await _counterPartyTypeRepository.FindAll())
                .Where(t => !t.IsDeleted)
                .OrderBy(t => t.Name)
                .ToList();

            ViewBag.Branches = branches;
            ViewBag.Denominations = denominations;
            ViewBag.Categories = categories;
            ViewBag.SubCategories = subCategories;
            ViewBag.CounterPartyTypes = counterPartyTypes;
            ViewBag.CounterParties = await BuildCounterPartyOptions(counterPartyTypes, branches);
        }

        /// <summary>
        /// CounterPartyId means a different thing per counter party type, so the options are built
        /// per type: Branch → BOU branches, Commercial Bank → banks, Currency Category → categories,
        /// Vendor → currency vendors. A type with no known source contributes nothing and the form
        /// falls back to a plain numeric entry for it.
        /// </summary>
        private async Task<List<CounterPartyOption>> BuildCounterPartyOptions(
            List<CounterPartyType> counterPartyTypes, List<BranchDto> branches)
        {
            var options = new List<CounterPartyOption>();

            foreach (var type in counterPartyTypes)
            {
                var typeName = type.Name?.Trim() ?? string.Empty;

                if (typeName.Equals(StockConstants.StockCounterPartyType.Branch, StringComparison.OrdinalIgnoreCase))
                {
                    options.AddRange(branches.Select(b => new CounterPartyOption
                    {
                        CounterPartyTypeId = type.Id,
                        Id = b.Id,
                        Name = b.Name
                    }));
                }
                else if (typeName.Equals(StockConstants.StockCounterPartyType.Category, StringComparison.OrdinalIgnoreCase))
                {
                    var categories = (await _stockService.GetAllCategories())
                        .Where(c => !c.IsDeleted)
                        .OrderBy(c => c.Name);

                    options.AddRange(categories.Select(c => new CounterPartyOption
                    {
                        CounterPartyTypeId = type.Id,
                        Id = c.Id,
                        Name = c.Name
                    }));
                }
                else if (typeName.Equals(StockConstants.StockCounterPartyType.CommercialBank, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var banks = (await _commercialBankRepository.FindAll()).OrderBy(b => b.Name);
                        options.AddRange(banks.Select(b => new CounterPartyOption
                        {
                            CounterPartyTypeId = type.Id,
                            Id = b.Id,
                            Name = b.Name
                        }));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not load commercial banks for the ledger counter party dropdown");
                    }
                }
                else if (typeName.Equals(StockConstants.StockCounterPartyType.Vendor, StringComparison.OrdinalIgnoreCase))
                {
                    // FindAll already excludes soft-deleted vendors.
                    var vendors = (await _currencyVendorRepository.FindAll()).OrderBy(v => v.Name);

                    options.AddRange(vendors.Select(v => new CounterPartyOption
                    {
                        CounterPartyTypeId = type.Id,
                        Id = v.Id,
                        Name = v.Name
                    }));
                }
            }

            return options;
        }
    }
}

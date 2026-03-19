using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using System.Diagnostics;
using VMSService.Core.Models.Branches;
using VMSService.Core.Models.Denominations;
using VMSService.Core.Models.ExPrintMints;
using VMSService.Core.Repositories.Branches;
using VMSService.Core.Repositories.Categories;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.ExpectedCurrencies;
using VMSService.Core.Repositories.Stocks;
using VMSService.Core.Services.StockPositions;
using VMSService.Models;

namespace VMSService.Controllers
{
    [Authorize]
    [DefaultBreadcrumb("Dashboard")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IExPrintMintRepository _exPrintMintRepository;
        private readonly IDenominationRepository _denominationRepository;
        private readonly IBranchRepository _branchRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IStockLedgerRepository _stockLedgerRepository;
        private readonly IStockPositionService _stockPositionService;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public HomeController(
            ILogger<HomeController> logger,
            IExPrintMintRepository exPrintMintRepository,
            IDenominationRepository denominationRepository,
            IBranchRepository branchRepository,
            ICategoryRepository categoryRepository,
            IStockLedgerRepository stockLedgerRepository,
            IStockPositionService stockPositionService,
            IMapper mapper,
            IConfiguration configuration)
        {
            _logger = logger;
            _exPrintMintRepository = exPrintMintRepository;
            _denominationRepository = denominationRepository;
            _branchRepository = branchRepository;
            _categoryRepository = categoryRepository;
            _stockLedgerRepository = stockLedgerRepository;
            _stockPositionService = stockPositionService;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index(int? branchId)
        {
            try
            {
                // Get pending deliveries
                var pendingDeliveries = await _exPrintMintRepository.FindPendingDeliveries();
                var model = _mapper.Map<List<ExPrintMintViewModel>>(pendingDeliveries);

                // Populate related data for each pending delivery
                foreach (var delivery in model)
                {
                    // Map Denomination
                    if (delivery.DenominationId > 0)
                    {
                        var denomination = await _denominationRepository.FindById(delivery.DenominationId);
                        if (denomination != null)
                        {
                            delivery.Denomination = _mapper.Map<DenominationViewModel>(denomination);
                        }
                    }

                    // Map Branch
                    if (delivery.BranchId > 0)
                    {
                        var branch = await _branchRepository.FindById(delivery.BranchId);
                        if (branch != null)
                        {
                            delivery.Branch = _mapper.Map<BranchViewModel>(branch);
                        }
                    }
                }

                // Get all branches for dropdown
                var allBranches = await _branchRepository.FindAll();
                ViewBag.AllBranches = allBranches;

                // Determine which branch to display (use branchId param or default to Kampala)
                BranchViewModel selectedBranch = null;
                if (branchId.HasValue)
                {
                    var branch = await _branchRepository.FindById(branchId.Value);
                    if (branch != null)
                    {
                        selectedBranch = _mapper.Map<BranchViewModel>(branch);
                    }
                }
                
                // Fallback to Kampala if no valid branchId provided
                if (selectedBranch == null)
                {
                    var kampalaBranch = await _branchRepository.FindByName("Kampala Branch");
                    if (kampalaBranch != null)
                    {
                        selectedBranch = _mapper.Map<BranchViewModel>(kampalaBranch);
                    }
                }

                if (selectedBranch != null)
                {
                    // Get all categories
                    var allCategories = await _categoryRepository.FindAll();
                    List<string> selectedCategories = ["New", "Re-Issue", "Paids", "Cancelled", "CFB", "MI", "Fit", "MWB"];
                    allCategories = allCategories.Where(c => selectedCategories.Contains(c.Name)).ToList();

                    // Get all denominations sorted by value
                    var allDenominations = await _denominationRepository.FindAll();
                    var sortedDenominations = allDenominations?.OrderBy(d => d.Value).ToList();
                    
                    // Get stock position matrix from service with filtered categories and denominations
                    var stockPositionMatrix = await _stockPositionService.GetStockMatrixAsync(
                        selectedBranch.Id, 
                        allCategories.Select(c => c.Id).ToList(),
                        sortedDenominations.Select(d => d.Id).ToList());
                    
                    ViewBag.KampalaBranch = selectedBranch;
                    ViewBag.SelectedBranchId = selectedBranch.Id;
                    ViewBag.Categories = allCategories;
                    ViewBag.Denominations = sortedDenominations;
                    ViewBag.StockMatrix = stockPositionMatrix;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard data");
                TempData["Error"] = "An error occurred while retrieving dashboard data.";
                return View(new List<ExPrintMintViewModel>());
            }
        }

        [Breadcrumb("About")]
        public IActionResult About()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Logout()
        {
            Response.Cookies.Delete("AuthToken");
            Response.Cookies.Delete("Username");
            var gatewayUrl = _configuration["Microservices:gatewayservice"];
            return Redirect($"{gatewayUrl}/Login/Logout");
        }
    }
}

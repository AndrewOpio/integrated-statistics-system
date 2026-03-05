using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using System.Diagnostics;
using VMSService.Core.Models.Branches;
using VMSService.Core.Models.Denominations;
using VMSService.Core.Models.ExPrintMints;
using VMSService.Core.Repositories.Branches;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.ExpectedCurrencies;
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
        private readonly IMapper _mapper;

        public HomeController(
            ILogger<HomeController> logger,
            IExPrintMintRepository exPrintMintRepository,
            IDenominationRepository denominationRepository,
            IBranchRepository branchRepository,
            IMapper mapper)
        {
            _logger = logger;
            _exPrintMintRepository = exPrintMintRepository;
            _denominationRepository = denominationRepository;
            _branchRepository = branchRepository;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index()
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

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending deliveries for dashboard");
                TempData["Error"] = "An error occurred while retrieving pending deliveries.";
                return View(new List<ExPrintMintViewModel>());
            }
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
            return Redirect("https://localhost:7204");
        }
    }
}

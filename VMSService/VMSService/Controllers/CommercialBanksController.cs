using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.CommercialBanks;
using VMSService.Core.Repositories.CommercialBanks;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class CommercialBanksController(
        ICommercialBankRepository _commercialBankRepository,
        IMapper _mapper,
        ILogger<CommercialBanksController> _logger) : Controller
    {
        [Breadcrumb("Commercial Banks", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: CommercialBanks
        public async Task<IActionResult> Index()
        {
            try
            {
                var commercialBanks = (await _commercialBankRepository.FindAll());
                var model = _mapper.Map<List<CommercialBankViewModel>>(commercialBanks);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial banks");
                TempData["Error"] = "An error occurred while retrieving commercial banks. Please try again.";
                return View(new List<CommercialBankViewModel>());
            }
        }

        // GET: CommercialBanks/Details/5
        [Breadcrumb("Details", FromController = typeof(CommercialBanksController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var commercialBank = await _commercialBankRepository.FindById(id.Value);
                bool exists = commercialBank != null;
                if (!exists)
                {
                    return NotFound();
                }

                if (commercialBank == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<CommercialBankViewModel>(commercialBank);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial bank with id {CommercialBankId}", id);
                TempData["Error"] = "An error occurred while retrieving the commercial bank. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
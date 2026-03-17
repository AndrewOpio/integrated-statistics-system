using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Core.Helpers;
using VMSService.Core.Models.CommercialBankUser;
using VMSService.Core.Repositories.CommercialBanks;
using VMSService.Core.Repositories.CommercialBankUsers;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Data.Entities;

namespace VMSService.Controllers
{
    [Authorize]
    public class CommercialBankUsersController(
        ICommercialBankUserRepository _commercialBankUserRepository,
        ICommercialBankRepository _commercialBankRepository,
        IMapper _mapper,
        ILogger<CommercialBankUsersController> _logger) : Controller
    {
        [Breadcrumb("Commercial Bank Users", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: CommercialBankUsers
        public async Task<IActionResult> Index()
        {
            try
            {
                var commercialBankUsers = (await _commercialBankUserRepository.FindAll()).Where(c => c.IsDeleted == false);
                var model = _mapper.Map<List<CommercialBankUserViewModel>>(commercialBankUsers);

                // Get all commercial banks and non-staff users to populate display names
                var commercialBanks = await _commercialBankRepository.FindAll();
                var nonStaffUsers = await _commercialBankUserRepository.GetNonStaffUsers();

                foreach (var item in model)
                {
                    item.CommercialBank = commercialBanks.FirstOrDefault(cb => cb.Id == item.CommercialBankId);
                    var user = nonStaffUsers.FirstOrDefault(u => u.Id == item.UserId);
                    if (user != null)
                    {
                        item.UserName = user.UserName;
                        item.UserFullName = $"{user.FirstName} {user.MiddleName} {user.LastName}".Replace("  ", " ").Trim();
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial bank users");
                TempData["Error"] = "An error occurred while retrieving commercial bank users. Please try again.";
                return View(new List<CommercialBankUserViewModel>());
            }
        }

        // GET: CommercialBankUsers/Details/5
        [Breadcrumb("Details", FromController = typeof(CommercialBankUsersController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _commercialBankUserRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var commercialBankUser = await _commercialBankUserRepository.FindById(id.Value);
                if (commercialBankUser == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<CommercialBankUserViewModel>(commercialBankUser);

                // Populate display data
                var commercialBanks = await _commercialBankRepository.FindAll();
                var nonStaffUsers = await _commercialBankUserRepository.GetNonStaffUsers();

                model.CommercialBank = commercialBanks.FirstOrDefault(cb => cb.Id == model.CommercialBankId);
                var user = nonStaffUsers.FirstOrDefault(u => u.Id == model.UserId);
                if (user != null)
                {
                    model.UserName = user.UserName;
                    model.UserFullName = $"{user.FirstName} {user.MiddleName} {user.LastName}".Replace("  ", " ").Trim();
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial bank user with id {CommercialBankUserId}", id);
                TempData["Error"] = "An error occurred while retrieving the commercial bank user. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: CommercialBankUsers/Create
        [Breadcrumb("Create", FromController = typeof(CommercialBankUsersController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            try
            {
                var commercialBanks = await _commercialBankRepository.FindAll();
                var nonStaffUsers = await _commercialBankUserRepository.GetNonStaffUsers();

                var model = new CommercialBankUserViewModel
                {
                    CommercialBankList = CommercialBanksHelper.GetCommercialBanksAsSelectList((List<CommercialBank>)commercialBanks),
                    NonStaffUserList = _mapper.Map<List<NonStaffUserViewModel>>(nonStaffUsers)
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data for create");
                TempData["Error"] = "An error occurred while loading data. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CommercialBankUsers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(CommercialBankUsersController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(CommercialBankUserViewModel model)
        {
            // Check if UserId already exists
            if (await _commercialBankUserRepository.IsUserIdExists(model.UserId))
            {
                ModelState.AddModelError("UserId", "This user is already assigned to a commercial bank.");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    _logger.LogWarning("ModelState Error: {ErrorMessage}", error.ErrorMessage);
                }

                var commercialBanks = await _commercialBankRepository.FindAll();
                var nonStaffUsers = await _commercialBankUserRepository.GetNonStaffUsers();
                model.CommercialBankList = CommercialBanksHelper.GetCommercialBanksAsSelectList((List<CommercialBank>)commercialBanks);
                model.NonStaffUserList = _mapper.Map<List<NonStaffUserViewModel>>(nonStaffUsers);
                return View(model);
            }

            try
            {
                var commercialBankUser = _mapper.Map<CommercialBankUser>(model);
                bool isCreated = await _commercialBankUserRepository.Create(commercialBankUser);

                if (isCreated)
                {
                    TempData["Success"] = "Commercial bank user created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to create commercial bank user. Please try again.";
                    _logger.LogError("Error creating commercial bank user");
                    ModelState.AddModelError("", "Failed to create commercial bank user. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating commercial bank user");
                ModelState.AddModelError("", "Failed to create commercial bank user. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            var banks = await _commercialBankRepository.FindAll();
            var users = await _commercialBankUserRepository.GetNonStaffUsers();
            model.CommercialBankList = CommercialBanksHelper.GetCommercialBanksAsSelectList((List<CommercialBank>)banks);
            model.NonStaffUserList = _mapper.Map<List<NonStaffUserViewModel>>(users);
            return View(model);
        }

        // GET: CommercialBankUsers/Edit/5
        [Breadcrumb("Edit", FromController = typeof(CommercialBankUsersController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _commercialBankUserRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var commercialBankUser = await _commercialBankUserRepository.FindById(id.Value);
                if (commercialBankUser == null)
                {
                    return NotFound();
                }

                var commercialBanks = await _commercialBankRepository.FindAll();
                var nonStaffUsers = await _commercialBankUserRepository.GetNonStaffUsers();

                var model = _mapper.Map<CommercialBankUserViewModel>(commercialBankUser);
                model.CommercialBankList = CommercialBanksHelper.GetCommercialBanksAsSelectList((List<CommercialBank>)commercialBanks);
                model.NonStaffUserList = _mapper.Map<List<NonStaffUserViewModel>>(nonStaffUsers);

                // Set display name for current user
                var user = nonStaffUsers.FirstOrDefault(u => u.Id == model.UserId);
                if (user != null)
                {
                    model.UserName = user.UserName;
                    model.UserFullName = $"{user.FirstName} {user.MiddleName} {user.LastName}".Replace("  ", " ").Trim();
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial bank user for edit with id {CommercialBankUserId}", id);
                TempData["Error"] = "An error occurred while retrieving the commercial bank user. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CommercialBankUsers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(CommercialBankUsersController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, CommercialBankUserViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            // Check if UserId already exists (excluding current record)
            if (await _commercialBankUserRepository.IsUserIdExists(model.UserId, id))
            {
                ModelState.AddModelError("UserId", "This user is already assigned to a commercial bank.");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    _logger.LogWarning("ModelState Error: {ErrorMessage}", error.ErrorMessage);
                }

                var commercialBanks = await _commercialBankRepository.FindAll();
                var nonStaffUsers = await _commercialBankUserRepository.GetNonStaffUsers();
                model.CommercialBankList = CommercialBanksHelper.GetCommercialBanksAsSelectList((List<CommercialBank>)commercialBanks);
                model.NonStaffUserList = _mapper.Map<List<NonStaffUserViewModel>>(nonStaffUsers);
                return View(model);
            }

            try
            {
                var commercialBankUser = await _commercialBankUserRepository.FindById(id);
                if (commercialBankUser == null)
                {
                    return NotFound();
                }

                commercialBankUser.UserId = model.UserId;
                commercialBankUser.CommercialBankId = model.CommercialBankId;
                commercialBankUser.CommercialBankBranchId = model.CommercialBankBranchId;

                bool isUpdated = await _commercialBankUserRepository.Update(commercialBankUser);
                if (isUpdated)
                {
                    TempData["Success"] = "Commercial bank user updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to update commercial bank user. Please try again.";
                    ModelState.AddModelError("", "Failed to update commercial bank user. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating commercial bank user with id {CommercialBankUserId}", id);
                ModelState.AddModelError("", "Failed to update commercial bank user. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            var banks = await _commercialBankRepository.FindAll();
            var users = await _commercialBankUserRepository.GetNonStaffUsers();
            model.CommercialBankList = CommercialBanksHelper.GetCommercialBanksAsSelectList((List<CommercialBank>)banks);
            model.NonStaffUserList = _mapper.Map<List<NonStaffUserViewModel>>(users);
            return View(model);
        }

        // GET: CommercialBankUsers/Delete/5
        [Breadcrumb("Delete", FromController = typeof(CommercialBankUsersController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _commercialBankUserRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var commercialBankUser = await _commercialBankUserRepository.FindById(id.Value);
                if (commercialBankUser == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<CommercialBankUserViewModel>(commercialBankUser);

                // Populate display data
                var commercialBanks = await _commercialBankRepository.FindAll();
                var nonStaffUsers = await _commercialBankUserRepository.GetNonStaffUsers();

                model.CommercialBank = commercialBanks.FirstOrDefault(cb => cb.Id == model.CommercialBankId);
                var user = nonStaffUsers.FirstOrDefault(u => u.Id == model.UserId);
                if (user != null)
                {
                    model.UserName = user.UserName;
                    model.UserFullName = $"{user.FirstName} {user.MiddleName} {user.LastName}".Replace("  ", " ").Trim();
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial bank user for deletion with id {CommercialBankUserId}", id);
                TempData["Error"] = "An error occurred while retrieving the commercial bank user. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CommercialBankUsers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(CommercialBankUsersController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool exists = await _commercialBankUserRepository.IsExists(id);
                if (!exists)
                {
                    return NotFound();
                }

                var commercialBankUser = await _commercialBankUserRepository.FindById(id);
                if (commercialBankUser != null)
                {
                    commercialBankUser.IsDeleted = true;
                    bool isDeleted = await _commercialBankUserRepository.Update(commercialBankUser);

                    if (isDeleted)
                    {
                        TempData["Success"] = "Commercial bank user deleted successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to delete commercial bank user. Please try again.";
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting commercial bank user with id {CommercialBankUserId}", id);
                TempData["Error"] = "An error occurred while deleting the commercial bank user. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
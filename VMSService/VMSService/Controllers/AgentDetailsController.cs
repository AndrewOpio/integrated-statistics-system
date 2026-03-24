using AutoMapper;
using ConfigurationsService.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;
using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.CommercialBankUsers;
using VMSService.Core.Services.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Controllers
{
    [Authorize]
    public class AgentDetailsController(
        IAgentDetailRepository _agentDetailRepository,
        IConfigurationService _configService,
        IMapper _mapper,
        IWebHostEnvironment _webHostEnvironment,
        ILogger<AgentDetailsController> _logger,
        IHttpContextAccessor _httpContextAccessor,
        ICommercialBankUserRepository _commercialBankUserRepo) : Controller
    {
        [Breadcrumb("Agent Details", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: AgentDetails
        public async Task<IActionResult> Index()
        {
            try
            {
                var agentDetails = (await _agentDetailRepository.FindAll()).Where(a => a.IsDeleted == false);
                var model = _mapper.Map<List<AgentDetailViewModel>>(agentDetails);

                // Populate branch names
                var branches = await _configService.GetAllBranches();
                foreach (var agent in model)
                {
                    var branch = branches.FirstOrDefault(b => b.Id == agent.BranchId);
                    agent.BranchName = branch?.Name ?? "Unknown";
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving agent details");
                TempData["Error"] = "An error occurred while retrieving agent details. Please try again.";
                return View(new List<AgentDetailViewModel>());
            }
        }

        // GET: AgentDetails/Details/5
        [Breadcrumb("Details", FromController = typeof(AgentDetailsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _agentDetailRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var agentDetail = await _agentDetailRepository.FindById(id.Value);
                if (agentDetail == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<AgentDetailViewModel>(agentDetail);

                // Populate branch name
                var branches = await _configService.GetAllBranches();
                var branch = branches.FirstOrDefault(b => b.Id == model.BranchId);
                model.BranchName = branch?.Name ?? "Unknown";

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving agent detail with id {AgentDetailId}", id);
                TempData["Error"] = "An error occurred while retrieving the agent detail. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: AgentDetails/Create
        [Breadcrumb("Create", FromController = typeof(AgentDetailsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            try
            {
                // Get user ID from claims
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) { return NotFound(); }

                CommercialBankUser commercialBankUser = await _commercialBankUserRepo.FindByUserId(userId);
                if (commercialBankUser == null) { return NotFound(); }
                var commercialBankBranches = await _configService.GetCommercialBankBranchesByBankId(commercialBankUser.CommercialBankId);

                var branches = await _configService.GetAllBranches();
                var model = new AgentDetailViewModel
                {
                    BranchList = [.. branches.Select(b => new SelectListItem
                    {
                        Value = b.Id.ToString(),
                        Text = b.Name
                    })],
                    CommercialBankBranchList = [.. commercialBankBranches.Select(cb => new SelectListItem
                    {
                        Value = cb.Id.ToString(),
                        Text = cb.Name
                    })]
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading branches for create");
                TempData["Error"] = "An error occurred while loading branches. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: AgentDetails/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(AgentDetailsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(AgentDetailViewModel model)
        {
            // Get user ID from claims
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) { return NotFound(); }

            CommercialBankUser commercialBankUser = await _commercialBankUserRepo.FindByUserId(userId);
            if (commercialBankUser == null) { return NotFound(); }
            var commercialBankBranches = await _configService.GetCommercialBankBranchesByBankId(commercialBankUser.CommercialBankId);

            var branches = await _configService.GetAllBranches();
            model.BranchList = [.. branches.Select(b => new SelectListItem
                    {
                        Value = b.Id.ToString(),
                        Text = b.Name
                    })];
            model.CommercialBankBranchList = [.. commercialBankBranches.Select(cb => new SelectListItem
                    {
                        Value = cb.Id.ToString(),
                        Text = cb.Name
                    })];

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    _logger.LogWarning("ModelState Error: {ErrorMessage}", error.ErrorMessage);
                    ModelState.AddModelError("", error.ErrorMessage);
                }
                ModelState.AddModelError("", "Please correct the errors and try again.");

                return View(model);
            }

            try
            {
                // Handle file uploads
                model.PassportPhoto = await SaveFileAsync(model.PassportPhotoFile, "passportphotos");
                model.WorkIdScan = await SaveFileAsync(model.WorkIdScanFile, "workidscans");
                model.SignatureScan = await SaveFileAsync(model.SignatureScanFile, "signaturescans");
                model.ApprovalAttachment = await SaveFileAsync(model.ApprovalAttachmentFile, "approvalattachments");

                var agentDetail = _mapper.Map<AgentDetail>(model);
                //bool isCreated = await _agentDetailRepository.Create(agentDetail);
                int agentDetailId = await _agentDetailRepository.CreateNew(agentDetail);

                if (agentDetailId > 0)
                {
                    TempData["Success"] = "Agent detail created successfully!";
                    return RedirectToAction(nameof(Details), new { id = agentDetailId });
                }
                else
                {
                    TempData["Error"] = "Failed to create agent detail. Please try again.";
                    _logger.LogError("Error creating agent detail");
                    ModelState.AddModelError("", "Failed to create agent detail. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating agent detail");
                ModelState.AddModelError("", "Failed to create agent detail. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            return View(model);
        }

        // GET: AgentDetails/Edit/5
        [Breadcrumb("Edit", FromController = typeof(AgentDetailsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _agentDetailRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var agentDetail = await _agentDetailRepository.FindById(id.Value);
                if (agentDetail == null)
                {
                    return NotFound();
                }

                var branches = await _configService.GetAllBranches();
                var model = _mapper.Map<AgentDetailViewModel>(agentDetail);
                model.BranchList = branches.Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.Name
                }).ToList();

                // Get commercial bank branches for the user's bank
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    var commercialBankUser = await _commercialBankUserRepo.FindByUserId(userId);
                    if (commercialBankUser != null)
                    {
                        var commercialBankBranches = await _configService.GetCommercialBankBranchesByBankId(commercialBankUser.CommercialBankId);
                        model.CommercialBankBranchList = commercialBankBranches.Select(cb => new SelectListItem
                        {
                            Value = cb.Id.ToString(),
                            Text = cb.Name
                        }).ToList();
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving agent detail for edit with id {AgentDetailId}", id);
                TempData["Error"] = "An error occurred while retrieving the agent detail. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: AgentDetails/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(AgentDetailsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, AgentDetailViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    _logger.LogWarning("ModelState Error: {ErrorMessage}", error.ErrorMessage);
                    ModelState.AddModelError("", error.ErrorMessage);
                }
                ModelState.AddModelError("", "Please correct the errors and try again.");

                var branches = await _configService.GetAllBranches();
                model.BranchList = branches.Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.Name
                }).ToList();
                return View(model);
            }

            try
            {
                var agentDetail = await _agentDetailRepository.FindById(id);
                if (agentDetail == null)
                {
                    return NotFound();
                }

                agentDetail.BranchId = model.BranchId;
                agentDetail.CommercialBankBranchId = model.CommercialBankBranchId;
                agentDetail.AgentNames = model.AgentNames;
                agentDetail.NIN = model.NIN;
                agentDetail.PhoneNumber = model.PhoneNumber;

                // Handle file uploads - only update if new file is provided
                if (model.PassportPhotoFile != null)
                {
                    agentDetail.PassportPhoto = await SaveFileAsync(model.PassportPhotoFile, "passportphotos");
                }
                if (model.WorkIdScanFile != null)
                {
                    agentDetail.WorkIdScan = await SaveFileAsync(model.WorkIdScanFile, "workidscans");
                }
                if (model.SignatureScanFile != null)
                {
                    agentDetail.SignatureScan = await SaveFileAsync(model.SignatureScanFile, "signaturescans");
                }
                if (model.ApprovalAttachmentFile != null)
                {
                    agentDetail.ApprovalAttachment = await SaveFileAsync(model.ApprovalAttachmentFile, "approvalattachments");
                }

                bool isUpdated = await _agentDetailRepository.Update(agentDetail);
                if (isUpdated)
                {
                    TempData["Success"] = "Agent detail updated successfully!";
                    return RedirectToAction(nameof(Details), new { id });
                }
                else
                {
                    TempData["Error"] = "Failed to update agent detail. Please try again.";
                    ModelState.AddModelError("", "Failed to update agent detail. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating agent detail with id {AgentDetailId}", id);
                ModelState.AddModelError("", "Failed to update agent detail. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            var branchList = await _configService.GetAllBranches();
            model.BranchList = branchList.Select(b => new SelectListItem
            {
                Value = b.Id.ToString(),
                Text = b.Name
            }).ToList();
            return View(model);
        }

        // GET: AgentDetails/Delete/5
        [Breadcrumb("Delete", FromController = typeof(AgentDetailsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _agentDetailRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var agentDetail = await _agentDetailRepository.FindById(id.Value);
                if (agentDetail == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<AgentDetailViewModel>(agentDetail);

                // Populate branch name
                var branches = await _configService.GetAllBranches();
                var branch = branches.FirstOrDefault(b => b.Id == model.BranchId);
                model.BranchName = branch?.Name ?? "Unknown";

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving agent detail for deletion with id {AgentDetailId}", id);
                TempData["Error"] = "An error occurred while retrieving the agent detail. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: AgentDetails/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(AgentDetailsController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool exists = await _agentDetailRepository.IsExists(id);
                if (!exists)
                {
                    return NotFound();
                }

                var agentDetail = await _agentDetailRepository.FindById(id);
                if (agentDetail != null)
                {
                    agentDetail.IsDeleted = true;
                    bool isDeleted = await _agentDetailRepository.Update(agentDetail);

                    if (isDeleted)
                    {
                        TempData["Success"] = "Agent detail deleted successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to delete agent detail. Please try again.";
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting agent detail with id {AgentDetailId}", id);
                TempData["Error"] = "An error occurred while deleting the agent detail. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveFileAsync(IFormFile? file, string folderName)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", folderName);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/{folderName}/{uniqueFileName}";
        }
    }
}
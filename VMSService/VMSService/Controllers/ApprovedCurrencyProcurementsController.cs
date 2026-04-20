using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.ApprovedCurrencyDenominations;
using VMSService.Core.Models.ApprovedCurrencyProcurements;
using VMSService.Core.Models.ApprovedCurrencyStatusHistories;
using VMSService.Core.Models.CurrencyVendors;
using VMSService.Core.Models.Denominations;
using VMSService.Core.Repositories.ApprovedCurrencyDenominations;
using VMSService.Core.Repositories.ApprovedCurrencyProcurements;
using VMSService.Core.Repositories.ApprovedCurrencyStatusHistories;
using VMSService.Core.Repositories.CurrencyVendors;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class ApprovedCurrencyProcurementsController(
        IApprovedCurrencyProcurementRepository _approvedCurrencyProcurementRepository,
        IMapper _mapper,
        ILogger<ApprovedCurrencyProcurementsController> _logger,
        IApprovedCurrencyDenominationRepository _approvedCurrencyDenominationRepo,
        IDenominationRepository _denominationRepository,
        IWorkflowService _workflowService,
        IApprovedCurrencyStatusHistoryRepository _statusHistoryRepository,
        ICurrencyVendorRepository _currencyVendorRepository,
        IWorkflowServiceClient _workflowServiceClient,
        HttpClient _httpClient) : Controller
    {

        [Breadcrumb("Approved Currency Procurements", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: ApprovedCurrencyProcurements
        public async Task<IActionResult> Index()
        {
            try
            {
                var approvedCurrencyProcurements = (await _approvedCurrencyProcurementRepository.FindAll());
                var model = _mapper.Map<List<ApprovedCurrencyProcurementsViewModel>>(approvedCurrencyProcurements);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving approved currency procurements");
                TempData["Error"] = "An error occurred while retrieving approved currency procurements. Please try again.";
                return View(new List<ApprovedCurrencyProcurementsViewModel>());
            }
        }

        // GET: ApprovedCurrencyProcurements/Details/5
        [Breadcrumb("Details", FromController = typeof(ApprovedCurrencyProcurementsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _approvedCurrencyProcurementRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var approvedCurrencyProcurement = await _approvedCurrencyProcurementRepository.FindById(id.Value);
                if (approvedCurrencyProcurement == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<ApprovedCurrencyProcurementsViewModel>(approvedCurrencyProcurement);
                model.ApprovedCurrencyDenominations = _mapper.Map<List<ApprovedCurrencyDenominationViewModel>>(
                    await _approvedCurrencyDenominationRepo.FindByApprovedCurrencyId(id ?? 0));
                foreach(var approvedCurrencyDenomination in model.ApprovedCurrencyDenominations)
                {
                    approvedCurrencyDenomination.Denomination = _mapper.Map<DenominationViewModel>(await _denominationRepository.FindById(approvedCurrencyDenomination.DenominationId));
                }

                // Get available transitions for current user
                //var userRole = User.FindFirst(SystemPolicy.ClaimType.AppRole)?.Value ?? "";
                var userRoles = User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                    .Select(c => int.TryParse(c.Value.Split(":")[1], out var appRoleId) ? appRoleId : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList();

                var availableTransitions = new List<WorkflowTransitionDto>();
                foreach (var userRole in userRoles)
                {
                    var requestData = new GetAvailableTransitionsRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName = StatusConstants.Entity.ApprovedCurrencyProcurement,
                        CurrentStatusId = approvedCurrencyProcurement.CurrentStatusId,
                        AppRole = userRole,
                    };

                    var transitions = await _workflowServiceClient.GetAvailableTransitions(requestData);
                    if (transitions != null)
                    {
                        availableTransitions.AddRange(transitions);
                    }
                }

                model.AvailableTransitions = availableTransitions.Distinct().ToList();

                model.StatusHistory = _mapper.Map<ICollection<ApprovedCurrencyStatusHistoryViewModel>>(
                    await _statusHistoryRepository.FindByApprovedCurrencyId(id ?? 0));

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving approved currency procurement with id {ApprovedCurrencyProcurementId}", id);
                TempData["Error"] = "An error occurred while retrieving the approved currency procurement. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: ApprovedCurrencyProcurements/Create
        [Breadcrumb("Create", FromController = typeof(ApprovedCurrencyProcurementsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            try
            {
                ViewBag.Denominations = _mapper.Map<List<DenominationViewModel>>(await _denominationRepository.FindAll());
                ViewBag.CurrencyVendors = _mapper.Map<List<CurrencyVendorViewModel>>(await _currencyVendorRepository.FindAll());
                return View(new ApprovedCurrencyProcurementsViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                TempData["Error"] = $"An error occurred. Please try again. \n{ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ApprovedCurrencyProcurements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(ApprovedCurrencyProcurementsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(ApprovedCurrencyProcurementsViewModel model)
        {
            ViewBag.Denominations = _mapper.Map<List<DenominationViewModel>>(await _denominationRepository.FindAll());
            ViewBag.CurrencyVendors = _mapper.Map<List<CurrencyVendorViewModel>>(await _currencyVendorRepository.FindAll());
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

            if (model.AttachmentFile.ContentType != "application/pdf")
            {
                ModelState.AddModelError("", "Invalid file type");
                return View(model);
            }

            using var reader = new StreamReader(model.AttachmentFile.OpenReadStream());
            char[] buffer = new char[4];
            await reader.ReadAsync(buffer, 0, 4);
            var header = new string(buffer);

            if (header != "%PDF")
            {
                ModelState.AddModelError("", "File is not a valid PDF");
                return View(model);
            }

            try
            {
                string uploadsFolder = "C:\\VMSFiles\\Uploads";

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, model.AttachmentFile.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AttachmentFile.CopyToAsync(stream);
                }

                model.BoardApprovalAttachment = filePath;

                var requestData = new GetStatusByNameRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    StatusName = StatusConstants.ApprovedCurrencyProcurement.Draft,
                    EntityTypeName = StatusConstants.Entity.ApprovedCurrencyProcurement
                };

                StatusTypeDto status = await _workflowServiceClient.FindStatusByName(requestData);
                model.CurrentStatusId = status.Id;
                model.CurrentStatusName = status.Name;

                ApprovedCurrencyProcurement newApprovedCurrencyProcurement = _mapper.Map<ApprovedCurrencyProcurement>(model);
                //bool isCreated = await _approvedCurrencyProcurementRepository.Create(newApprovedCurrencyProcurement);
                int newApprovedCurrencyProcurementId = await _approvedCurrencyProcurementRepository.CreateNew(newApprovedCurrencyProcurement);

                if (newApprovedCurrencyProcurementId > 0)
                {
                    foreach (var denomination in model.ApprovedCurrencyDenominations)
                    {
                        ApprovedCurrencyDenomination newApprovedCurrencyDenomination = new()
                        {
                            DenominationId = denomination.DenominationId,
                            ApprovedCurrencyId = newApprovedCurrencyProcurement.Id,
                            Amount = denomination.Amount,
                            NumberOfPieces = denomination.NumberOfPieces,
                            CurrencyVendorId = denomination.CurrencyVendorId
                        };

                        await _approvedCurrencyDenominationRepo.Create(newApprovedCurrencyDenomination);
                    }

                    TempData["Success"] = "Approved currency procurement created successfully!";
                    return RedirectToAction(nameof(Details), new { id = newApprovedCurrencyProcurementId });
                }
                else
                {
                    TempData["Error"] = "Failed to create approved currency procurement. Please try again.";
                    _logger.LogError("Error creating approved currency procurement");
                    ModelState.AddModelError("", "Failed to create approved currency procurement. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating approved currency procurement");
                ModelState.AddModelError("", "Failed to create approved currency procurement. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            var types = await _workflowServiceClient.FindAllStatuses();
            //ViewBag.StatusTypes = types.Where(st => st.Category == "ApprovedCurrency").ToList();
            TempData["FileInputRequired"] = true;
            model.BoardApprovalAttachment = null; // Clear the file path to avoid showing it in the view after a failed creation attempt
            return View(model);
        }

        // GET: ApprovedCurrencyProcurements/Edit/5
        [Breadcrumb("Edit", FromController = typeof(ApprovedCurrencyProcurementsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _approvedCurrencyProcurementRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var approvedCurrencyProcurement = await _approvedCurrencyProcurementRepository.FindById(id.Value);
                if (approvedCurrencyProcurement == null)
                {
                    return NotFound();
                }

                ViewBag.Denominations = _mapper.Map<List<DenominationViewModel>>(await _denominationRepository.FindAll());
                ViewBag.CurrencyVendors = _mapper.Map<List<CurrencyVendorViewModel>>(await _currencyVendorRepository.FindAll());

                var model = _mapper.Map<ApprovedCurrencyProcurementsViewModel>(approvedCurrencyProcurement);
                model.ApprovedCurrencyDenominations = _mapper.Map<List<ApprovedCurrencyDenominationViewModel>>(await _approvedCurrencyDenominationRepo.FindByApprovedCurrencyId(id ?? 0));
                foreach (var approvedCurrencyDenomination in model.ApprovedCurrencyDenominations)
                {
                    approvedCurrencyDenomination.Denomination = _mapper.Map<DenominationViewModel>(await _denominationRepository.FindById(approvedCurrencyDenomination.DenominationId));
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving approved currency procurement for edit with id {ApprovedCurrencyProcurementId}", id);
                TempData["Error"] = "An error occurred while retrieving the approved currency procurement. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ApprovedCurrencyProcurements/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(ApprovedCurrencyProcurementsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, ApprovedCurrencyProcurementsViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            ViewBag.Denominations = _mapper.Map<List<DenominationViewModel>>(await _denominationRepository.FindAll());
            ViewBag.CurrencyVendors = _mapper.Map<List<CurrencyVendorViewModel>>(await _currencyVendorRepository.FindAll());

            if (!ModelState.IsValid && !(ModelState.ErrorCount == 1 && ModelState["AttachmentFile"]?.ValidationState == ModelValidationState.Invalid))
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
                var approvedCurrencyProcurement = await _approvedCurrencyProcurementRepository.FindById(id);
                if (approvedCurrencyProcurement == null)
                {
                    return NotFound();
                }

                approvedCurrencyProcurement.Amount = model.Amount;

                if (model.AttachmentFile != null && model.AttachmentFile.ContentType != "application/pdf")
                {
                    ModelState.AddModelError("", "Invalid file type");
                    return View(model);
                }

                if (model.AttachmentFile != null)
                {
                    using var reader = new StreamReader(model.AttachmentFile.OpenReadStream());
                    char[] buffer = new char[4];
                    await reader.ReadAsync(buffer, 0, 4);
                    var header = new string(buffer);

                    if (header != "%PDF")
                    {
                        ModelState.AddModelError("", "File is not a valid PDF");
                        return View(model);
                    }

                    string uploadsFolder = "C:\\VMSFiles\\Uploads";

                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var filePath = Path.Combine(uploadsFolder, model.AttachmentFile.FileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.AttachmentFile.CopyToAsync(stream);
                    }

                    model.BoardApprovalAttachment = filePath;
                }

                bool isUpdated = await _approvedCurrencyProcurementRepository.Update(approvedCurrencyProcurement);
                if (isUpdated)
                {
                    // Properly update the ApprovedCurrencyDenominations, delete removed ones and update existing ones and create new ones
                    var existingDenominations = await _approvedCurrencyDenominationRepo.FindByApprovedCurrencyId(approvedCurrencyProcurement.Id);
                    var modelDenominationIds = model.ApprovedCurrencyDenominations.Where(d => d.Id != 0).Select(d => d.Id).ToList();

                    // Delete denominations that are no longer in the model
                    foreach (var existingDenom in existingDenominations)
                    {
                        if (!modelDenominationIds.Contains(existingDenom.Id))
                        {
                            existingDenom.IsDeleted = true;
                            await _approvedCurrencyDenominationRepo.Update(existingDenom);
                        }
                    }

                    // Create new and update existing denominations
                    foreach (var denomination in model.ApprovedCurrencyDenominations)
                    {
                        if (denomination.Id == 0)
                        {
                            ApprovedCurrencyDenomination newApprovedCurrencyDenomination = new()
                            {
                                DenominationId = denomination.DenominationId,
                                ApprovedCurrencyId = approvedCurrencyProcurement.Id,
                                Amount = denomination.Amount,
                                NumberOfPieces = denomination.NumberOfPieces,
                                CurrencyVendorId = denomination.CurrencyVendorId
                            };

                            await _approvedCurrencyDenominationRepo.Create(newApprovedCurrencyDenomination);
                        }
                        else
                        {
                            ApprovedCurrencyDenomination approvedCurrencyDenomination = await _approvedCurrencyDenominationRepo.FindById(denomination.Id);
                            approvedCurrencyDenomination.Amount = denomination.Amount;
                            approvedCurrencyDenomination.DenominationId = denomination.DenominationId;
                            approvedCurrencyDenomination.NumberOfPieces = denomination.NumberOfPieces;
                            approvedCurrencyDenomination.CurrencyVendorId = denomination.CurrencyVendorId;

                            await _approvedCurrencyDenominationRepo.Update(approvedCurrencyDenomination);
                        }
                    }

                    TempData["Success"] = "Approved currency procurement updated successfully!";
                    return RedirectToAction(nameof(Details), new { id });
                }
                else
                {
                    TempData["Error"] = "Failed to update approved currency procurement. Please try again.";
                    ModelState.AddModelError("", "Failed to update approved currency procurement. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating approved currency procurement with id {ApprovedCurrencyProcurementId}", id);
                ModelState.AddModelError("", "Failed to update approved currency procurement. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            return View(model);
        }

        // GET: ApprovedCurrencyProcurements/Delete/5
        [Breadcrumb("Delete", FromController = typeof(ApprovedCurrencyProcurementsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _approvedCurrencyProcurementRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var approvedCurrencyProcurement = await _approvedCurrencyProcurementRepository.FindById(id.Value);
                if (approvedCurrencyProcurement == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<ApprovedCurrencyProcurementsViewModel>(approvedCurrencyProcurement);
                model.ApprovedCurrencyDenominations = _mapper.Map<List<ApprovedCurrencyDenominationViewModel>>(await _approvedCurrencyDenominationRepo.FindByApprovedCurrencyId(id ?? 0));
                foreach (var approvedCurrencyDenomination in model.ApprovedCurrencyDenominations)
                {
                    approvedCurrencyDenomination.Denomination = _mapper.Map<DenominationViewModel>(await _denominationRepository.FindById(approvedCurrencyDenomination.DenominationId));
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving approved currency procurement for deletion with id {ApprovedCurrencyProcurementId}", id);
                TempData["Error"] = "An error occurred while retrieving the approved currency procurement. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ApprovedCurrencyProcurements/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(ApprovedCurrencyProcurementsController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool exists = await _approvedCurrencyProcurementRepository.IsExists(id);
                if (!exists)
                {
                    return NotFound();
                }

                var approvedCurrencyProcurement = await _approvedCurrencyProcurementRepository.FindById(id);
                if (approvedCurrencyProcurement != null)
                {
                    approvedCurrencyProcurement.IsDeleted = true;
                    bool isDeleted = await _approvedCurrencyProcurementRepository.Update(approvedCurrencyProcurement);

                    if (isDeleted)
                    {
                        TempData["Success"] = "Approved currency procurement deleted successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to delete approved currency procurement. Please try again.";
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting approved currency procurement with id {ApprovedCurrencyProcurementId}", id);
                TempData["Error"] = "An error occurred while deleting the approved currency procurement. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ViewAttachment(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            try
            {
                ApprovedCurrencyProcurement approvedCurrencyProcurement = await _approvedCurrencyProcurementRepository.FindById(id ?? 0);
                var path = approvedCurrencyProcurement.BoardApprovalAttachment;

                if (!System.IO.File.Exists(path))
                    return NotFound();

                var bytes = System.IO.File.ReadAllBytes(path);

                return File(bytes, "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing attachment for approved currency procurement with id {ApprovedCurrencyProcurementId}", id);
                TempData["Error"] = "An error occurred while viewing the approved currency procurement attachment. Please try again.";
            }
            return RedirectToAction(nameof(Index));
        }

        // Add generic transition action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(int id, string actionName, string remarks)
        {
            try
            {
                var procurement = await _approvedCurrencyProcurementRepository.FindById(id);
                if (procurement == null)
                {
                    TempData["Error"] = "Procurement not found.";
                    return RedirectToAction(nameof(Index));
                }

                var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "System";
                //var userRole = User.FindFirst(SystemPolicy.ClaimType.AppRole)?.Value ?? "";
                var userRoles = User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                    .Select(c => int.TryParse(c.Value.Split(":")[1], out var appRoleId) ? appRoleId : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList();

                bool isExecuted = await _workflowService.ExecuteTransition(
                    StatusConstants.Entity.ApprovedCurrencyProcurement,
                    id,
                    procurement.CurrentStatusId,
                    actionName,
                    username,
                    userRoles,
                    remarks);

                if (isExecuted)
                {
                    //TempData["Success"] = $"Procurement {actionName} successfully!";
                    TempData["Success"] = $"Review completed successfully!";
                }
                else
                {
                    TempData["Error"] = $"Failed to execute {actionName} action. You may not have permission or the action is not valid for current status.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing action {Action} on procurement {Id}", actionName, id);
                TempData["Error"] = "An error occurred while executing the action.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
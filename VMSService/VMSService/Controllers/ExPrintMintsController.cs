using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.ApprovedCurrencyProcurements;
using VMSService.Core.Models.CurrencyVendors;
using VMSService.Core.Models.Denominations;
using VMSService.Core.Models.ExPrintMintStatusHistories;
using VMSService.Core.Models.ExPrintMints;
using VMSService.Core.Models.WorkflowTransitions;
using VMSService.Core.Repositories.ApprovedCurrencyProcurements;
using VMSService.Core.Repositories.CurrencyVendors;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.ExpectedCurrencies;
using VMSService.Core.Repositories.ExPrintMintStatusHistories;
using VMSService.Core.Repositories.StatusTypes;
using VMSService.Core.Repositories.VendorTypes;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Core.Models.DenominationTypes;
using VMSService.Core.Models.Branches;
using VMSService.Core.Repositories.Branches;
using VMSService.Core.Repositories.DenominationTypes;

using VMSService.Core.Repositories.ExPrintMintDetails;
using VMSService.Core.Models.ExPrintMintDetails;
using VMSService.Core.Repositories.PackingListRanges;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class ExPrintMintsController(
        IExPrintMintRepository _exPrintMintRepository,
        IExPrintMintDetailRepository _exPrintMintDetailRepository,
        IStatusTypeRepository _statusTypeRepository,
        IMapper _mapper,
        ILogger<ExPrintMintsController> _logger,
        ICurrencyVendorRepository _currencyVendorRepository,
        IDenominationRepository _denominationRepository,
        IVendorTypeRepository _vendorTypeRepository,
        IApprovedCurrencyProcurementRepository _approvedCurrencyProcurementRepo,
        IWorkflowService _workflowService,
        IExPrintMintStatusHistoryRepository _statusHistoryRepository,
        IBranchRepository _branchRepository,
        IDenominationTypeRepository _denominationTypeRepository,
        IPackingListRangeRepository _packingListRangeRepository) : Controller
    {
        [Breadcrumb("Expected Stock", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: ExPrintMints
        public async Task<IActionResult> Index()
        {
            try
            {
                var exPrintMints = (await _exPrintMintRepository.FindAll());
                var model = _mapper.Map<List<ExPrintMintViewModel>>((List<ExPrintMint>)exPrintMints);

                foreach (var exPrintMint in model)
                {
                    exPrintMint.Denomination = _mapper.Map<DenominationViewModel>(await _denominationRepository.FindById(exPrintMint.DenominationId));
                    exPrintMint.Branch = _mapper.Map<BranchViewModel>(await _branchRepository.FindById(exPrintMint.BranchId));
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ex print mints");
                TempData["Error"] = "An error occurred while retrieving ex print mints. Please try again.";
                return View(new List<ExPrintMintViewModel>());
            }
        }

        // GET: ExPrintMints/Details/5
        [Breadcrumb("Details", FromController = typeof(ExPrintMintsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _exPrintMintRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var exPrintMint = await _exPrintMintRepository.FindById(id.Value);
                if (exPrintMint == null)
                {
                    return NotFound();
                }

                ExPrintMintViewModel model = _mapper.Map<ExPrintMintViewModel>(exPrintMint);

                // Get available transitions for current user
                var userRoles = User.FindAll("appRole")
                    .Where(c => c.Value.StartsWith("VMS:"))
                    .Select(c => c.Value.Split(":")[1])
                    .ToList();

                var availableTransitions = new List<WorkflowTransitionViewModel>();
                foreach (var userRole in userRoles)
                {
                    var transitions = await _workflowService.GetAvailableTransitions(
                        StatusConstants.Entity.ExPrintMint,
                        exPrintMint.CurrentStatusId,
                        userRole);
                    if (transitions != null)
                    {
                        availableTransitions.AddRange(transitions);
                    }
                }

                model.AvailableTransitions = availableTransitions.Distinct().ToList();

                model.StatusHistory = _mapper.Map<ICollection<ExPrintMintStatusHistoryViewModel>>(
                    await _statusHistoryRepository.FindByExPrintMintId(id ?? 0));

                // Fetch the details (denominations) for this batch
                var details = await _exPrintMintDetailRepository.FindByExPrintMintId(id.Value);
                if (details != null)
                {
                    var detailViewModels = _mapper.Map<ICollection<ExPrintMintDetailViewModel>>(details);
                    
                    // Enrich with denomination names from Stock Service
                    foreach (var detail in detailViewModels)
                    {
                        var denomination = await _denominationRepository.FindById(detail.DenominationId);
                        detail.DenominationName = denomination?.Name ?? $"Denomination {detail.DenominationId}";
                    }
                    
                    model.Details = detailViewModels;
                }

                model.Branch = _mapper.Map<BranchViewModel>(await _branchRepository.FindById(model.BranchId));

                // Check if packing list ranges exist for this ExPrintMint
                var packingListRanges = await _packingListRangeRepository.FindByExStockIdAsync(id.Value);
                ViewBag.PackingListRanges = packingListRanges ?? new List<PackingListRange>();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ex print mint with id {ExPrintMintId}", id);
                TempData["Error"] = "An error occurred while retrieving the ex print mint. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: ExPrintMints/Create
        [Breadcrumb("Create", FromController = typeof(ExPrintMintsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            try
            {
                ViewBag.CurrencyVendors = _mapper.Map<List<CurrencyVendorViewModel>>(await _currencyVendorRepository.FindAll());
                ViewBag.Denominations = _mapper.Map<List<DenominationViewModel>>(await _denominationRepository.FindAll());
                ViewBag.VendorTypes = _mapper.Map<List<VendorType>>(await _vendorTypeRepository.FindAll());
                ViewBag.ApprovedCurrencyProcurements = _mapper.Map<List<ApprovedCurrencyProcurementsViewModel>>(await _approvedCurrencyProcurementRepo.FindVerified());
                ViewBag.BranchList = _mapper.Map<List<BranchViewModel>>(await _branchRepository.FindAll());
                return View(new ExPrintMintBatchCreateViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                TempData["Error"] = $"An error occurred. Please try again. \n{ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ExPrintMints/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(ExPrintMintsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(ExPrintMintBatchCreateViewModel model)
        {
            ViewBag.CurrencyVendors = _mapper.Map<List<CurrencyVendorViewModel>>(await _currencyVendorRepository.FindAll());
            ViewBag.Denominations = _mapper.Map<List<DenominationViewModel>>(await _denominationRepository.FindAll());
            ViewBag.VendorTypes = _mapper.Map<List<VendorType>>(await _vendorTypeRepository.FindAll());
            ViewBag.ApprovedCurrencyProcurements = _mapper.Map<List<ApprovedCurrencyProcurementsViewModel>>(await _approvedCurrencyProcurementRepo.FindVerified());
            ViewBag.BranchList = _mapper.Map<List<BranchViewModel>>(await _branchRepository.FindAll());

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

            if (model.AttachmentFile != null && model.AttachmentFile.ContentType != "application/pdf")
            {
                ModelState.AddModelError("", "Invalid file type");
                return View(model);
            }

            try
            {
                string uploadsFolder = "C:\\VMSFiles\\Uploads";

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string attachmentPath = null;

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

                    var filePath = Path.Combine(uploadsFolder, model.AttachmentFile.FileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.AttachmentFile.CopyToAsync(stream);
                    }

                    attachmentPath = filePath;
                }

                StatusType draftStatus = await _statusTypeRepository.FindByName(StatusConstants.ExPrintMint.Draft, StatusConstants.Category.ExPrintMint);

                // Create the parent ExPrintMint record (the batch)
                var exPrintMint = new ExPrintMint
                {
                    ExPrintMintReference = model.ExPrintMintReference,
                    CurrencyVendorId = model.CurrencyVendorId,
                    DenominationId = 0, // No specific denomination for the parent
                    DeliveryDate = model.DeliveryDate,
                    Attachment = attachmentPath,
                    ApprovedCurrencyProcurementId = model.ApprovedCurrencyProcurementId,
                    CurrentStatusId = draftStatus.Id,
                    BranchId = model.BranchId,
                    IsDeleted = false
                };

                bool isParentCreated = await _exPrintMintRepository.Create(exPrintMint);
                
                if (!isParentCreated)
                {
                    _logger.LogError("Error creating parent Expected Stock");
                    ModelState.AddModelError("", "Failed to create Expected Stock batch. Please try again.");
                    return View(model);
                }

                // Create an ExPrintMintDetail record for each denomination entry
                foreach (var denomEntry in model.Denominations)
                {
                    if (denomEntry.DenominationId <= 0 || denomEntry.NumberOfPieces <= 0)
                        continue;

                    var detail = new ExPrintMintDetail
                    {
                        ExPrintMintId = exPrintMint.Id,
                        DenominationId = denomEntry.DenominationId,
                        NumberOfPieces = denomEntry.NumberOfPieces,
                        Value = denomEntry.Value,
                        NumberOfBoxes = denomEntry.NumberOfBoxes,
                        IsDeleted = false
                    };

                    bool isDetailCreated = await _exPrintMintDetailRepository.Create(detail);
                    if (!isDetailCreated)
                    {
                        _logger.LogError("Error creating Expected Stock detail for denomination {DenominationId}", denomEntry.DenominationId);
                        ModelState.AddModelError("", $"Failed to create Expected Stock detail for denomination. Please try again.");
                        return View(model);
                    }
                }

                TempData["Success"] = "Expected Stock batch created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Expected Stock batch");
                ModelState.AddModelError("", "Failed to create Expected Stock. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            return View(model);
        }

        // GET: ExPrintMints/Edit/5
        [Breadcrumb("Edit", FromController = typeof(ExPrintMintsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _exPrintMintRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var exPrintMint = await _exPrintMintRepository.FindById(id.Value);
                if (exPrintMint == null)
                {
                    return NotFound();
                }

                ViewBag.CurrencyVendors = _mapper.Map<List<CurrencyVendorViewModel>>(await _currencyVendorRepository.FindAll());
                ViewBag.Denominations = _mapper.Map<List<DenominationViewModel>>(await _denominationRepository.FindAll());
                ViewBag.VendorTypes = _mapper.Map<List<VendorType>>(await _vendorTypeRepository.FindAll());
                ViewBag.ApprovedCurrencyProcurements = _mapper.Map<List<ApprovedCurrencyProcurementsViewModel>>(await _approvedCurrencyProcurementRepo.FindVerified());
                ViewBag.BranchList = _mapper.Map<List<BranchViewModel>>(await _branchRepository.FindAll());

                var model = _mapper.Map<ExPrintMintViewModel>(exPrintMint);
                
                // Load and enrich details with denomination names
                if (model.Details != null && model.Details.Any())
                {
                    foreach (var detail in model.Details)
                    {
                        var denomination = await _denominationRepository.FindById(detail.DenominationId);
                        detail.DenominationName = denomination?.Name ?? $"Denomination {detail.DenominationId}";
                    }
                }
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ex print mint for edit with id {ExPrintMintId}", id);
                TempData["Error"] = "An error occurred while retrieving the ex print mint. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ExPrintMints/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(ExPrintMintsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, ExPrintMintViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            ViewBag.CurrencyVendors = _mapper.Map<List<CurrencyVendorViewModel>>(await _currencyVendorRepository.FindAll());
            ViewBag.Denominations = _mapper.Map<List<DenominationViewModel>>(await _denominationRepository.FindAll());
            ViewBag.VendorTypes = _mapper.Map<List<VendorType>>(await _vendorTypeRepository.FindAll());
            ViewBag.ApprovedCurrencyProcurements = _mapper.Map<List<ApprovedCurrencyProcurementsViewModel>>(await _approvedCurrencyProcurementRepo.FindVerified());
            ViewBag.BranchList = _mapper.Map<List<BranchViewModel>>(await _branchRepository.FindAll());

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
                var exPrintMint = await _exPrintMintRepository.FindById(id);
                if (exPrintMint == null)
                {
                    return NotFound();
                }

                exPrintMint.ExPrintMintReference = model.ExPrintMintReference;
                exPrintMint.CurrencyVendorId = model.CurrencyVendorId;
                exPrintMint.DeliveryDate = model.DeliveryDate;
                exPrintMint.ApprovedCurrencyProcurementId = model.ApprovedCurrencyProcurementId;
                exPrintMint.BranchId = model.BranchId;

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

                    exPrintMint.Attachment = filePath;
                }

                bool isUpdated = await _exPrintMintRepository.Update(exPrintMint);
                
                // Update denomination details
                if (isUpdated && model.Details != null && model.Details.Any())
                {
                    // Get current details from database
                    var currentDetails = await _exPrintMintDetailRepository.FindByExPrintMintId(id);
                    
                    // Process updates and additions
                    foreach (var detailModel in model.Details)
                    {
                        if (detailModel.DenominationId <= 0) continue; // Skip empty selections
                        
                        if (detailModel.Id > 0)
                        {
                            // Update existing detail
                            var existingDetail = currentDetails.FirstOrDefault(d => d.Id == detailModel.Id);
                            if (existingDetail != null)
                            {
                                existingDetail.DenominationId = detailModel.DenominationId;
                                existingDetail.NumberOfPieces = detailModel.NumberOfPieces;
                                existingDetail.Value = detailModel.Value;
                                existingDetail.NumberOfBoxes = detailModel.NumberOfBoxes;
                                await _exPrintMintDetailRepository.Update(existingDetail);
                            }
                        }
                        else
                        {
                            // Create new detail
                            var newDetail = new ExPrintMintDetail
                            {
                                ExPrintMintId = id,
                                DenominationId = detailModel.DenominationId,
                                NumberOfPieces = detailModel.NumberOfPieces,
                                Value = detailModel.Value,
                                NumberOfBoxes = detailModel.NumberOfBoxes,
                                IsDeleted = false
                            };
                            await _exPrintMintDetailRepository.Create(newDetail);
                        }
                    }
                    
                    // Delete removed details (those not in the current request)
                    var submittedDetailIds = model.Details.Where(d => d.Id > 0).Select(d => d.Id).ToList();
                    foreach (var detail in currentDetails.Where(d => !submittedDetailIds.Contains(d.Id)))
                    {
                        detail.IsDeleted = true;
                        await _exPrintMintDetailRepository.Update(detail);
                    }
                    
                    await _exPrintMintDetailRepository.Save();
                }
                
                if (isUpdated)
                {
                    TempData["Success"] = "Expected Stock updated successfully!";
                    return RedirectToAction(nameof(Details), new { id });
                }
                else
                {
                    TempData["Error"] = "Failed to update Expected Stock. Please try again.";
                    ModelState.AddModelError("", "Failed to update Expected Stock. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Expected Stock with id {ExPrintMintId}", id);
                ModelState.AddModelError("", "Failed to update Expected Stock. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            return View(model);
        }

        // GET: ExPrintMints/Delete/5
        [Breadcrumb("Delete", FromController = typeof(ExPrintMintsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _exPrintMintRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var exPrintMint = await _exPrintMintRepository.FindById(id.Value);
                if (exPrintMint == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<ExPrintMintViewModel>(exPrintMint);
                model.Denomination = _mapper.Map<DenominationViewModel>(await _denominationRepository.FindById(model.DenominationId));
                model.Branch = _mapper.Map<BranchViewModel>(await _branchRepository.FindById(model.BranchId));
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Expected Stock for deletion with id {ExPrintMintId}", id);
                TempData["Error"] = "An error occurred while retrieving the Expected Stock. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ExPrintMints/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(ExPrintMintsController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool exists = await _exPrintMintRepository.IsExists(id);
                if (!exists)
                {
                    return NotFound();
                }

                var exPrintMint = await _exPrintMintRepository.FindById(id);
                if (exPrintMint != null)
                {
                    exPrintMint.IsDeleted = true;
                    bool isDeleted = await _exPrintMintRepository.Update(exPrintMint);

                    if (isDeleted)
                    {
                        TempData["Success"] = "Expected Stock deleted successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to delete Expected Stock. Please try again.";
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Expected Stock with id {ExPrintMintId} \n", id);
                TempData["Error"] = "An error occurred while deleting the Expected Stock. Please try again.";
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
                ExPrintMint exPrintMint = await _exPrintMintRepository.FindById(id ?? 0);
                var path = exPrintMint.Attachment;

                if (!System.IO.File.Exists(path))
                    return NotFound();

                var bytes = System.IO.File.ReadAllBytes(path);

                return File(bytes, "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing attachment for Expected Stock with id {ExPrintMintId}", id);
                TempData["Error"] = "An error occurred while viewing the Expected Stock attachment. Please try again.";
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
                var exPrintMint = await _exPrintMintRepository.FindById(id);
                if (exPrintMint == null)
                {
                    TempData["Error"] = "Expected Stock not found.";
                    return RedirectToAction(nameof(Index));
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "System";
                var userRoles = User.FindAll("appRole")
                    .Where(c => c.Value.StartsWith("VMS:"))
                    .Select(c => c.Value.Split(":")[1])
                    .ToList();

                bool isExecuted = await _workflowService.ExecuteTransition(
                    StatusConstants.Entity.ExPrintMint,
                    id,
                    exPrintMint.CurrentStatusId,
                    actionName,
                    userId,
                    userRoles,
                    remarks);

                if (isExecuted)
                {
                    TempData["Success"] = $"Review completed successfully and Notification Sent!";
                }
                else
                {
                    TempData["Error"] = $"Failed to execute {actionName} action. You may not have permission or the action is not valid for current status.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing action {Action} on Expected Stock {Id}", actionName, id);
                TempData["Error"] = "An error occurred while executing the action.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
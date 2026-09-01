using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.ApprovedCurrencyProcurements;
using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.Branches;
using VMSService.Core.Models.CurrencyVendors;
using VMSService.Core.Models.Denominations;
using VMSService.Core.Models.DenominationTypes;
using VMSService.Core.Models.ExPrintMintDetails;
using VMSService.Core.Models.ExPrintMints;
using VMSService.Core.Models.ExPrintMintStatusHistories;
using VMSService.Core.Repositories.ApprovedCurrencyProcurements;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.Branches;
using VMSService.Core.Repositories.CurrencyVendors;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.DenominationTypes;
using VMSService.Core.Repositories.ExpectedCurrencies;
using VMSService.Core.Repositories.ExPrintMintDetails;
using VMSService.Core.Repositories.ExPrintMintStatusHistories;
using VMSService.Core.Repositories.Logistics;
using VMSService.Core.Repositories.PackingListRanges;
using VMSService.Core.Repositories.VendorTypes;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class ExPrintMintsController(
        IExPrintMintRepository _exPrintMintRepository,
        IExPrintMintDetailRepository _exPrintMintDetailRepository,
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
        IPackingListRangeRepository _packingListRangeRepository,
        IConfigurationsServiceClient _configServiceClient,
        IWorkflowServiceClient _workflowServiceClient,
        IWebHostEnvironment _environment,
        IAttachmentRepository _attachmentRepository,
        ILogisticsRepository _logisticsRepository) : Controller
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
                        EntityTypeName = StatusConstants.Entity.ExPrintMint,
                        CurrentStatusId = exPrintMint.CurrentStatusId,
                        AppRole = userRole,
                    };

                    var transitions = await _workflowServiceClient.GetAvailableTransitions(requestData);
                    if (transitions != null)
                    {
                        availableTransitions.AddRange(transitions);
                    }
                }

                model.AvailableTransitions = availableTransitions.Distinct().ToList();

                // Load status history
                model.StatusHistory = _mapper.Map<ICollection<ExPrintMintStatusHistoryViewModel>>(
                    await _statusHistoryRepository.FindByExPrintMintId(id ?? 0));

                // Load attachments for each status history entry
                foreach (var history in model.StatusHistory)
                {
                    history.NewStatusName = (await _workflowServiceClient.FindStatusById(history.NewStatusId)).Name;
                    history.OldStatusName = (await _workflowServiceClient.FindStatusById(history.OldStatusId)).Name;

                    // Load attachments for this status history entry
                    if (!string.IsNullOrEmpty(history.AttachmentIds))
                    {
                        var attachmentIds = history.AttachmentIds.Split(',')
                            .Where(id => int.TryParse(id.Trim(), out _))
                            .Select(id => int.Parse(id.Trim()))
                            .ToList();

                        var attachments = new List<AttachmentViewModel>();
                        foreach (var attachmentId in attachmentIds)
                        {
                            var attachment = await _attachmentRepository.FindById(attachmentId);
                            if (attachment != null && !attachment.IsDeleted)
                            {
                                attachments.Add(new AttachmentViewModel
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
                        history.Attachments = attachments;
                    }
                }

                // Load all attachments for this ExPrintMint
                var allAttachments = await _attachmentRepository.FindByEntityId(id.Value.ToString());
                var exPrintMintAttachments = allAttachments
                    .Where(a => !a.IsDeleted && a.EntityType == StatusConstants.Entity.ExPrintMint)
                    .Select(a => new AttachmentViewModel
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        FilePath = a.FilePath,
                        FileType = a.FileType,
                        FileSize = a.FileSize,
                        UploadedDate = a.CreatedDate
                    })
                    .ToList();

                model.AllAttachments = exPrintMintAttachments;

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

                ViewBag.LogisticsList = await _logisticsRepository.GetAllAsync();

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
                ViewBag.BranchList = _mapper.Map<List<BranchDto>>(await _configServiceClient.GetAllBouBranchesAsync());

                //ViewBag.BranchList = _mapper.Map<List<BranchViewModel>>(await _branchRepository.FindAll());
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
            ViewBag.BranchList = _mapper.Map<List<BranchDto>>(await _configServiceClient.GetAllBouBranchesAsync());

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

                StatusTypeDto draftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    StatusName = StatusConstants.ReceivedStock.Draft,
                    EntityTypeName = StatusConstants.Entity.ExPrintMint
                });

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
                    CurrentStatusName = draftStatus.Name,
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
                ViewBag.BranchList = _mapper.Map<List<BranchDto>>(await _configServiceClient.GetAllBouBranchesAsync());

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
            ViewBag.BranchList = _mapper.Map<List<BranchDto>>(await _configServiceClient.GetAllBouBranchesAsync());

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

        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            try
            {
                var attachment = await _attachmentRepository.FindById(id);
                if (attachment == null || attachment.IsDeleted)
                {
                    TempData["Error"] = "Attachment not found.";
                    return NotFound();
                }

                var filePath = Path.Combine(_environment.WebRootPath, attachment.FilePath);
                
                if (!System.IO.File.Exists(filePath))
                {
                    TempData["Error"] = "File not found on server.";
                    return NotFound();
                }

                var memory = new MemoryStream();
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                return File(memory, attachment.FileType, attachment.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading attachment {AttachmentId}", id);
                TempData["Error"] = "An error occurred while downloading the file.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Add generic transition action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(int id, string actionName, string remarks, int? logisticsId)
        {
            try
            {
                var exPrintMint = await _exPrintMintRepository.FindById(id);
                if (exPrintMint == null)
                {
                    TempData["Error"] = "Expected Stock not found.";
                    return RedirectToAction(nameof(Index));
                }

                var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "System";
                var userRoles = User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                    .Select(c => int.TryParse(c.Value.Split(":")[1], out var appRoleId) ? appRoleId : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList();

                // Get the transition to check if attachments are required
                var requestData = new GetTransitionByKeyRequestDto
                {
                    EntityTypeName = StatusConstants.Entity.ExPrintMint,
                    FromStatusId = exPrintMint.CurrentStatusId,
                    ActionName = actionName,
                };

                var transition = await _workflowServiceClient.FindByTransitionKey(requestData);

                // Handle file uploads if attachments are required
                List<long>? uploadedAttachmentIds = null;

                if (transition != null && transition.RequiresAttachments && transition.AttachmentRequirements.Any())
                {
                    uploadedAttachmentIds = await HandleAttachmentUploads(
                        transition.AttachmentRequirements.ToList(),
                        transition.Id,
                        id,
                        StatusConstants.Entity.ExPrintMint,
                        username);

                    // Validate required attachments
                    var requiredAttachments = transition.AttachmentRequirements.Where(r => r.IsRequired).ToList();
                    if (requiredAttachments.Any())
                    {
                        var uploadedRequirementIds = uploadedAttachmentIds?
                            .Select(aid =>
                            {
                                // You'll need to track which requirement each attachment belongs to
                                // This is a simplified check - you may need to enhance this
                                return transition.AttachmentRequirements
                                    .FirstOrDefault(r => Request.Form.Files[$"attachments_{r.Id}"]?.Length > 0)?.Id ?? 0;
                            })
                            .Where(id => id > 0)
                            .ToList();

                        var missingRequirements = requiredAttachments
                            .Where(r => !uploadedRequirementIds.Contains(r.Id))
                            .ToList();

                        if (missingRequirements.Any())
                        {
                            TempData["Error"] = $"Missing required attachments: {string.Join(", ", missingRequirements.Select(r => r.AttachmentName))}";
                            return RedirectToAction(nameof(Details), new { id });
                        }
                    }
                }


                bool isExecuted = await _workflowService.ExecuteTransition(
                    StatusConstants.Entity.ExPrintMint,
                    id,
                    exPrintMint.CurrentStatusId,
                    actionName,
                    username,
                    userRoles,
                    remarks,
                    uploadedAttachmentIds);

                if (isExecuted)
                {
                    if (logisticsId.HasValue && logisticsId.Value > 0)
                    {
                        exPrintMint.LogisticsId = logisticsId.Value;
                        await _exPrintMintRepository.Update(exPrintMint);
                    }

                    var attachmentMessage = uploadedAttachmentIds?.Any() == true
                        ? $" {uploadedAttachmentIds.Count} attachment(s) uploaded."
                        : "";
                    TempData["Success"] = $"Action completed successfully.{attachmentMessage}";
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

        private async Task<List<long>> HandleAttachmentUploads(
            List<TransitionAttachmentRequirementDto> requirements,
            int workflowTransitionId,
            int entityId,
            string entityType,
            string username)
        {
            var uploadedAttachmentIds = new List<long>();

            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(FileStorageConstants.UploadsRoot, "exprintmint", entityId.ToString());
            Directory.CreateDirectory(uploadsPath);

            foreach (var requirement in requirements)
            {
                var fileInputName = $"attachments_{requirement.Id}";
                var file = Request.Form.Files[fileInputName];

                if (file != null && file.Length > 0)
                {
                    try
                    {
                        // Validate file format
                        var fileExtension = Path.GetExtension(file.FileName).TrimStart('.').ToLower();
                        var allowedFormats = requirement.AllowedFormats.Split(',')
                            .Select(f => f.Trim().ToLower())
                            .ToList();

                        // Map common format names
                        var formatMap = new Dictionary<string, List<string>>
                        {
                            { "excel", new List<string> { "xls", "xlsx" } },
                            { "word", new List<string> { "doc", "docx" } },
                            { "docx", new List<string> { "doc", "docx" } },
                            { "jpg", new List<string> { "jpg", "jpeg" } },
                            { "jpeg", new List<string> { "jpg", "jpeg" } }
                        };

                        bool isValidFormat = false;
                        foreach (var format in allowedFormats)
                        {
                            if (formatMap.ContainsKey(format))
                            {
                                if (formatMap[format].Contains(fileExtension))
                                {
                                    isValidFormat = true;
                                    break;
                                }
                            }
                            else if (format == fileExtension)
                            {
                                isValidFormat = true;
                                break;
                            }
                        }

                        if (!isValidFormat)
                        {
                            _logger.LogWarning("Invalid file format {Extension} for requirement {RequirementId}",
                                fileExtension, requirement.Id);
                            continue;
                        }

                        // Generate unique filename
                        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                        var uniqueFileName = $"{requirement.Id}_{timestamp}_{Path.GetFileNameWithoutExtension(file.FileName)}.{fileExtension}";
                        var filePath = Path.Combine(uploadsPath, uniqueFileName);

                        // Save file
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        // Create attachment record
                        var attachment = new Attachment
                        {
                            FileName = file.FileName,
                            FilePath = Path.Combine("uploads", "exprintmint", entityId.ToString(), uniqueFileName),
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

                        _logger.LogInformation(
                            "Attachment uploaded successfully. File: {FileName}, Size: {Size}, AttachmentId: {AttachmentId}",
                            file.FileName, file.Length, attachment.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading attachment for requirement {RequirementId}", requirement.Id);
                    }
                }
            }

            return uploadedAttachmentIds;
        }
    }
}
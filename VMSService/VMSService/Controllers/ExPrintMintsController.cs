using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.ApprovedCurrencyProcurements;
using VMSService.Core.Models.Branches;
using VMSService.Core.Models.CurrencyVendors;
using VMSService.Core.Models.Denominations;
using VMSService.Core.Models.DenominationTypes;
using VMSService.Core.Models.ExPrintMints;
using VMSService.Core.Models.ExPrintMintStatusHistories;
using VMSService.Core.Repositories.ApprovedCurrencyProcurements;
using VMSService.Core.Repositories.Branches;
using VMSService.Core.Repositories.CurrencyVendors;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.DenominationTypes;
using VMSService.Core.Repositories.ExpectedCurrencies;
using VMSService.Core.Repositories.ExPrintMintStatusHistories;
using VMSService.Core.Repositories.VendorTypes;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class ExPrintMintsController(
        IExPrintMintRepository _exPrintMintRepository,
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
        IWorkflowServiceClient _workflowServiceClient) : Controller
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

                model.StatusHistory = _mapper.Map<ICollection<ExPrintMintStatusHistoryViewModel>>(
                    await _statusHistoryRepository.FindByExPrintMintId(id ?? 0));

                model.Denomination = _mapper.Map<DenominationViewModel>(await _denominationRepository.FindById(model.DenominationId));

                DenominationType denominationType = await _denominationTypeRepository.FindById(model.Denomination.DenominationTypeId);

                model.Denomination.DenominationType = _mapper.Map<DenominationTypeViewModel>(denominationType);

                model.Branch = _mapper.Map<BranchViewModel>(await _branchRepository.FindById(model.BranchId));

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
                return View(new ExPrintMintViewModel());
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
        public async Task<IActionResult> Create(ExPrintMintViewModel model)
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

            if (model.Attachment != null && model.AttachmentFile.ContentType != "application/pdf")
            {
                ModelState.AddModelError("", "Invalid file type");
                return View(model);
            }

            try
            {
                string uploadsFolder = "C:\\VMSFiles\\Uploads";

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

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

                    model.Attachment = filePath;
                }

                var requestData = new GetStatusByNameRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    StatusName = StatusConstants.ReceivedStock.Draft,
                    EntityTypeName = StatusConstants.Entity.ExPrintMint
                };
                StatusTypeDto status = await _workflowServiceClient.FindStatusByName(requestData);
                model.CurrentStatusId = status.Id;
                model.CurrentStatusName = status.Name;

                ExPrintMint newExPrintMint = _mapper.Map<ExPrintMint>(model);
                //bool isCreated = await _exPrintMintRepository.Create(newExPrintMint);
                int newExPrintMintId = await _exPrintMintRepository.CreateNew(newExPrintMint);

                if (newExPrintMintId > 0)
                {
                    TempData["Success"] = "Expected Stock created successfully!";
                    return RedirectToAction(nameof(Details), new { id = newExPrintMintId });
                }
                else
                {
                    TempData["Error"] = "Failed to create Expected Stock. Please try again.";
                    _logger.LogError("Error creating Expected Stock");
                    ModelState.AddModelError("", "Failed to create Expected Stock. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Expected Stock");
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
                exPrintMint.DenominationId = model.DenominationId;
                exPrintMint.DeliveryDate = model.DeliveryDate;
                exPrintMint.NumberOfPieces = model.NumberOfPieces;
                exPrintMint.Value = model.Value;
                exPrintMint.BoxNumberPrefix = model.BoxNumberPrefix;
                exPrintMint.BoxNumberRangeStart = model.BoxNumberRangeStart;
                exPrintMint.BoxNumberRangeEnd = model.BoxNumberRangeEnd;
                exPrintMint.SerialNumberPrefix = model.SerialNumberPrefix;
                exPrintMint.SerialNumberRangeStart = model.SerialNumberRangeStart;
                exPrintMint.SerialNumberRangeEnd = model.SerialNumberRangeEnd;
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

                var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "System";
                var userRoles = User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                    .Select(c => int.TryParse(c.Value.Split(":")[1], out var appRoleId) ? appRoleId : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList();

                bool isExecuted = await _workflowService.ExecuteTransition(
                    StatusConstants.Entity.ExPrintMint,
                    id,
                    exPrintMint.CurrentStatusId,
                    actionName,
                    username,
                    userRoles,
                    remarks);

                if (isExecuted)
                {
                    TempData["Success"] = $"Review completed successfully!";
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
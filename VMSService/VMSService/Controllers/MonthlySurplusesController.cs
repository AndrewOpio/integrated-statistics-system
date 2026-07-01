using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;
using VMSService.Controllers;
using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.MonthlySurpluses;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.MonthlySurpluses;
using VMSService.Core.Repositories.MonthlySurplusStatusHistories;
using VMSService.Core.Repositories.StockMovements;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;
using VMSService.Web.Authorization;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class MonthlySurplusesController(
        IMonthlySurplusRepository _surplusRepo,
        IMonthlySurplusStatusHistoryRepository _historyRepo,
        IAttachmentRepository _attachmentRepo,
        IWorkflowServiceClient _workflowServiceClient,
        IWorkflowService _workflowService,
        IConfigurationsServiceClient _configServiceClient,
        IStockService _stockService,
        IStockMovementRepository _stockMovementRepository,
        VMSServiceDbContext _db,
        IWebHostEnvironment _environment) : Controller
    {
        [RequirePermission(SystemPermissions.MonthlySurpluses.View)]
        [Breadcrumb("Monthly Surplus", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public async Task<IActionResult> Index()
        {
            var surpluses = await _surplusRepo.FindAll();
            var banks     = await _configServiceClient.GetAllCommercialBankAsync();
            ViewBag.BankNames = banks?.ToDictionary(b => b.Id, b => b.Name) ?? new Dictionary<int, string>();
            return View(surpluses);
        }

        [RequirePermission(SystemPermissions.MonthlySurpluses.Create)]
        [HttpGet]
        [Breadcrumb("Create", FromController = typeof(MonthlySurplusesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(int? branchId, string? period, decimal? amount)
        {
            if (TempData["AllowSurplusCreate"] == null)
            {
                TempData["Error"] = "Please use the Check Net Position page to initiate a surplus record.";
                return RedirectToAction(nameof(CheckPosition));
            }

            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            var banks    = await _configServiceClient.GetAllCommercialBankAsync();
            ViewBag.Branches = branches?.ToList() ?? new();
            ViewBag.Banks    = banks?.ToList() ?? new();
            return View();
        }

        [RequirePermission(SystemPermissions.MonthlySurpluses.Create)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMonthlySurplusDto dto)
        {
            try
            {
                int initialStatusId = 0;
                string initialStatusName = StatusConstants.MonthlySurplus.Draft;
                try
                {
                    var draftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName     = StatusConstants.Entity.MonthlySurpluses,
                        StatusName         = StatusConstants.MonthlySurplus.Draft,
                    });
                    if (draftStatus?.Name != null)
                    {
                        initialStatusId   = draftStatus.Id;
                        initialStatusName = draftStatus.Name;
                    }
                }
                catch { /* WorkflowService may not be configured yet */ }

                var surplus = new MonthlySurplus
                {
                    ReferenceNumber   = dto.ReferenceNumber,
                    BranchId          = dto.BranchId,
                    Period            = dto.Period,
                    Amount            = dto.Amount,
                    DepositDate       = dto.DepositDate,
                    CommercialBankId  = dto.CommercialBankId,
                    AccountNumber     = dto.AccountNumber,
                    Remarks           = dto.Remarks,
                    CurrentStatusId   = initialStatusId,
                    CurrentStatusName = initialStatusName,
                };

                await _surplusRepo.Create(surplus);

                return Ok(new { success = true, redirectUrl = Url.Action(nameof(Details), new { id = surplus.Id }) });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message });
            }
        }

        [RequirePermission(SystemPermissions.MonthlySurpluses.Create)]
        [HttpGet]
        public IActionResult InitiateSurplus(int branchId, string period, decimal amount)
        {
            TempData["AllowSurplusCreate"] = "1";
            return RedirectToAction(nameof(Create), new { branchId, period, amount });
        }

        [RequirePermission(SystemPermissions.MonthlySurpluses.CheckPosition)]
        [HttpGet]
        [Breadcrumb("Check Position", FromController = typeof(MonthlySurplusesController), FromAction = nameof(Index))]
        public async Task<IActionResult> CheckPosition()
        {
            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            ViewBag.Branches = branches?.ToList() ?? new();
            return View();
        }

        [RequirePermission(SystemPermissions.MonthlySurpluses.Create)]
        [HttpPost]
        public async Task<IActionResult> RecordStockDeduction(int branchId, decimal amount, string period, string? notes)
        {
            try
            {
                // 1. Resolve CFB category from StockService
                var categories = await _stockService.GetAllCategories();
                var cfbCategory = categories?.FirstOrDefault(c =>
                    c.Name.Equals("CFB", StringComparison.OrdinalIgnoreCase));
                if (cfbCategory == null)
                    return BadRequest(new { message = "CFB category not found in Stock Service." });

                var counterPartyTypes = await _stockService.GetAllCounterPartyTypes();
                var branchType = counterPartyTypes?
                    .FirstOrDefault(c => c.Name.Equals("Branch", StringComparison.OrdinalIgnoreCase))
                    ?? counterPartyTypes?.FirstOrDefault();
                if (branchType == null)
                    return BadRequest(new { message = "No counterparty types configured in Stock Service." });

                // 2. Get Draft status for StockMovement from WorkflowService
                int draftStatusId = 0;
                string draftStatusName = StatusConstants.StockMovement.Draft;
                try
                {
                    var draftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName     = StatusConstants.Entity.StockMovement,
                        StatusName         = StatusConstants.StockMovement.Draft,
                    });
                    if (draftStatus?.Id > 0)
                    {
                        draftStatusId   = draftStatus.Id;
                        draftStatusName = draftStatus.Name;
                    }
                }
                catch { /* WorkflowService may not be reachable */ }

                // 3. Create StockMovement record in VMSService
                var refCode = $"SLD/{branchId}/{period.Replace("-", "/")}";
                var movement = new VMSService.Data.Entities.StockMovement
                {
                    MovementType        = "branch",
                    Date                = DateTime.Now,
                    ReferenceNumber     = refCode,
                    DispatchingBranchId = branchId,
                    GrandTotalValue     = amount,
                    CurrentStatusId     = draftStatusId,
                    CurrentStatusName   = draftStatusName,
                    Denominations       = new List<VMSService.Data.Entities.StockMovementDenomination>(),
                };
                var createdMovement = await _stockMovementRepository.CreateAsync(movement);
                if (createdMovement == null)
                    return StatusCode(500, new { message = "Failed to create stock movement record." });

                // 4. Create StockLedger entry in StockService using the movement ID
                var ledger = new StockLedger
                {
                    ReferenceCode      = refCode,
                    StockMovementId    = createdMovement.Id,
                    IsIn               = false,
                    CounterPartyTypeId = branchType.Id,
                    CounterPartyId     = branchId,
                    StoreId            = branchId,
                    Notes              = string.IsNullOrWhiteSpace(notes)
                                             ? $"CFB surplus deduction UGX {amount:N0} for {period}"
                                             : notes,
                    StockLedgerDetails =
                    [
                        new StockLedgerDetail
                        {
                            DenominationId = 0,
                            CategoryId     = cfbCategory.Id,
                            NumberOfPieces = 0,
                            NumberOfBoxes  = 0,
                            Value          = amount,
                        }
                    ]
                };

                var createdLedger = await _stockService.CreateStockLedger(ledger);
                if (createdLedger == null)
                    return StatusCode(500, new { message = "Stock movement created but failed to post stock ledger entry. Please try again." });

                return Ok(new { referenceCode = createdLedger.ReferenceCode });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the stock position." });
            }
        }

        [RequirePermission(SystemPermissions.MonthlySurpluses.CheckPosition)]
        [HttpGet]
        public async Task<IActionResult> PreviewNetPosition(int branchId, int year, int month)
        {
            if (branchId <= 0 || year < 2000 || month < 1 || month > 12)
                return BadRequest(new { message = "Invalid branch or period." });

            var periodStart = new DateTime(year, month, 1);
            var periodEnd   = periodStart.AddMonths(1);

            var eodReports = await _db.EodDiscrepancyReports
                .AsNoTracking()
                .Include(r => r.Entries)
                    .ThenInclude(e => e.BoardSessionSurplusShortage)
                .Where(r => r.BranchId == branchId
                         && r.SessionDate >= periodStart && r.SessionDate < periodEnd
                         && r.CurrentStatusName == StatusConstants.CfbReport.Approved)
                .ToListAsync();

            var transactions = eodReports
                .SelectMany(r => r.Entries.Select(e => e.BoardSessionSurplusShortage))
                .ToList();

            var totalSurplus  = transactions.Where(t => t.IsSurplus).Sum(t => t.Amount);
            var totalShortage = transactions.Where(t => !t.IsSurplus).Sum(t => t.Amount);
            var net           = totalSurplus - totalShortage;

            return Ok(new
            {
                totalSurplus,
                totalShortage,
                net       = Math.Abs(net),
                isSurplus  = net > 0,
                isShortage = net < 0,
                isBalanced = net == 0,
                hasData    = transactions.Any(),
            });
        }

        [RequirePermission(SystemPermissions.MonthlySurpluses.Details)]
        [HttpGet]
        [Breadcrumb("Details", FromController = typeof(MonthlySurplusesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var surplus = await _surplusRepo.FindById(id.Value);
            if (surplus == null) return NotFound();

            try
            {
                var status = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    EntityTypeName     = StatusConstants.Entity.MonthlySurpluses,
                    StatusName         = surplus.CurrentStatusName,
                });
                if (status?.Id > 0 && status.Id != surplus.CurrentStatusId)
                {
                    surplus.CurrentStatusId = status.Id;
                    await _surplusRepo.Update(surplus);
                }
            }
            catch { }

            var availableTransitions = new List<WorkflowTransitionDto>();
            foreach (var roleId in GetUserRoles())
            {
                var transitions = await _workflowServiceClient.GetAvailableTransitions(new GetAvailableTransitionsRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    EntityTypeName     = StatusConstants.Entity.MonthlySurpluses,
                    CurrentStatusId    = surplus.CurrentStatusId,
                    AppRole            = roleId,
                });
                if (transitions != null)
                    availableTransitions.AddRange(transitions);
            }

            var branches   = await _configServiceClient.GetAllBouBranchesAsync();
            var banks      = await _configServiceClient.GetAllCommercialBankAsync();
            var branchName = branches?.FirstOrDefault(b => b.Id == surplus.BranchId)?.Name ?? surplus.BranchId.ToString();
            var bankName   = banks?.FirstOrDefault(b => b.Id == surplus.CommercialBankId)?.Name ?? surplus.CommercialBankId?.ToString() ?? string.Empty;

            var historyEntities = await _historyRepo.FindByMonthlySurplusId(id.Value);
            var statusHistory   = new List<MonthlySurplusStatusHistoryViewModel>();
            foreach (var h in historyEntities)
            {
                var entry = new MonthlySurplusStatusHistoryViewModel
                {
                    OldStatusId   = h.OldStatusId,
                    OldStatusName = h.OldStatusName,
                    NewStatusId   = h.NewStatusId,
                    NewStatusName = h.NewStatusName,
                    ChangedBy     = h.ChangedBy,
                    ChangedAt     = h.ChangedAt,
                    Remarks       = h.Remarks,
                };

                if (!string.IsNullOrEmpty(h.AttachmentIds))
                {
                    var attachments = new List<AttachmentViewModel>();
                    foreach (var aid in h.AttachmentIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (int.TryParse(aid.Trim(), out var attachmentId))
                        {
                            var att = await _attachmentRepo.FindById(attachmentId);
                            if (att != null && !att.IsDeleted)
                                attachments.Add(new AttachmentViewModel
                                {
                                    Id       = att.Id,
                                    FileName = att.FileName,
                                    FilePath = att.FilePath,
                                    FileSize = att.FileSize,
                                    FileType = att.FileType,
                                });
                        }
                    }
                    entry.Attachments = attachments;
                }

                statusHistory.Add(entry);
            }

            var allAttachments = (await _attachmentRepo.FindByEntityId(id.Value.ToString()))
                .Where(a => !a.IsDeleted && a.EntityType == StatusConstants.Entity.MonthlySurpluses)
                .Select(a => new AttachmentViewModel
                {
                    Id       = a.Id,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    FileSize = a.FileSize,
                    FileType = a.FileType,
                });

            var vm = new MonthlySurplusDetailsViewModel
            {
                Id                   = surplus.Id,
                ReferenceNumber      = surplus.ReferenceNumber,
                BranchId             = surplus.BranchId,
                BranchName           = branchName,
                Period               = surplus.Period,
                Amount               = surplus.Amount,
                DepositDate          = surplus.DepositDate,
                CommercialBankId     = surplus.CommercialBankId,
                CommercialBankName   = bankName,
                AccountNumber        = surplus.AccountNumber,
                Remarks              = surplus.Remarks,
                CurrentStatusId      = surplus.CurrentStatusId,
                CurrentStatusName    = surplus.CurrentStatusName,
                CreatedBy            = surplus.CreatedBy,
                CreatedDate          = surplus.CreatedDate,
                AvailableTransitions = availableTransitions,
                StatusHistory        = statusHistory,
                Attachments          = allAttachments,
            };

            return View(vm);
        }

        [RequirePermission(SystemPermissions.MonthlySurpluses.Edit)]
        [HttpGet]
        [Breadcrumb("Edit", FromController = typeof(MonthlySurplusesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var surplus = await _surplusRepo.FindById(id.Value);
            if (surplus == null) return NotFound();

            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            var banks    = await _configServiceClient.GetAllCommercialBankAsync();
            ViewBag.Branches = branches?.ToList() ?? new();
            ViewBag.Banks    = banks?.ToList() ?? new();
            return View(surplus);
        }

        [RequirePermission(SystemPermissions.MonthlySurpluses.Edit)]
        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] UpdateMonthlySurplusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errors = ModelState });

            try
            {
                var surplus = await _surplusRepo.FindById(dto.Id);
                if (surplus == null) return NotFound();

                surplus.ReferenceNumber  = dto.ReferenceNumber;
                surplus.BranchId         = dto.BranchId;
                surplus.Period           = dto.Period;
                surplus.Amount           = dto.Amount;
                surplus.DepositDate      = dto.DepositDate;
                surplus.CommercialBankId = dto.CommercialBankId;
                surplus.AccountNumber    = dto.AccountNumber;
                surplus.Remarks          = dto.Remarks;

                await _surplusRepo.Update(surplus);
                return Ok(new { success = true, redirectUrl = Url.Action(nameof(Details), new { id = dto.Id }) });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(int id, string actionName, string remarks)
        {
            var surplus = await _surplusRepo.FindById(id);
            if (surplus == null)
            {
                TempData["Error"] = "Surplus not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var username  = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var userRoles = GetUserRoles();

                var transition = await _workflowServiceClient.FindByTransitionKey(new GetTransitionByKeyRequestDto
                {
                    EntityTypeName = StatusConstants.Entity.MonthlySurpluses,
                    FromStatusId   = surplus.CurrentStatusId,
                    ActionName     = actionName,
                });

                List<long>? uploadedAttachmentIds = null;
                if (transition != null && transition.RequiresAttachments && transition.AttachmentRequirements.Any())
                {
                    uploadedAttachmentIds = await HandleAttachmentUploads(
                        transition.AttachmentRequirements.ToList(),
                        transition.Id,
                        id,
                        StatusConstants.Entity.MonthlySurpluses,
                        username);

                    var missingRequired = transition.AttachmentRequirements
                        .Where(r => r.IsRequired && !(Request.Form.Files[$"attachments_{r.Id}"]?.Length > 0))
                        .Select(r => r.AttachmentName)
                        .ToList();

                    if (missingRequired.Any())
                    {
                        TempData["Error"] = $"Required attachment(s) missing: {string.Join(", ", missingRequired)}.";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                }

                var executed = await _workflowService.ExecuteTransition(
                    entityType:      StatusConstants.Entity.MonthlySurpluses,
                    entityId:        id,
                    currentStatusId: surplus.CurrentStatusId,
                    actionName:      actionName,
                    username:        username,
                    userRoles:       userRoles,
                    remarks:         remarks,
                    attachmentIds:   uploadedAttachmentIds);

                TempData[executed ? "Success" : "Error"] = executed
                    ? "Action completed successfully."
                    : $"Failed to execute '{actionName}'. You may not have the required role or the action is unavailable.";
            }
            catch
            {
                TempData["Error"] = "An error occurred while executing the action.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var attachment = await _attachmentRepo.FindById(id);
            if (attachment == null || attachment.IsDeleted)
                return NotFound();

            var fullPath = Path.Combine(_environment.WebRootPath, attachment.FilePath);
            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var memory = new MemoryStream();
            await using (var stream = new FileStream(fullPath, FileMode.Open))
                await stream.CopyToAsync(memory);
            memory.Position = 0;

            return File(memory, attachment.FileType, attachment.FileName);
        }

        private async Task<List<long>> HandleAttachmentUploads(
            List<TransitionAttachmentRequirementDto> requirements,
            int workflowTransitionId,
            int entityId,
            string entityType,
            string username)
        {
            var uploadedIds = new List<long>();
            var uploadDir   = Path.Combine(_environment.WebRootPath, "uploads", "surpluses", entityId.ToString());
            Directory.CreateDirectory(uploadDir);

            foreach (var req in requirements)
            {
                var file = Request.Form.Files[$"attachments_{req.Id}"];
                if (file == null || file.Length == 0) continue;

                var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var fullPath   = Path.Combine(uploadDir, uniqueName);

                await using (var stream = new FileStream(fullPath, FileMode.Create))
                    await file.CopyToAsync(stream);

                var attachment = new Attachment
                {
                    FileName                          = file.FileName,
                    FilePath                          = Path.Combine("uploads", "surpluses", entityId.ToString(), uniqueName),
                    FileType                          = file.ContentType,
                    FileSize                          = file.Length,
                    WorkflowTransitionId              = workflowTransitionId,
                    TransitionAttachmentRequirementId = req.Id,
                    EntityId                          = entityId.ToString(),
                    EntityType                        = entityType,
                    IsDeleted                         = false,
                    CreatedBy                         = username,
                };

                await _attachmentRepo.Create(attachment);
                uploadedIds.Add(attachment.Id);
            }

            return uploadedIds;
        }

        private List<int> GetUserRoles()
            => User.FindAll("appRoleId")
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => int.TryParse(c.Value.Split(":")[1], out var roleId) ? roleId : (int?)null)
                .Where(r => r.HasValue)
                .Select(r => r!.Value)
                .ToList();
    }
}

using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;
using VMSService.Controllers;
using VMSService.Core.Models.DestructionAuthorizations;
using VMSService.Core.Repositories.DestructionAuthorizationStatusHistories;
using VMSService.Core.Repositories.DestructionAuthorizations;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class DestructionAuthorizationsController : Controller
    {
        private readonly IDestructionAuthorizationRepository _authorizationRepository;
        private readonly IDestructionAuthorizationStatusHistoryRepository _statusHistoryRepository;
        private readonly IStockService _stockService;
        private readonly IWorkflowServiceClient _workflowServiceClient;
        private readonly IWorkflowService _workflowService;

        public DestructionAuthorizationsController(
            IDestructionAuthorizationRepository authorizationRepository,
            IDestructionAuthorizationStatusHistoryRepository statusHistoryRepository,
            IStockService stockService,
            IWorkflowServiceClient workflowServiceClient,
            IWorkflowService workflowService)
        {
            _authorizationRepository  = authorizationRepository;
            _statusHistoryRepository  = statusHistoryRepository;
            _stockService             = stockService;
            _workflowServiceClient    = workflowServiceClient;
            _workflowService          = workflowService;
        }

        [Breadcrumb("Destruction Authorizations", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public async Task<IActionResult> Index()
        {
            var authorizations = await _authorizationRepository.FindAll();
            return View(authorizations);
        }

        [HttpGet]
        [Breadcrumb("Create", FromController = typeof(DestructionAuthorizationsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            ViewBag.Denominations = await _stockService.GetAllDenominations();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDestructionAuthorizationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errors = ModelState });

            try
            {
                int initialStatusId = 0;
                string initialStatusName = StatusConstants.DestructionAuthorization.Draft;
                try
                {
                    var draftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName     = StatusConstants.Entity.DestructionAuthorization,
                        StatusName         = StatusConstants.DestructionAuthorization.Draft,
                    });
                    if (draftStatus?.Name != null)
                    {
                        initialStatusId   = draftStatus.Id;
                        initialStatusName = draftStatus.Name;
                    }
                }
                catch { /* WorkflowService may not be configured yet */ }

                var authorization = new DestructionAuthorization
                {
                    ReferenceNumber   = dto.ReferenceNumber,
                    Date              = dto.Date,
                    CurrentStatusId   = initialStatusId,
                    CurrentStatusName = initialStatusName,
                    DestructionAuthorizationDenominations = dto.Denominations.Select(d => new DestructionAuthorizationDenomination
                    {
                        DenominationId = d.DenominationId,
                        NumberOfBags   = d.NumberOfBags,
                        Amount         = d.Amount
                    }).ToList()
                };

                await _authorizationRepository.Create(authorization);
                return Ok(new { success = true, redirectUrl = Url.Action(nameof(Index)) });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message });
            }
        }

        [HttpGet]
        [Breadcrumb("Details", FromController = typeof(DestructionAuthorizationsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var authorization = await _authorizationRepository.FindById(id.Value);
            if (authorization == null) return NotFound();

            var denominations = await _stockService.GetAllDenominations();

            // If created before workflow was configured, CurrentStatusId is 0 — resolve and persist the real ID
            if (authorization.CurrentStatusId == 0)
            {
                try
                {
                    var draftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName     = StatusConstants.Entity.DestructionAuthorization,
                        StatusName         = authorization.CurrentStatusName,
                    });
                    if (draftStatus?.Id > 0)
                    {
                        authorization.CurrentStatusId = draftStatus.Id;
                        await _authorizationRepository.Update(authorization);
                    }
                }
                catch { /* workflow may not be reachable */ }
            }

            var availableTransitions = new List<WorkflowTransitionDto>();
            foreach (var roleId in GetUserRoles())
            {
                var transitions = await _workflowServiceClient.GetAvailableTransitions(new GetAvailableTransitionsRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    EntityTypeName     = StatusConstants.Entity.DestructionAuthorization,
                    CurrentStatusId    = authorization.CurrentStatusId,
                    AppRole            = roleId,
                });
                if (transitions != null)
                    availableTransitions.AddRange(transitions);
            }

            var historyEntities = await _statusHistoryRepository.FindByDestructionAuthorizationId(id.Value);
            var statusHistory = historyEntities.Select(h => new DestructionAuthorizationStatusHistoryViewModel
            {
                OldStatusId   = h.OldStatusId,
                OldStatusName = h.OldStatusName,
                NewStatusId   = h.NewStatusId,
                NewStatusName = h.NewStatusName,
                ChangedBy     = h.ChangedBy,
                ChangedAt     = h.ChangedAt,
                Remarks       = h.Remarks
            });

            var vm = new DestructionAuthorizationDetailsViewModel
            {
                Id                   = authorization.Id,
                ReferenceNumber      = authorization.ReferenceNumber,
                Date                 = authorization.Date,
                CurrentStatusId      = authorization.CurrentStatusId,
                CurrentStatusName    = authorization.CurrentStatusName,
                CreatedBy            = authorization.CreatedBy,
                CreatedDate          = authorization.CreatedDate,
                Denominations        = authorization.DestructionAuthorizationDenominations ?? [],
                AvailableTransitions = availableTransitions,
                StatusHistory        = statusHistory,
            };

            ViewBag.Denominations = denominations;
            return View(vm);
        }

        [HttpGet]
        [Breadcrumb("Edit", FromController = typeof(DestructionAuthorizationsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var authorization = await _authorizationRepository.FindById(id.Value);
            if (authorization == null) return NotFound();

            ViewBag.Denominations = await _stockService.GetAllDenominations();
            return View(authorization);
        }

        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] UpdateDestructionAuthorizationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errors = ModelState });

            try
            {
                var authorization = await _authorizationRepository.FindById(dto.Id);
                if (authorization == null) return NotFound();

                authorization.ReferenceNumber = dto.ReferenceNumber;
                authorization.Date            = dto.Date;
                authorization.DestructionAuthorizationDenominations = dto.Denominations.Select(d => new DestructionAuthorizationDenomination
                {
                    DestructionAuthorizationId = dto.Id,
                    DenominationId             = d.DenominationId,
                    NumberOfBags               = d.NumberOfBags,
                    Amount                     = d.Amount
                }).ToList();

                await _authorizationRepository.UpdateWithChildren(authorization);
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
            var authorization = await _authorizationRepository.FindById(id);
            if (authorization == null)
            {
                TempData["Error"] = "Destruction authorization not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var username  = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var userRoles = GetUserRoles();

                var executed = await _workflowService.ExecuteTransition(
                    entityType:      StatusConstants.Entity.DestructionAuthorization,
                    entityId:        id,
                    currentStatusId: authorization.CurrentStatusId,
                    actionName:      actionName,
                    username:        username,
                    userRoles:       userRoles,
                    remarks:         remarks);

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

        private List<int> GetUserRoles()
            => User.FindAll("appRoleId")
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => int.TryParse(c.Value.Split(":")[1], out var roleId) ? roleId : (int?)null)
                .Where(r => r.HasValue)
                .Select(r => r!.Value)
                .ToList();
    }
}

using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WorkflowService.Client.Dto;
using WorkflowService.Core.Repositories.EntityTypes;
using WorkflowService.Core.Repositories.WorkflowTransitions;
using WorkflowService.Core.Services.Identity;

namespace WorkflowService.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransitionController(
        IWorkflowTransitionRepository _transitionRepository,
        IEntityTypeRepository _entityTypeRepository,
        IMapper _mapper,
        ILogger<TransitionController> _logger,
        IIdentityService _identityService) : ControllerBase
    {
        /// <summary>
        /// Get available transitions for a given entity type, current status, and application role.
        /// </summary>
        /// <param name="request">The request containing applicationAcronym, entityTypeName, currentStatusId, and appRole</param>
        /// <returns>A list of available workflow transitions</returns>
        [HttpPost("available-transitions")]
        public async Task<IActionResult> GetAvailableTransitions([FromBody] GetAvailableTransitionsRequest request)
        {
            try
            {
                // Validate input parameters
                if (request == null)
                {
                    _logger.LogWarning("Request body is null");
                    return BadRequest(new { message = "Request body cannot be null." });
                }

                if (string.IsNullOrWhiteSpace(request.ApplicationAcronym))
                {
                    _logger.LogWarning("Invalid applicationAcronym: {ApplicationAcronym}", request.ApplicationAcronym);
                    return BadRequest(new { message = "applicationAcronym cannot be empty." });
                }

                if (string.IsNullOrWhiteSpace(request.EntityTypeName))
                {
                    _logger.LogWarning("Invalid entityTypeName: empty or null");
                    return BadRequest(new { message = "entityTypeName cannot be empty." });
                }

                if (request.CurrentStatusId <= 0)
                {
                    _logger.LogWarning("Invalid currentStatusId: {CurrentStatusId}", request.CurrentStatusId);
                    return BadRequest(new { message = "currentStatusId must be greater than 0." });
                }

                if (request.AppRole <= 0)
                {
                    _logger.LogWarning("Invalid appRole: {AppRole}", request.AppRole);
                    return BadRequest(new { message = "appRole must be greater than 0." });
                }

                // Get entity type by name and application ID
                var application = await _identityService.GetApplicationByAcronymAsync(request.ApplicationAcronym);
                if (application == null)
                {
                    _logger.LogWarning("Application not found - ApplicationAcronym: {ApplicationAcronym}", request.ApplicationAcronym);
                    return NotFound(new { message = $"Application with acronym '{request.ApplicationAcronym}' not found." });
                }

                var entityType = await _entityTypeRepository.FindByNameAndApplicationId(request.EntityTypeName, application.Id);
                if (entityType == null)
                {
                    _logger.LogWarning("Entity type not found - EntityTypeName: {EntityTypeName}, ApplicationAcronym: {ApplicationAcronym}",
                        request.EntityTypeName, request.ApplicationAcronym);
                    return NotFound(new { message = $"Entity type '{request.EntityTypeName}' not found for Application {request.ApplicationAcronym}." });
                }

                // Get transitions for the entity type and current status
                var transitions = await _transitionRepository.FindByEntityTypeStatusAndRole(entityType.Id, request.CurrentStatusId, request.AppRole);

                if (transitions == null || transitions.Count == 0)
                {
                    _logger.LogInformation("No transitions found - EntityTypeId: {EntityTypeId}, StatusId: {StatusId}",
                        entityType.Id, request.CurrentStatusId);
                    return Ok(new List<WorkflowTransitionDto>());
                }

                // Map to DTOs
                var transitionDtos = _mapper.Map<List<WorkflowTransitionDto>>(transitions);

                _logger.LogInformation("Retrieved {TransitionCount} available transitions - EntityTypeId: {EntityTypeId}, StatusId: {StatusId}, AppRole: {AppRole}",
                    transitionDtos.Count, entityType.Id, request.CurrentStatusId, request.AppRole);

                return Ok(transitionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available transitions - Application: {ApplicationAcronym}, EntityTypeName: {EntityTypeName}, CurrentStatusId: {CurrentStatusId}, AppRole: {AppRole}",
                    request?.ApplicationAcronym, request?.EntityTypeName, request?.CurrentStatusId, request?.AppRole);
                return StatusCode(500, new { message = "An error occurred while retrieving available transitions." });
            }
        }

        /// <summary>
        /// Get a specific transition by entity type name, from status, and action name.
        /// </summary>
        /// <param name="request">The request containing entityTypeName, fromStatusId, and actionName</param>
        /// <returns>The requested workflow transition</returns>
        [HttpPost("get-transition")]
        public async Task<IActionResult> GetTransitionByKey([FromBody] GetTransitionByKeyRequest request)
        {
            try
            {
                // Validate input parameters
                if (request == null)
                {
                    _logger.LogWarning("Request body is null");
                    return BadRequest(new { message = "Request body cannot be null." });
                }

                if (string.IsNullOrWhiteSpace(request.EntityTypeName))
                {
                    _logger.LogWarning("Invalid entityTypeName: empty or null");
                    return BadRequest(new { message = "entityTypeName cannot be empty." });
                }

                if (request.FromStatusId <= 0)
                {
                    _logger.LogWarning("Invalid fromStatusId: {FromStatusId}", request.FromStatusId);
                    return BadRequest(new { message = "fromStatusId must be greater than 0." });
                }

                if (string.IsNullOrWhiteSpace(request.ActionName))
                {
                    _logger.LogWarning("Invalid actionName: {ActionName}", request.ActionName);
                    return BadRequest(new { message = "actionName cannot be empty." });
                }

                // Get entity type by name from all entity types
                // Note: This queries across all applications, you may want to add applicationId/applicationAcronym if needed
                var allEntityTypes = await _entityTypeRepository.FindAll();
                var entityType = allEntityTypes?.FirstOrDefault(et => et.Name == request.EntityTypeName);

                if (entityType == null)
                {
                    _logger.LogWarning("Entity type not found - EntityTypeName: {EntityTypeName}", request.EntityTypeName);
                    return NotFound(new { message = $"Entity type '{request.EntityTypeName}' not found." });
                }

                var transition = await _transitionRepository.FindByTransitionKey(entityType.Id, request.FromStatusId, request.ActionName);
                if (transition == null)
                {
                    _logger.LogInformation("Transition not found - EntityTypeName: {EntityTypeName}, FromStatusId: {FromStatusId}, ActionName: {ActionName}",
                        request.EntityTypeName, request.FromStatusId, request.ActionName);
                    return NotFound(new { message = "Transition not found." });
                }

                var transitionDto = _mapper.Map<WorkflowTransitionDto>(transition);
                _logger.LogInformation("Retrieved transition - EntityTypeName: {EntityTypeName}, FromStatusId: {FromStatusId}, ActionName: {ActionName}",
                    request.EntityTypeName, request.FromStatusId, request.ActionName);
                return Ok(transitionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transition - EntityTypeName: {EntityTypeName}, FromStatusId: {FromStatusId}, ActionName: {ActionName}",
                    request?.EntityTypeName, request?.FromStatusId, request?.ActionName);
                return StatusCode(500, new { message = "An error occurred while retrieving the transition." });
            }
        }
    }

    /// <summary>
    /// Request model for getting available transitions
    /// </summary>
    public class GetAvailableTransitionsRequest
    {
        /// <summary>
        /// The application acronym (e.g., "VMS" for Vault Management System)
        /// </summary>
        public string ApplicationAcronym { get; set; }

        /// <summary>
        /// The entity type name (e.g., "ApprovedCurrencyProcurement", "StockMovement")
        /// </summary>
        public string EntityTypeName { get; set; }

        /// <summary>
        /// The current status ID
        /// </summary>
        public int CurrentStatusId { get; set; }

        /// <summary>
        /// The application role ID to filter transitions
        /// </summary>
        public int AppRole { get; set; }
    }

    /// <summary>
    /// Request model for getting a transition by key
    /// </summary>
    public class GetTransitionByKeyRequest
    {
        /// <summary>
        /// The entity type name
        /// </summary>
        public string EntityTypeName { get; set; }

        /// <summary>
        /// The from status ID
        /// </summary>
        public int FromStatusId { get; set; }

        /// <summary>
        /// The action name
        /// </summary>
        public string ActionName { get; set; }
    }
}
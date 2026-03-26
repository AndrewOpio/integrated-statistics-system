using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WorkflowService.Client.Dto;
using WorkflowService.Core.Repositories.EntityTypes;
using WorkflowService.Core.Repositories.StatusTypes;
using WorkflowService.Core.Services.Identity;

namespace WorkflowService.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatusTypeController(
        IStatusTypeRepository _statusTypeRepository,
        IEntityTypeRepository _entityTypeRepository,
        IIdentityService _identityService,
        IMapper _mapper,
        ILogger<StatusTypeController> _logger) : ControllerBase
    {
        /// <summary>
        /// Get all status types.
        /// </summary>
        /// <returns>A list of all status types</returns>
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var statusTypes = await _statusTypeRepository.FindAll();

                if (statusTypes == null || statusTypes.Count == 0)
                {
                    _logger.LogInformation("No status types found");
                    return Ok(new List<StatusTypeDto>());
                }

                var statusTypeDtos = _mapper.Map<List<StatusTypeDto>>(statusTypes);

                _logger.LogInformation("Retrieved {StatusTypeCount} status types", statusTypeDtos.Count);
                return Ok(statusTypeDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all status types");
                return StatusCode(500, new { message = "An error occurred while retrieving status types." });
            }
        }

        /// <summary>
        /// Get a status type by application acronym, status name, and entity type name.
        /// </summary>
        /// <param name="request">The request containing applicationAcronym, statusName, and entityTypeName</param>
        /// <returns>The requested status type</returns>
        [HttpPost("by-name")]
        public async Task<IActionResult> GetByName([FromBody] GetStatusTypeByNameRequest request)
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
                    _logger.LogWarning("Invalid applicationAcronym: empty or null");
                    return BadRequest(new { message = "applicationAcronym cannot be empty." });
                }

                if (string.IsNullOrWhiteSpace(request.StatusName))
                {
                    _logger.LogWarning("Invalid statusName: empty or null");
                    return BadRequest(new { message = "statusName cannot be empty." });
                }

                if (string.IsNullOrWhiteSpace(request.EntityTypeName))
                {
                    _logger.LogWarning("Invalid entityTypeName: empty or null");
                    return BadRequest(new { message = "entityTypeName cannot be empty." });
                }

                // Get application by acronym
                var application = await _identityService.GetApplicationByAcronymAsync(request.ApplicationAcronym);
                if (application == null)
                {
                    _logger.LogWarning("Application not found - ApplicationAcronym: {ApplicationAcronym}", request.ApplicationAcronym);
                    return NotFound(new { message = $"Application with acronym '{request.ApplicationAcronym}' not found." });
                }

                // Get entity type by name and application ID
                var entityType = await _entityTypeRepository.FindByNameAndApplicationId(request.EntityTypeName, application.Id);
                if (entityType == null)
                {
                    _logger.LogWarning("Entity type not found - EntityTypeName: {EntityTypeName}, ApplicationAcronym: {ApplicationAcronym}",
                        request.EntityTypeName, request.ApplicationAcronym);
                    return NotFound(new { message = $"Entity type '{request.EntityTypeName}' not found for application '{request.ApplicationAcronym}'." });
                }

                // Get status type by name and entity type ID
                var statusType = await _statusTypeRepository.FindByName(request.StatusName, entityType.Id);

                if (statusType == null)
                {
                    _logger.LogInformation("Status type not found - Name: {StatusName}, EntityTypeId: {EntityTypeId}, ApplicationAcronym: {ApplicationAcronym}",
                        request.StatusName, entityType.Id, request.ApplicationAcronym);
                    return NotFound(new { message = $"Status type '{request.StatusName}' not found for entity type '{request.EntityTypeName}' in application '{request.ApplicationAcronym}'." });
                }

                var statusTypeDto = _mapper.Map<StatusTypeDto>(statusType);

                _logger.LogInformation("Retrieved status type - Name: {StatusName}, EntityTypeId: {EntityTypeId}, ApplicationAcronym: {ApplicationAcronym}",
                    request.StatusName, entityType.Id, request.ApplicationAcronym);
                return Ok(statusTypeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving status type - ApplicationAcronym: {ApplicationAcronym}, StatusName: {StatusName}, EntityTypeName: {EntityTypeName}",
                    request?.ApplicationAcronym, request?.StatusName, request?.EntityTypeName);
                return StatusCode(500, new { message = "An error occurred while retrieving the status type." });
            }
        }

        /// <summary>
        /// Get a status type by ID.
        /// </summary>
        /// <param name="statusId">The status type ID from query string</param>
        /// <returns>The requested status type</returns>
        [HttpGet("by-id")]
        public async Task<IActionResult> GetById([FromQuery] int statusId)
        {
            try
            {
                // Validate input parameters
                if (statusId <= 0)
                {
                    _logger.LogWarning("Invalid statusId: {StatusId}", statusId);
                    return BadRequest(new { message = "statusId must be greater than 0." });
                }

                // Get status type by ID
                var statusType = await _statusTypeRepository.FindById(statusId);

                if (statusType == null)
                {
                    _logger.LogInformation("Status type not found - StatusId: {StatusId}", statusId);
                    return NotFound(new { message = $"Status type with ID {statusId} not found." });
                }

                var statusTypeDto = _mapper.Map<StatusTypeDto>(statusType);

                _logger.LogInformation("Retrieved status type by ID - StatusId: {StatusId}", statusId);
                return Ok(statusTypeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving status type by ID - StatusId: {StatusId}", statusId);
                return StatusCode(500, new { message = "An error occurred while retrieving the status type." });
            }
        }
    }

    /// <summary>
    /// Request model for getting a status type by name
    /// </summary>
    public class GetStatusTypeByNameRequest
    {
        /// <summary>
        /// The application acronym (e.g., "VMS" for Vault Management System)
        /// </summary>
        public string ApplicationAcronym { get; set; }

        /// <summary>
        /// The status type name
        /// </summary>
        public string StatusName { get; set; }

        /// <summary>
        /// The entity type name
        /// </summary>
        public string EntityTypeName { get; set; }
    }
}
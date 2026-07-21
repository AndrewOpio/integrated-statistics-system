using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using WorkflowService.Client.Dto;

namespace WorkflowService.Client
{
    public class WorkflowServiceClient(HttpClient _httpClient, ILogger<WorkflowServiceClient> _logger) : IWorkflowServiceClient
    {
        /// <summary>
        /// Get all status types from the WorkflowService.
        /// </summary>
        /// <returns>A list of all status types</returns>
        public async Task<List<StatusTypeDto>> FindAllStatuses()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/statustype/all");

                if (response.IsSuccessStatusCode)
                {
                    var statusTypes = await response.Content.ReadFromJsonAsync<List<StatusTypeDto>>();
                    return statusTypes ?? new List<StatusTypeDto>();
                }

                _logger.LogError("Failed to retrieve status types. Status: {StatusCode}", response.StatusCode);
                return new List<StatusTypeDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all status types");
                return new List<StatusTypeDto>();
            }
        }

        /// <summary>
        /// Get a specific status type by application acronym, status name, and entity type name.
        /// </summary>
        /// <param name="requestData">The request containing applicationAcronym, statusName, and entityTypeName</param>
        /// <returns>The requested status type</returns>
        public async Task<StatusTypeDto> FindStatusByName(GetStatusByNameRequestDto requestData)
        {
            try
            {
                // Validate input parameters
                if (requestData == null)
                {
                    _logger.LogWarning("Request data is null");
                    return new StatusTypeDto();
                }

                if (string.IsNullOrWhiteSpace(requestData.ApplicationAcronym))
                {
                    _logger.LogWarning("Invalid applicationAcronym: empty or null");
                    return new StatusTypeDto();
                }

                if (string.IsNullOrWhiteSpace(requestData.StatusName))
                {
                    _logger.LogWarning("Invalid statusName: empty or null");
                    return new StatusTypeDto();
                }

                if (string.IsNullOrWhiteSpace(requestData.EntityTypeName))
                {
                    _logger.LogWarning("Invalid entityTypeName: empty or null");
                    return new StatusTypeDto();
                }

                var response = await _httpClient.PostAsJsonAsync("/api/statustype/by-name", requestData);

                if (response.IsSuccessStatusCode)
                {
                    var statusType = await response.Content.ReadFromJsonAsync<StatusTypeDto>();
                    return statusType ?? new StatusTypeDto();
                }

                _logger.LogError("Failed to retrieve status type. Status: {StatusCode}", response.StatusCode);
                return new StatusTypeDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status type for application {ApplicationAcronym}, status name {StatusName} and entity type {EntityTypeName}",
                    requestData?.ApplicationAcronym, requestData?.StatusName, requestData?.EntityTypeName);
                return new StatusTypeDto();
            }
        }

        /// <summary>
        /// Get a specific transition by entity type name, from status ID, and action name.
        /// </summary>
        /// <param name="requestData">The request containing entityTypeName, fromStatusId, and actionName</param>
        /// <returns>The requested workflow transition</returns>
        public async Task<WorkflowTransitionDto> FindByTransitionKey(GetTransitionByKeyRequestDto requestData)
        {
            try
            {
                // Validate input parameters
                if (requestData == null)
                {
                    _logger.LogWarning("Request data is null");
                    return new WorkflowTransitionDto();
                }

                if (string.IsNullOrWhiteSpace(requestData.EntityTypeName))
                {
                    _logger.LogWarning("Invalid entityTypeName: empty or null");
                    return new WorkflowTransitionDto();
                }

                if (requestData.FromStatusId <= 0)
                {
                    _logger.LogWarning("Invalid fromStatusId: {FromStatusId}", requestData.FromStatusId);
                    return new WorkflowTransitionDto();
                }

                if (string.IsNullOrWhiteSpace(requestData.ActionName))
                {
                    _logger.LogWarning("Invalid actionName: empty or null");
                    return new WorkflowTransitionDto();
                }

                var response = await _httpClient.PostAsJsonAsync("/api/transition/get-transition", requestData);

                if (response.IsSuccessStatusCode)
                {
                    var transition = await response.Content.ReadFromJsonAsync<WorkflowTransitionDto>();
                    return transition ?? new WorkflowTransitionDto();
                }

                _logger.LogError("Failed to retrieve transition. Status: {StatusCode}", response.StatusCode);
                return new WorkflowTransitionDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transition for entityTypeName {EntityTypeName}, fromStatusId {FromStatusId} and actionName {ActionName}",
                    requestData?.EntityTypeName, requestData?.FromStatusId, requestData?.ActionName);
                return new WorkflowTransitionDto();
            }
        }

        /// <summary>
        /// Get available transitions for a given entity type, current status, and application role.
        /// </summary>
        /// <param name="requestData">The request containing applicationAcronym, entityTypeName, currentStatusId, and appRole</param>
        /// <returns>A list of available workflow transitions</returns>
        public async Task<List<WorkflowTransitionDto>> GetAvailableTransitions(GetAvailableTransitionsRequestDto requestData)
        {
            try
            {
                // Validate input parameters
                if (requestData == null)
                {
                    _logger.LogWarning("Request data is null");
                    return new List<WorkflowTransitionDto>();
                }

                if (string.IsNullOrWhiteSpace(requestData.EntityTypeName))
                {
                    _logger.LogWarning("Invalid entityTypeName: empty or null");
                    return new List<WorkflowTransitionDto>();
                }

                if (requestData.CurrentStatusId <= 0)
                {
                    _logger.LogWarning("Invalid currentStatusId: {CurrentStatusId}", requestData.CurrentStatusId);
                    return new List<WorkflowTransitionDto>();
                }

                if (requestData.AppRole <= 0)
                {
                    _logger.LogWarning("Invalid appRole: {AppRole}", requestData.AppRole);
                    return new List<WorkflowTransitionDto>();
                }

                var response = await _httpClient.PostAsJsonAsync("/api/transition/available-transitions", requestData);

                if (response.IsSuccessStatusCode)
                {
                    var transitions = await response.Content.ReadFromJsonAsync<ICollection<WorkflowTransitionDto>>();
                    return transitions?.ToList() ?? new List<WorkflowTransitionDto>();
                }

                _logger.LogError("Failed to retrieve transitions. Status: {StatusCode}", response.StatusCode);
                return new List<WorkflowTransitionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available transitions for {EntityTypeName} status {CurrentStatusId} and Role ID {AppRole}",
                    requestData?.EntityTypeName, requestData?.CurrentStatusId, requestData?.AppRole);
                return new List<WorkflowTransitionDto>();
            }
        }

        /// <summary>
        /// Get a specific status type by ID.
        /// </summary>
        /// <param name="statusId">The status type ID</param>
        /// <returns>The requested status type</returns>
        public async Task<StatusTypeDto> FindStatusById(int statusId)
        {
            _logger.LogInformation("Status code: {Status}", statusId);
            try
            {
                // Validate input parameters
                if (statusId <= 0)
                {
                    _logger.LogWarning("Invalid statusId: {StatusId}", statusId);
                    return new StatusTypeDto();
                }

                _logger.LogInformation("Status code: {Status} before response: ", statusId);
                var response = await _httpClient.GetAsync($"/api/statustype/by-id?statusId={statusId}");

                if (response.IsSuccessStatusCode)
                {
                    var statusType = await response.Content.ReadFromJsonAsync<StatusTypeDto>();
                    return statusType ?? new StatusTypeDto();
                }

                _logger.LogError("Failed to retrieve status type by ID. Status: {StatusCode}", response.StatusCode);
                return new StatusTypeDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status type by ID for statusId {StatusId}", statusId);
                return new StatusTypeDto();
            }
        }

        public async Task<List<EntityActionableStatusesDto>> GetFromStatusesByRoles(string appAcronym, IEnumerable<int> roleIds)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/transition/from-statuses-by-roles",
                    new { appAcronym, roleIds });

                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<List<EntityActionableStatusesDto>>() ?? [];

                _logger.LogError("GetFromStatusesByRoles failed: {Status}", response.StatusCode);
                return [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFromStatusesByRoles for {AppAcronym}", appAcronym);
                return [];
            }
        }

        public async Task<List<WorkflowTransitionDto>> GetAvailableTransitionsAsync(string appAcronym, string entityType, int statusId, int userRole)
        {
            try
            {
                var requestData = new GetAvailableTransitionsRequestDto
                {
                    ApplicationAcronym = appAcronym,
                    EntityTypeName = entityType,
                    CurrentStatusId = statusId,
                    AppRole = userRole,
                };

                return await this.GetAvailableTransitions(requestData)
                       ?? new List<WorkflowTransitionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching available transitions for {EntityType} status {StatusId}",
                    entityType, statusId);
                return new List<WorkflowTransitionDto>();
            }
        }
    }
}

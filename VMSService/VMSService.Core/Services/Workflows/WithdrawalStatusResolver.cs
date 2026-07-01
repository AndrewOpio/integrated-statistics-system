using System.Collections.Concurrent;
using VMSService.Core.Static;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Services.Workflows
{
    public class WithdrawalStatusResolver(IWorkflowServiceClient workflowServiceClient) : IWithdrawalStatusResolver
    {
        private static readonly ConcurrentDictionary<string, int> _cache = new();

        public async Task<int> GetStatusIdAsync(string statusName)
        {
            if (_cache.TryGetValue(statusName, out var cachedId))
                return cachedId;

            var status = await workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = statusName,
                EntityTypeName = StatusConstants.Entity.WithdrawalRequest
            });

            _cache.TryAdd(statusName, status.Id);
            return status.Id;
        }
    }
}

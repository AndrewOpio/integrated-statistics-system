using IdentityService.Dto;

namespace WorkflowService.Core.Services.Identity
{
    public interface IIdentityService
    {
        Task<List<ApplicationDto>> GetApplicationsAsync();
        Task<ApplicationDto> GetApplicationByIdAsync(int applicationId);
        Task<List<ApplicationRoleDto>> GetRolesForApplicationAsync(int applicationId);
        Task<ApplicationRoleDto> GetRoleByIdAsync(int id);
    }
}
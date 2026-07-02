using NotificationService.Client.Dtos;
using VMSService.Core.Services.Identity.IdentityEntities;

namespace VMSService.Core.Services.Identity
{
    public interface IIdentityService
    {
        Task<ICollection<NonStaffUser>> GetNonStaffUsers();
        Task<ICollection<RecipientDto>> GetRecipients(string? notificationEmails, string? notificationRoleIds);
        Task<UserDto> GetUserByUsername(string username);
        Task<ICollection<UserDto>> GetUsersByRoleName(string roleName);
    }
}
using NotificationService.Client.Dtos;
using VMSService.Core.Services.Identity.IdentityEntities;

namespace VMSService.Core.Services.Identity
{
    public interface IIdentityService
    {
        Task<ICollection<NonStaffUser>> GetNonStaffUsers();
        Task<ICollection<RecipientDto>> GetRecipients(string? notificationEmails, string? notificationRoleIds);
    }
}
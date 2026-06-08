using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.Attachments
{
    public interface IAttachmentRepository
    {
        Task<bool> Create(Attachment attachment);
        Task<Attachment?> FindById(int id);
        Task<ICollection<Attachment>> FindByEntityId(string entityId);
        Task<bool> Update(Attachment attachment);
        Task<bool> Save();
    }
}
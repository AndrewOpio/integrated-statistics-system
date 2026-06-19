using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.Attachments
{
    public class AttachmentRepository : IAttachmentRepository
    {
        private readonly VMSServiceDbContext _context;

        public AttachmentRepository(VMSServiceDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Create(Attachment attachment)
        {
            await _context.Attachments.AddAsync(attachment);
            return await Save();
        }

        public async Task<Attachment?> FindById(int id)
        {
            return await _context.Attachments
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        }

        public async Task<ICollection<Attachment>> FindByEntityId(string entityId)
        {
            return await _context.Attachments
                .Where(a => a.EntityId == entityId && !a.IsDeleted)
                .ToListAsync();
        }

        public async Task<bool> Update(Attachment attachment)
        {
            _context.Attachments.Update(attachment);
            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}

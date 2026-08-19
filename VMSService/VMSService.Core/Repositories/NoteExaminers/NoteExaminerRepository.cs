using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.NoteExaminers
{
    public class NoteExaminerRepository(VMSServiceDbContext _db) : INoteExaminerRepository
    {
        public async Task<bool> Create(NoteExaminer model)
        {
            _db.NoteExaminers.Add(model);
            return await Save();
        }

        public async Task<ICollection<NoteExaminer>> FindAll()
        {
            return await _db.NoteExaminers
                .Where(x => !x.IsDeleted)
                .ToListAsync();
        }

        public async Task<NoteExaminer> FindById(int id)
        {
            return await _db.NoteExaminers
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.NoteExaminers
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(NoteExaminer model)
        {
            _db.NoteExaminers.Update(model);
            return await Save();
        }
    }
}

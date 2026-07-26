using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class NoteExaminer : AuditableEntity
    {
        public int Id { get; set; }
        public string FirstName { get; set; } 
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public int BranchId { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

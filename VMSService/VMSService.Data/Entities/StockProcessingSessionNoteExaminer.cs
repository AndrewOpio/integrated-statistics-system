using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class StockProcessingSessionNoteExaminer : AuditableEntity
    {
        public int Id { get; set; }
        public int StockProcessingSessionId { get; set; }
        public virtual StockProcessingSession? StockProcessingSession { get; set; }
        public int NoteExaminerId { get; set; }
        public virtual NoteExaminer? NoteExaminer { get; set; }
        public int NumberOfBundles { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}

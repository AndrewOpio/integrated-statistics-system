namespace VMSService.Core.Models.StockProcessingSessions
{
    public class NoteExaminerEntryViewModel
    {
        public int NoteExaminerId { get; set; }
        public int NumberOfBundles { get; set; }
    }

    public class SessionNoteExaminerViewModel
    {
        public int Id { get; set; }
        public int NoteExaminerId { get; set; }
        public string? NoteExaminerName { get; set; }
        public int NumberOfBundles { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

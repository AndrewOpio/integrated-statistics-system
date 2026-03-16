using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class AgentDetail : AuditableEntity
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public string AgentNames { get; set; } //Exactly as they appear in their work-issued identity cards
        public string NIN { get; set; } //National Identification Number
        public string? PhoneNumber { get; set; }
        public string PassportPhoto { get; set; }
        public string WorkIdScan { get; set; }
        public string SignatureScan { get; set; }
        public bool IsDeleted { get; set; }
    }
}
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class ProcessingBoard : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int BranchId { get; set; }
        public int? BPSMachineId { get; set; }
        public BPSMachine? BPSMachine { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}

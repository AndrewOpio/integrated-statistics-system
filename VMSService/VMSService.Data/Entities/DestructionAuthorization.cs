using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class DestructionAuthorization : AuditableEntity
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = null!;
        public int BranchId { get; set; }
        public DateTime Date { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = "Draft";

        public ICollection<DestructionAuthorizationDenomination>? DestructionAuthorizationDenominations { get; set; }
        public ICollection<DestructionAuthorizationStatusHistory>? DestructionAuthorizationStatusHistory { get; set; }
        public ICollection<DestructionRequest>? DestructionRequests { get; set; }
    }
}

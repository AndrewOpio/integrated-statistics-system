using System.ComponentModel.DataAnnotations;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class DestructionRequest : AuditableEntity
    {
        public int Id { get; set; }
        public int? DestructionAuthorizationId { get; set; }
        public int DestructionTypeId { get; set; }
        public int? DestructionLocationId { get; set; }
        public string ReferenceNumber { get; set; }
        public DateTime Date { get; set; }
        public string VerificationOfficerUserId { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = "Draft";
        public int BranchId { get; set; }

        public virtual DestructionAuthorization? DestructionAuthorization { get; set; }
        public virtual DestructionType DestructionType { get; set; } = null!;
        public virtual DestructionLocation? DestructionLocation { get; set; }

        public ICollection<DestructionRequestDenomination>? DestructionRequestDenominations { get; set; }
        public ICollection<DestructionRequestMember>? DestructionRequestMembers { get; set; }
        public ICollection<DestructionRequestStatusHistory>? DestructionRequestStatusHistory { get; set; }
    }
}

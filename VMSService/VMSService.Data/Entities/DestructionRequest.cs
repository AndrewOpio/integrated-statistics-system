using System.ComponentModel.DataAnnotations;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class DestructionRequest : AuditableEntity
    {
        public int Id { get; set; }
        public string DestructionType { get; set; }
        public string ReferenceNumber { get; set; }
        public DateTime Date { get; set; }
        public string VerificationOfficer { get; set; }

        public ICollection<DestructionRequestDenomination>? DestructionRequestDenominations { get; set; }
        public ICollection<DestructionRequestWitness>? DestructionRequestWitnesses { get; set; }
        public ICollection<DestructionRequestMember>? DestructionRequestMembers { get; set; }
    }
}

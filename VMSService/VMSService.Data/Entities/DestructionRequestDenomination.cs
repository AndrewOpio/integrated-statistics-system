using System.ComponentModel.DataAnnotations;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class DestructionRequestDenomination : AuditableEntity
    {
        public int Id { get; set; }
        public int DestructionRequestId { get; set; }
        public int DenominationId { get; set; }
        public string? NumberOfBags { get; set; }
        public string Amount { get; set; }

        public virtual DestructionRequest DestructionRequest { get; set; } = null!;
    } 
}

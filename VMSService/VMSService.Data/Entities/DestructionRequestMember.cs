using System.ComponentModel.DataAnnotations;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class DestructionRequestMember : AuditableEntity
    {
        public int Id { get; set; }
        public int DestructionRequestId { get; set; }
        public string IDNumber { get; set; }
        public string FullName { get; set; }

        public virtual DestructionRequest DestructionRequest { get; set; } = null!;
    } 
}

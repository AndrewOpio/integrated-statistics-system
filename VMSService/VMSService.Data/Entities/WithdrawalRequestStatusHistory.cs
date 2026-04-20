using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class WithdrawalRequestStatusHistory : AuditableEntity
    {

        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WithdrawalRequestId { get; set; }
        public WithdrawalRequest WithdrawalRequest { get; set; } = null!;
        public int OldStatusId { get; set; }
        public string OldStatusName { get; set; }
        public int NewStatusId { get; set; }
        public string NewStatusName { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}

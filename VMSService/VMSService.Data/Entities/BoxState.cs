using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class BoxState : AuditableEntity
    {
        public int Id { get; set; }
        public long PackingListDetailsId { get; set; }
        public PackingListDetails PackingListDetails { get; set; } = null!;

        public string CaseNumber { get; set; } = null!;
        public int DenominationId { get; set; }
        public int BranchIsAtId { get; set; } = 8;

        public long CurrentSerialCursor { get; set; }
        public int RemainingBundles { get; set; }
        public bool IsAvailable { get; set; } = true;

        public DateTime? LastDispatchedDate { get; set; }
        public Guid? LastDispatchedRequestId { get; set; }
        public bool IsDeleted { get; set; }

        public string BoxStatusLabel => RemainingBundles switch
        {
            50 => "Available",
            0 => "Exhausted",
            _ => "Partial"
        };
    }
}

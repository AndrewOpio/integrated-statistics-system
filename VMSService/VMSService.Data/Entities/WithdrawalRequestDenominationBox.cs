using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class WithdrawalRequestDenominationBox : AuditableEntity
    {
        public int Id { get; set; }
        public Guid WithdrawalRequestDenominationId { get; set; }
        public WithdrawalRequestDenomination WithdrawalRequestDenomination { get; set; } = null!;
        public string BoxNo { get; set; } = null!;
    }
}

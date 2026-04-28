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
        public string CaseNumber { get; set; } = null!;
        public int BundlesTaken { get; set; }
        public long SerialStartTaken { get; set; }
        public long SerialEndTaken { get; set; }
        public string SerialPrefix { get; set; } = "";
        public bool IsFull => BundlesTaken == 50;
        public bool IsPartial => BundlesTaken > 0 && BundlesTaken < 50;
    }
}

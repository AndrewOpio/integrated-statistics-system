using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMSService.Core.Models.CbRequests
{
    public class RecentRequestRowViewModel
    {
        public Guid Id { get; set; }
        public string TransactionRefNumber { get; set; } = "Not Generated";
        public string Type => TransactionRefNumber switch
        {
            var s when s.Contains('W') => "Withdrawal",
            var s when s.Contains('D') => "Deposit",
            _ => "—"
        };
        public decimal Amount { get; set; }
        //public string Status { get; set; } = string.Empty;
        public string StatusTypeName { get; set; } = string.Empty;
        public DateOnly CollectionDate { get; set; }
        public DateTime CreationDate { get; set; }
        public bool RequiresSpecialApproval { get; set; }
        public string RtgsCode { get; set; } = "—";
        public string CbName { get; set; } = "—";
        public string BouBranchCode { get; set; } = "—";
        public bool ReturnRtgsPending { get; set; }
    }
}

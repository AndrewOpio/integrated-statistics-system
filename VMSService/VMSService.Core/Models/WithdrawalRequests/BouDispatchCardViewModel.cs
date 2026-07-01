using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMSService.Core.Models.WithdrawalRequests
{
    public class BouDispatchCardViewModel
    {
        public Guid Id { get; set; }
        public int DenominationId { get; set; }
        public decimal Denomination { get; set; }
        public int StartBox { get; set; }
        public int EndBox { get; set; }
        public bool IsFull { get; set; }
        public int Bundles { get; set; }
        public long SerialStart { get; set; }
        public long SerialEnd { get; set; }
        public decimal Amount { get; set; }
        public string SerialPrefix { get; set; } = "";
    }
}

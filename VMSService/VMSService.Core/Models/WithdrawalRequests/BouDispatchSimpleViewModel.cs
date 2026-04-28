using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMSService.Core.Models.WithdrawalRequests
{
    public class BouDispatchSimpleViewModel
    {
        public Guid Id { get; set; }
        public int DenominationId { get; set; }
        public decimal Denomination { get; set; }
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
    }
}

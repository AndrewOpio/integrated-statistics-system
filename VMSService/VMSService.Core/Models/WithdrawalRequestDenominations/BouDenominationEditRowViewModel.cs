using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Core.Models.CbRequests;
using static VMSService.Core.Static.WithdrawStatuses;

namespace VMSService.Core.Models.WithdrawalRequestDenominations
{
    public class BouDenominationEditRowViewModel
    {
        public Guid? Id { get; set; }    
        public int DenominationId { get; set; }
        public decimal DenominationValue { get; set; }
        public int DenominationTypeId { get; set; }
        public string BundleType { get; set; } = BundleTypes.NotApplicable;
        public int? Bundles { get; set; }
        public int? Boxes { get; set; }
        public decimal Amount { get; set; }
        public List<string> BoxNumbers { get; set; } = new();
    }
}

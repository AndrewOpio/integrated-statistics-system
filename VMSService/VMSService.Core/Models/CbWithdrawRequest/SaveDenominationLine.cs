using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMSService.Core.Models.CbWithdrawRequest
{
    public class SaveDenominationLine
    {
        public int DenominationId { get; set; }
        public int DenominationValue { get; set; }
        public int DenominationTypeId { get; set; }
        public int? Bundles { get; set; }             // for notes
        public int? Boxes { get; set; }               // for coins
        public decimal Amount { get; set; }
    }
}

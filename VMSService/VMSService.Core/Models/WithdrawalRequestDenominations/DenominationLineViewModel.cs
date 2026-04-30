using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMSService.Core.Models.WithdrawalRequestDenominations
{
    public class DenominationLineViewModel
    {
        public string DenominationLabel { get; init; } = string.Empty;
        public int? Bundles { get; init; }
        public int? Boxes { get; init; }
        public decimal Amount { get; init; }
    }
}

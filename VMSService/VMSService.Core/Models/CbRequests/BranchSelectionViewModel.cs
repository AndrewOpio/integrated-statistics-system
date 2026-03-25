using ConfigurationsService.Client;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMSService.Core.Models.CbRequests
{
    public class BranchSelectionViewModel
    {
        public IEnumerable<BranchDto> BouBranches { get; init; } = [];
        public IEnumerable<CommercialBankBranchDto> CbBranches { get; init; } = [];
        //public IReadOnlyList<CommercialBankBranchDto> CbBranches { get; init; } = [];
    }
}

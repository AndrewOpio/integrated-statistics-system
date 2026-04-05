using ConfigurationsService.Client;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMSService.Core.Models.CbRequests
{
    public class FromToSelectionViewModel
    {
        public IEnumerable<BranchDto> BouBranches { get; init; } = [];
        public CommercialBankDto Cb { get; init; } = null!;
        public int SelectedCbBranchId { get; set; }
        public int SelectedbankBranchId { get; set; }
    }
}

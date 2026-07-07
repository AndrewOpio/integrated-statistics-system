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
        //public int SelectedCbBranchId { get; set; }
        public int SelectedBouBranchId { get; set; }
        public DateOnly CollectionDate { get; set; }
        public DateTime? SubmissionDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsEdit { get; set; } = false;
        public bool RequiresSpecialApproval { get; set; } = false;
        public string? SpecialApprovalReason { get; set; }
    }
}

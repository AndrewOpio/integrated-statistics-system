using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMSService.Core.Models.CbWithdrawRequest
{
    public class SaveWithdrawalRequestViewModel
    {
        public Guid? Id { get; set; } 
        public int CbId { get; set; }
        // Branch
        //public int CbBranchId { get; set; }
        public string CbCode { get; set; }
        public int BouBranchId { get; set; }
        public string BouBranchCode{ get; set; }

        public DateOnly CollectionDate { get; set; }
        public DateOnly? RtgsDate { get; set; }
        public string? RtgsCode { get; set; }
        public bool RequiresSpecialApproval { get; set; }
        public string? SpecialApprovalReason { get; set; }
        public List<int> AgentIds { get; set; } = [];

        // Denominations
        public List<SaveDenominationLine> Denominations { get; set; } = [];
        public bool IsLumpSum { get; set; } = false;
        public decimal? LumpSumAmount { get; set; }

    }
}

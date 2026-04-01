using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMSService.Core.Models.CbWithdrawRequest
{
    public class SaveWithdrawalRequestViewModel
    {
        public Guid? Id { get; set; }                          // null = new, set = edit draft/revision
        public int CbId { get; set; }
        // Branch
        //public int CbBranchId { get; set; }
        public string CbCode { get; set; }
        public int BouBranchId { get; set; }
        public string BouBranchCode{ get; set; }

        // Dates
        public DateOnly CollectionDate { get; set; }
        public DateOnly? RtgsDate { get; set; }
        public string? RtgsCode { get; set; }

        // Collectors — exactly 2
        public List<int> AgentIds { get; set; } = [];

        // Denominations
        public List<SaveDenominationLine> Denominations { get; set; } = [];

    }
}

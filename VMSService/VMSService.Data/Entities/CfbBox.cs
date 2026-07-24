using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class CfbBox : AuditableEntity
    {
        public int Id { get; set; }

        public int BouBranchId { get; set; }
        public string BouBranchCode { get; set; } = null!;

        /// <summary>"Notes" or "Coins" — see CfbConstants.BoxTypes. One of each per branch.</summary>
        [MaxLength(10)]
        public string BoxType { get; set; } = null!;

        [MaxLength(120)]
        public string Name { get; set; }

        public ICollection<CfbCashHolding> Holdings { get; set; } = new List<CfbCashHolding>();
    }
}

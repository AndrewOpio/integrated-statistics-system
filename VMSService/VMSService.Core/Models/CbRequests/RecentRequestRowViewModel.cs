using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMSService.Core.Models.CbRequests
{
    public class RecentRequestRowViewModel
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = "Withdrawal";
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public string Purpose { get; set; } = "—";
    }
}

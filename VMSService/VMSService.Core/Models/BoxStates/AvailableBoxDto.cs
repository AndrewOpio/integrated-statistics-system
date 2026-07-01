using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMSService.Core.Models.BoxStates
{
    public class AvailableBoxDto
    {
        public string CaseNumber { get; set; } = null!;
        public string SerialStart { get; set; } = null!;
        public string SerialEnd { get; set; } = null!;
        public int RemainingBundles { get; set; }
    }
}

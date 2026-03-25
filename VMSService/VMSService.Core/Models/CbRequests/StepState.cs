using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMSService.Core.Models.CbRequests
{
    public enum StepState
    {
        InProcess,    // not yet reached
        Active,  // current step
        Done     // completed
    }

}

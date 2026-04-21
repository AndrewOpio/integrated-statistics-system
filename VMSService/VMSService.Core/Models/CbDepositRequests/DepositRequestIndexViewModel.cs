using VMSService.Core.Authorization;
using VMSService.Core.Models.CbRequests;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.CbDepositRequests;

public class DepositRequestIndexViewModel
{
    public IEnumerable<RecentRequestRowViewModel> Deposits { get; set; } = [];
    public bool IsBouUser { get; set; }
}
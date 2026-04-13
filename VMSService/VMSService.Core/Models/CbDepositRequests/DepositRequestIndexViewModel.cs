using VMSService.Core.Authorization;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.CbDepositRequests;

public class DepositRequestIndexViewModel
{
    public IEnumerable<DepositRequestRowViewModel> Deposits { get; set; } = [];
    public bool IsBouUser { get; set; }
}

public class DepositRequestRowViewModel
{
    public Guid Id { get; set; }
    public string TransactionRefNumber { get; set; } = string.Empty;
    public string CbName { get; set; } = string.Empty;
    public string CbCode { get; set; } = string.Empty;
    public string BouBranchName { get; set; } = string.Empty;
    public DateOnly DepositDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int StatusTypeId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}
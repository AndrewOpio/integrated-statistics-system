namespace VMSService.Core.Models.CbDepositRequest
{
    public class SaveDepositRequestViewModel
    {
        public Guid? Id { get; set; }                          // null = new, set = edit draft/revision
        public int CbId { get; set; }
        public string CbCode { get; set; } = string.Empty;
        public int BouBranchId { get; set; }
        public string BouBranchCode { get; set; } = string.Empty;

        // Dates
        public DateOnly DepositDate { get; set; }
        public DateOnly? RtgsDate { get; set; }
        public string? RtgsCode { get; set; }

        // Collectors — exactly 2
        public List<int> AgentIds { get; set; } = [];

        // Denominations — each line includes SubCategoryId (1=Fit, 2=Unfit, 3=N/A)
        public List<SaveDepositDenominationLine> Denominations { get; set; } = [];
    }
}
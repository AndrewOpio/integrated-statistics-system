namespace VMSService.Core.Models.CfbTopupRequests
{
    public class TopupCreateViewModel
    {
        /// <summary>Null when creating a new request; set when editing an existing Draft/Returned one.</summary>
        public int? Id { get; init; }
        public int BouBranchId { get; init; }
        public string BouBranchName { get; init; } = string.Empty;
        public List<TopupBoxSectionViewModel> Boxes { get; init; } = [];
    }
}

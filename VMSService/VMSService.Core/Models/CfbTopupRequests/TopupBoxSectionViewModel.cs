namespace VMSService.Core.Models.CfbTopupRequests
{
    public class TopupBoxSectionViewModel
    {
        public int BoxId { get; init; }
        public string BoxName { get; init; } = string.Empty;
        public string BoxType { get; init; } = string.Empty;
        public List<TopupCreateLineViewModel> Lines { get; init; } = [];
    }
}

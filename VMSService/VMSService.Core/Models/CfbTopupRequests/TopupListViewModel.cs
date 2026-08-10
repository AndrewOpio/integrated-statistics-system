namespace VMSService.Core.Models.CfbTopupRequests
{
    public class TopupListViewModel
    {
        public ICollection<TopupListRowViewModel> Rows { get; init; } = [];
        public bool IsCfbSupervisor { get; init; }
    }
}

using VMSService.Core.Models.Bags;

namespace VMSService.Core.Models.StockProcessingPlans
{
    public class StockProcessingPlanCreateViewModel
    {
        public DateTime ProcessingDate { get; set; }
        public int BranchId { get; set; }
        public string? BranchName { get; set; }
        public List<CountingRoomBoardsViewModel> CountingRooms { get; set; } = new();
        public List<BagViewModel> AvailableBags { get; set; } = new();
    }

    public class CountingRoomBoardsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<ProcessingBoardSimpleViewModel> Boards { get; set; } = new();
    }

    public class ProcessingBoardSimpleViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? MachineName { get; set; }
    }

    public class BoardBagAssignment
    {
        public int ProcessingBoardId { get; set; }
        public List<int> BagIds { get; set; } = new();
    }
}

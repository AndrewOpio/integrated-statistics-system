using VMSService.Core.Models.Bags;

namespace VMSService.Core.Models.StockProcessingPlans
{
    public class StockProcessingPlanDetailsViewModel
    {
        public int Id { get; set; }
        public DateTime ProcessingDate { get; set; }
        public int BranchId { get; set; }
        public string? BranchName { get; set; }
        public string CurrentStatusName { get; set; } = string.Empty;
        public List<CountingRoomWithBagsViewModel> CountingRooms { get; set; } = new();
        public List<ProcessingSessionViewModel> Sessions { get; set; } = new();
    }

    public class CountingRoomWithBagsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<ProcessingBoardWithBagsViewModel> Boards { get; set; } = new();
    }

    public class ProcessingBoardWithBagsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? MachineName { get; set; }
        public List<BagViewModel> Bags { get; set; } = new();
    }

    public class ProcessingSessionViewModel
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string? BoardName { get; set; }
        public string? MachineName { get; set; }
        public string? CommercialBankName { get; set; }
        public string CurrentStatusName { get; set; } = string.Empty;
        public List<string> BagReferenceNumbers { get; set; } = new();
    }
}

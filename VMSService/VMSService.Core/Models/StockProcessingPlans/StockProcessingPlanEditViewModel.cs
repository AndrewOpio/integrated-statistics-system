using VMSService.Core.Models.Bags;
using VMSService.Core.Services.Identity.IdentityEntities;

namespace VMSService.Core.Models.StockProcessingPlans
{
    public class StockProcessingPlanEditViewModel
    {
        public int Id { get; set; }
        public DateTime ProcessingDate { get; set; }
        public int BranchId { get; set; }
        public string? BranchName { get; set; }
        public string CurrentStatusName { get; set; } = string.Empty;
        public List<CountingRoomEditViewModel> CountingRooms { get; set; } = new();
        public List<BagViewModel> AvailableBags { get; set; } = new();
        public List<UserDto> BoardSupervisors { get; set; } = new();
    }

    public class CountingRoomEditViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? CurrentBoardSupervisorUserId { get; set; }
        public List<ProcessingBoardEditViewModel> Boards { get; set; } = new();
    }

    public class ProcessingBoardEditViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? MachineName { get; set; }
        public List<BagViewModel> AssignedBags { get; set; } = new();
    }
}

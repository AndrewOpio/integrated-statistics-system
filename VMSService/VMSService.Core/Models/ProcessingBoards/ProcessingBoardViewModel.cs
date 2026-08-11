using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using VMSService.Core.Models.CountingRooms;

namespace VMSService.Core.Models.ProcessingBoards
{
    public class ProcessingBoardViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int BranchId { get; set; }
        public string? BranchName { get; set; }

        public int? BPSMachineId { get; set; }
        public string? BPSMachineName { get; set; }

        [Required]
        public int CountingRoomId { get; set; }
        public string? CountingRoomName { get; set; }

        public List<SelectListItem> BranchList { get; set; } = new();
        public List<SelectListItem> BPSMachineList { get; set; } = new();
        public List<SelectListItem> CountingRoomList { get; set; } = new();

        // All counting rooms (with their BranchId) for client-side branch filtering on Create
        public List<CountingRoomViewModel> CountingRooms { get; set; } = new();
    }
}

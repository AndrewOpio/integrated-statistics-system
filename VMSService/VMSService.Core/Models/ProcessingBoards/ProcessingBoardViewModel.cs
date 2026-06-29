using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

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
    }
}

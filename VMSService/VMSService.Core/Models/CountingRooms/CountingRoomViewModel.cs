using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace VMSService.Core.Models.CountingRooms
{
    public class CountingRoomViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int BranchId { get; set; }
        public string? BranchName { get; set; }

        public List<SelectListItem> BranchList { get; set; } = new();
    }
}

using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace VMSService.Core.Models.BPSMachines
{
    public class BPSMachineViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Status is required.")]
        public int CurrentStatusId { get; set; }

        public string? CurrentStatusName { get; set; }

        public List<SelectListItem> StatusList { get; set; } = new();

        public static List<SelectListItem> GetStatusList() =>
        [
            new SelectListItem { Value = "1", Text = "Active" },
            new SelectListItem { Value = "2", Text = "Inactive" },
            new SelectListItem { Value = "3", Text = "Under Maintenance" },
        ];

        public static string GetStatusName(int statusId) => statusId switch
        {
            1 => "Active",
            2 => "Inactive",
            3 => "Under Maintenance",
            _ => "Unknown"
        };
    }
}

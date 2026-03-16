using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace VMSService.Core.Models.AgentDetails
{
    public class AgentDetailViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Branch is required")]
        [Display(Name = "Branch")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "Agent Names are required")]
        [Display(Name = "Agent Names")]
        public string AgentNames { get; set; } //Exactly as they appear in their work-issued identity cards

        [Required(ErrorMessage = "National Identification Number is required")]
        [Display(Name = "NIN")]
        public string NIN { get; set; } //National Identification Number

        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Passport Photo")]
        public string PassportPhoto { get; set; }

        [Display(Name = "Work ID Scan")]
        public string WorkIdScan { get; set; }

        [Display(Name = "Signature Scan")]
        public string SignatureScan { get; set; }

        // File upload properties
        [Display(Name = "Passport Photo")]
        public IFormFile? PassportPhotoFile { get; set; }

        [Display(Name = "Work ID Scan")]
        public IFormFile? WorkIdScanFile { get; set; }

        [Display(Name = "Signature Scan")]
        public IFormFile? SignatureScanFile { get; set; }

        // Navigation property
        public string? BranchName { get; set; }

        // Select lists
        public List<SelectListItem> BranchList { get; set; } = new List<SelectListItem>();
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using VMSService.Core.Model.AgentDetailStatusHistories;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.AgentDetails
{
    public class AgentDetailViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Commercial Bank")]
        public int CommercialBankId { get; set; }

        public int? CommercialBankBranchId { get; set; }

        [Required(ErrorMessage = "BOU Branch is required")]
        [Display(Name = "BOU Branch")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "Agent Names are required")]
        [Display(Name = "Agent Names")]
        public string AgentNames { get; set; } //Exactly as they appear in their work-issued identity cards

        [Required(ErrorMessage = "National Identification Number is required")]
        [Display(Name = "NIN")]
        public string NIN { get; set; } //National Identification Number

        [Display(Name = "Phone Number")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "Phone number must be exactly 9 digits (e.g. 712345678)")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Passport Photo")]
        public string? PassportPhoto { get; set; }

        [Display(Name = "Work ID Scan")]
        public string? WorkIdScan { get; set; }

        [Display(Name = "Signature Scan")]
        public string? SignatureScan { get; set; }

        [Display(Name = "Approval Attachment")]
        public string? ApprovalAttachment { get; set; }

        // File upload properties
        [Display(Name = "Passport Photo")]
        public IFormFile PassportPhotoFile { get; set; }

        [Display(Name = "Work ID Scan")]
        public IFormFile WorkIdScanFile { get; set; }

        [Display(Name = "Signature Scan")]
        public IFormFile SignatureScanFile { get; set; }

        [Display(Name = "Approval Attachment")]
        public IFormFile ApprovalAttachmentFile { get; set; }

        // Navigation properties
        public string? BranchName { get; set; }
        public string? CommercialBankName { get; set; }

        public int CurrentStatusId { get; set; }

        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; } = true;
        public string? CurrentStatusName { get; set; }

        // Select lists
        public List<SelectListItem> BranchList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> CommercialBankBranchList { get; set; } = new List<SelectListItem>();
        public ICollection<WorkflowTransitionDto> AvailableTransitions { get; set; } = new List<WorkflowTransitionDto>();
        public ICollection<AgentDetailStatusHistoryViewModel> StatusHistory { get; set; } = new List<AgentDetailStatusHistoryViewModel>();
    }
}

using System.ComponentModel.DataAnnotations;
using ISSService.Data.Entities;
using Microsoft.AspNetCore.Http;

namespace ISSService.Web.Models.TemplateVersions;

public class TemplateVersionViewModel
{
    public int Id { get; set; }

    [Required]
    public int DataCollectionTemplateId { get; set; }

    public int StatisticalProcessId { get; set; }

    public string StatisticalProcessCode { get; set; } = string.Empty;

    public string StatisticalProcessName { get; set; } = string.Empty;

    public string TemplateCode { get; set; } = string.Empty;

    public string TemplateName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Version number is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Version number must be at least 1.")]
    [Display(Name = "Version Number")]
    public int VersionNumber { get; set; } = 1;

    [Required(ErrorMessage = "Version label is required.")]
    [StringLength(
        100,
        ErrorMessage = "Version label cannot exceed 100 characters.")]
    [Display(Name = "Version Label")]
    public string VersionLabel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Effective-from date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Effective From")]
    public DateTime EffectiveFrom { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Effective To")]
    public DateTime? EffectiveTo { get; set; }

    [Required]
    public TemplateVersionStatus Status { get; set; }
        = TemplateVersionStatus.Draft;

    [StringLength(
        2000,
        ErrorMessage = "Change notes cannot exceed 2,000 characters.")]
    [Display(Name = "Change Notes")]
    public string? ChangeNotes { get; set; }

    [Display(Name = "Template Workbook")]
    public IFormFile? TemplateFile { get; set; }

    public string? FileName { get; set; }

    public string? FilePath { get; set; }

    public string? FileHash { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public string? ApprovedBy { get; set; }

    public int CycleAssignmentCount { get; set; }
}
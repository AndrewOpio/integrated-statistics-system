using System.ComponentModel.DataAnnotations;
using ISSService.Data.Entities;

namespace ISSService.Web.Models.DataCollectionTemplates;

public class DataCollectionTemplateViewModel
{
    public int Id { get; set; }

    [Required]
    public int StatisticalProcessId { get; set; }

    public string StatisticalProcessCode { get; set; } = string.Empty;

    public string StatisticalProcessName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Template code is required.")]
    [StringLength(
        50,
        ErrorMessage = "Template code cannot exceed 50 characters.")]
    [Display(Name = "Template Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Template name is required.")]
    [StringLength(
        200,
        ErrorMessage = "Template name cannot exceed 200 characters.")]
    [Display(Name = "Template Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(
        1000,
        ErrorMessage = "Description cannot exceed 1,000 characters.")]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Template Type")]
    public DataCollectionTemplateType TemplateType { get; set; }
        = DataCollectionTemplateType.ExcelTemplate;

    [Display(Name = "Respondent Specific")]
    public bool IsRespondentSpecific { get; set; }

    [Display(Name = "Supports Previous Period Data")]
    public bool SupportsPreviousPeriodData { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public int VersionCount { get; set; }

    public int ActiveVersionCount { get; set; }
}
using System.ComponentModel.DataAnnotations;
using ISSService.Data.Entities;

namespace ISSService.Web.Models.Respondents;

public class RespondentViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Respondent Code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    [Display(Name = "Respondent Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Respondent Type")]
    public RespondentType RespondentType { get; set; }
        = RespondentType.SACCO;

    [StringLength(100)]
    public string? Region { get; set; }

    [StringLength(100)]
    public string? District { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(200)]
    [Display(Name = "Primary Contact Name")]
    public string? PrimaryContactName { get; set; }

    [StringLength(250)]
    [EmailAddress]
    [Display(Name = "Primary Contact Email")]
    public string? PrimaryContactEmail { get; set; }

    [StringLength(50)]
    [Phone]
    [Display(Name = "Primary Contact Phone")]
    public string? PrimaryContactPhone { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public int RespondentCaseCount { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string? ModifiedBy { get; set; }
}
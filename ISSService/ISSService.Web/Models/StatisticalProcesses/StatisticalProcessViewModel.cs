using System.ComponentModel.DataAnnotations;
using ISSService.Data.Entities;

namespace ISSService.Web.Models.StatisticalProcesses;

public class StatisticalProcessViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Process code is required.")]
    [StringLength(
        50,
        ErrorMessage = "Process code cannot exceed 50 characters.")]
    [Display(Name = "Process Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Process name is required.")]
    [StringLength(
        200,
        ErrorMessage = "Process name cannot exceed 200 characters.")]
    [Display(Name = "Process Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(
        1000,
        ErrorMessage = "Description cannot exceed 1,000 characters.")]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Process Type")]
    public StatisticalProcessType ProcessType { get; set; }

    [Required]
    [Display(Name = "Collection Mode")]
    public CollectionMode CollectionMode { get; set; }

    [Required]
    [Display(Name = "Reporting Frequency")]
    public ReportingFrequency ReportingFrequency { get; set; }

    [StringLength(
        200,
        ErrorMessage = "Responsible unit cannot exceed 200 characters.")]
    [Display(Name = "Responsible Unit")]
    public string? ResponsibleUnit { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public int ReportingCycleCount { get; set; }

    public int TemplateCount { get; set; }
}
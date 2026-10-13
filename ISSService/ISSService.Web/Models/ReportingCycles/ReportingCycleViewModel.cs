using System.ComponentModel.DataAnnotations;
using ISSService.Data.Entities;

namespace ISSService.Web.Models.ReportingCycles;

public class ReportingCycleViewModel
{
    public int Id { get; set; }

    [Required]
    public int StatisticalProcessId { get; set; }

    public string StatisticalProcessCode { get; set; } = string.Empty;

    public string StatisticalProcessName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Cycle Code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [Display(Name = "Cycle Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Period Start Date")]
    public DateTime PeriodStartDate { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Period End Date")]
    public DateTime PeriodEndDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Collection Open Date")]
    public DateTime? CollectionOpenDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Submission Due Date")]
    public DateTime? SubmissionDueDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Collection Close Date")]
    public DateTime? CollectionCloseDate { get; set; }

    [Required]
    [Display(Name = "Cycle Status")]
    public ReportingCycleStatus Status { get; set; } = ReportingCycleStatus.Draft;

    [StringLength(1000)]
    public string? Notes { get; set; }

    public int RespondentCaseCount { get; set; }

    public int TemplateAssignmentCount { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string? ModifiedBy { get; set; }
}
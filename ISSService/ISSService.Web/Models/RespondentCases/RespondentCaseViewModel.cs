using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ISSService.Web.Models.RespondentCases;

public class RespondentCaseViewModel
{
    public int Id { get; set; }

    [Display(Name = "Reporting Cycle")]
    public int ReportingCycleId { get; set; }

    [Display(Name = "Respondent")]
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Please select a respondent.")]
    public int RespondentId { get; set; }

    [Display(Name = "Case Reference")]
    public string? CaseReference { get; set; }

    [Display(Name = "Statistical Process")]
    public string? StatisticalProcessName { get; set; }

    [Display(Name = "Reporting Cycle")]
    public string? ReportingCycleName { get; set; }

    [Display(Name = "Reporting Period")]
    public string? ReportingPeriod { get; set; }

    [Display(Name = "Respondent Code")]
    public string? RespondentCode { get; set; }

    [Display(Name = "Respondent")]
    public string? RespondentName { get; set; }

    [Display(Name = "Respondent Type")]
    public string? RespondentTypeName { get; set; }

    [Display(Name = "Current Status")]
    public int CurrentStatusId { get; set; }

    [Display(Name = "Current Status")]
    public string? CurrentStatusName { get; set; }

    [Display(Name = "Distributed")]
    public DateTime? DistributedDate { get; set; }

    [Display(Name = "First Submission")]
    public DateTime? FirstSubmissionDate { get; set; }

    [Display(Name = "Latest Submission")]
    public DateTime? LatestSubmissionDate { get; set; }

    [Display(Name = "Accepted")]
    public DateTime? AcceptedDate { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created")]
    public DateTime CreatedDate { get; set; }

    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    [Display(Name = "Modified")]
    public DateTime? ModifiedDate { get; set; }

    [Display(Name = "Modified By")]
    public string? ModifiedBy { get; set; }

    public List<SelectListItem> Respondents { get; set; }
        = new();
}
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ISSService.Web.Models.Submissions;

public class SubmissionViewModel
{
    public int Id { get; set; }

    [Display(Name = "Respondent Case")]
    public int RespondentCaseId { get; set; }

    [Display(Name = "Template Version")]
    public int DataCollectionTemplateVersionId { get; set; }

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

    [Display(Name = "Case Status")]
    public string? CurrentCaseStatusName { get; set; }

    [Display(Name = "Template")]
    public string? TemplateName { get; set; }

    [Display(Name = "Template Version")]
    public string? TemplateVersionLabel { get; set; }

    [Display(Name = "Submission Number")]
    public int SubmissionNumber { get; set; }

    [Display(Name = "Submission Method")]
    public string? SubmissionMethodName { get; set; }

    [Display(Name = "Submission Status")]
    public string? StatusName { get; set; }

    [Display(Name = "Submitted")]
    public DateTime? SubmittedDate { get; set; }

    [Display(Name = "Submitted By")]
    public string? SubmittedBy { get; set; }

    [Display(Name = "File Name")]
    public string? FileName { get; set; }

    [Display(Name = "File Size")]
    public long? FileSizeBytes { get; set; }

    [Display(Name = "File Hash")]
    public string? FileHash { get; set; }

    [Display(Name = "Current Submission")]
    public bool IsCurrent { get; set; }

    [Display(Name = "Validation History")]
    public bool HasValidationRuns { get; set; }

    [StringLength(
        2000,
        ErrorMessage = "Notes cannot exceed 2,000 characters.")]
    public string? Notes { get; set; }

    [Display(Name = "SACCO Return File")]
    [Required(ErrorMessage = "Please select the completed SACCO return file.")]
    public IFormFile? SubmissionFile { get; set; }
}
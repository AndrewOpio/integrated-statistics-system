using System.ComponentModel.DataAnnotations;
using ISSService.Data.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ISSService.Web.Models.CycleTemplateAssignments;

public class CycleTemplateAssignmentViewModel
{
    public int Id { get; set; }

    [Required]
    public int ReportingCycleId { get; set; }

    public int StatisticalProcessId { get; set; }

    public string StatisticalProcessCode { get; set; } = string.Empty;

    public string StatisticalProcessName { get; set; } = string.Empty;

    public string ReportingCycleCode { get; set; } = string.Empty;

    public string ReportingCycleName { get; set; } = string.Empty;

    public DateTime PeriodStartDate { get; set; }

    public DateTime PeriodEndDate { get; set; }

    [Required(ErrorMessage = "Please select a template version.")]
    [Display(Name = "Template Version")]
    public int DataCollectionTemplateVersionId { get; set; }

    [Display(Name = "Primary Collection Instrument")]
    public bool IsPrimary { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public DateTime AssignedDate { get; set; }

    public string? AssignedBy { get; set; }

    public string TemplateCode { get; set; } = string.Empty;

    public string TemplateName { get; set; } = string.Empty;

    public int VersionNumber { get; set; }

    public string VersionLabel { get; set; } = string.Empty;

    public TemplateVersionStatus VersionStatus { get; set; }

    public string? FileName { get; set; }

    public List<SelectListItem> AvailableTemplateVersions { get; set; }
        = new();
}
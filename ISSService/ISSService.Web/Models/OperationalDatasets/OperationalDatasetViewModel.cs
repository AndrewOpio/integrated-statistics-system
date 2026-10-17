namespace ISSService.Web.Models.OperationalDatasets;

public class OperationalDatasetViewModel
{
    public int Id { get; set; }

    public int SubmissionId { get; set; }

    public int RespondentCaseId { get; set; }

    public string StatusName { get; set; } = string.Empty;

    public bool IsCurrent { get; set; }

    public DateTime? ExtractionStartedDate { get; set; }

    public DateTime? ExtractionCompletedDate { get; set; }

    public int ObservationCount { get; set; }

    public string? Summary { get; set; }

    public string? ExtractedBy { get; set; }

    public string StatisticalProcessName { get; set; } = string.Empty;

    public string ReportingCycleName { get; set; } = string.Empty;

    public string ReportingCycleCode { get; set; } = string.Empty;

    public DateTime PeriodStartDate { get; set; }

    public DateTime PeriodEndDate { get; set; }

    public string CaseReference { get; set; } = string.Empty;

    public string CaseStatusName { get; set; } = string.Empty;

    public string RespondentCode { get; set; } = string.Empty;

    public string RespondentName { get; set; } = string.Empty;

    public int SubmissionNumber { get; set; }

    public string SubmissionStatusName { get; set; } = string.Empty;

    public DateTime SubmittedDate { get; set; }

    public string? SubmittedBy { get; set; }

    public string? FileName { get; set; }

    public string TemplateName { get; set; } = string.Empty;

    public string TemplateVersionLabel { get; set; } = string.Empty;

    public List<OperationalDatasetSectionViewModel> Sections { get; set; }
        = new();
}

public class OperationalDatasetSectionViewModel
{
    public string SectionCode { get; set; } = string.Empty;

    public string SectionName { get; set; } = string.Empty;

    public int ObservationCount { get; set; }

    public List<OperationalDatasetObservationViewModel> Observations { get; set; }
        = new();
}

public class OperationalDatasetObservationViewModel
{
    public long Id { get; set; }

    public string IndicatorCode { get; set; } = string.Empty;

    public string IndicatorName { get; set; } = string.Empty;

    public string? RecordReference { get; set; }

    public DateTime? ObservationDate { get; set; }

    public decimal? NumericValue { get; set; }

    public string? TextValue { get; set; }

    public DateTime? DateValue { get; set; }

    public bool? BooleanValue { get; set; }

    public string ValueTypeName { get; set; } = string.Empty;

    public string? UnitOfMeasure { get; set; }

    public string? SheetName { get; set; }

    public string? CellReference { get; set; }

    public string DisplayValue
    {
        get
        {
            if (NumericValue.HasValue)
            {
                return NumericValue.Value.ToString("#,##0.######");
            }

            if (DateValue.HasValue)
            {
                return DateValue.Value.ToString("dd MMM yyyy");
            }

            if (BooleanValue.HasValue)
            {
                return BooleanValue.Value ? "Yes" : "No";
            }

            return TextValue ?? string.Empty;
        }
    }
}
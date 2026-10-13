using ISSService.Data.Entities;

namespace ISSService.Web.Models.Sacco;

public class SaccoDashboardViewModel
{
    public int? SelectedCycleId { get; set; }

    public string ProcessName { get; set; } = "SACCO Returns";

    public bool ProcessConfigured { get; set; }

    public List<SaccoCycleOptionViewModel> AvailableCycles { get; set; } = new();

    public SaccoCycleSummaryViewModel? Cycle { get; set; }

    public List<SaccoRespondentCaseViewModel> RespondentCases { get; set; } = new();

    public bool HasSelectedCycle => Cycle != null;
}

public class SaccoCycleOptionViewModel
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public ReportingCycleStatus Status { get; set; }

    public DateTime PeriodEndDate { get; set; }
}

public class SaccoCycleSummaryViewModel
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTime PeriodStartDate { get; set; }

    public DateTime PeriodEndDate { get; set; }

    public DateTime? CollectionOpenDate { get; set; }

    public DateTime? SubmissionDueDate { get; set; }

    public DateTime? CollectionCloseDate { get; set; }

    public ReportingCycleStatus Status { get; set; }

    public string? TemplateName { get; set; }

    public string? TemplateVersion { get; set; }

    public int TotalCases { get; set; }

    public int DistributedCases { get; set; }

    public int CasesWithSubmissions { get; set; }

    public int AcceptedCases { get; set; }

    public int CasesWithValidationFailures { get; set; }

    public decimal SubmissionRate =>
        TotalCases == 0
            ? 0
            : Math.Round(
                CasesWithSubmissions * 100m / TotalCases,
                1);

    public decimal AcceptanceRate =>
        TotalCases == 0
            ? 0
            : Math.Round(
                AcceptedCases * 100m / TotalCases,
                1);
}

public class SaccoRespondentCaseViewModel
{
    public int RespondentCaseId { get; set; }

    public string CaseReference { get; set; } = string.Empty;

    public string RespondentCode { get; set; } = string.Empty;

    public string RespondentName { get; set; } = string.Empty;

    public string? Region { get; set; }

    public string? District { get; set; }

    public int CurrentStatusId { get; set; }

    public string CurrentStatusName { get; set; } = string.Empty;

    public DateTime? DistributedDate { get; set; }

    public DateTime? LatestSubmissionDate { get; set; }

    public DateTime? AcceptedDate { get; set; }

    public int SubmissionCount { get; set; }

    public SubmissionStatus? LatestSubmissionStatus { get; set; }

    public ValidationRunStatus? LatestValidationStatus { get; set; }
}
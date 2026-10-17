using ISSService.Data.Configurations;
using ISSService.Web.Models.OperationalDatasets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISSService.Web.Controllers;

public class OperationalDatasetsController : Controller
{
    private readonly ISSServiceDbContext _db;

    public OperationalDatasetsController(
        ISSServiceDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        int id,
        CancellationToken cancellationToken)
    {
        var dataset = await _db.OperationalDatasets
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.SubmissionId,
                x.Status,
                x.IsCurrent,
                x.ExtractionStartedDate,
                x.ExtractionCompletedDate,
                x.ObservationCount,
                x.Summary,
                x.ExtractedBy,

                RespondentCaseId =
                    x.Submission.RespondentCaseId,

                StatisticalProcessName =
                    x.Submission.RespondentCase
                        .ReportingCycle
                        .StatisticalProcess.Name,

                ReportingCycleName =
                    x.Submission.RespondentCase
                        .ReportingCycle.Name,

                ReportingCycleCode =
                    x.Submission.RespondentCase
                        .ReportingCycle.Code,

                PeriodStartDate =
                    x.Submission.RespondentCase
                        .ReportingCycle.PeriodStartDate,

                PeriodEndDate =
                    x.Submission.RespondentCase
                        .ReportingCycle.PeriodEndDate,

                CaseReference =
                    x.Submission.RespondentCase
                        .CaseReference,

                CaseStatusName =
                    x.Submission.RespondentCase
                        .CurrentStatusName,

                RespondentCode =
                    x.Submission.RespondentCase
                        .Respondent.Code,

                RespondentName =
                    x.Submission.RespondentCase
                        .Respondent.Name,

                SubmissionNumber =
                    x.Submission.SubmissionNumber,

                SubmissionStatus =
                    x.Submission.Status,

                SubmittedDate =
                    x.Submission.SubmittedDate,

                SubmittedBy =
                    x.Submission.SubmittedBy,

                FileName =
                    x.Submission.FileName,

                TemplateName =
                    x.Submission
                        .DataCollectionTemplateVersion
                        .DataCollectionTemplate.Name,

                TemplateVersionLabel =
                    x.Submission
                        .DataCollectionTemplateVersion
                        .VersionLabel,

                Observations =
                    x.Observations
                        .OrderBy(observation =>
                            observation.SectionName)
                        .ThenBy(observation =>
                            observation.RecordReference)
                        .ThenBy(observation =>
                            observation.ObservationDate)
                        .ThenBy(observation =>
                            observation.Id)
                        .Select(observation => new
                        {
                            observation.Id,
                            observation.IndicatorCode,
                            observation.IndicatorName,
                            observation.SectionCode,
                            observation.SectionName,
                            observation.RecordReference,
                            observation.ObservationDate,
                            observation.NumericValue,
                            observation.TextValue,
                            observation.DateValue,
                            observation.BooleanValue,
                            observation.ValueType,
                            observation.UnitOfMeasure,
                            observation.SheetName,
                            observation.CellReference
                        })
                        .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (dataset == null)
        {
            return NotFound();
        }

        var model = new OperationalDatasetViewModel
        {
            Id =
                dataset.Id,

            SubmissionId =
                dataset.SubmissionId,

            RespondentCaseId =
                dataset.RespondentCaseId,

            StatusName =
                dataset.Status.ToString(),

            IsCurrent =
                dataset.IsCurrent,

            ExtractionStartedDate =
                dataset.ExtractionStartedDate,

            ExtractionCompletedDate =
                dataset.ExtractionCompletedDate,

            ObservationCount =
                dataset.ObservationCount,

            Summary =
                dataset.Summary,

            ExtractedBy =
                dataset.ExtractedBy,

            StatisticalProcessName =
                dataset.StatisticalProcessName,

            ReportingCycleName =
                dataset.ReportingCycleName,

            ReportingCycleCode =
                dataset.ReportingCycleCode,

            PeriodStartDate =
                dataset.PeriodStartDate,

            PeriodEndDate =
                dataset.PeriodEndDate,

            CaseReference =
                dataset.CaseReference,

            CaseStatusName =
                dataset.CaseStatusName,

            RespondentCode =
                dataset.RespondentCode,

            RespondentName =
                dataset.RespondentName,

            SubmissionNumber =
                dataset.SubmissionNumber,

            SubmissionStatusName =
                dataset.SubmissionStatus.ToString(),

            SubmittedDate =
                dataset.SubmittedDate,

            SubmittedBy =
                dataset.SubmittedBy,

            FileName =
                dataset.FileName,

            TemplateName =
                dataset.TemplateName,

            TemplateVersionLabel =
                dataset.TemplateVersionLabel,

            Sections =
                dataset.Observations
                    .GroupBy(observation => new
                    {
                        SectionCode =
                            observation.SectionCode ??
                            "UNCLASSIFIED",

                        SectionName =
                            observation.SectionName ??
                            "Unclassified"
                    })
                    .Select(group =>
                        new OperationalDatasetSectionViewModel
                        {
                            SectionCode =
                                group.Key.SectionCode,

                            SectionName =
                                group.Key.SectionName,

                            ObservationCount =
                                group.Count(),

                            Observations =
                                group
                                    .OrderBy(observation =>
                                        GetRecordSortOrder(
                                            observation.RecordReference))
                                    .ThenBy(observation =>
                                        observation.RecordReference)
                                    .ThenBy(observation =>
                                        observation.ObservationDate)
                                    .ThenBy(observation =>
                                        observation.Id)
                                    .Select(observation =>
                                        new OperationalDatasetObservationViewModel
                                        {
                                            Id =
                                                observation.Id,

                                            IndicatorCode =
                                                observation.IndicatorCode,

                                            IndicatorName =
                                                observation.IndicatorName,

                                            RecordReference =
                                                observation.RecordReference,

                                            ObservationDate =
                                                observation.ObservationDate,

                                            NumericValue =
                                                observation.NumericValue,

                                            TextValue =
                                                observation.TextValue,

                                            DateValue =
                                                observation.DateValue,

                                            BooleanValue =
                                                observation.BooleanValue,

                                            ValueTypeName =
                                                observation.ValueType.ToString(),

                                            UnitOfMeasure =
                                                observation.UnitOfMeasure,

                                            SheetName =
                                                observation.SheetName,

                                            CellReference =
                                                observation.CellReference
                                        })
                                    .ToList()
                        })
                    .OrderBy(section =>
                        GetSectionSortOrder(section.SectionCode))
                    .ThenBy(section =>
                        section.SectionName)
                    .ToList()
        };

        return View(model);
    }

    private static int GetSectionSortOrder(
        string? sectionCode)
    {
        return sectionCode switch
        {
            "ASSET" => 1,
            "LIABILITY" => 2,
            "LOAN-SECTOR" => 3,
            "MEMBERSHIP" => 4,
            "CLIENT-LOAN" => 5,
            _ => 99
        };
    }

    private static int GetRecordSortOrder(
        string? recordReference)
    {
        if (int.TryParse(
            recordReference,
            out var numericReference))
        {
            return numericReference;
        }

        return recordReference switch
        {
            "MALE" => 1,
            "FEMALE" => 2,
            "GROUP" => 3,
            "YOUTH" => 4,
            "INSTITUTION" => 5,
            "TOTAL" => 6,
            _ => 999
        };
    }
}
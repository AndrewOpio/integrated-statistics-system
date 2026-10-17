using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ISSService.Data.Configurations;
using ISSService.Data.Entities;
using ISSService.Web.Services.Sacco;
using Microsoft.EntityFrameworkCore;

namespace ISSService.Web.Services.Extraction;

public sealed class SaccoDataExtractionService(
    ISSServiceDbContext db,
    ISaccoWorkbookReader workbookReader,
    ILogger<SaccoDataExtractionService> logger)
    : ISaccoDataExtractionService
{
    private const string AssetsLiabilitiesSheet =
        "Asset & Liabilities";

    private const string MembershipSheet =
        "Membership";

    private const string SaccoUnitOfMeasure =
        "UGX";

    public async Task<SaccoDataExtractionResult> ExtractAsync(
        int submissionId,
        string extractedBy,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var submission = await db.Submissions
            .Include(x => x.ValidationRuns)
            .Include(x => x.OperationalDatasets)
            .Include(x => x.RespondentCase)
                .ThenInclude(x => x.ReportingCycle)
                    .ThenInclude(x => x.StatisticalProcess)
            .FirstOrDefaultAsync(
                x => x.Id == submissionId,
                cancellationToken);

        if (submission is null)
        {
            return SaccoDataExtractionResult.Failed(
                "The submission could not be found.");
        }

        if (!submission.IsCurrent)
        {
            return SaccoDataExtractionResult.Failed(
                "Only the current submission for a respondent case can be extracted.");
        }

        if (submission.Status != SubmissionStatus.Validated)
        {
            return SaccoDataExtractionResult.Failed(
                "The submission must be successfully validated before operational data can be extracted.");
        }

        var latestValidationRun = submission.ValidationRuns
            .Where(x =>
                x.Status != ValidationRunStatus.Pending &&
                x.Status != ValidationRunStatus.Running)
            .OrderByDescending(x => x.RunNumber)
            .ThenByDescending(x => x.StartedDate)
            .FirstOrDefault();

        if (latestValidationRun is null)
        {
            return SaccoDataExtractionResult.Failed(
                "The submission does not have a completed validation run.");
        }

        if (latestValidationRun.Status != ValidationRunStatus.Passed &&
            latestValidationRun.Status != ValidationRunStatus.PassedWithWarnings)
        {
            return SaccoDataExtractionResult.Failed(
                "The latest validation run did not pass. Operational data cannot be extracted.");
        }

        var process = submission
            .RespondentCase
            .ReportingCycle
            .StatisticalProcess;

        if (process.ProcessType != StatisticalProcessType.InstitutionalReturn ||
            (!process.Code.Contains(
                 "SACCO",
                 StringComparison.OrdinalIgnoreCase) &&
             !process.Name.Contains(
                 "SACCO",
                 StringComparison.OrdinalIgnoreCase)))
        {
            return SaccoDataExtractionResult.Failed(
                "This extractor can only process SACCO institutional returns.");
        }

        if (string.IsNullOrWhiteSpace(submission.FilePath))
        {
            return SaccoDataExtractionResult.Failed(
                "The validated submission does not have a stored workbook path.");
        }

        if (!File.Exists(submission.FilePath))
        {
            return SaccoDataExtractionResult.Failed(
                "The stored submission workbook could not be found.");
        }

        var existingCurrentDataset =
            submission.OperationalDatasets
                .Where(x => x.IsCurrent)
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();

        if (existingCurrentDataset is not null &&
            existingCurrentDataset.Status ==
                OperationalDatasetStatus.Extracted)
        {
            return SaccoDataExtractionResult.Completed(
                existingCurrentDataset.Id,
                existingCurrentDataset.ObservationCount,
                "Operational data has already been extracted from this submission.");
        }

        var now = DateTime.UtcNow;

        var dataset = new OperationalDataset
        {
            SubmissionId = submission.Id,
            Status = OperationalDatasetStatus.Extracting,
            IsCurrent = true,
            ExtractionStartedDate = now,
            ExtractedBy = extractedBy,
            CreatedDate = now,
            CreatedBy = extractedBy
        };

        db.OperationalDatasets.Add(dataset);

        await db.SaveChangesAsync(
            cancellationToken);

        try
        {
            using var document = SpreadsheetDocument.Open(
                submission.FilePath,
                false);

            var workbookPart = document.WorkbookPart;

            if (workbookPart is null ||
                workbookPart.Workbook is null)
            {
                return await MarkExtractionFailedAsync(
                    dataset,
                    "The SACCO workbook could not be read.",
                    extractedBy,
                    cancellationToken);
            }

            var sheets = workbookReader
                .GetSheets(workbookPart)
                .ToList();

            if (!workbookReader.HasSheet(
                    sheets,
                    AssetsLiabilitiesSheet) ||
                !workbookReader.HasSheet(
                    sheets,
                    MembershipSheet))
            {
                return await MarkExtractionFailedAsync(
                    dataset,
                    "The SACCO workbook does not contain the worksheets required for operational-data extraction.",
                    extractedBy,
                    cancellationToken);
            }

            var financialPeriods =
                GetFinancialPeriods(
                    workbookPart,
                    sheets);

            var membershipPeriods =
                GetMembershipPeriods(
                    workbookPart,
                    sheets);

            if (financialPeriods.Count == 0)
            {
                return await MarkExtractionFailedAsync(
                    dataset,
                    "No valid reporting periods were found on the Asset & Liabilities worksheet.",
                    extractedBy,
                    cancellationToken);
            }

            if (membershipPeriods.Count == 0)
            {
                return await MarkExtractionFailedAsync(
                    dataset,
                    "No valid reporting periods were found on the Membership worksheet.",
                    extractedBy,
                    cancellationToken);
            }

            var observations =
                new List<StatisticalObservation>();

            ExtractFinancialSection(
                observations,
                dataset,
                workbookPart,
                sheets,
                financialPeriods,
                startRow: 5,
                endRow: 38,
                sectionCode: "ASSET",
                sectionName: "Assets",
                extractedBy: extractedBy);

            ExtractFinancialSection(
                observations,
                dataset,
                workbookPart,
                sheets,
                financialPeriods,
                startRow: 47,
                endRow: 86,
                sectionCode: "LIABILITY",
                sectionName: "Liabilities",
                extractedBy: extractedBy);

            ExtractFinancialSection(
                observations,
                dataset,
                workbookPart,
                sheets,
                financialPeriods,
                startRow: 100,
                endRow: 111,
                sectionCode: "LOAN-SECTOR",
                sectionName: "Loans Outstanding by Economic Sector",
                extractedBy: extractedBy);

            ExtractMembershipSection(
                observations,
                dataset,
                workbookPart,
                sheets,
                membershipPeriods,
                startRow: 6,
                endRow: 11,
                sectionCode: "MEMBERSHIP",
                sectionName: "Membership",
                extractedBy: extractedBy);

            ExtractMembershipSection(
                observations,
                dataset,
                workbookPart,
                sheets,
                membershipPeriods,
                startRow: 19,
                endRow: 24,
                sectionCode: "CLIENT-LOAN",
                sectionName: "Gross Loans by Client Category",
                extractedBy: extractedBy,
                unitOfMeasure: SaccoUnitOfMeasure);

            if (observations.Count == 0)
            {
                return await MarkExtractionFailedAsync(
                    dataset,
                    "No statistical observations could be extracted from the validated SACCO workbook.",
                    extractedBy,
                    cancellationToken);
            }

            db.StatisticalObservations.AddRange(
                observations);

            // A later valid submission for the same respondent case
            // becomes the current operational position. Preserve older
            // datasets for audit, but mark them as superseded.
            var previousCurrentDatasets =
                await db.OperationalDatasets
                    .Where(x =>
                        x.Id != dataset.Id &&
                        x.IsCurrent &&
                        x.Submission.RespondentCaseId ==
                            submission.RespondentCaseId)
                    .ToListAsync(
                        cancellationToken);

            foreach (var previousDataset
                     in previousCurrentDatasets)
            {
                previousDataset.IsCurrent = false;
                previousDataset.Status =
                    OperationalDatasetStatus.Superseded;
                previousDataset.ModifiedDate =
                    DateTime.UtcNow;
                previousDataset.ModifiedBy =
                    extractedBy;
            }

            dataset.Status =
                OperationalDatasetStatus.Extracted;

            dataset.ExtractionCompletedDate =
                DateTime.UtcNow;

            dataset.ObservationCount =
                observations.Count;

            dataset.Summary =
                $"Extracted {observations.Count} SACCO statistical observations from submission {submission.SubmissionNumber}.";

            dataset.ModifiedDate =
                DateTime.UtcNow;

            dataset.ModifiedBy =
                extractedBy;

            await db.SaveChangesAsync(
                cancellationToken);

            return SaccoDataExtractionResult.Completed(
                dataset.Id,
                observations.Count,
                $"Successfully extracted {observations.Count} statistical observations.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Operational-data extraction failed for SACCO submission {SubmissionId}.",
                submission.Id);

            return await MarkExtractionFailedAsync(
                dataset,
                "An unexpected error occurred while extracting operational data from the SACCO workbook.",
                extractedBy,
                cancellationToken);
        }
    }

    private void ExtractFinancialSection(
        ICollection<StatisticalObservation> observations,
        OperationalDataset dataset,
        WorkbookPart workbookPart,
        IReadOnlyCollection<Sheet> sheets,
        IReadOnlyCollection<ReportingPeriodColumn> periods,
        int startRow,
        int endRow,
        string sectionCode,
        string sectionName,
        string extractedBy)
    {
        for (var row = startRow;
             row <= endRow;
             row++)
        {
            var label = workbookReader.ReadCellText(
                workbookPart,
                sheets,
                AssetsLiabilitiesSheet,
                $"A{row}");

            var lineNumber =
                workbookReader.ReadCellText(
                    workbookPart,
                    sheets,
                    AssetsLiabilitiesSheet,
                    $"B{row}");

            if (string.IsNullOrWhiteSpace(label) ||
                string.IsNullOrWhiteSpace(lineNumber))
            {
                continue;
            }

            foreach (var period in periods)
            {
                var cellReference =
                    $"{period.Column}{row}";

                var value = workbookReader.ReadDecimal(
                    workbookPart,
                    sheets,
                    AssetsLiabilitiesSheet,
                    cellReference);

                if (!value.HasValue)
                {
                    continue;
                }

                observations.Add(
                    CreateNumericObservation(
                        dataset,
                        $"{sectionCode}-{lineNumber.Trim()}",
                        NormalizeIndicatorName(label),
                        sectionCode,
                        sectionName,
                        lineNumber.Trim(),
                        period.Date,
                        value.Value,
                        SaccoUnitOfMeasure,
                        AssetsLiabilitiesSheet,
                        cellReference,
                        extractedBy));
            }
        }
    }

    private void ExtractMembershipSection(
        ICollection<StatisticalObservation> observations,
        OperationalDataset dataset,
        WorkbookPart workbookPart,
        IReadOnlyCollection<Sheet> sheets,
        IReadOnlyCollection<ReportingPeriodColumn> periods,
        int startRow,
        int endRow,
        string sectionCode,
        string sectionName,
        string extractedBy,
        string? unitOfMeasure = null)
    {
        for (var row = startRow;
             row <= endRow;
             row++)
        {
            var label = workbookReader.ReadCellText(
                workbookPart,
                sheets,
                MembershipSheet,
                $"A{row}");

            if (string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            var recordReference =
                GetMembershipRecordReference(
                    sectionCode,
                    row);

            foreach (var period in periods)
            {
                var cellReference =
                    $"{period.Column}{row}";

                var value = workbookReader.ReadDecimal(
                    workbookPart,
                    sheets,
                    MembershipSheet,
                    cellReference);

                if (!value.HasValue)
                {
                    continue;
                }

                observations.Add(
                    CreateNumericObservation(
                        dataset,
                        $"{sectionCode}-{recordReference}",
                        NormalizeIndicatorName(label),
                        sectionCode,
                        sectionName,
                        recordReference,
                        period.Date,
                        value.Value,
                        unitOfMeasure,
                        MembershipSheet,
                        cellReference,
                        extractedBy));
            }
        }
    }

    private List<ReportingPeriodColumn>
        GetFinancialPeriods(
            WorkbookPart workbookPart,
            IReadOnlyCollection<Sheet> sheets)
    {
        var result =
            new List<ReportingPeriodColumn>();

        foreach (var column in new[] { "C", "D" })
        {
            var rawValue =
                workbookReader.ReadCellText(
                    workbookPart,
                    sheets,
                    AssetsLiabilitiesSheet,
                    $"{column}4");

            if (workbookReader.TryConvertExcelDate(
                    rawValue,
                    out var date))
            {
                result.Add(
                    new ReportingPeriodColumn(
                        column,
                        date));
            }
        }

        return result;
    }

    private List<ReportingPeriodColumn>
        GetMembershipPeriods(
            WorkbookPart workbookPart,
            IReadOnlyCollection<Sheet> sheets)
    {
        var result =
            new List<ReportingPeriodColumn>();

        foreach (var column in new[] { "B", "C" })
        {
            var rawValue =
                workbookReader.ReadCellText(
                    workbookPart,
                    sheets,
                    MembershipSheet,
                    $"{column}5");

            if (workbookReader.TryConvertExcelDate(
                    rawValue,
                    out var date))
            {
                result.Add(
                    new ReportingPeriodColumn(
                        column,
                        date));
            }
        }

        return result;
    }

    private static StatisticalObservation
        CreateNumericObservation(
            OperationalDataset dataset,
            string indicatorCode,
            string indicatorName,
            string sectionCode,
            string sectionName,
            string recordReference,
            DateTime observationDate,
            decimal numericValue,
            string? unitOfMeasure,
            string sheetName,
            string cellReference,
            string extractedBy)
    {
        return new StatisticalObservation
        {
            OperationalDataset = dataset,
            IndicatorCode = indicatorCode,
            IndicatorName = indicatorName,
            SectionCode = sectionCode,
            SectionName = sectionName,
            RecordReference = recordReference,
            ObservationDate = observationDate,
            NumericValue = numericValue,
            ValueType =
                StatisticalObservationValueType.Numeric,
            UnitOfMeasure = unitOfMeasure,
            SheetName = sheetName,
            CellReference = cellReference,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = extractedBy
        };
    }

    private static string GetMembershipRecordReference(
        string sectionCode,
        int row)
    {
        return sectionCode switch
        {
            "MEMBERSHIP" => row switch
            {
                6 => "MALE",
                7 => "FEMALE",
                8 => "GROUP",
                9 => "YOUTH",
                10 => "INSTITUTION",
                11 => "TOTAL",
                _ => $"ROW-{row}"
            },

            "CLIENT-LOAN" => row switch
            {
                19 => "MALE",
                20 => "FEMALE",
                21 => "GROUP",
                22 => "YOUTH",
                23 => "INSTITUTION",
                24 => "TOTAL",
                _ => $"ROW-{row}"
            },

            _ => $"ROW-{row}"
        };
    }

    private static string NormalizeIndicatorName(
        string value)
    {
        return string.Join(
            " ",
            value
                .Split(
                    [' ', '\t', '\r', '\n'],
                    StringSplitOptions.RemoveEmptyEntries))
            .Trim();
    }

    private async Task<SaccoDataExtractionResult>
        MarkExtractionFailedAsync(
            OperationalDataset dataset,
            string message,
            string extractedBy,
            CancellationToken cancellationToken)
    {
        dataset.Status =
            OperationalDatasetStatus.ExtractionFailed;

        dataset.IsCurrent = false;

        dataset.ExtractionCompletedDate =
            DateTime.UtcNow;

        dataset.ObservationCount = 0;

        dataset.Summary = message;

        dataset.ModifiedDate =
            DateTime.UtcNow;

        dataset.ModifiedBy =
            extractedBy;

        await db.SaveChangesAsync(
            cancellationToken);

        return SaccoDataExtractionResult.Failed(
            message);
    }

    private sealed record ReportingPeriodColumn(
        string Column,
        DateTime Date);
}
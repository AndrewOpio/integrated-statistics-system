using System.Globalization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ISSService.Data.Entities;
using ISSService.Web.Services.Sacco;

namespace ISSService.Web.Services.Validation;

public sealed class SaccoWorkbookValidationService(
    ISaccoWorkbookReader workbookReader,
    ILogger<SaccoWorkbookValidationService> logger)
    : ISaccoWorkbookValidationService
{
    private const string CoverPageSheet = "Cover Page";
    private const string AssetsLiabilitiesSheet = "Asset & Liabilities";
    private const string MembershipSheet = "Membership";
    private const string ContactsSheet = "Contacts";
    private const string MainDatabaseRecordsSheet = "Main dbase recs";
    private const string SaccoListSheet = "SACCO list";

    private static readonly string[] RequiredSheetNames =
    [
        CoverPageSheet,
        AssetsLiabilitiesSheet,
        MembershipSheet,
        ContactsSheet,
        MainDatabaseRecordsSheet,
        SaccoListSheet
    ];

    public Task<SaccoWorkbookValidationResult> ValidateAsync(
        string filePath,
        string expectedRespondentCode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = new SaccoWorkbookValidationResult();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            AddFailure(
                result,
                "SACCO-FILE-001",
                "Submission file availability",
                ValidationSeverity.Critical,
                "The submission does not have a stored workbook path.");

            return Task.FromResult(result);
        }

        if (!File.Exists(filePath))
        {
            AddFailure(
                result,
                "SACCO-FILE-001",
                "Submission file availability",
                ValidationSeverity.Critical,
                "The stored SACCO submission file could not be found.",
                observedValue: filePath,
                expectedValue: "Existing stored submission file");

            return Task.FromResult(result);
        }

        try
        {
            using var document = SpreadsheetDocument.Open(
                filePath,
                false);

            var workbookPart = document.WorkbookPart;

            if (workbookPart is null)
            {
                AddFailure(
                    result,
                    "SACCO-FILE-002",
                    "Workbook structure",
                    ValidationSeverity.Critical,
                    "The submitted file does not contain a readable Excel workbook.");

                return Task.FromResult(result);
            }

            var workbook = workbookPart.Workbook;

            if (workbook is null)
            {
                AddFailure(
                    result,
                    "SACCO-FILE-002",
                    "Workbook structure",
                    ValidationSeverity.Critical,
                    "The submitted file does not contain a readable workbook definition.");

                return Task.FromResult(result);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var sheets = workbookReader
                .GetSheets(workbookPart)
                .ToList();

            ValidateRequiredSheets(
                result,
                sheets);

            // The remaining checks depend on the respondent-facing
            // worksheets being available.
            if (!workbookReader.HasSheet(
                    sheets,
                    CoverPageSheet) ||
                !workbookReader.HasSheet(
                    sheets,
                    AssetsLiabilitiesSheet) ||
                !workbookReader.HasSheet(
                    sheets,
                    MembershipSheet))
            {
                return Task.FromResult(result);
            }

            ValidateRespondentCode(
                result,
                workbookPart,
                sheets,
                expectedRespondentCode);

            ValidateAssetsEqualLiabilities(
                result,
                workbookPart,
                sheets);

            ValidateLoansOutstandingAgainstGrossLoans(
                result,
                workbookPart,
                sheets);

            ValidateMembershipTotals(
                result,
                workbookPart,
                sheets);

            ValidateGrossLoansByClientCategory(
                result,
                workbookPart,
                sheets);

            return Task.FromResult(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (OpenXmlPackageException ex)
        {
            logger.LogWarning(
                ex,
                "SACCO submission {FilePath} could not be opened as an Open XML workbook.",
                filePath);

            AddFailure(
                result,
                "SACCO-FILE-002",
                "Workbook structure",
                ValidationSeverity.Critical,
                "The submitted file is not a valid readable .xlsx workbook.");

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error while validating SACCO submission {FilePath}.",
                filePath);

            AddFailure(
                result,
                "SACCO-FILE-003",
                "Workbook validation execution",
                ValidationSeverity.Critical,
                "An unexpected error occurred while validating the SACCO workbook.");

            return Task.FromResult(result);
        }
    }

    private void ValidateRequiredSheets(
        SaccoWorkbookValidationResult result,
        IReadOnlyCollection<Sheet> sheets)
    {
        var missingSheets = RequiredSheetNames
            .Where(required =>
                !workbookReader.HasSheet(
                    sheets,
                    required))
            .ToList();

        if (missingSheets.Count == 0)
        {
            AddPass(result);
            return;
        }

        AddFailure(
            result,
            "SACCO-STRUCT-001",
            "Required SACCO worksheets",
            ValidationSeverity.Critical,
            $"The workbook is missing required worksheet(s): {string.Join(", ", missingSheets)}.",
            expectedValue: string.Join(", ", RequiredSheetNames));
    }

    private void ValidateRespondentCode(
        SaccoWorkbookValidationResult result,
        WorkbookPart workbookPart,
        IReadOnlyCollection<Sheet> sheets,
        string expectedRespondentCode)
    {
        const string cellReference = "B23";

        var observedCode = workbookReader.ReadCellText(
            workbookPart,
            sheets,
            CoverPageSheet,
            cellReference);

        if (string.IsNullOrWhiteSpace(observedCode))
        {
            AddFailure(
                result,
                "SACCO-ID-001",
                "SACCO respondent code",
                ValidationSeverity.Error,
                "The SACCO code is missing from the Cover Page.",
                CoverPageSheet,
                "SACCO Code",
                cellReference,
                observedCode,
                expectedRespondentCode);

            return;
        }

        if (string.IsNullOrWhiteSpace(expectedRespondentCode))
        {
            AddFailure(
                result,
                "SACCO-ID-001",
                "SACCO respondent code",
                ValidationSeverity.Error,
                "ISS does not have an expected respondent code for this submission.",
                CoverPageSheet,
                "SACCO Code",
                cellReference,
                observedCode,
                "RespondentCase respondent code");

            return;
        }

        if (!string.Equals(
                observedCode.Trim(),
                expectedRespondentCode.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            AddFailure(
                result,
                "SACCO-ID-001",
                "SACCO respondent code",
                ValidationSeverity.Error,
                "The SACCO code in the submitted workbook does not match the respondent assigned to this case.",
                CoverPageSheet,
                "SACCO Code",
                cellReference,
                observedCode,
                expectedRespondentCode);

            return;
        }

        AddPass(result);
    }

    private void ValidateAssetsEqualLiabilities(
        SaccoWorkbookValidationResult result,
        WorkbookPart workbookPart,
        IReadOnlyCollection<Sheet> sheets)
    {
        var periodColumns = GetReportingPeriodColumns(
            workbookPart,
            sheets);

        if (periodColumns.Count == 0)
        {
            AddFailure(
                result,
                "SACCO-FIN-001",
                "Total Assets equal Total Liabilities",
                ValidationSeverity.Error,
                "No reporting-period columns could be identified from the Assets & Liabilities worksheet.",
                AssetsLiabilitiesSheet,
                "Reporting Period",
                expectedValue: "At least one valid reporting-period column");

            return;
        }

        foreach (var period in periodColumns)
        {
            var totalAssets = workbookReader.ReadDecimal(
                workbookPart,
                sheets,
                AssetsLiabilitiesSheet,
                $"{period.Column}38");

            var totalLiabilities = workbookReader.ReadDecimal(
                workbookPart,
                sheets,
                AssetsLiabilitiesSheet,
                $"{period.Column}86");

            if (!totalAssets.HasValue ||
                !totalLiabilities.HasValue)
            {
                AddFailure(
                    result,
                    "SACCO-FIN-001",
                    "Total Assets equal Total Liabilities",
                    ValidationSeverity.Error,
                    $"Total Assets and Total Liabilities must both contain numeric values for {period.DisplayDate}.",
                    AssetsLiabilitiesSheet,
                    "Balance Sheet Reconciliation",
                    $"{period.Column}38;{period.Column}86",
                    $"Assets={FormatDecimal(totalAssets)}; Liabilities={FormatDecimal(totalLiabilities)}",
                    "Total Assets = Total Liabilities",
                    period.DisplayDate);

                continue;
            }

            if (totalAssets.Value != totalLiabilities.Value)
            {
                AddFailure(
                    result,
                    "SACCO-FIN-001",
                    "Total Assets equal Total Liabilities",
                    ValidationSeverity.Error,
                    $"Total Assets do not equal Total Liabilities for {period.DisplayDate}.",
                    AssetsLiabilitiesSheet,
                    "Balance Sheet Reconciliation",
                    $"{period.Column}38;{period.Column}86",
                    $"Assets={totalAssets.Value}; Liabilities={totalLiabilities.Value}",
                    "Difference = 0",
                    period.DisplayDate);

                continue;
            }

            AddPass(result);
        }
    }

    private void ValidateLoansOutstandingAgainstGrossLoans(
        SaccoWorkbookValidationResult result,
        WorkbookPart workbookPart,
        IReadOnlyCollection<Sheet> sheets)
    {
        var periodColumns = GetReportingPeriodColumns(
            workbookPart,
            sheets);

        if (periodColumns.Count == 0)
        {
            AddFailure(
                result,
                "SACCO-LOAN-001",
                "Loans outstanding reconcile to Gross Loans",
                ValidationSeverity.Error,
                "No reporting-period columns could be identified for the loan reconciliation.",
                AssetsLiabilitiesSheet,
                "Reporting Period",
                expectedValue: "At least one valid reporting-period column");

            return;
        }

        foreach (var period in periodColumns)
        {
            var grossLoans = workbookReader.ReadDecimal(
                workbookPart,
                sheets,
                AssetsLiabilitiesSheet,
                $"{period.Column}31");

            var sectorLoanValues = Enumerable
                .Range(100, 11)
                .Select(row =>
                    workbookReader.ReadDecimal(
                        workbookPart,
                        sheets,
                        AssetsLiabilitiesSheet,
                        $"{period.Column}{row}"))
                .ToList();

            if (!grossLoans.HasValue ||
                sectorLoanValues.Any(x => !x.HasValue))
            {
                AddFailure(
                    result,
                    "SACCO-LOAN-001",
                    "Loans outstanding reconcile to Gross Loans",
                    ValidationSeverity.Error,
                    $"Gross Loans and all sector loan values must contain numeric values for {period.DisplayDate}.",
                    AssetsLiabilitiesSheet,
                    "Loans Outstanding",
                    $"{period.Column}31;{period.Column}100:{period.Column}110",
                    expectedValue: "Gross Loans = sum of loan sectors",
                    recordReference: period.DisplayDate);

                continue;
            }

            var calculatedTotal =
                sectorLoanValues.Sum(x => x!.Value);

            if (grossLoans.Value != calculatedTotal)
            {
                AddFailure(
                    result,
                    "SACCO-LOAN-001",
                    "Loans outstanding reconcile to Gross Loans",
                    ValidationSeverity.Error,
                    $"The sum of loans outstanding by economic sector does not equal Gross Loans for {period.DisplayDate}.",
                    AssetsLiabilitiesSheet,
                    "Loans Outstanding",
                    $"{period.Column}31;{period.Column}100:{period.Column}110",
                    $"Gross Loans={grossLoans.Value}; Sector Total={calculatedTotal}",
                    "Difference = 0",
                    period.DisplayDate);

                continue;
            }

            AddPass(result);
        }
    }

    private void ValidateMembershipTotals(
        SaccoWorkbookValidationResult result,
        WorkbookPart workbookPart,
        IReadOnlyCollection<Sheet> sheets)
    {
        var periodColumns = GetMembershipPeriodColumns(
            workbookPart,
            sheets);

        if (periodColumns.Count == 0)
        {
            AddFailure(
                result,
                "SACCO-MEM-001",
                "Membership total",
                ValidationSeverity.Error,
                "No reporting-period columns could be identified from the Membership worksheet.",
                MembershipSheet,
                "Reporting Period",
                expectedValue: "At least one valid reporting-period column");

            return;
        }

        foreach (var period in periodColumns)
        {
            var categoryValues = Enumerable
                .Range(6, 5)
                .Select(row =>
                    workbookReader.ReadDecimal(
                        workbookPart,
                        sheets,
                        MembershipSheet,
                        $"{period.Column}{row}"))
                .ToList();

            if (categoryValues.Any(x => !x.HasValue))
            {
                AddFailure(
                    result,
                    "SACCO-MEM-001",
                    "Membership total",
                    ValidationSeverity.Error,
                    $"All membership categories must contain numeric values for {period.DisplayDate}.",
                    MembershipSheet,
                    "Membership Categories",
                    $"{period.Column}6:{period.Column}10",
                    expectedValue: "Numeric values for all membership categories",
                    recordReference: period.DisplayDate);

                continue;
            }

            var calculatedTotal =
                categoryValues.Sum(x => x!.Value);

            var reportedTotal = workbookReader.ReadDecimal(
                workbookPart,
                sheets,
                MembershipSheet,
                $"{period.Column}11");

            if (!reportedTotal.HasValue)
            {
                AddFailure(
                    result,
                    "SACCO-MEM-001",
                    "Membership total",
                    ValidationSeverity.Error,
                    $"Total Membership is not numeric for {period.DisplayDate}.",
                    MembershipSheet,
                    "Total Membership",
                    $"{period.Column}11",
                    expectedValue: calculatedTotal.ToString(
                        CultureInfo.InvariantCulture),
                    recordReference: period.DisplayDate);

                continue;
            }

            if (reportedTotal.Value != calculatedTotal)
            {
                AddFailure(
                    result,
                    "SACCO-MEM-001",
                    "Membership total",
                    ValidationSeverity.Error,
                    $"Total Membership does not equal the sum of the membership categories for {period.DisplayDate}.",
                    MembershipSheet,
                    "Total Membership",
                    $"{period.Column}6:{period.Column}11",
                    $"Reported={reportedTotal.Value}; Calculated={calculatedTotal}",
                    calculatedTotal.ToString(
                        CultureInfo.InvariantCulture),
                    period.DisplayDate);

                continue;
            }

            AddPass(result);
        }
    }

    private void ValidateGrossLoansByClientCategory(
        SaccoWorkbookValidationResult result,
        WorkbookPart workbookPart,
        IReadOnlyCollection<Sheet> sheets)
    {
        var periodColumns = GetMembershipPeriodColumns(
            workbookPart,
            sheets);

        if (periodColumns.Count == 0)
        {
            AddFailure(
                result,
                "SACCO-LOAN-002",
                "Gross loans by client category",
                ValidationSeverity.Error,
                "No reporting-period columns could be identified for gross loans by client category.",
                MembershipSheet,
                "Reporting Period",
                expectedValue: "At least one valid reporting-period column");

            return;
        }

        foreach (var period in periodColumns)
        {
            var categoryValues = Enumerable
                .Range(19, 5)
                .Select(row =>
                    workbookReader.ReadDecimal(
                        workbookPart,
                        sheets,
                        MembershipSheet,
                        $"{period.Column}{row}"))
                .ToList();

            var grossLoans = workbookReader.ReadDecimal(
                workbookPart,
                sheets,
                AssetsLiabilitiesSheet,
                $"{period.Column}31");

            if (!grossLoans.HasValue ||
                categoryValues.Any(x => !x.HasValue))
            {
                AddFailure(
                    result,
                    "SACCO-LOAN-002",
                    "Gross loans by client category",
                    ValidationSeverity.Error,
                    $"Gross Loans and all client-category loan values must contain numeric values for {period.DisplayDate}.",
                    MembershipSheet,
                    "Gross Loans by Client Category",
                    $"{period.Column}19:{period.Column}23",
                    expectedValue: "Client-category total = Gross Loans",
                    recordReference: period.DisplayDate);

                continue;
            }

            var calculatedTotal =
                categoryValues.Sum(x => x!.Value);

            if (calculatedTotal != grossLoans.Value)
            {
                AddFailure(
                    result,
                    "SACCO-LOAN-002",
                    "Gross loans by client category",
                    ValidationSeverity.Error,
                    $"Gross loans by client category do not reconcile to Gross Loans for {period.DisplayDate}.",
                    MembershipSheet,
                    "Gross Loans by Client Category",
                    $"{period.Column}19:{period.Column}23",
                    $"Category Total={calculatedTotal}; Gross Loans={grossLoans.Value}",
                    "Difference = 0",
                    period.DisplayDate);

                continue;
            }

            AddPass(result);
        }
    }

    private List<ReportingPeriodColumn> GetReportingPeriodColumns(
        WorkbookPart workbookPart,
        IReadOnlyCollection<Sheet> sheets)
    {
        var result = new List<ReportingPeriodColumn>();

        foreach (var column in new[] { "C", "D" })
        {
            var rawValue = workbookReader.ReadCellText(
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

    private List<ReportingPeriodColumn> GetMembershipPeriodColumns(
        WorkbookPart workbookPart,
        IReadOnlyCollection<Sheet> sheets)
    {
        var result = new List<ReportingPeriodColumn>();

        foreach (var column in new[] { "B", "C" })
        {
            var rawValue = workbookReader.ReadCellText(
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

    private static string FormatDecimal(
        decimal? value)
    {
        return value.HasValue
            ? value.Value.ToString(
                CultureInfo.InvariantCulture)
            : "<missing/non-numeric>";
    }

    private static void AddPass(
        SaccoWorkbookValidationResult result)
    {
        result.TotalChecks++;
        result.PassedChecks++;
    }

    private static void AddFailure(
        SaccoWorkbookValidationResult result,
        string ruleCode,
        string ruleName,
        ValidationSeverity severity,
        string message,
        string? sheetName = null,
        string? fieldName = null,
        string? cellReference = null,
        string? observedValue = null,
        string? expectedValue = null,
        string? recordReference = null)
    {
        result.TotalChecks++;
        result.FailedChecks++;

        result.Findings.Add(
            new SaccoValidationFindingResult
            {
                RuleCode = ruleCode,
                RuleName = ruleName,
                Severity = severity,
                Message = message,
                SheetName = sheetName,
                FieldName = fieldName,
                CellReference = cellReference,
                RecordReference = recordReference,
                ObservedValue = observedValue,
                ExpectedValue = expectedValue
            });
    }

    private sealed record ReportingPeriodColumn(
        string Column,
        DateTime Date)
    {
        public string DisplayDate =>
            Date.ToString(
                "dd-MMM-yyyy",
                CultureInfo.InvariantCulture);
    }
}
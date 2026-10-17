using System.Globalization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ISSService.Web.Services.Sacco;

public sealed class SaccoWorkbookReader : ISaccoWorkbookReader
{
    public IReadOnlyList<Sheet> GetSheets(
        WorkbookPart workbookPart)
    {
        var workbook = workbookPart.Workbook;

        if (workbook?.Sheets is null)
        {
            return Array.Empty<Sheet>();
        }

        return workbook.Sheets
            .Elements<Sheet>()
            .ToList();
    }

    public bool HasSheet(
        IEnumerable<Sheet> sheets,
        string sheetName)
    {
        return sheets.Any(x =>
            string.Equals(
                x.Name?.Value,
                sheetName,
                StringComparison.OrdinalIgnoreCase));
    }

    public string? ReadCellText(
        WorkbookPart workbookPart,
        IReadOnlyCollection<Sheet> sheets,
        string sheetName,
        string cellReference)
    {
        var worksheetPart = GetWorksheetPart(
            workbookPart,
            sheets,
            sheetName);

        if (worksheetPart is null)
        {
            return null;
        }

        var worksheet = worksheetPart.Worksheet;

        if (worksheet is null)
        {
            return null;
        }

        var cell = worksheet
            .Descendants<Cell>()
            .FirstOrDefault(x =>
                string.Equals(
                    x.CellReference?.Value,
                    cellReference,
                    StringComparison.OrdinalIgnoreCase));

        if (cell is null)
        {
            return null;
        }

        var rawValue = cell.CellValue?.InnerText;

        if (cell.DataType?.Value == CellValues.SharedString &&
            int.TryParse(
                rawValue,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var sharedStringIndex))
        {
            var sharedStringTable =
                workbookPart.SharedStringTablePart?
                    .SharedStringTable;

            var item = sharedStringTable?
                .Elements<SharedStringItem>()
                .ElementAtOrDefault(sharedStringIndex);

            return item?.InnerText;
        }

        if (cell.DataType?.Value == CellValues.InlineString)
        {
            return cell.InlineString?.InnerText;
        }

        // For formula cells Open XML exposes the cached value
        // stored in the workbook. This reader does not calculate
        // or recalculate Excel formulas.
        return rawValue;
    }

    public decimal? ReadDecimal(
        WorkbookPart workbookPart,
        IReadOnlyCollection<Sheet> sheets,
        string sheetName,
        string cellReference)
    {
        var value = ReadCellText(
            workbookPart,
            sheets,
            sheetName,
            cellReference);

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (decimal.TryParse(
                value,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var parsed))
        {
            return parsed;
        }

        return null;
    }

    public WorksheetPart? GetWorksheetPart(
        WorkbookPart workbookPart,
        IReadOnlyCollection<Sheet> sheets,
        string sheetName)
    {
        var sheet = sheets.FirstOrDefault(x =>
            string.Equals(
                x.Name?.Value,
                sheetName,
                StringComparison.OrdinalIgnoreCase));

        var relationshipId = sheet?.Id?.Value;

        if (string.IsNullOrWhiteSpace(relationshipId))
        {
            return null;
        }

        return workbookPart.GetPartById(
            relationshipId) as WorksheetPart;
    }

    public bool TryConvertExcelDate(
        string? rawValue,
        out DateTime date)
    {
        date = default;

        if (!double.TryParse(
                rawValue,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var serialDate))
        {
            return false;
        }

        try
        {
            date = DateTime.FromOADate(serialDate);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
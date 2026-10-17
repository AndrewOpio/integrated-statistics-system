using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ISSService.Web.Services.Sacco;

public interface ISaccoWorkbookReader
{
    IReadOnlyList<Sheet> GetSheets(
        WorkbookPart workbookPart);

    bool HasSheet(
        IEnumerable<Sheet> sheets,
        string sheetName);

    string? ReadCellText(
        WorkbookPart workbookPart,
        IReadOnlyCollection<Sheet> sheets,
        string sheetName,
        string cellReference);

    decimal? ReadDecimal(
        WorkbookPart workbookPart,
        IReadOnlyCollection<Sheet> sheets,
        string sheetName,
        string cellReference);

    WorksheetPart? GetWorksheetPart(
        WorkbookPart workbookPart,
        IReadOnlyCollection<Sheet> sheets,
        string sheetName);

    bool TryConvertExcelDate(
        string? rawValue,
        out DateTime date);
}
namespace ISSService.Data.Entities;

public class StatisticalObservation
{
    public long Id { get; set; }

    public int OperationalDatasetId { get; set; }

    // Stable process/template-level identifier for the statistical
    // concept represented by this observation. For a SACCO return,
    // this may initially correspond to the return line number or
    // another controlled indicator code.
    public string IndicatorCode { get; set; } = string.Empty;

    public string IndicatorName { get; set; } = string.Empty;

    // Optional section or logical grouping from the source
    // instrument, for example Assets, Liabilities or Membership.
    public string? SectionCode { get; set; }

    public string? SectionName { get; set; }

    // Some collection instruments identify observations using
    // line numbers, question numbers or source record identifiers.
    public string? RecordReference { get; set; }

    // ObservationDate allows one submission to contain values
    // for more than one reference date, as occurs in the SACCO
    // workbook with multiple reporting-period columns.
    public DateTime? ObservationDate { get; set; }

    // Values are represented using typed nullable fields rather
    // than forcing every statistical observation into text.
    public decimal? NumericValue { get; set; }

    public string? TextValue { get; set; }

    public DateTime? DateValue { get; set; }

    public bool? BooleanValue { get; set; }

    public StatisticalObservationValueType ValueType { get; set; }
        = StatisticalObservationValueType.Numeric;

    public string? UnitOfMeasure { get; set; }

    // Source provenance makes an operational value traceable
    // back to its original collection instrument.
    public string? SheetName { get; set; }

    public string? CellReference { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public OperationalDataset OperationalDataset { get; set; } = null!;
}

public enum StatisticalObservationValueType
{
    Numeric = 1,
    Text = 2,
    Date = 3,
    Boolean = 4
}
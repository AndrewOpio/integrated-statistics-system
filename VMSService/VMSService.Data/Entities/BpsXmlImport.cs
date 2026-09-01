using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    /// <summary>
    /// One BPS machine XML result file that has been imported into a processing result.
    /// A file may only ever back a single result — availability in the picker is derived from
    /// the absence of a non-deleted row here, so these rows are what "locks" a file.
    /// </summary>
    public class BpsXmlImport : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>File name as it appears on disk, including the trailing "______&lt;digits&gt;" suffix.</summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>Machine serial number, which is what BPSMachine.Name holds.</summary>
        public string MachineSerialNumber { get; set; } = string.Empty;

        /// <summary>Site code embedded in the file name (KLA, MBL, MBR ...).</summary>
        public string SiteCode { get; set; } = string.Empty;

        /// <summary>Timestamp parsed out of the file name (yyyyMMddHHmmss).</summary>
        public DateTime FileGeneratedAt { get; set; }

        /// <summary>Face value of the notes in the file — always matches the session's denomination.</summary>
        public decimal DenominationValue { get; set; }

        public int FitPieces { get; set; }
        public int UnFitShredPieces { get; set; }
        public int FitShredPieces { get; set; }

        /// <summary>Rejected notes are recorded for traceability but excluded from all imported values.</summary>
        public int RejectPieces { get; set; }

        public decimal FitValue { get; set; }
        public decimal UnFitShredValue { get; set; }
        public decimal FitShredValue { get; set; }

        public int StockProcessingSessionId { get; set; }
        public StockProcessingSession? StockProcessingSession { get; set; }

        public int StockProcessingResultId { get; set; }
        public StockProcessingResult? StockProcessingResult { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}

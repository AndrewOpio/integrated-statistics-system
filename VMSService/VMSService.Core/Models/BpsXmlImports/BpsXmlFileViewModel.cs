namespace VMSService.Core.Models.BpsXmlImports
{
    /// <summary>
    /// One BPS machine result file as offered in the picker — also the shape returned by parsing,
    /// so the same type carries the figures all the way from disk to the form.
    /// </summary>
    public class BpsXmlFileViewModel
    {
        /// <summary>File name on disk, including the trailing "______&lt;digits&gt;" suffix.</summary>
        public string FileName { get; set; } = string.Empty;

        public string MachineSerialNumber { get; set; } = string.Empty;
        public string SiteCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public decimal DenominationValue { get; set; }

        public int FitPieces { get; set; }
        public int UnFitShredPieces { get; set; }
        public int FitShredPieces { get; set; }

        /// <summary>Recorded for traceability only — rejects are excluded from every imported value.</summary>
        public int RejectPieces { get; set; }

        public decimal FitValue { get; set; }
        public decimal UnFitShredValue { get; set; }
        public decimal FitShredValue { get; set; }

        /// <summary>True when this file already backs the result currently being edited.</summary>
        public bool IsAlreadyLinked { get; set; }

        /// <summary>Set when the row is reconstructed from the database because the file is gone from disk.</summary>
        public bool IsMissingOnDisk { get; set; }

        public int TotalPieces => FitPieces + UnFitShredPieces + FitShredPieces;
        public decimal TotalValue => FitValue + UnFitShredValue + FitShredValue;

        /// <summary>Short label for the picker, e.g. "23 Jun 2026 11:53 · MBL".</summary>
        public string DisplayName =>
            $"{GeneratedAt:dd MMM yyyy HH:mm}" + (string.IsNullOrEmpty(SiteCode) ? "" : $" · {SiteCode}");
    }
}

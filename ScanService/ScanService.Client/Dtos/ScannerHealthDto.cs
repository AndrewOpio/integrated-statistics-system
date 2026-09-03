namespace ScanService.Client.Dtos
{
    /// <summary>Reports whether AMSI is usable on the scan service host.</summary>
    public class ScannerHealthDto
    {
        /// <summary>True when AmsiInitialize succeeded and a provider answered a probe scan.</summary>
        public bool IsAvailable { get; set; }

        /// <summary>Application name AMSI was initialised with.</summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>Detail about why the scanner is unavailable, when it is.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>True when the service rejects files it could not scan. Should be true in production.</summary>
        public bool FailClosed { get; set; }

        public long MaxFileSizeBytes { get; set; }
    }
}

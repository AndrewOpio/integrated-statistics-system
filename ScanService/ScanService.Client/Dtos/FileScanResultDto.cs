using System.Text.Json.Serialization;

namespace ScanService.Client.Dtos
{
    /// <summary>
    /// Result of a single scan (and, for the store endpoint, of the write that followed it).
    /// </summary>
    public class FileScanResultDto
    {
        /// <summary>True only when the engine returned a clean verdict. Callers should key off this.</summary>
        public bool IsClean { get; set; }

        public ScanVerdict Verdict { get; set; } = ScanVerdict.Error;

        /// <summary>File name as supplied by the uploading client.</summary>
        public string FileName { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        /// <summary>Human readable explanation, safe to surface to an end user.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Raw AMSI_RESULT value returned by AmsiScanBuffer. -1 when no scan took place.</summary>
        public int AmsiResultCode { get; set; } = -1;

        /// <summary>Name AMSI was initialised with, plus the host that performed the scan.</summary>
        public string? ScannedBy { get; set; }

        /// <summary>Absolute path the cleared file was written to. Null unless the store endpoint was used.</summary>
        public string? StoredPath { get; set; }

        /// <summary>File name on disk (unique, includes the GUID prefix). Null unless the file was stored.</summary>
        public string? StoredFileName { get; set; }

        /// <summary>Directory the cleared file was written to. Null unless the file was stored.</summary>
        public string? StoredDirectory { get; set; }

        public DateTimeOffset ScannedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>Correlation id echoed back so callers can tie a rejection to the scanner log entry.</summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// The exact bytes the antimalware engine cleared, returned by
        /// <see cref="IScanServiceClient.ScanAndGetContentAsync"/> so the caller can write them to
        /// its own storage. Null for every other call and for any non-clean verdict.
        /// </summary>
        /// <remarks>
        /// Carried alongside the verdict rather than in the JSON body - the bytes come back as the
        /// raw response body, so they are never base64 inflated. Writing these bytes, rather than a
        /// second copy of the upload the caller kept for itself, is what makes the guarantee hold:
        /// what was scanned is exactly what is saved.
        /// </remarks>
        [JsonIgnore]
        public byte[]? Content { get; set; }

        public static FileScanResultDto Failed(string fileName, ScanVerdict verdict, string message, long sizeBytes = 0) =>
            new()
            {
                IsClean = false,
                Verdict = verdict,
                FileName = fileName,
                FileSizeBytes = sizeBytes,
                Message = message,
                ScannedAtUtc = DateTimeOffset.UtcNow
            };
    }
}

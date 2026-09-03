namespace ScanService.Client
{
    /// <summary>
    /// Response headers the content endpoint uses to carry the verdict alongside the cleared bytes.
    /// Shared so the service and its clients cannot drift apart on the spelling.
    /// </summary>
    public static class ScanHeaders
    {
        public const string Verdict = "X-Scan-Verdict";
        public const string AmsiResult = "X-Scan-Amsi-Result";
        public const string CorrelationId = "X-Scan-Correlation-Id";
        public const string Provider = "X-Scan-Provider";
        public const string ScannedAt = "X-Scan-Scanned-At";
    }
}

namespace ScanService.Api.Options
{
    public class ScanServiceOptions
    {
        public const string SectionName = "ScanService";

        /// <summary>Name AMSI is initialised with. Shows up in Defender's telemetry.</summary>
        public string AmsiApplicationName { get; set; } = "Aswam.ScanService";

        /// <summary>
        /// Largest upload accepted, in bytes. The whole file is held in memory while it is scanned,
        /// so this is also the per-scan memory cost.
        /// </summary>
        public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024;

        /// <summary>How many buffers may be in the antimalware engine at once.</summary>
        public int MaxConcurrentScans { get; set; } = 4;

        /// <summary>
        /// When true (the default, and the only safe production setting) a file that could not be
        /// scanned is rejected instead of stored.
        /// </summary>
        public bool FailClosed { get; set; } = true;

        /// <summary>
        /// Directories the service is allowed to write cleared files into. A destination supplied by
        /// a requesting microservice must sit under one of these, which stops a compromised or
        /// buggy caller from planting files anywhere on the host.
        /// </summary>
        public List<string> AllowedDestinationRoots { get; set; } = [];

        /// <summary>
        /// Extensions rejected outright, before the buffer even reaches the engine. Defence in
        /// depth: these have no business being uploaded to Aswam regardless of the AMSI verdict.
        /// </summary>
        public List<string> BlockedExtensions { get; set; } =
        [
            ".exe", ".dll", ".com", ".scr", ".pif", ".bat", ".cmd", ".ps1", ".psm1", ".vbs", ".vbe",
            ".js", ".jse", ".wsf", ".wsh", ".hta", ".msi", ".msp", ".jar", ".cpl", ".reg", ".lnk",
            ".sys", ".chm", ".application", ".gadget", ".msc", ".inf", ".scf"
        ];

        /// <summary>
        /// When set, only these extensions are accepted. Empty means "anything that is not blocked".
        /// </summary>
        public List<string> AllowedExtensions { get; set; } = [];

        /// <summary>Boundary length limit for multipart parsing.</summary>
        public int MultipartBoundaryLengthLimit { get; set; } = 128;
    }
}

namespace ScanService.Client.Dtos
{
    /// <summary>
    /// Outcome of a scan request. Only <see cref="Clean"/> means the caller may keep the file;
    /// every other value must be treated as a rejection (fail closed).
    /// </summary>
    public enum ScanVerdict
    {
        /// <summary>The antimalware engine looked at the buffer and found nothing.</summary>
        Clean = 0,

        /// <summary>The antimalware engine flagged the content as malware (AMSI_RESULT_DETECTED).</summary>
        Malicious = 1,

        /// <summary>Blocked by administrator policy (AMSI_RESULT_BLOCKED_BY_ADMIN_*).</summary>
        BlockedByPolicy = 2,

        /// <summary>Rejected before scanning: empty, too large, or a blocked file extension.</summary>
        Rejected = 3,

        /// <summary>AMSI could not be initialised (not Windows, or no antimalware provider registered).</summary>
        ScannerUnavailable = 4,

        /// <summary>The scan or the store step failed unexpectedly.</summary>
        Error = 5
    }
}

namespace ScanService.Client
{
    /// <summary>
    /// Builds the on-disk name for a cleared upload. Lives in the client library so that every
    /// microservice storing scanned files names them the same way, and sanitises them the same way,
    /// instead of each one reinventing it.
    /// </summary>
    public static class SafeFileName
    {
        /// <summary>
        /// Produces "{guid}_{original}" from a user supplied file name, stripped of any path and of
        /// characters the file system would refuse. The GUID prefix keeps concurrent uploads of the
        /// same name from colliding and stops a user choosing where in a folder their file lands.
        /// </summary>
        public static string Create(string? originalFileName)
        {
            var candidate = $"{Guid.NewGuid()}_{Path.GetFileName(originalFileName ?? string.Empty)}";
            return Sanitise(candidate) ?? $"{Guid.NewGuid()}.bin";
        }

        /// <summary>
        /// Sanitises a caller supplied name without adding a prefix. Returns null when nothing
        /// usable is left, so the caller can fall back to a generated name.
        /// </summary>
        public static string? Sanitise(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            // GetFileName first: it drops any directory part, so "..\..\web.config" cannot survive.
            var name = Path.GetFileName(fileName);
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string([.. name.Where(c => !invalid.Contains(c))]).Trim().Trim('.');

            return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
        }
    }
}

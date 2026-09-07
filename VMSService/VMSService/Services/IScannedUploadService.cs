namespace VMSService.Web.Services
{
    /// <summary>
    /// Saves user uploads by way of the Scan Service. Nothing here writes to disk directly: the
    /// bytes are streamed to the scanner, Windows Defender (via AMSI) gives a verdict, and only a
    /// clean file is written - by the scan service - to the folder this service asks for.
    /// </summary>
    public interface IScannedUploadService
    {
        /// <summary>
        /// Scans and saves a batch of uploads. Either every file in the batch is stored, or none
        /// is: if one file is rejected the ones already written are removed again, so a half
        /// finished form never leaves orphaned attachments behind.
        /// </summary>
        Task<ScannedUploadBatchResult> ScanAndSaveAsync(
            IEnumerable<ScannedUploadRequest> uploads,
            CancellationToken cancellationToken = default);
    }

    /// <summary>One file to scan and save.</summary>
    /// <param name="FieldName">Model property the file came from; used to look the result up again.</param>
    /// <param name="File">The posted file. Null or empty entries are skipped.</param>
    /// <param name="FolderName">Sub-folder of the uploads root, e.g. "passportphotos".</param>
    public sealed record ScannedUploadRequest(string FieldName, IFormFile? File, string FolderName);

    public sealed class ScannedUploadBatchResult
    {
        /// <summary>True when every supplied file was clean and saved (or there was nothing to save).</summary>
        public bool Success { get; init; }

        /// <summary>Messages to show the user, one per rejected file.</summary>
        public IReadOnlyList<string> Errors { get; init; } = [];

        /// <summary>Field name to web path ("/uploads/{folder}/{file}") for each file that was saved.</summary>
        public IReadOnlyDictionary<string, string> SavedPaths { get; init; } =
            new Dictionary<string, string>();

        /// <summary>
        /// Web path for a field, or empty string when no file was supplied for it. Matches what the
        /// old inline SaveFileAsync helpers returned.
        /// </summary>
        public string PathFor(string fieldName) =>
            SavedPaths.TryGetValue(fieldName, out var path) ? path : string.Empty;

        /// <summary>True when a file was actually saved for this field.</summary>
        public bool HasFileFor(string fieldName) => SavedPaths.ContainsKey(fieldName);
    }
}

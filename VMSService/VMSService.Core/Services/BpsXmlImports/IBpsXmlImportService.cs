using VMSService.Core.Models.BpsXmlImports;

namespace VMSService.Core.Services.BpsXmlImports
{
    public interface IBpsXmlImportService
    {
        /// <summary>Folder the machine drops its result files into.</summary>
        string FolderPath { get; }

        /// <summary>
        /// Files the operator may pick for this session: same machine serial, same denomination, and
        /// not already consumed by another result. When <paramref name="currentResultId"/> is supplied
        /// (editing), that result's own files are included and flagged as already linked.
        /// </summary>
        Task<IReadOnlyList<BpsXmlFileViewModel>> GetSelectableFilesAsync(
            string machineSerialNumber, decimal denominationValue, int? currentResultId, CancellationToken ct = default);

        /// <summary>
        /// Re-reads and re-parses the named files server-side, rejecting any that no longer match the
        /// machine/denomination or that another result has taken. Never trusts figures from the browser.
        /// </summary>
        Task<BpsXmlImportSelection> ResolveSelectionAsync(
            IEnumerable<string> fileNames, string machineSerialNumber, decimal denominationValue,
            int? currentResultId, CancellationToken ct = default);
    }

    /// <summary>Outcome of validating a set of picked files.</summary>
    public class BpsXmlImportSelection
    {
        public List<BpsXmlFileViewModel> Files { get; set; } = [];
        public List<string> Errors { get; set; } = [];

        public bool HasFiles => Files.Count > 0;
        public bool IsValid => Errors.Count == 0;

        public decimal FitValue => Files.Sum(f => f.FitValue);
        public decimal UnFitShredValue => Files.Sum(f => f.UnFitShredValue);
        public decimal FitShredValue => Files.Sum(f => f.FitShredValue);
    }
}

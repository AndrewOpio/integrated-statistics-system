using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VMSService.Core.Models.BpsXmlImports;
using VMSService.Data.Configurations;

namespace VMSService.Core.Services.BpsXmlImports
{
    public partial class BpsXmlImportService(
        VMSServiceDbContext _db,
        string folderPath,
        ILogger<BpsXmlImportService> _logger) : IBpsXmlImportService
    {
        public string FolderPath { get; } = folderPath;

        // BPSUnit_{serial}_{site}_{yyyyMMddHHmmss}.xml — the files carry a trailing "______<digits>"
        // after the extension, so this deliberately does not anchor at the end.
        [GeneratedRegex(@"^BPSUnit_(?<serial>[^_]+)_(?<site>[^_]+)_(?<ts>\d{14})\.xml",
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex FileNamePattern();

        public async Task<IReadOnlyList<BpsXmlFileViewModel>> GetSelectableFilesAsync(
            string machineSerialNumber, decimal denominationValue, int? currentResultId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(machineSerialNumber) || denominationValue <= 0)
                return [];

            var (lockedElsewhere, linkedToCurrent) = await GetLockStateAsync(currentResultId, ct);

            var results = new List<BpsXmlFileViewModel>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in EnumerateCandidateFiles())
            {
                ct.ThrowIfCancellationRequested();

                var fileName = Path.GetFileName(path);
                if (lockedElsewhere.Contains(fileName)) continue;

                var parsed = TryParse(path, machineSerialNumber, denominationValue);
                if (parsed == null) continue;

                parsed.IsAlreadyLinked = linkedToCurrent.ContainsKey(fileName);
                results.Add(parsed);
                seen.Add(fileName);
            }

            // A file that already backs the result being edited but has since disappeared from the
            // drop folder is still shown, rebuilt from what was stored at import time, so editing a
            // result never silently drops figures that are already part of it.
            foreach (var (fileName, stored) in linkedToCurrent)
            {
                if (seen.Contains(fileName)) continue;
                stored.IsAlreadyLinked = true;
                stored.IsMissingOnDisk = true;
                results.Add(stored);
            }

            return [.. results.OrderByDescending(f => f.GeneratedAt).ThenBy(f => f.FileName)];
        }

        public async Task<BpsXmlImportSelection> ResolveSelectionAsync(
            IEnumerable<string> fileNames, string machineSerialNumber, decimal denominationValue,
            int? currentResultId, CancellationToken ct = default)
        {
            var selection = new BpsXmlImportSelection();

            var wanted = (fileNames ?? [])
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Select(f => f.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (wanted.Count == 0) return selection;

            var selectable = await GetSelectableFilesAsync(machineSerialNumber, denominationValue, currentResultId, ct);
            var byName = selectable.ToDictionary(f => f.FileName, StringComparer.OrdinalIgnoreCase);

            foreach (var name in wanted)
            {
                if (byName.TryGetValue(name, out var file))
                    selection.Files.Add(file);
                else
                    selection.Errors.Add(
                        $"\"{name}\" is no longer available to import — it may have been imported by another " +
                        "result, or it no longer matches this session's machine and denomination.");
            }

            return selection;
        }

        /// <summary>
        /// File names already consumed, split into those taken by other results (unavailable) and
        /// those belonging to the result being edited (offered, pre-ticked).
        /// </summary>
        private async Task<(HashSet<string> LockedElsewhere, Dictionary<string, BpsXmlFileViewModel> LinkedToCurrent)>
            GetLockStateAsync(int? currentResultId, CancellationToken ct)
        {
            var rows = await _db.BpsXmlImports
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Select(x => new
                {
                    x.FileName,
                    x.StockProcessingResultId,
                    x.MachineSerialNumber,
                    x.SiteCode,
                    x.FileGeneratedAt,
                    x.DenominationValue,
                    x.FitPieces,
                    x.UnFitShredPieces,
                    x.FitShredPieces,
                    x.RejectPieces,
                    x.FitValue,
                    x.UnFitShredValue,
                    x.FitShredValue
                })
                .ToListAsync(ct);

            var lockedElsewhere = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var linkedToCurrent = new Dictionary<string, BpsXmlFileViewModel>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                if (currentResultId.HasValue && row.StockProcessingResultId == currentResultId.Value)
                {
                    linkedToCurrent[row.FileName] = new BpsXmlFileViewModel
                    {
                        FileName = row.FileName,
                        MachineSerialNumber = row.MachineSerialNumber,
                        SiteCode = row.SiteCode,
                        GeneratedAt = row.FileGeneratedAt,
                        DenominationValue = row.DenominationValue,
                        FitPieces = row.FitPieces,
                        UnFitShredPieces = row.UnFitShredPieces,
                        FitShredPieces = row.FitShredPieces,
                        RejectPieces = row.RejectPieces,
                        FitValue = row.FitValue,
                        UnFitShredValue = row.UnFitShredValue,
                        FitShredValue = row.FitShredValue
                    };
                }
                else
                {
                    lockedElsewhere.Add(row.FileName);
                }
            }

            return (lockedElsewhere, linkedToCurrent);
        }

        private IEnumerable<string> EnumerateCandidateFiles()
        {
            if (string.IsNullOrWhiteSpace(FolderPath) || !Directory.Exists(FolderPath))
            {
                _logger.LogWarning("BPS XML import folder {Folder} does not exist or is not reachable.", FolderPath);
                return [];
            }

            try
            {
                return Directory.EnumerateFiles(FolderPath, "BPSUnit_*", SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not list BPS XML files in {Folder}.", FolderPath);
                return [];
            }
        }

        /// <summary>
        /// Reads one file and returns its totals, or null when it is not a usable match for the given
        /// machine and denomination. Unreadable or malformed files are skipped rather than thrown,
        /// so one bad drop cannot take the whole picker down.
        /// </summary>
        private BpsXmlFileViewModel? TryParse(string path, string machineSerialNumber, decimal denominationValue)
        {
            var fileName = Path.GetFileName(path);

            var match = FileNamePattern().Match(fileName);
            if (!match.Success) return null;

            var serialFromName = match.Groups["serial"].Value;
            if (!string.Equals(serialFromName, machineSerialNumber, StringComparison.OrdinalIgnoreCase))
                return null;

            if (!DateTime.TryParseExact(match.Groups["ts"].Value, "yyyyMMddHHmmss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var generatedAt))
                return null;

            XDocument doc;
            try
            {
                doc = XDocument.Load(path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipping unreadable BPS XML file {File}.", fileName);
                return null;
            }

            var machine = doc.Root;
            if (machine == null) return null;

            // The serial inside the file wins over the one in the name if the two ever disagree.
            var serialInFile = (string?)machine.Attribute("serialnumber");
            if (!string.IsNullOrWhiteSpace(serialInFile) &&
                !string.Equals(serialInFile.Trim(), machineSerialNumber, StringComparison.OrdinalIgnoreCase))
                return null;

            var counters = machine.Descendants("COUNTER").ToList();
            if (counters.Count == 0) return null;

            // Only whole-file matches are offered. A file mixing denominations is left out entirely
            // rather than part-imported, so nothing is quietly dropped.
            var denomValues = counters
                .Select(c => ParseDecimal((string?)c.Attribute("value")))
                .Distinct()
                .ToList();

            if (denomValues.Count != 1 || denomValues[0] != denominationValue) return null;

            int fitPieces = 0, unfitShredPieces = 0, fitShredPieces = 0, rejectPieces = 0;

            foreach (var counter in counters)
            {
                var pieces = ParseInt((string?)counter.Attribute("number"));
                if (pieces <= 0) continue;

                // oqu carries the machine's output classification (Unfit_Shred, Fit_Shred ...) and is
                // more precise than quality, which reports "Shredded" for the same notes.
                var classification = (string?)counter.Attribute("oqu");
                if (string.IsNullOrWhiteSpace(classification))
                    classification = (string?)counter.Attribute("quality");

                switch ((classification ?? string.Empty).Trim().ToLowerInvariant())
                {
                    case "fit":
                        fitPieces += pieces;
                        break;

                    // Unfit and Unfit_Shred are both notes bound for destruction and are reported as
                    // one figure, so no unfit value is left out of the import.
                    case "unfit":
                    case "unfit_shred":
                    case "unfitshred":
                    case "shredded":
                        unfitShredPieces += pieces;
                        break;

                    case "fit_shred":
                    case "fitshred":
                        fitShredPieces += pieces;
                        break;

                    case "reject":
                        rejectPieces += pieces;
                        break;

                    default:
                        _logger.LogWarning(
                            "Unrecognised note classification \"{Classification}\" in {File}; its {Pieces} pieces were not imported.",
                            classification, fileName, pieces);
                        break;
                }
            }

            return new BpsXmlFileViewModel
            {
                FileName = fileName,
                MachineSerialNumber = machineSerialNumber,
                SiteCode = match.Groups["site"].Value,
                GeneratedAt = generatedAt,
                DenominationValue = denominationValue,
                FitPieces = fitPieces,
                UnFitShredPieces = unfitShredPieces,
                FitShredPieces = fitShredPieces,
                RejectPieces = rejectPieces,
                FitValue = fitPieces * denominationValue,
                UnFitShredValue = unfitShredPieces * denominationValue,
                FitShredValue = fitShredPieces * denominationValue
            };
        }

        private static decimal ParseDecimal(string? raw) =>
            decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;

        private static int ParseInt(string? raw) =>
            int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }
}

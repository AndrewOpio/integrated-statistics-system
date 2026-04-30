using ConfigurationsService.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VMSService.Core.Repositories.WithdrawalRequests;

namespace VMSService.Core.Repositories.WithdrawalTransactionRefNumberGenerators
{
    public class WithdrawalTransactionRefNumberGenerator(
        IWithdrawalRequestRepository _requestRepo,
        IConfigurationsServiceClient _configServiceClient
        )
        : IWithdrawalTransactionRefNumberGenerator
    {
        private const string Prefix = "W";

        public async Task<string> GenerateAsync(
            int bouBranchId,
            string bouBranchCode,
            int cbId,
            string cbCode,
            DateOnly collectionDate,
            CancellationToken ct = default)
        {
            // Sequence resets per CB + bank branch + collection date
            var existingCount = await _requestRepo.CountByCbBranchAndDateAsync(
                cbId, bouBranchId, collectionDate, ct);

            var sequence = (existingCount + 1).ToString("D2");
            var collectiondate = collectionDate.ToString("ddMMyy");

            // Format: {BouBranchName}{CbCode}{W}{Date}{Sequence}
            // e.g.   KLANACMEW25032501
            return $"{Sanitize(bouBranchCode)}{Sanitize(cbCode)}{Prefix}{collectiondate}{sequence}";
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            return Regex.Replace(value.ToUpperInvariant(), @"[^A-Z0-9]", "");
        }
    }
}

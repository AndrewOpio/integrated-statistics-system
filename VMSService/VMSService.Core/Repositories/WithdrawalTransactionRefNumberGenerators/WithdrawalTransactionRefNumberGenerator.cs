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
        private const string Prefix = "WR";

        public async Task<string> GenerateAsync(int bouBranchId, int cbId, DateOnly collectionDate, CancellationToken ct = default)
        {
            // Resolve branch details from microservice
            var cbBranches = await _configServiceClient.GetCbBranchesByCbIdAsync(2);
            var bouBranches = await _configServiceClient.GetAllBouBranchesAsync();

            var cbBranch = cbBranches.FirstOrDefault(b => b.Id == cbId)
                ?? throw new InvalidOperationException($"Company branch {cbId} not found.");

            var bouBranch = bouBranches.FirstOrDefault(b => b.Id == bouBranchId)
                ?? throw new InvalidOperationException($"Bank branch {bouBranchId} not found.");

            // Sequence resets per company + bank branch + collection date
            var existingCount = await _requestRepo.CountByCbBranchAndDateAsync(
                cbId, bouBranchId, collectionDate, ct);

            var sequence = (existingCount + 1).ToString("D2");
            var date = collectionDate.ToString("yyyyMMdd");

            // Format:
            return $"{Sanitize(bouBranch.Name)}{Sanitize(cbBranch.Location)}{Prefix}{date}{sequence}";
        }


        //private static string Sanitize(string value)
        //    => value.Trim().ToUpperInvariant().Replace(" ", "-");


        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return Regex.Replace(value.ToUpperInvariant(), @"[^A-Z0-9]", "");
        }
    }
}

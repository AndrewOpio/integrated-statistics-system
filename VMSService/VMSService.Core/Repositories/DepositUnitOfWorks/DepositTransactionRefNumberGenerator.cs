using ConfigurationsService.Client;
using System.Text.RegularExpressions;

namespace VMSService.Core.Repositories.DepositUnitOfWorks
{
    public class DepositTransactionRefNumberGenerator(
        IDepositRequestRepository _requestRepo,
        IConfigurationsServiceClient _configServiceClient
        )
        : IDepositTransactionRefNumberGenerator
    {
        private const string Prefix = "D";

        public async Task<string> GenerateAsync(int bouBranchId, int cbId, DateOnly depositDate, CancellationToken ct = default)
        {
            // Resolve branch details from microservice
            var cb = await _configServiceClient.GetCbIdAsync(cbId);
            var bouBranch = await _configServiceClient.GetBouBranchByIdAsync(bouBranchId);

            // Sequence resets per commercial bank + bank branch + deposit date
            var existingCount = await _requestRepo.CountByCbBranchAndDateAsync(
                cbId, bouBranchId, depositDate, ct);

            var sequence = (existingCount + 1).ToString("D2");
            var date = depositDate.ToString("yyyyMMdd");

            // Format:
            return $"{Sanitize(bouBranch.BranchCode)}{Sanitize(cb.CBCode)}{Prefix}{date}{sequence}";
        }


        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return Regex.Replace(value.ToUpperInvariant(), @"[^A-Z0-9]", "");
        }
    }
}

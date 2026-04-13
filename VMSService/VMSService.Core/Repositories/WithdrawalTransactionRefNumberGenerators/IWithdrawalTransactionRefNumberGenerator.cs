using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMSService.Core.Repositories.WithdrawalTransactionRefNumberGenerators
{
    public interface IWithdrawalTransactionRefNumberGenerator
    {
        Task<string> GenerateAsync(
            int bouBranchId,
            string bouBranchCode,
            int cbId,
            string cbCode,
            DateOnly collectionDate,
        CancellationToken ct = default);
    }
}

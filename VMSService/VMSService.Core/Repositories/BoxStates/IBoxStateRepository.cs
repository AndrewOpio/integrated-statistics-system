using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.BoxStates;
public interface IBoxStateRepository
{
    Task<BoxState?> FindByBoxAndDenominationAsync(string caseNumber, int denominationId, CancellationToken ct = default);
    Task<(BoxState? State, string SerialPrefix)> FindByBoxAndDenominationWithPrefixAsync(string caseNumber, int denominationId, CancellationToken ct);
    Task<List<BoxState>> FindManyForReversalAsync(IEnumerable<(string CaseNumber, int DenominationId)> keys, CancellationToken ct = default);
    Task<List<BoxState>> FindForRequestAsync(Guid requestId, CancellationToken ct = default);
}

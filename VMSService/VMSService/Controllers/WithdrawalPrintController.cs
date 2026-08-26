using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Models.CbWithdrawRequest;
using VMSService.Core.Models.WithdrawalRequests;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Services.Identity;
using VMSService.Core.Services.Workflows;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Web.Controllers
{
    // Ticket 23: PrintRequest is the one deliberate exception to the CB/BOU controller split —
    // read-only, renders identically regardless of who's viewing it, so duplicating it into a
    // second BOU-side action would just create drift risk between two copies of the same
    // rendering logic. Lives outside the Cb/Bou split entirely, gated by the same ownership check
    // ticket 08 built (own-bank CB, or any BOU role) rather than by CbOnly/BouOnly — neither of
    // which fits a controller that both sides are meant to reach. Still needs a bare [Authorize]
    // though: this app has no global fallback authorization policy (see ticket 09, finding #1),
    // so an unguarded controller is reachable unauthenticated.
    [Authorize]
    public class WithdrawalPrintController(
        IAuthorizationService authorizationService,
        IAgentDetailRepository _agentRepo,
        IConfigurationsServiceClient _configServiceClient,
        IWithdrawalUnitOfWork _uow,
        IWithdrawalStatusResolver _statusResolver,
        IIdentityService _identityService,
        IMapper _mapper) : BaseWithdrawController(authorizationService)
    {
        public async Task<IActionResult> PrintRequest(Guid id, CancellationToken ct)
        {
            var request = await _uow.Requests.GetWithDetailsAsync(id, ct);
            if (request is null) return NotFound();
            if (!await CanAccessRequestAsync(request.CbId)) return Forbid();

            if (request.StatusTypeName != WithdrawalStatusNames.Completed
                && request.StatusTypeName != WithdrawalStatusNames.Approved
                && request.StatusTypeName != WithdrawalStatusNames.PendingApproval)
                return RedirectToAction("Details", "CbWithdrawRequests", new { id });

            var cb = await _configServiceClient.GetCbIdAsync(request.CbId);
            var bouBranch = await _configServiceClient.GetBouBranchByIdAsync(request.BouBranchId);

            // Agents
            var agentIds = request.WithdrawalRequestAgents
                .Where(a => a.EnteredBy == WithdrawalEnteredBy.Cb)
                .Select(a => a.AgentId)
                .ToList();

            var agents = new List<AgentDetailViewModel>();
            foreach (var agentId in agentIds)
            {
                var agent = await _agentRepo.FindById(agentId);
                if (agent is not null)
                    agents.Add(_mapper.Map<AgentDetailViewModel>(agent));
            }

            // BOU dispatch denominations
            var bankRows = request.WithdrawalRequestDenominations
                .Where(d => d.Origin == WithdrawalOrigin.Bou)
                .ToList();

            var newBanknotes = new List<BouDispatchCardViewModel>();
            var reissueNotes = new List<BouDispatchSimpleViewModel>();
            var coins = new List<BouDispatchSimpleViewModel>();

            foreach (var row in bankRows)
            {
                if (string.Equals(row.BundleType, BundleTypes.New, StringComparison.OrdinalIgnoreCase))
                {
                    var details = row.DispatchedBoxes?
                        .OrderBy(b => int.TryParse(b.CaseNumber, out var n) ? n : int.MaxValue)
                        .ToList() ?? [];

                    if (!details.Any()) continue;

                    var bundlesTotal = row.Bundles ?? 0;
                    var isFull = bundlesTotal == details.Count * 50;
                    var firstBox = int.TryParse(details.First().CaseNumber, out var fn) ? fn : 0;
                    var lastBox = int.TryParse(details.Last().CaseNumber, out var ln) ? ln : 0;

                    newBanknotes.Add(new BouDispatchCardViewModel
                    {
                        Id = row.Id,
                        DenominationId = row.DenominationId,
                        Denomination = row.DenominationValue,
                        StartBox = firstBox,
                        EndBox = lastBox,
                        IsFull = isFull,
                        Bundles = bundlesTotal,
                        SerialStart = details.First().SerialStartTaken,
                        SerialEnd = details.Last().SerialEndTaken,
                        SerialPrefix = details.First().SerialPrefix,
                        Amount = row.Amount,
                    });
                }
                else if (string.Equals(row.BundleType, BundleTypes.Reissue, StringComparison.OrdinalIgnoreCase))
                {
                    reissueNotes.Add(new BouDispatchSimpleViewModel
                    {
                        Id = row.Id,
                        DenominationId = row.DenominationId,
                        Denomination = row.DenominationValue,
                        Quantity = row.Bundles ?? 0,
                        Amount = row.Amount,
                    });
                }
                else if (string.Equals(row.BundleType, BundleTypes.Coin, StringComparison.OrdinalIgnoreCase))
                {
                    coins.Add(new BouDispatchSimpleViewModel
                    {
                        Id = row.Id,
                        DenominationId = row.DenominationId,
                        Denomination = row.DenominationValue,
                        Quantity = row.Boxes ?? 0,
                        Amount = row.Amount,
                    });
                }
                else
                {
                    // BundleType = "NA" — copied from CB before Panel A entered dispatch.
                    // Fall back: notes have Bundles, coins have Boxes.
                    if (row.Bundles.HasValue && row.Bundles > 0)
                        reissueNotes.Add(new BouDispatchSimpleViewModel
                        {
                            Id = row.Id,
                            DenominationId = row.DenominationId,
                            Denomination = row.DenominationValue,
                            Quantity = row.Bundles.Value,
                            Amount = row.Amount,
                        });
                    else if (row.Boxes.HasValue && row.Boxes > 0)
                        coins.Add(new BouDispatchSimpleViewModel
                        {
                            Id = row.Id,
                            DenominationId = row.DenominationId,
                            Denomination = row.DenominationValue,
                            Quantity = row.Boxes.Value,
                            Amount = row.Amount,
                        });
                }
            }

            // Panel names from status history
            var history = request.WithdrawalRequestStatusHistory.Where(h => !h.IsDeleted).ToList();

            var panelAStatusId = await _statusResolver.GetStatusIdAsync(WithdrawalStatusNames.PendingApproval);
            var panelBStatusId = await _statusResolver.GetStatusIdAsync(WithdrawalStatusNames.Approved);

            string panelAName = string.Empty;
            string panelBName = string.Empty;

            var panelAEntry = history.FirstOrDefault(h => h.NewStatusId == panelAStatusId);
            if (panelAEntry is not null)
            {
                var user = await _identityService.GetUserByUsername(panelAEntry.ChangedBy);
                if (user is not null)
                    panelAName = System.Text.RegularExpressions.Regex.Replace(
                        $"{user.FirstName} {user.MiddleName} {user.LastName}".Trim(), @"\s+", " ");
            }

            var panelBEntry = history.FirstOrDefault(h => h.NewStatusId == panelBStatusId);
            if (panelBEntry is not null)
            {
                var user = await _identityService.GetUserByUsername(panelBEntry.ChangedBy);
                if (user is not null)
                    panelBName = System.Text.RegularExpressions.Regex.Replace(
                        $"{user.FirstName} {user.MiddleName} {user.LastName}".Trim(), @"\s+", " ");
            }

            var vm = new WithdrawalPrintViewModel
            {
                TransactionRefNumber = request.TransactionRefNumber,
                CollectionDate = request.CollectionDate,
                RtgsDate = request.FundSettlementValueDate,
                RtgsCode = request.RtgsCode,
                CbName = cb?.Name ?? "—",
                BouBranchName = bouBranch?.Name ?? "—",
                BouBranchCode = request.BouBranchCode,
                TotalAmount = request.TotalAmount,
                TotalNotesAmount = request.TotalNotesAmount,
                TotalCoinsAmount = request.TotalCoinsAmount,
                Agents = agents,
                NewBanknotes = newBanknotes.OrderBy(n => n.Denomination).ThenBy(n => n.StartBox).ToList(),
                ReissueBanknotes = reissueNotes.OrderBy(n => n.Denomination).ToList(),
                Coins = coins.OrderBy(c => c.Denomination).ToList(),
                PanelAName = panelAName,
                PanelBName = panelBName,
                RequiresSpecialApproval = request.RequiresSpecialApproval,
            };

            return View(vm);
        }
    }
}

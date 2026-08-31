using VMSService.Core.Repositories.AppliedPenalties;
using VMSService.Core.Repositories.Penalties;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Services.Penalties;
using VMSService.Core.Static;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Web.BackgroundServices;

/// <summary>
/// Penalty #5 ("Failure to amend in time"): a request the ABM returned for revision before 11:00
/// EAT, still sitting unresubmitted at 11:00, owes the penalty — a return at/after 11:00 is BOU's
/// own lateness and is waived instead (computed on the BOU Details page, never applied here).
/// Mirrors <see cref="WithdrawalExpiryService"/>'s daily-EAT-cutoff shape.
/// </summary>
public class WithdrawalAmendmentPenaltyService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WithdrawalAmendmentPenaltyService> _logger;

    private static readonly TimeOnly CutoffTime = new(11, 0, 0);

    public WithdrawalAmendmentPenaltyService(IServiceScopeFactory scopeFactory, ILogger<WithdrawalAmendmentPenaltyService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // If the app starts after 11am EAT, run the check immediately to catch requests missed
        // while the server was down — each check is idempotent per request (guarded against
        // double-application), so running it more than once for the same request is harmless.
        if (NowEat().TimeOfDay >= CutoffTime.ToTimeSpan())
            await ApplyOverdueAmendmentPenaltiesAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNext11amEat();
            _logger.LogInformation("WithdrawalAmendmentPenaltyService: next check in {Delay:hh\\:mm\\:ss}", delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await ApplyOverdueAmendmentPenaltiesAsync(stoppingToken);
        }
    }

    // EAT is UTC+3 with no daylight saving — a fixed offset is sufficient.
    private static DateTime NowEat() => DateTime.UtcNow.AddHours(3);

    private static TimeSpan TimeUntilNext11amEat()
    {
        var nowEat = NowEat();
        var next11am = nowEat.Date.Add(CutoffTime.ToTimeSpan());
        if (nowEat >= next11am)
            next11am = next11am.AddDays(1);
        return next11am - nowEat;
    }

    private async Task ApplyOverdueAmendmentPenaltiesAsync(CancellationToken ct)
    {
        _logger.LogInformation("WithdrawalAmendmentPenaltyService: running check at {Time} EAT", NowEat());

        await using var scope = _scopeFactory.CreateAsyncScope();
        var uow               = scope.ServiceProvider.GetRequiredService<IWithdrawalUnitOfWork>();
        var penaltyRepo        = scope.ServiceProvider.GetRequiredService<IPenaltyRepository>();
        var appliedPenaltyRepo = scope.ServiceProvider.GetRequiredService<IAppliedPenaltyRepository>();

        try
        {
            var appliedCount = await ApplyOverdueAmendmentPenaltiesAsync(uow, penaltyRepo, appliedPenaltyRepo, _logger, ct);
            _logger.LogInformation("WithdrawalAmendmentPenaltyService: applied {Count} penalty(ies)", appliedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WithdrawalAmendmentPenaltyService: error during run");
        }
    }

    /// <summary>
    /// The actual check, factored out from scope/logging plumbing so it's directly callable from
    /// tests without exercising <see cref="BackgroundService"/>'s timer machinery. Returns the
    /// number of penalties applied.
    /// </summary>
    public static async Task<int> ApplyOverdueAmendmentPenaltiesAsync(
        IWithdrawalUnitOfWork uow, IPenaltyRepository penaltyRepo, IAppliedPenaltyRepository appliedPenaltyRepo,
        ILogger logger, CancellationToken ct)
    {
        var candidates = await uow.Requests.GetReturnedForRevisionAsync(ct);

        var appliedCount = 0;
        foreach (var request in candidates)
        {
            var lastReturn = await uow.StatusHistory.FindMostRecentByNewStatusNameAsync(
                request.Id, WithdrawalStatusNames.ReturnedForRevision, ct);
            if (lastReturn is null || lastReturn.OldStatusName != WithdrawalStatusNames.PendingVerification)
                continue;

            var returnedAtEat = lastReturn.ChangedAt.AddHours(3);
            if (returnedAtEat.TimeOfDay >= CutoffTime.ToTimeSpan()) continue;

            var applied = await AutomaticPenaltyApplier.ApplyIfNotAlreadyAppliedAsync(
                penaltyRepo, appliedPenaltyRepo, logger,
                PenaltyConstants.Source.Withdrawal, request.TransactionRefNumber,
                PenaltyConstants.AutomaticPenalties.FailureToAmendInTime,
                "ABM returned before 11:00 EAT; not resubmitted by the 11:00 deadline.");
            if (applied) appliedCount++;
        }

        return appliedCount;
    }
}

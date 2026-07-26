using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Services.Workflows;
using VMSService.Data.Entities;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Web.BackgroundServices;

public class WithdrawalExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WithdrawalExpiryService> _logger;

    public WithdrawalExpiryService(IServiceScopeFactory scopeFactory, ILogger<WithdrawalExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // If the app starts after 3 pm EAT, run the expiry check immediately
        // to catch requests that were missed while the server was down.
        if (NowEat().TimeOfDay >= TimeSpan.FromHours(15))
            await ExpireOverdueRequestsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNext3pmEat();
            _logger.LogInformation("WithdrawalExpiryService: next expiry run in {Delay:hh\\:mm\\:ss}", delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await ExpireOverdueRequestsAsync(stoppingToken);
        }
    }

    // EAT is UTC+3 with no daylight saving — a fixed offset is sufficient.
    private static DateTime NowEat() => DateTime.UtcNow.AddHours(3);

    private static TimeSpan TimeUntilNext3pmEat()
    {
        var nowEat = NowEat();
        var next3pm = nowEat.Date.AddHours(15);
        if (nowEat >= next3pm)
            next3pm = next3pm.AddDays(1);
        return next3pm - nowEat;
    }

    private async Task ExpireOverdueRequestsAsync(CancellationToken ct)
    {
        _logger.LogInformation("WithdrawalExpiryService: running expiry check at {Time} EAT", NowEat());

        await using var scope = _scopeFactory.CreateAsyncScope();
        var uow            = scope.ServiceProvider.GetRequiredService<IWithdrawalUnitOfWork>();
        var statusResolver = scope.ServiceProvider.GetRequiredService<IWithdrawalStatusResolver>();

        try
        {
            var expiredStatusId = await statusResolver.GetStatusIdAsync(WithdrawalStatusNames.Expired);
            var requests        = (await uow.Requests.GetPendingRtgsWithoutCodeAsync(ct)).ToList();

            if (requests.Count == 0)
            {
                _logger.LogInformation("WithdrawalExpiryService: no requests to expire");
                return;
            }

            foreach (var request in requests)
            {
                var oldStatusId   = request.StatusTypeId;
                var oldStatusName = request.StatusTypeName;

                request.StatusTypeId   = expiredStatusId;
                request.StatusTypeName = WithdrawalStatusNames.Expired;
                request.ModifiedBy     = "System";
                request.ModifiedDate   = DateTime.Now;

                await uow.StatusHistory.Create(new WithdrawalRequestStatusHistory
                {
                    WithdrawalRequestId = request.Id,
                    OldStatusId         = oldStatusId,
                    OldStatusName       = oldStatusName,
                    NewStatusId         = expiredStatusId,
                    NewStatusName       = WithdrawalStatusNames.Expired,
                    ChangedBy           = "System",
                    ChangedAt           = DateTime.UtcNow,
                    Remarks             = "Automatically expired: RTGS code was not provided by 3:00 PM EAT.",
                    CreatedBy           = "System",
                    CreatedDate         = DateTime.Now,
                });
            }

            await uow.SaveAsync(ct);
            _logger.LogInformation("WithdrawalExpiryService: expired {Count} request(s)", requests.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WithdrawalExpiryService: error during expiry run");
        }
    }
}

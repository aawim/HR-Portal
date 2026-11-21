using HRM.Api.Infrastructure;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Leave;

public sealed class LeaveAccrualWorker(IServiceScopeFactory scopes, ILogger<LeaveAccrualWorker> logger) : BackgroundService
{
    private const string AppLockResource = "HRM_LeaveAccrualWorker";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Allow host startup to settle before running the first scheduled pass.
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HrmTeContext>>();
                await using var db = await factory.CreateDbContextAsync(stoppingToken);

                await using var lockLease = await SqlAppLock.TryAcquireAsync(db, AppLockResource, stoppingToken);
                if (lockLease == null)
                {
                    logger.LogDebug("Leave accrual worker skipped pass because another instance holds lock {LockResource}.", AppLockResource);
                    continue;
                }

                var rules = await db.JobTypeLeaveAccrualRules.AsNoTracking()
                    .Where(x => x.IsEnabled && x.CarryForwardMode != "Unspecified" && x.EffectiveFrom <= LeaveAccrualCalculator.Today)
                    .Select(x => new { x.Id, x.OrganisationId }).ToListAsync(stoppingToken);
                foreach (var rule in rules)
                {
                    if (stoppingToken.IsCancellationRequested) break;
                    try { await new LeaveAccrualProcessor(factory).RunAsync(rule.OrganisationId, rule.Id); }
                    catch (Exception ex) { logger.LogError(ex, "Leave accrual failed for tenant {Tenant}, rule {Rule}", rule.OrganisationId, rule.Id); }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Leave accrual scheduler could not load enabled rules"); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

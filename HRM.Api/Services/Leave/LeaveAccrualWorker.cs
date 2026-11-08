using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Leave;

public sealed class LeaveAccrualWorker(IServiceScopeFactory scopes, ILogger<LeaveAccrualWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HrmTeContext>>();
                await using var db = await factory.CreateDbContextAsync(stoppingToken);
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

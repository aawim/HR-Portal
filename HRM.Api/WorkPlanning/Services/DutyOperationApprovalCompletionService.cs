using HRM.Models;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRM.WorkPlanning.Services;

/// <summary>Completes approved duty-operation requests created by the workforce workflow without a second scheduler.</summary>
public sealed class DutyOperationApprovalCompletionService(IServiceScopeFactory scopes, ILogger<DutyOperationApprovalCompletionService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await ApplyApprovedOperationsAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Failed to poll approved duty operations."); }
        }
    }

    private async Task ApplyApprovedOperationsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HrmTeContext>();
        var operations = await db.DutyOperations.AsNoTracking().Where(x => x.Status == "Requested" && x.Request.RequestStateId == 4).Select(x => x.Id).Take(100).ToListAsync(cancellationToken);
        var applicator = scope.ServiceProvider.GetRequiredService<IDutyOperationService>();
        foreach (var operationId in operations)
        {
            try { await applicator.ApplyApprovedAsync(operationId, cancellationToken); }
            catch (Exception ex) { logger.LogError(ex, "Failed to apply approved duty operation {DutyOperationId}", operationId); }
        }
    }
}

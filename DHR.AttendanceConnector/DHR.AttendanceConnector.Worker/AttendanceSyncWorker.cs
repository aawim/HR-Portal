using DHR.AttendanceConnector.Core.Interfaces;
using DHR.AttendanceConnector.Core.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DHR.AttendanceConnector.Worker;

public sealed class AttendanceSyncWorker(
    IConnectorRepository repository,
    AttendanceSyncService syncService,
    ILogger<AttendanceSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await repository.InitializeAsync(stoppingToken);
        logger.LogInformation("AttendanceSyncWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var profile = await repository.GetProfileAsync(stoppingToken);
                if (profile is not null)
                {
                    await syncService.DrainPendingAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in AttendanceSyncWorker loop.");
            }

            try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }

        logger.LogInformation("AttendanceSyncWorker stopped gracefully.");
    }
}

using DHR.AttendanceConnector.Core.Interfaces;
using DHR.AttendanceConnector.Core.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DHR.AttendanceConnector.Worker;

public sealed class CommandWorker(
    IConnectorRepository repository,
    CommandExecutionService commandService,
    ILogger<CommandWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await repository.InitializeAsync(stoppingToken);
        logger.LogInformation("CommandWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var profile = await repository.GetProfileAsync(stoppingToken);
                if (profile is not null)
                {
                    await commandService.PollAndExecuteAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Command polling failed.");
            }

            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }

        logger.LogInformation("CommandWorker stopped gracefully.");
    }
}

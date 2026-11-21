using System;
using System.Threading;
using System.Threading.Tasks;
using HRM.Application.Features.Payroll;
using HRM.Models.Payroll;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HRM.ErrorLogs.Models;
using HRM.ErrorLogs.Services;

namespace HRM.Payroll.Worker;

public sealed class Worker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Worker> _logger;
    private readonly IErrorLogService? _errorLogService;
    private readonly string _workerInstanceId = $"Worker-{Environment.MachineName}-{Guid.NewGuid():N}";
    private readonly TimeSpan _leaseDuration = TimeSpan.FromMinutes(2);

    public Worker(IServiceProvider serviceProvider, ILogger<Worker> logger, IErrorLogService? errorLogService = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _errorLogService = errorLogService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payroll Worker instance {InstanceId} starting...", _workerInstanceId);

        using var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        DateTime lastRecoveryCheck = DateTime.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var queueService = scope.ServiceProvider.GetRequiredService<IPayrollQueueService>();
                var processor = scope.ServiceProvider.GetRequiredService<IPayrollProcessor>();

                // Periodically recover expired/orphaned jobs
                if (DateTime.UtcNow - lastRecoveryCheck > TimeSpan.FromSeconds(30))
                {
                    lastRecoveryCheck = DateTime.UtcNow;
                    await queueService.RecoverExpiredJobsAsync();
                }

                // Claim next pending or orphaned job
                var job = await queueService.ClaimNextJobAsync(_workerInstanceId, _leaseDuration, stoppingToken);
                if (job != null)
                {
                    _logger.LogInformation("Worker {Instance} picked up job {JobId} (Type: {Type}, RunId: {RunId}, OrgId: {OrgId})",
                        _workerInstanceId, job.JobId, job.JobType, job.PayrollRunId, job.OrganisationId);

                    await ProcessJobWithHeartbeatAsync(job, queueService, processor, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Payroll Worker loop");
            }

            try
            {
                await periodicTimer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Payroll Worker instance {InstanceId} stopped.", _workerInstanceId);
    }

    private async Task ProcessJobWithHeartbeatAsync(
        PayrollBackgroundJob job,
        IPayrollQueueService queueService,
        IPayrollProcessor processor,
        CancellationToken stoppingToken)
    {
        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var heartbeatToken = heartbeatCts.Token;

        // Start heartbeat renewal loop
        var heartbeatTask = Task.Run(async () =>
        {
            using var heartbeatTimer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            while (!heartbeatToken.IsCancellationRequested)
            {
                try
                {
                    if (await heartbeatTimer.WaitForNextTickAsync(heartbeatToken))
                    {
                        bool renewed = await queueService.RenewLeaseAsync(job.JobId, _workerInstanceId, job.FencingToken, _leaseDuration);
                        if (!renewed)
                        {
                            _logger.LogWarning("Lost lease for job {JobId}. Aborting heartbeat.", job.JobId);
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Heartbeat failure for job {JobId}", job.JobId);
                }
            }
        }, heartbeatToken);

        try
        {
            if (job.PayrollRunId.HasValue)
            {
                switch (job.JobType)
                {
                    case PayrollJobType.CalculateBatch:
                    case PayrollJobType.PrepareSnapshot:
                        await processor.ProcessRunBatchesAsync(job.OrganisationId, job.PayrollRunId.Value, stoppingToken);
                        break;
                    default:
                        _logger.LogInformation("No special handler needed for job type {Type}", job.JobType);
                        break;
                }
            }

            // Stop heartbeat and mark complete
            await heartbeatCts.CancelAsync();
            try { await heartbeatTask; } catch (OperationCanceledException) { }

            await queueService.CompleteJobAsync(job.JobId, _workerInstanceId, job.FencingToken);
            _logger.LogInformation("Successfully completed job {JobId}", job.JobId);
        }
        catch (Exception ex)
        {
            await heartbeatCts.CancelAsync();
            try { await heartbeatTask; } catch (OperationCanceledException) { }

            _logger.LogError(ex, "Execution failed for job {JobId}", job.JobId);
            if (_errorLogService != null)
            {
                try
                {
                    await _errorLogService.CaptureExceptionAsync(ex, ErrorCriticalityLevel.Critical, new ErrorContext
                    {
                        ApplicationSource = "HRM.Payroll.Worker",
                        PageLocation = $"PayrollBackgroundJob:{job.JobType}",
                        HttpMethod = "BACKGROUND_WORKER",
                        CustomDataJson = $"{{\"JobId\": {job.JobId}, \"JobType\": \"{job.JobType}\", \"PayrollRunId\": {job.PayrollRunId}, \"OrgId\": {job.OrganisationId}}}"
                    });
                }
                catch { }
            }
            await queueService.FailJobAsync(job.JobId, _workerInstanceId, job.FencingToken, ex.Message, allowRetry: true);
        }
    }
}

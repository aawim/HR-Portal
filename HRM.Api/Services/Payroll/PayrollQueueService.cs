using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HRM.Application.Features.Payroll;
using HRM.Models;
using HRM.Models.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRM.Services.Payroll;

public sealed class PayrollQueueService : IPayrollQueueService
{
    private readonly IDbContextFactory<HrmTeContext> _factory;
    private readonly ILogger<PayrollQueueService> _logger;

    public PayrollQueueService(IDbContextFactory<HrmTeContext> factory, ILogger<PayrollQueueService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<long> EnqueueJobAsync(int organisationId, int? payrollRunId, PayrollJobType jobType, string? payloadJson = null, int priority = 100)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var job = new PayrollBackgroundJob
        {
            OrganisationId = organisationId,
            PayrollRunId = payrollRunId,
            JobType = jobType,
            Status = PayrollJobStatus.Pending,
            Priority = priority,
            AttemptCount = 0,
            MaxAttempts = 3,
            CreatedAtUtc = DateTime.UtcNow,
            PayloadJson = payloadJson,
            FencingToken = 1
        };

        db.PayrollBackgroundJobs.Add(job);
        await db.SaveChangesAsync();

        _logger.LogInformation("Enqueued payroll job {JobId} of type {JobType} for Org {Org}, Run {Run}", job.JobId, jobType, organisationId, payrollRunId);
        return job.JobId;
    }

    public async Task<PayrollBackgroundJob?> ClaimNextJobAsync(string workerInstanceId, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var innerDb = await _factory.CreateDbContextAsync(cancellationToken);
            var now = DateTime.UtcNow;
            var leaseExpiry = now.Add(leaseDuration);

            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
            if (innerDb.Database.IsRelational())
            {
                tx = await innerDb.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
            }

            try
            {
                var job = await innerDb.PayrollBackgroundJobs
                    .Where(j => j.Status == PayrollJobStatus.Pending ||
                               (j.Status == PayrollJobStatus.Processing && j.LeaseExpiresAtUtc.HasValue && j.LeaseExpiresAtUtc < now && j.AttemptCount < j.MaxAttempts))
                    .OrderBy(j => j.Priority)
                    .ThenBy(j => j.CreatedAtUtc)
                    .FirstOrDefaultAsync(cancellationToken);

                if (job == null)
                {
                    if (tx != null) await tx.RollbackAsync(cancellationToken);
                    return null;
                }

                job.Status = PayrollJobStatus.Processing;
                job.LeaseOwner = workerInstanceId;
                job.LeaseExpiresAtUtc = leaseExpiry;
                job.LastHeartbeatAtUtc = now;
                job.StartedAtUtc ??= now;
                job.AttemptCount += 1;
                job.FencingToken += 1;

                await innerDb.SaveChangesAsync(cancellationToken);
                if (tx != null) await tx.CommitAsync(cancellationToken);

                _logger.LogInformation("Worker {Worker} claimed job {JobId} with fencing token {Token}", workerInstanceId, job.JobId, job.FencingToken);
                return job;
            }
            catch (Exception ex)
            {
                if (tx != null) await tx.RollbackAsync(cancellationToken);
                _logger.LogWarning(ex, "Failed to claim next payroll job");
                return null;
            }
            finally
            {
                if (tx != null) await tx.DisposeAsync();
            }
        });
    }

    public async Task<bool> RenewLeaseAsync(long jobId, string workerInstanceId, long fencingToken, TimeSpan leaseDuration)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var job = await db.PayrollBackgroundJobs.SingleOrDefaultAsync(j => j.JobId == jobId);
        if (job == null || job.LeaseOwner != workerInstanceId || job.FencingToken != fencingToken || job.Status != PayrollJobStatus.Processing)
        {
            _logger.LogWarning("Worker {Worker} failed to renew lease for job {JobId}: lease expired or token mismatch", workerInstanceId, jobId);
            return false;
        }

        job.LeaseExpiresAtUtc = now.Add(leaseDuration);
        job.LastHeartbeatAtUtc = now;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task CompleteJobAsync(long jobId, string workerInstanceId, long fencingToken)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var job = await db.PayrollBackgroundJobs.SingleOrDefaultAsync(j => j.JobId == jobId);
        if (job == null || job.LeaseOwner != workerInstanceId || job.FencingToken != fencingToken)
        {
            _logger.LogWarning("Worker {Worker} attempted to complete job {JobId} with invalid token {Token}", workerInstanceId, jobId, fencingToken);
            return;
        }

        job.Status = PayrollJobStatus.Completed;
        job.CompletedAtUtc = DateTime.UtcNow;
        job.ErrorMessage = null;
        await db.SaveChangesAsync();
        _logger.LogInformation("Job {JobId} completed by worker {Worker}", jobId, workerInstanceId);
    }

    public async Task FailJobAsync(long jobId, string workerInstanceId, long fencingToken, string errorMessage, bool allowRetry = true)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var job = await db.PayrollBackgroundJobs.SingleOrDefaultAsync(j => j.JobId == jobId);
        if (job == null || job.LeaseOwner != workerInstanceId || job.FencingToken != fencingToken)
        {
            return;
        }

        job.ErrorMessage = errorMessage.Length > 2000 ? errorMessage[..2000] : errorMessage;
        if (allowRetry && job.AttemptCount < job.MaxAttempts)
        {
            job.Status = PayrollJobStatus.Pending; // Retry eligible
            job.LeaseExpiresAtUtc = null;
            job.LeaseOwner = null;
            _logger.LogWarning("Job {JobId} failed (attempt {Attempt}/{Max}); scheduled retry. Error: {Error}", jobId, job.AttemptCount, job.MaxAttempts, errorMessage);
        }
        else
        {
            job.Status = PayrollJobStatus.Failed;
            job.CompletedAtUtc = DateTime.UtcNow;
            _logger.LogError("Job {JobId} permanently failed. Error: {Error}", jobId, errorMessage);
        }

        await db.SaveChangesAsync();
    }

    public async Task<int> RecoverExpiredJobsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var expired = await db.PayrollBackgroundJobs
            .Where(j => j.Status == PayrollJobStatus.Processing && j.LeaseExpiresAtUtc.HasValue && j.LeaseExpiresAtUtc < now)
            .ToListAsync();

        int recoveredCount = 0;
        foreach (var job in expired)
        {
            if (job.AttemptCount < job.MaxAttempts)
            {
                job.Status = PayrollJobStatus.Pending;
                job.LeaseOwner = null;
                job.LeaseExpiresAtUtc = null;
                recoveredCount++;
            }
            else
            {
                job.Status = PayrollJobStatus.Failed;
                job.CompletedAtUtc = now;
                job.ErrorMessage = "Worker lease expired and max retry attempts exceeded.";
            }
        }

        if (recoveredCount > 0)
        {
            await db.SaveChangesAsync();
            _logger.LogInformation("Recovered {Count} expired payroll background jobs", recoveredCount);
        }

        return recoveredCount;
    }
}

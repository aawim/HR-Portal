using HRM.Api.Infrastructure;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Attendance.Deductions;

public sealed class AttendanceDeductionWorker(IServiceScopeFactory scopes, ILogger<AttendanceDeductionWorker> logger) : BackgroundService
{
    private const string AppLockResource = "HRM_AttendanceDeductionWorker";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Allow host startup to settle before running the first scheduled pass.
            await Task.Delay(TimeSpan.FromSeconds(25), stoppingToken);
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
                await RunPassAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Attendance deduction background worker pass failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunPassAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HrmTeContext>>();
        var service = scope.ServiceProvider.GetRequiredService<AttendanceDeductionService>();

        await using var db = await factory.CreateDbContextAsync(ct);
        await using var lockLease = await SqlAppLock.TryAcquireAsync(db, AppLockResource, ct);
        if (lockLease == null)
        {
            logger.LogDebug("Attendance deduction worker skipped pass because another instance holds lock {LockResource}.", AppLockResource);
            return;
        }

        // Maldives local date (UTC+5); evaluate up through yesterday so ongoing shifts are not flagged as absent.
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5));
        var yesterday = today.AddDays(-1);
        var defaultStart = new DateOnly(today.Year, today.Month, 1);
        if (defaultStart > yesterday) defaultStart = yesterday;
        var maxLookback = today.AddDays(-62);

        var orgIds = await db.Jobs.AsNoTracking()
            .Where(j => j.JobStateId == 4 && j.OrganisationID > 0)
            .Select(j => j.OrganisationID)
            .Distinct()
            .ToListAsync(ct);

        if (orgIds.Count == 0) return;

        var openPeriods = await db.PayrollPeriods.AsNoTracking()
            .Where(p => orgIds.Contains(p.OrganisationId)
                && p.PeriodEndDate >= maxLookback
                && p.PeriodStartDate <= yesterday)
            .Select(p => new { p.OrganisationId, p.PeriodStartDate, p.PeriodEndDate })
            .ToListAsync(ct);

        foreach (var orgId in orgIds)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var fromDate = defaultStart;
                var orgPeriodStart = openPeriods
                    .Where(p => p.OrganisationId == orgId)
                    .Select(p => p.PeriodStartDate)
                    .DefaultIfEmpty(defaultStart)
                    .Min();
                if (orgPeriodStart < fromDate)
                    fromDate = orgPeriodStart < maxLookback ? maxLookback : orgPeriodStart;

                var evaluated = await service.RunScheduledEvaluationForOrganisationAsync(orgId, fromDate, yesterday, ct);
                if (evaluated > 0)
                {
                    logger.LogInformation("Attendance deduction worker evaluated {Count} deduction item(s) for Org {OrgId} ({From} to {To}).",
                        evaluated, orgId, fromDate, yesterday);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Attendance deduction worker failed for Org {OrgId}.", orgId);
            }
        }
    }
}

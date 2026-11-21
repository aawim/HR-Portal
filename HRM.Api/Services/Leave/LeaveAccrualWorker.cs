using System.Data;
using HRM.Models;
using Microsoft.Data.SqlClient;
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

                await using var lockLease = await TryAcquireDistributedLockAsync(db, stoppingToken);
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

    private static async Task<IAsyncDisposable?> TryAcquireDistributedLockAsync(HrmTeContext db, CancellationToken ct)
    {
        if (!db.Database.IsSqlServer())
            return NoOpDisposable.Instance;

        var connString = db.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connString))
            return NoOpDisposable.Instance;

        var conn = new SqlConnection(connString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_getapplock";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@Resource", AppLockResource);
        cmd.Parameters.AddWithValue("@LockMode", "Exclusive");
        cmd.Parameters.AddWithValue("@LockOwner", "Session");
        cmd.Parameters.AddWithValue("@LockTimeout", 0);
        var returnParam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
        returnParam.Direction = ParameterDirection.ReturnValue;

        await cmd.ExecuteNonQueryAsync(ct);
        var result = (int)(returnParam.Value ?? -999);
        if (result < 0)
        {
            await conn.DisposeAsync();
            return null;
        }

        return new SqlAppLockLease(conn);
    }

    private sealed class SqlAppLockLease(SqlConnection connection) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                if (connection.State == ConnectionState.Open)
                {
                    await using var cmd = connection.CreateCommand();
                    cmd.CommandText = "sp_releaseapplock";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Resource", AppLockResource);
                    cmd.Parameters.AddWithValue("@LockOwner", "Session");
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                // Closing the session connection releases the lock automatically.
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }
    }

    private sealed class NoOpDisposable : IAsyncDisposable
    {
        public static readonly NoOpDisposable Instance = new();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

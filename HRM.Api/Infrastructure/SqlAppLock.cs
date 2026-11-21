using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HRM.Api.Infrastructure;

/// <summary>
/// Acquires an exclusive session-scoped SQL Server application lock (<c>sp_getapplock</c>)
/// so scheduled background workers execute safely across multi-instance deployments.
/// Returns a no-op lease when running against non-SQL-Server providers (e.g., InMemory tests).
/// </summary>
public static class SqlAppLock
{
    public static async Task<IAsyncDisposable?> TryAcquireAsync(
        DbContext db,
        string resourceName,
        CancellationToken ct = default)
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
        cmd.Parameters.AddWithValue("@Resource", resourceName);
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

        return new SqlAppLockLease(conn, resourceName);
    }

    private sealed class SqlAppLockLease(SqlConnection connection, string resourceName) : IAsyncDisposable
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
                    cmd.Parameters.AddWithValue("@Resource", resourceName);
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

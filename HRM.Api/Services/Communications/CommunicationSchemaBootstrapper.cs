using HRM.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Data;

namespace HRM.Services.Communications;

public static class CommunicationSchemaBootstrapper
{
    private sealed class SchemaState
    {
        public readonly SemaphoreSlim Gate = new(1, 1);
        public bool Initialized;
    }
    private static readonly ConcurrentDictionary<string, SchemaState> States = new(StringComparer.Ordinal);

    public static async Task EnsureSchemaAsync(HrmTeContext db, CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsSqlServer()) return;
        var connection = db.Database.GetDbConnection();
        var state = States.GetOrAdd(connection.DataSource + "\u001f" + connection.Database, _ => new());
        await state.Gate.WaitAsync(cancellationToken);
        try
        {
            if (state.Initialized) return;
            var openedHere = connection.State != ConnectionState.Open;
            var lockAcquired = false;
            if (openedHere) await db.Database.OpenConnectionAsync(cancellationToken);
            try
            {
                // Coordinate API and worker processes as well as callers in the same process.
                await db.Database.ExecuteSqlRawAsync("""
                    DECLARE @result int;
                    EXEC @result = sys.sp_getapplock @Resource=N'DHR.Communications.Schema', @LockMode=N'Exclusive', @LockOwner=N'Session', @LockTimeout=30000;
                    IF @result < 0 THROW 51000, 'Communication Hub schema lock could not be acquired.', 1;
                    """, cancellationToken);
                lockAcquired = true;
                var assembly = typeof(CommunicationSchemaBootstrapper).Assembly;
                foreach (var resource in new[] { "HRM.Database.Scripts.20261121_CommunicationHub.sql", "HRM.Database.Scripts.20261121_CommunicationDeliveries.sql", "HRM.Database.Scripts.20261121_CommunicationRouting.sql" })
                {
                    using var stream = assembly.GetManifestResourceStream(resource);
                    if (stream is null) throw new InvalidOperationException($"Missing Communication Hub database resource: {resource}");
                    using var reader = new StreamReader(stream);
                    await db.Database.ExecuteSqlRawAsync(await reader.ReadToEndAsync(cancellationToken), cancellationToken);
                }
                state.Initialized = true;
            }
            finally
            {
                try
                {
                    if (lockAcquired && connection.State == ConnectionState.Open)
                        await db.Database.ExecuteSqlRawAsync("EXEC sys.sp_releaseapplock @Resource=N'DHR.Communications.Schema', @LockOwner=N'Session';", CancellationToken.None);
                }
                finally { if (openedHere) await db.Database.CloseConnectionAsync(); }
            }
        }
        finally { state.Gate.Release(); }
    }
}

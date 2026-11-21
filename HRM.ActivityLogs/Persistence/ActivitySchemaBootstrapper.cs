using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HRM.ActivityLogs.Persistence;

public static class ActivitySchemaBootstrapper
{
    public static async Task EnsureSchemaAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = services.GetService<IDbContextFactory<ActivityDbContext>>();
            if (factory is null) return;

            await using var db = await factory.CreateDbContextAsync(cancellationToken);
            if (!db.Database.IsSqlServer()) return;

            var connStr = db.Database.GetConnectionString();
            if (!string.IsNullOrWhiteSpace(connStr))
            {
                var targetBuilder = new SqlConnectionStringBuilder(connStr);
                var targetDbName = targetBuilder.InitialCatalog;
                if (!string.IsNullOrWhiteSpace(targetDbName) && !targetDbName.Equals("master", StringComparison.OrdinalIgnoreCase))
                {
                    var masterBuilder = new SqlConnectionStringBuilder(connStr) { InitialCatalog = "master" };
                    await using var masterConn = new SqlConnection(masterBuilder.ConnectionString);
                    await masterConn.OpenAsync(cancellationToken);
                    await using var cmd = masterConn.CreateCommand();
                    cmd.CommandText = "IF DB_ID(@dbName) IS NULL EXEC('CREATE DATABASE [' + REPLACE(@dbName, ']', ']]') + ']');";
                    cmd.Parameters.AddWithValue("@dbName", targetDbName);
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            await db.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var logger = services.GetService<ILoggerFactory>()?.CreateLogger("HRM.ActivityLogs.Bootstrap");
            logger?.LogWarning(ex, "Activity schema bootstrap skipped or failed.");
        }
    }
}

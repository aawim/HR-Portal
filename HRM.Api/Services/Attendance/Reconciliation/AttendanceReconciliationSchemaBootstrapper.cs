using System.IO;
using HRM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HRM.Services.Attendance.Reconciliation;

public static class AttendanceReconciliationSchemaBootstrapper
{
    private static int _initialized;

    public static async Task EnsureSchemaAsync(IServiceProvider services)
    {
        try
        {
            var factory = services.GetService<IDbContextFactory<HrmTeContext>>();
            if (factory is null)
                return;

            await using var db = await factory.CreateDbContextAsync();
            await EnsureSchemaAsync(db);
        }
        catch (Exception ex)
        {
            var logger = services.GetService<ILoggerFactory>()?.CreateLogger("AttendanceReconciliationSchemaBootstrapper");
            logger?.LogWarning(ex, "Could not auto-verify attendance reconciliation schema on startup.");
        }
    }

    public static async Task EnsureSchemaAsync(HrmTeContext db, CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsSqlServer())
            return;

        if (Interlocked.Exchange(ref _initialized, 1) == 1)
            return;

        try
        {
            using var stream = typeof(AttendanceReconciliationSchemaBootstrapper).Assembly
                .GetManifestResourceStream("HRM.Database.Scripts.20261103_AttendanceReconciliationEngine.sql");
            if (stream is null)
                return;

            using var reader = new StreamReader(stream);
            var sql = await reader.ReadToEndAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(sql))
            {
                await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
            }
        }
        catch
        {
            Interlocked.Exchange(ref _initialized, 0);
        }
    }
}

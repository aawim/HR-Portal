using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HRM.Models;

namespace HRM.Services.Calendar;

public static class OrganisationCalendarSchemaBootstrapper
{
    private static int _schemaInitialized;

    public static async Task EnsureSchemaAsync(HrmTeContext db, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsSqlServer()) return;
        if (Interlocked.Exchange(ref _schemaInitialized, 1) == 1) return;

        try
        {
            using var stream = typeof(OrganisationCalendarSchemaBootstrapper).Assembly
                .GetManifestResourceStream("HRM.Database.Scripts.20261111_OrganisationCalendar.sql");
            if (stream is null) return;

            using var reader = new StreamReader(stream);
            var sql = await reader.ReadToEndAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(sql))
            {
                await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Interlocked.Exchange(ref _schemaInitialized, 0);
            logger?.LogWarning(ex, "Could not auto-verify Organisation Calendar (OC_) schema.");
        }
    }
}

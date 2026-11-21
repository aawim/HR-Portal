using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Communications;

public static class CommunicationSchemaBootstrapper
{
    private static int initialized;

    public static async Task EnsureSchemaAsync(HrmTeContext db, CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsSqlServer() || Interlocked.Exchange(ref initialized, 1) == 1) return;
        try
        {
            var assembly = typeof(CommunicationSchemaBootstrapper).Assembly;
            foreach (var resource in new[] { "HRM.Database.Scripts.20261121_CommunicationHub.sql", "HRM.Database.Scripts.20261121_CommunicationDeliveries.sql", "HRM.Database.Scripts.20261121_CommunicationRouting.sql" })
            {
                using var stream = assembly.GetManifestResourceStream(resource);
                if (stream is null) throw new InvalidOperationException($"Missing Communication Hub database resource: {resource}");
                using var reader = new StreamReader(stream);
                await db.Database.ExecuteSqlRawAsync(await reader.ReadToEndAsync(cancellationToken), cancellationToken);
            }
        }
        catch
        {
            Interlocked.Exchange(ref initialized, 0);
            throw;
        }
    }
}

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using HRM.Models;
using System.Text.RegularExpressions;

internal static class UnifiedLeaveTypeUpgrade
{
    private static string Identifier(string value) => "[" + value.Replace("]", "]]") + "]";
    private static string Literal(string value) => "N'" + value.Replace("'", "''") + "'";
    public static async Task RehearseAsync(string sourceConnection, bool scopedHandover = false, bool overtime = false)
    {
        var sourceBuilder = new SqlConnectionStringBuilder(sourceConnection);
        var source = sourceBuilder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(source)) throw new InvalidOperationException("A source database is required.");
        var name = "HRM_LeaveUpgrade_" + Guid.NewGuid().ToString("N");
        var masterBuilder = new SqlConnectionStringBuilder(sourceConnection) { InitialCatalog = "master", Pooling = false };
        await using var master = new SqlConnection(masterBuilder.ConnectionString); await master.OpenAsync();
        async Task<object?> Scalar(string sql) { await using var c = master.CreateCommand(); c.CommandText = sql; c.CommandTimeout = 600; return await c.ExecuteScalarAsync(); }
        async Task Execute(string sql) { await using var c = master.CreateCommand(); c.CommandText = sql; c.CommandTimeout = 600; await c.ExecuteNonQueryAsync(); }
        var backupDir = Convert.ToString(await Scalar("SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'))"));
        var dataDir = Convert.ToString(await Scalar("SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultDataPath'))"));
        var logDir = Convert.ToString(await Scalar("SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultLogPath'))"));
        if (string.IsNullOrWhiteSpace(backupDir) || string.IsNullOrWhiteSpace(dataDir) || string.IsNullOrWhiteSpace(logDir)) throw new InvalidOperationException("SQL Server's backup/data/log directories must be configured.");
        var backup = Environment.GetEnvironmentVariable("HRM_LEAVE_UPGRADE_BACKUP") ?? backupDir.TrimEnd('\\','/') + "/" + source + "_BeforeUnifiedLeaveTypes_" + Guid.NewGuid().ToString("N") + ".bak";
        if (Environment.GetEnvironmentVariable("HRM_LEAVE_UPGRADE_BACKUP") == null) await Execute($"BACKUP DATABASE {Identifier(source)} TO DISK={Literal(backup)} WITH COPY_ONLY,CHECKSUM");
        await Execute($"RESTORE VERIFYONLY FROM DISK={Literal(backup)} WITH CHECKSUM");
        Console.WriteLine("Verified pre-upgrade backup retained: " + backup);
        var files = new List<(string Logical, string Type)>();
        await using (var cmd = master.CreateCommand())
        {
            cmd.CommandText = $"RESTORE FILELISTONLY FROM DISK={Literal(backup)}";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) files.Add((reader.GetString(reader.GetOrdinal("LogicalName")), reader.GetString(reader.GetOrdinal("Type"))));
        }
        if (files.Count == 0 || files.Any(f => f.Type is not ("D" or "L"))) throw new InvalidOperationException("Unsupported backup file layout.");
        try
        {
            var moves = files.Select((f,i) => "MOVE " + Literal(f.Logical) + " TO " + Literal((f.Type == "L" ? logDir : dataDir).TrimEnd('\\','/') + "/" + name + "_" + i + (f.Type == "L" ? ".ldf" : ".mdf")));
            await Execute($"RESTORE DATABASE {Identifier(name)} FROM DISK={Literal(backup)} WITH " + string.Join(",", moves) + ",CHECKSUM");
            var testBuilder = new SqlConnectionStringBuilder(sourceConnection) { InitialCatalog = name, Pooling = false };
            await using var db = new HrmTeContext(new DbContextOptionsBuilder<HrmTeContext>().UseSqlServer(testBuilder.ConnectionString).Options);
            db.Database.SetCommandTimeout(300);
            var beforeClaims = await db.OvertimeRequests.AsNoTracking().OrderBy(x => x.RequestId)
                .Select(x => new { x.RequestId, x.OrganisationId, x.IndividualId, x.JobId, x.WorkDate, x.Minutes, x.Reason, x.PaidAtUtc, x.PaymentReference }).ToListAsync();
            var beforeBalances = await db.JobLeaveTypes.AsNoTracking().OrderBy(x=>x.JobLeaveTypeId).Select(x=>new{x.JobLeaveTypeId,x.JobId,x.LeaveTypeId,x.RemainingDays,x.IsValid}).ToListAsync();
            var beforeLeaves = await db.Leaves.AsNoTracking().OrderBy(x=>x.LeaveId).Select(x=>new{x.LeaveId,x.JobId,x.LeaveTypeId,x.FromDate,x.ToDate,x.Duration,x.LeaveStateId}).ToListAsync();
            if (!scopedHandover)
                await db.Database.ExecuteSqlRawAsync("INSERT dbo.LeaveDefinitions(Code,Name,OwnerOrganisationID,IsSystemType,IsGlobal,IsActive,CreatedDate) SELECT 'UPGRADE-SMOKE','Upgrade preservation fixture',MIN(BusinessEntityID),0,0,1,SYSUTCDATETIME() FROM dbo.Organisations;");
            await db.Database.MigrateAsync();
            await VerifyAsync(db);
            if (overtime)
            {
                var afterClaims = await db.OvertimeRequests.AsNoTracking().OrderBy(x => x.RequestId)
                    .Select(x => new { x.RequestId, x.OrganisationId, x.IndividualId, x.JobId, x.WorkDate, x.Minutes, x.Reason, x.PaidAtUtc, x.PaymentReference }).ToListAsync();
                if (!beforeClaims.SequenceEqual(afterClaims)) throw new InvalidOperationException("OT upgrade changed existing claims.");
                if (await db.RequestTypes.CountAsync(x => x.TypeName == "Overtime Preapproval Request" || x.TypeName == "Overtime Clock Event Request") != 2)
                    throw new InvalidOperationException("OT request metadata missing.");
                Console.WriteLine($"PASS OT upgrade: preserved {afterClaims.Count} existing claims; new work requests {await db.OvertimeWorkRequests.CountAsync()}, tasks {await db.OvertimeWorkTasks.CountAsync()}.");
            }
            if (!scopedHandover && !await db.LeaveTypes.AnyAsync(t=>t.Code=="UPGRADE-SMOKE"))throw new InvalidOperationException("An unmapped definition was lost.");
            if(scopedHandover)
            {
                var afterBalances = await db.JobLeaveTypes.AsNoTracking().OrderBy(x=>x.JobLeaveTypeId).Select(x=>new{x.JobLeaveTypeId,x.JobId,x.LeaveTypeId,x.RemainingDays,x.IsValid}).ToListAsync();
                var afterLeaves = await db.Leaves.AsNoTracking().OrderBy(x=>x.LeaveId).Select(x=>new{x.LeaveId,x.JobId,x.LeaveTypeId,x.FromDate,x.ToDate,x.Duration,x.LeaveStateId}).ToListAsync();
                if(!beforeBalances.SequenceEqual(afterBalances)||!beforeLeaves.SequenceEqual(afterLeaves))throw new InvalidOperationException("Scoped handover upgrade changed historical leave or balance data.");
                await VerifyHandoverAsync(db,verifyImported:true);
            }
            await db.Database.MigrateAsync();
            await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync("Database/Scripts/20261026_PrefixLeavePolicyTables.sql"));
            Console.WriteLine("PASS Isolated restored-data consolidation, exact balance/history checks, canonical queries and repeat upgrade.");
        }
        finally
        {
            if (!Regex.IsMatch(name, "^HRM_LeaveUpgrade_[a-f0-9]{32}$")) throw new InvalidOperationException("Unsafe test database name.");
            await Execute($"IF DB_ID({Literal(name)}) IS NOT NULL BEGIN ALTER DATABASE {Identifier(name)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {Identifier(name)}; END");
        }
    }
    public static async Task VerifyAsync(HrmTeContext db)
    {
        await db.Database.OpenConnectionAsync();
        await using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE name IN ('LeaveTypes','LeaveDefinitions','LeaveTypeMappings','LeaveFrameworkConfigurations','LeavePolicies','LeavePolicyAccrualRules')";
        if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) != 0) throw new InvalidOperationException("Retired type tables remain.");
        cmd.CommandText = "SELECT COUNT(*) FROM dbo.LT_TypeMigrationAudits a JOIN dbo.LT_LeaveTypes t ON t.LeaveTypeID=a.SourceId WHERE a.SourceEntity='ExistingLeaveType' AND (t.Name<>JSON_VALUE(a.SnapshotJson,'$.Name') OR t.OrganisationID<>CONVERT(int,JSON_VALUE(a.SnapshotJson,'$.OrganisationID')))";
        if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) != 0) throw new InvalidOperationException("An existing type name/owner changed.");
        Console.WriteLine($"Canonical types: {await db.LeaveTypes.CountAsync()}; balances: {await db.JobLeaveTypes.CountAsync()}; historical leaves: {await db.Leaves.CountAsync()}; policies: {await db.LeavePolicies.CountAsync()}; audit snapshots: {await db.LeaveTypeMigrationAudits.CountAsync()}.");
        if ((await db.Database.GetPendingMigrationsAsync()).Any()) throw new InvalidOperationException("Pending migrations remain.");
    }
    public static async Task VerifyHandoverAsync(HrmTeContext db,bool verifyImported=false)
    {
        Console.WriteLine($"Scoped handover rules: {await db.LeaveHandoverRules.CountAsync()}; selected type links: {await db.LeaveHandoverRuleTypes.CountAsync()}.");
        var balance = await db.JobLeaveTypes.AsNoTracking().Where(x=>x.LeaveTypeId==66&&x.IsValid&&x.Job.OrganisationID==3).Select(x=>new{x.JobId}).FirstOrDefaultAsync();
        if(balance!=null)
        {
            var r=await HRM.Services.Leave.LeaveHandoverRuleResolver.ResolveAsync(db,balance.JobId,66,DateTime.UtcNow.AddHours(5).Date);
            Console.WriteLine($"Annual local type 66: {r.Source}; threshold={r.AfterDays}; 2 days requires handover={r.Requires(2)}; 3 days requires handover={r.Requires(3)}.");
            if(verifyImported)
            {
                var old=await db.LeavePolicies.AsNoTracking().Where(p=>p.OrganisationId==3&&(p.LeaveTypeId==66||p.LeaveTypeId==1)&&p.IsActive&&p.EffectiveFrom<=DateTime.UtcNow.AddHours(5).Date&&(p.EffectiveTo==null||p.EffectiveTo>=DateTime.UtcNow.AddHours(5).Date)).OrderByDescending(p=>p.LeaveTypeId==66).ThenByDescending(p=>p.EffectiveFrom).Select(p=>new{p.RequireWorkHandover,p.WorkHandoverAfterDays}).FirstOrDefaultAsync();
                if(old!=null&&(old.RequireWorkHandover ? r.AfterDays!=old.WorkHandoverAfterDays||r.Requires(old.WorkHandoverAfterDays)||!r.Requires(old.WorkHandoverAfterDays+1) : r.Mode!="Exempt"))
                    throw new InvalidOperationException("The reviewed annual handover requirement was not preserved.");
            }
        }
    }
    public static async Task ApplyAsync(HrmTeContext db)
    {
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();
        if (pending.Any(x => !x.EndsWith("_UnifiedLeaveTypes", StringComparison.Ordinal) && !x.EndsWith("_PrefixLeavePolicyTables", StringComparison.Ordinal) && !x.EndsWith("_ScopedLeaveHandoverRules", StringComparison.Ordinal))) throw new InvalidOperationException("Unexpected pending migrations. Review deployment before applying.");
        db.Database.SetCommandTimeout(300);
        await db.Database.MigrateAsync(); await VerifyAsync(db);
        await VerifyHandoverAsync(db,pending.Any(x=>x.EndsWith("_ScopedLeaveHandoverRules",StringComparison.Ordinal)));
    }
}

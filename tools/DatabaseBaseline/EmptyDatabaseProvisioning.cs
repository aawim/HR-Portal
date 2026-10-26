using HRM.Models;
using HRM.Components.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

internal static class EmptyDatabaseProvisioning
{
    public static async Task CreateAsync(HrmTeContext db)
    {
        if(await db.Database.CanConnectAsync())
        {
            await db.Database.OpenConnectionAsync();await using var check=db.Database.GetDbConnection().CreateCommand();check.CommandText="SELECT COUNT(*) FROM sys.tables";
            if(Convert.ToInt32(await check.ExecuteScalarAsync())!=0)throw new InvalidOperationException("Provisioning requires an empty database. Existing tables and records will never be replaced.");
            await db.Database.CloseConnectionAsync();
        }
        await db.Database.EnsureCreatedAsync();
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("IF COLUMNPROPERTY(OBJECT_ID('dbo.Context'),'ContextID','IsIdentity')=1 SET IDENTITY_INSERT dbo.Context ON; INSERT dbo.Context(ContextID,Name,ContextKey,IsDefault) VALUES(1,'Admin','admin',0),(2,'Employee','online',1); IF COLUMNPROPERTY(OBJECT_ID('dbo.Context'),'ContextID','IsIdentity')=1 SET IDENTITY_INSERT dbo.Context OFF;");
        await db.Database.ExecuteSqlRawAsync("IF COLUMNPROPERTY(OBJECT_ID('dbo.OperationLogActionTypes'),'OperationLogActionTypes','IsIdentity')=1 SET IDENTITY_INSERT dbo.OperationLogActionTypes ON;");
        foreach(var field in typeof(HrmTeContext).Assembly.GetType("HRM.Components.Shared.SharedConfig+OperationLogActionTypes",throwOnError:true)!.GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static).Where(f=>f.IsLiteral&&f.FieldType==typeof(int)).GroupBy(f=>(int)f.GetRawConstantValue()!).Select(g=>g.First()))
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.OperationLogActionTypes(OperationLogActionTypes,Name) VALUES({(int)field.GetRawConstantValue()!},{field.Name})");
        await db.Database.ExecuteSqlRawAsync("IF COLUMNPROPERTY(OBJECT_ID('dbo.OperationLogActionTypes'),'OperationLogActionTypes','IsIdentity')=1 SET IDENTITY_INSERT dbo.OperationLogActionTypes OFF;");
        var assembly=db.GetService<IMigrationsAssembly>();
        foreach(var item in assembly.Migrations.OrderBy(x=>x.Key))
        {
            // The current model already contains these structural changes, including work-handover columns on LT_LeavePolicies.
            if(item.Key.EndsWith("_UnifiedLeaveTypes",StringComparison.Ordinal) || item.Key is "20261015090000_AddLeavePolicyWorkHandover" or "20260910062632_AddJobWorkTemplates" or "20261019100000_AddUserTodos" or "20261025120700_PreserveTransferHistory")continue;
            var migration=assembly.CreateMigration(item.Value,db.Database.ProviderName!);
            foreach(var operation in migration.UpOperations.OfType<SqlOperation>())
            {
                await using var command=db.Database.GetDbConnection().CreateCommand();command.CommandTimeout=120;command.CommandText=operation.Sql;await command.ExecuteNonQueryAsync();
            }
        }
        var history=db.GetService<IHistoryRepository>();await db.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());
        foreach(var id in assembly.Migrations.Keys.OrderBy(x=>x))await db.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(id,"8.0.27")));
        Console.WriteLine("Empty database provisioned from the current model; feature metadata installed and migration baseline recorded. Organisation/reference catalogues and initial privileged account must be provisioned before use.");
    }
    public static async Task SmokeAsync(string sourceConnection)
    {
        var name="HRM_Integration_"+Guid.NewGuid().ToString("N");var builder=new SqlConnectionStringBuilder(sourceConnection){InitialCatalog=name};
        await using var db=new HrmTeContext(new DbContextOptionsBuilder<HrmTeContext>().UseSqlServer(builder.ConnectionString).Options);
        try
        {
            await CreateAsync(db);await db.Database.MigrateAsync();
            await UnifiedLeaveTypeUpgrade.VerifyAsync(db);
            if((await db.Database.GetPendingMigrationsAsync()).Any())throw new InvalidOperationException("Fresh schema has pending migrations.");
            if(!await db.RequestTypes.AnyAsync(x=>x.TypeName=="Overtime Claim Request"))throw new InvalidOperationException("Overtime metadata missing on fresh install.");
            if(await db.RequestTypes.CountAsync(x=>x.TypeName=="Supervisor Assignment Request"||x.TypeName=="Supervisor Removal Request")!=2)
                throw new InvalidOperationException("Supervisor approval request types missing on fresh install.");
            if(await db.SupervisorChangeRequests.CountAsync()!=0)throw new InvalidOperationException("A fresh installation contains unexpected supervisor requests.");
            if(await db.LeaveHandoverRules.CountAsync()!=0)throw new InvalidOperationException("Unexpected scoped handover rules on fresh installation.");
            if(await db.JobTypeLeaveSets.CountAsync()!=0)throw new InvalidOperationException("Unexpected fresh-install Job Type leave set assignments.");
            // Rehearse the additive upgrade with a preserved business catalogue record.
            var sentinel = new JobType { TypeName = "Integration preservation check", IsActive = true };
            db.JobTypes.Add(sentinel); await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync("DROP TABLE dbo.OT_OvertimeRequests; DROP TABLE dbo.LS_JobTypeLeaveSets; DELETE dbo.__EFMigrationsHistory WHERE MigrationId IN ('20261025120600_SystemGapHardening','20261025120700_PreserveTransferHistory','20261026050306_JobTypeLeaveSetAssignments');");
            await db.Database.MigrateAsync(); await db.Database.MigrateAsync();
            if (!await db.JobTypes.AnyAsync(x => x.JobTypeId == sentinel.JobTypeId && x.TypeName == sentinel.TypeName) ||
                (await db.Database.GetPendingMigrationsAsync()).Any()) throw new InvalidOperationException("Upgrade lost existing data or left pending migrations.");
            await using var check = db.Database.GetDbConnection().CreateCommand();
            check.CommandText = "SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID('dbo.WorkAssignmentTransfers') AND delete_referential_action<>0";
            if (Convert.ToInt32(await check.ExecuteScalarAsync()) != 0) throw new InvalidOperationException("Transfer history has cascading delete paths.");
            Console.WriteLine("PASS isolated SQL Server fresh-install, data-preserving upgrade and repeat-upgrade smoke checks.");
        }
        finally
        {
            if(!name.StartsWith("HRM_Integration_",StringComparison.Ordinal)||new SqlConnectionStringBuilder(db.Database.GetConnectionString()).InitialCatalog!=name)throw new InvalidOperationException("Refusing cleanup outside the generated integration database.");
            await db.Database.EnsureDeletedAsync();
        }
    }
}

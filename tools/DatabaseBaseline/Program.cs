using HRM.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Data;

// Explicit deployment utility: never starts the web application or its background workers.
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT",Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")??"Development");
await using var db=new HrmTeContextFactory().CreateDbContext([]);
if(args.Contains("--audit-chat-references")){await ChatReferenceAudit.RunAsync(db);return 0;}
if(args.Contains("--audit-chat-visibility")){await ChatVisibilityAudit.RunAsync(db);return 0;}
if(args.Contains("--verify-group-chat-upgrade")){await UnifiedLeaveTypeUpgrade.RehearseAsync(db.Database.GetConnectionString()!,scopedHandover:true,overtime:true,chat:true,groupChat:true);return 0;}
if(args.Contains("--apply-group-chat-upgrade"))
{
    var pending=(await db.Database.GetPendingMigrationsAsync()).ToArray();
    if(pending.Any(x=>x!="20261027030613_StaffChatGroups"))throw new InvalidOperationException("Unexpected migrations; deployment refused.");
    await db.Database.MigrateAsync();
    Console.WriteLine($"Group chat schema installed; conversations {await db.StaffChatConversations.CountAsync()}, messages {await db.StaffChatMessages.CountAsync()}, memberships {await db.StaffChatMemberships.CountAsync()}.");
    return 0;
}
if(args.Contains("--verify-chat-upgrade")){await UnifiedLeaveTypeUpgrade.RehearseAsync(db.Database.GetConnectionString()!,scopedHandover:true,overtime:true,chat:true);return 0;}
if(args.Contains("--apply-chat-upgrade"))
{
    var pending=(await db.Database.GetPendingMigrationsAsync()).ToArray();
    if(pending.Any(x=>x!="20261026100840_StaffChat"))throw new InvalidOperationException("Unexpected migrations; deployment refused.");
    await db.Database.MigrateAsync();
    Console.WriteLine($"Chat schema installed; conversations {await db.StaffChatConversations.CountAsync()}, messages {await db.StaffChatMessages.CountAsync()}.");
    return 0;
}
if(args.Contains("--verify-font-size-upgrade")){await UnifiedLeaveTypeUpgrade.RehearseAsync(db.Database.GetConnectionString()!,scopedHandover:true,overtime:true);return 0;}
if(args.Contains("--apply-font-size-upgrade"))
{
    var pending=(await db.Database.GetPendingMigrationsAsync()).ToArray();
    if(pending.Any(x=>!x.EndsWith("_UserFontSizePreference",StringComparison.Ordinal)))throw new InvalidOperationException("Unexpected migrations; deployment refused.");
    await db.Database.MigrateAsync();
    Console.WriteLine($"Font preference column available; {await db.UserPreferences.CountAsync()} existing preference records retained.");
    return 0;
}
if(args.Contains("--audit-ot-preapprovals"))
{
    var rows=await db.OvertimeWorkRequests.AsNoTracking().Where(x=>x.Kind=="Preapproval")
        .Select(x=>new{x.RequestId,x.OrganisationId,x.IndividualId,x.FromDate,x.ToDate,x.Request.RequestStateId,x.Request.RequestState.StateName,Tasks=x.Tasks.Count}).ToListAsync();
    Console.WriteLine($"OT preapprovals: {rows.Count}");
    foreach(var row in rows)Console.WriteLine($"Request {row.RequestId}; organisation {row.OrganisationId}; staff {row.IndividualId}; {row.FromDate:yyyy-MM-dd} to {row.ToDate:yyyy-MM-dd}; {row.StateName}; tasks {row.Tasks}");
    return 0;
}
if(args.Contains("--audit-ot-policy"))
{
    var rows=await db.JobTypeOvertimeRules.AsNoTracking().OrderBy(x=>x.OrganisationId).ThenBy(x=>x.JobTypeId).ToListAsync();
    Console.WriteLine($"OT policies loaded successfully: {rows.Count}");
    foreach(var row in rows)Console.WriteLine($"Organisation {row.OrganisationId}, Job Type {row.JobTypeId}, allowed {row.IsAllowed}, weekly {row.MaximumWeeklyHours}, daily {row.MaximumDailyHours}, version {row.Version}, audit {row.OperationLogId}");
    return 0;
}
if(args.Contains("--verify-overtime-upgrade")){await UnifiedLeaveTypeUpgrade.RehearseAsync(db.Database.GetConnectionString()!,scopedHandover:true,overtime:true);return 0;}
if(args.Contains("--apply-overtime-upgrade"))
{
    var pending=(await db.Database.GetPendingMigrationsAsync()).ToArray();
    if(pending.Any(x=>x!="20261026085804_OvertimeWorkRequests"))throw new InvalidOperationException("Unexpected migrations; deployment refused.");
    await db.Database.MigrateAsync();
    Console.WriteLine($"OT work requests: {await db.OvertimeWorkRequests.CountAsync()}; tasks: {await db.OvertimeWorkTasks.CountAsync()}; existing claims: {await db.OvertimeRequests.CountAsync()}");
    return 0;
}
if(args.Contains("--audit-admin-access")){await AdminAccessAudit.RunAsync(db);return 0;}
if(args.Contains("--repair-annual-leave-reasons")){await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync("Database/Scripts/20261026_RepairAnnualLeaveReasons.sql"));await LeaveApplicationAudit.RunAsync(db);return 0;}
if(args.Contains("--repair-annual-leave-routing")){await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync("Database/Scripts/20261026_RepairAnnualLeaveRequestTypes.sql"));await LeaveApplicationAudit.RunAsync(db);return 0;}
if(args.Contains("--audit-leave-applications")){await LeaveApplicationAudit.RunAsync(db);return 0;}
if(args.Contains("--verify-scoped-handover-upgrade")){await UnifiedLeaveTypeUpgrade.RehearseAsync(db.Database.GetConnectionString()!,scopedHandover:true);return 0;}
if(args.Contains("--verify-leave-type-upgrade")){await UnifiedLeaveTypeUpgrade.RehearseAsync(db.Database.GetConnectionString()!);return 0;}
if(args.Contains("--apply-unified-leave-types")){await UnifiedLeaveTypeUpgrade.ApplyAsync(db);return 0;}
if(args.Contains("--audit-leave-types")) { await db.Database.OpenConnectionAsync(); await using var command=db.Database.GetDbConnection().CreateCommand(); command.CommandText=await File.ReadAllTextAsync("Database/Scripts/LeaveTypeAudit.sql"); await using var reader=await command.ExecuteReaderAsync(); do { while(await reader.ReadAsync()) Console.WriteLine(string.Join(" | ",Enumerable.Range(0,reader.FieldCount).Select(i=>reader.GetName(i)+"="+reader.GetValue(i)))); } while(await reader.NextResultAsync()); return 0; }
if(args.Contains("--verify-empty-schema")){await EmptyDatabaseProvisioning.SmokeAsync(db.Database.GetConnectionString()!);return 0;}
if(args.Contains("--provision-empty")){await EmptyDatabaseProvisioning.CreateAsync(db);return 0;}
if(args.Contains("--canonicalize-legacy-flows")){await LegacyWorkflowUpgrade.RunAsync(db);return 0;}
const string baseline="20260910062632_AddJobWorkTemplates";
var assembly=db.GetService<IMigrationsAssembly>();
var migration=assembly.CreateMigration(assembly.Migrations[baseline],db.Database.ProviderName!);
var tables=migration.UpOperations.OfType<CreateTableOperation>().ToList();
if(tables.Count<300||migration.UpOperations.Any(o=>o is not (CreateTableOperation or CreateIndexOperation or AddForeignKeyOperation)))throw new InvalidOperationException("Unexpected baseline operations. Manual review required.");
await db.Database.OpenConnectionAsync();var connection=db.Database.GetDbConnection();
await using var tx=await connection.BeginTransactionAsync(IsolationLevel.Serializable);
if(args.Contains("--repair-compatible-schema"))
{
    await using var repair=connection.CreateCommand();repair.Transaction=tx;repair.CommandText=await File.ReadAllTextAsync("Database/Scripts/20261025_LegacySchemaCompatibility.sql");await repair.ExecuteNonQueryAsync();
}
var errors=new List<string>();var reviewed=new List<string>();
var currentTables=db.Model.GetRelationalModel().Tables.ToDictionary(t=>(t.Schema??"dbo")+"."+t.Name,StringComparer.OrdinalIgnoreCase);
var columns=new Dictionary<string,HashSet<string>>(StringComparer.OrdinalIgnoreCase);
await using(var command=connection.CreateCommand())
{
    command.Transaction=tx;command.CommandText="SELECT s.name,t.name,c.name FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.columns c ON c.object_id=t.object_id";
    await using var reader=await command.ExecuteReaderAsync();while(await reader.ReadAsync()){var key=reader.GetString(0)+"."+reader.GetString(1);if(!columns.TryGetValue(key,out var set))columns[key]=set=new(StringComparer.OrdinalIgnoreCase);set.Add(reader.GetString(2));}
}
Console.WriteLine("Week tables: "+string.Join(",",columns.Keys.Where(k=>k.Contains("DayOf",StringComparison.OrdinalIgnoreCase))));
Console.WriteLine("Current week mappings: "+string.Join(",",currentTables.Keys.Where(k=>k.Contains("DayOf",StringComparison.OrdinalIgnoreCase))));
Console.WriteLine("Current Teams columns: "+string.Join(",",currentTables.GetValueOrDefault("dbo.Teams")?.Columns.Select(c=>c.Name)??[]));
foreach(var table in tables)
{
    var key=(table.Schema??"dbo")+"."+table.Name;
    if(key=="dbo.DayOfWeek"&&!columns.ContainsKey(key)&&columns.ContainsKey("dbo.DayOfWeeks")&&currentTables.ContainsKey("dbo.DayOfWeeks")){reviewed.Add("Legacy DayOfWeek name replaced by current DayOfWeeks mapping.");key="dbo.DayOfWeeks";}
    if(!columns.TryGetValue(key,out var available)){errors.Add("Missing table "+key);continue;}
    foreach(var column in table.Columns)if(!available.Contains(column.Name))
    {
        var retired=(key=="dbo.Teams"&&column.Name=="IsSuperVisor"||key=="dbo.UserOrganisationRoles"&&column.Name=="OrganisationBusinessEntityID")&&currentTables.TryGetValue(key,out var current)&&!current.Columns.Any(c=>c.Name.Equals(column.Name,StringComparison.OrdinalIgnoreCase));
        if(retired)reviewed.Add("Retired historical shadow column "+key+"."+column.Name+" is absent from current model and database.");else errors.Add("Missing column "+key+"."+column.Name);
    }
    if(table.PrimaryKey!=null)
    {
        await using var command=connection.CreateCommand();command.Transaction=tx;command.CommandText="SELECT c.name FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE i.is_primary_key=1 AND i.object_id=OBJECT_ID(@table) ORDER BY ic.key_ordinal";
        var p=command.CreateParameter();p.ParameterName="@table";p.Value=key;command.Parameters.Add(p);var actual=new List<string>();await using var reader=await command.ExecuteReaderAsync();while(await reader.ReadAsync())actual.Add(reader.GetString(0));if(!actual.SequenceEqual(table.PrimaryKey.Columns,StringComparer.OrdinalIgnoreCase))
        {
            var currentKey=currentTables.GetValueOrDefault(key)?.PrimaryKey?.Columns.Select(c=>c.Name).ToArray();
            if(key=="dbo.Users"&&currentKey!=null&&actual.SequenceEqual(currentKey,StringComparer.OrdinalIgnoreCase)&&actual.SequenceEqual(new[]{"UserID"},StringComparer.OrdinalIgnoreCase))reviewed.Add("Users primary key matches current UserID model; historical BusinessEntityID key is superseded.");else errors.Add("Primary key mismatch "+key);
        }
    }
}
if(errors.Count!=0){foreach(var error in errors)Console.WriteLine(error);Console.WriteLine($"Baseline refused: {errors.Count} schema discrepancies. No history changed.");await tx.RollbackAsync();return 2;}
foreach(var note in reviewed)Console.WriteLine("Reviewed: "+note);
Console.WriteLine($"Verified {tables.Count} legacy tables, their baseline columns and primary keys. No schema/data replacement performed.");
await using(var command=connection.CreateCommand())
{
    command.Transaction=tx;
    command.CommandText="SELECT COUNT(*) FROM dbo.__EFMigrationsHistory WHERE MigrationId=@id";var p=command.CreateParameter();p.ParameterName="@id";p.Value=baseline;command.Parameters.Add(p);var present=Convert.ToInt32(await command.ExecuteScalarAsync())>0;
    if(!present&&args.Contains("--apply"))
    {
        command.CommandText="INSERT dbo.__EFMigrationsHistory(MigrationId,ProductVersion) VALUES(@id,'8.0.27')";await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();command.CommandText="INSERT dbo.OperationLogs(OperationLogActionID,CreatedByContextID,CreatedDate,UpdatedDate,CreatedByIP,UpdatedByIP,Remarks,UpdatedContextID) VALUES(18,1,SYSUTCDATETIME(),SYSUTCDATETIME(),'','','Verified existing legacy schema and recorded its initial migration baseline; no application tables or records were replaced.',1)";await command.ExecuteNonQueryAsync();Console.WriteLine("Verified initial migration baseline recorded with an operation log.");
    }
    else Console.WriteLine(present?"Baseline already recorded.":"Read-only check complete. Use --apply to record the verified baseline.");
}
if(args.Contains("--apply")&&columns.TryGetValue("dbo.UT_UserTodos",out var todoColumns)&&!columns.ContainsKey("dbo.UserTodos"))
{
    var expected=new[]{"UserTodoId","UserId","Title","Notes","Priority","DueDate","IsCompleted","IsDeleted","CreatedAtUtc","UpdatedAtUtc","CompletedAtUtc","RowVersion"};
    if(expected.All(todoColumns.Contains))
    {
        await using var command=connection.CreateCommand();command.Transaction=tx;
        command.CommandText="IF NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261019100000_AddUserTodos') BEGIN INSERT dbo.__EFMigrationsHistory(MigrationId,ProductVersion) VALUES('20261019100000_AddUserTodos','8.0.27'); INSERT dbo.OperationLogs(OperationLogActionID,CreatedByContextID,CreatedDate,UpdatedDate,CreatedByIP,UpdatedByIP,Remarks,UpdatedContextID) VALUES(18,1,SYSUTCDATETIME(),SYSUTCDATETIME(),'','','Verified renamed UT_UserTodos schema and recorded the already-installed UserTodos migration; existing tasks preserved.',1); END";
        await command.ExecuteNonQueryAsync();Console.WriteLine("Verified already-installed UserTodos migration against its renamed physical table.");
    }
}
await tx.CommitAsync();
Console.WriteLine("Pending migrations: "+string.Join(", ",await db.Database.GetPendingMigrationsAsync()));return 0;

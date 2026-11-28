using HRM.Models;
using Microsoft.Data.SqlClient;
using Xunit;
using Microsoft.EntityFrameworkCore;
using HRM.Services.Communications;

namespace HRM.Api.UnitTests;

public sealed class CommunicationSqlIntegrationTests
{
    public sealed class LocalSqlFactAttribute : FactAttribute
    {
        public LocalSqlFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("DHR_COMMS_SQL_TESTS") != "1")
                Skip = "Set DHR_COMMS_SQL_TESTS=1 to run against local SQL Express using a disposable database.";
        }
    }

    [LocalSqlFact]
    [Trait("Category", "SqlIntegration")]
    public async Task RealScriptsPreserveRecordsAndEnforceTransactionalQueueSafety()
    {
        var name = "DHR_CommsTest_" + Guid.NewGuid().ToString("N");
        var masterBuilder = new SqlConnectionStringBuilder
        {
            DataSource = @".\SQLEXPRESS", InitialCatalog = "master", IntegratedSecurity = true,
            TrustServerCertificate = true, Pooling = false, ConnectTimeout = 5
        };
        await using var master = new SqlConnection(masterBuilder.ConnectionString);
        await master.OpenAsync();
        var created = false;
        try
        {
            await ExecuteAsync(master, $"CREATE DATABASE [{name}]");
            created = true;
            var testBuilder = new SqlConnectionStringBuilder(masterBuilder.ConnectionString) { InitialCatalog = name };
            await using (var db = new SqlConnection(testBuilder.ConnectionString))
            {
                await db.OpenAsync();
                // Only existing HRM boundary tables needed by the real CM_ foreign keys are stubbed.
                await ExecuteAsync(db, """
                    CREATE TABLE dbo.Organisations(BusinessEntityID int NOT NULL PRIMARY KEY);
                    CREATE TABLE dbo.Requests(RequestID int NOT NULL PRIMARY KEY);
                    CREATE TABLE dbo.Jobs(JobID int NOT NULL PRIMARY KEY);
                    INSERT dbo.Organisations VALUES (1);
                    INSERT dbo.Requests VALUES (1);
                    INSERT dbo.Jobs VALUES (1);
                    """);
                await ApplyScriptAsync(db, "20261121_CommunicationHub.sql");
                await ApplyScriptAsync(db, "20261121_CommunicationDeliveries.sql");
                await ExecuteAsync(db, """
                    INSERT dbo.CM_ProviderConnections(OrganisationBusinessEntityId,ProviderCode,ConnectionName,CredentialReference,CreatedAtUtc)
                        VALUES(1,'TELEGRAM','Test connection','not-a-live-credential',SYSUTCDATETIME());
                    INSERT dbo.CM_Destinations(CommunicationProviderConnectionId,OrganisationBusinessEntityId,DestinationType,ExternalDestinationId,DisplayName)
                        VALUES(1,1,'TelegramChat','-100123','Test destination');
                    INSERT dbo.CM_AutomationRules(OrganisationBusinessEntityId,EventCode,RuleName,TemplateBody)
                        VALUES(1,'organisation-event.published.v1','Test rule','{{Title}}');
                    INSERT dbo.CM_CommunicationOutbox(EventId,OrganisationBusinessEntityId,EventCode,AggregateType,AggregateId,PayloadJson,OccurredAtUtc,AvailableAtUtc,Status)
                        VALUES(NEWID(),1,'organisation-event.published.v1','OrganisationEvent','1','{}',SYSUTCDATETIME(),SYSUTCDATETIME(),'Pending');
                    INSERT dbo.CM_Messages(CommunicationOutboxId,OrganisationBusinessEntityId,Subject,RenderedBody,CreatedAtUtc)
                        VALUES(1,1,'Original subject','Original saved body',SYSUTCDATETIME());
                    """);

                await ApplyScriptAsync(db, "20261121_CommunicationRouting.sql");
                foreach (var script in new[] { "20261121_CommunicationHub.sql", "20261121_CommunicationDeliveries.sql", "20261121_CommunicationRouting.sql", "20261121_CommunicationPlatforms.sql" })
                    await ApplyScriptAsync(db, script);
                var options = new DbContextOptionsBuilder<HrmTeContext>().UseSqlServer(testBuilder.ConnectionString).Options;
                await using (var firstStartup = new HrmTeContext(options))
                await using (var secondStartup = new HrmTeContext(options))
                {
                    await ExecuteAsync(db, "EXEC sys.sp_getapplock @Resource=N'DHR.Communications.Schema', @LockMode=N'Exclusive', @LockOwner=N'Session', @LockTimeout=5000;");
                    var first = CommunicationSchemaBootstrapper.EnsureSchemaAsync(firstStartup);
                    var second = CommunicationSchemaBootstrapper.EnsureSchemaAsync(secondStartup);
                    var secondWaited = !second.IsCompleted;
                    await ExecuteAsync(db, "EXEC sys.sp_releaseapplock @Resource=N'DHR.Communications.Schema', @LockOwner=N'Session';");
                    await Task.WhenAll(first, second);
                    Assert.True(secondWaited, "A second caller must wait until schema verification has finished.");
                }
                Assert.Equal("Original saved body", await ScalarAsync(db, "SELECT RenderedBody FROM dbo.CM_Messages WHERE CommunicationMessageId=1"));
                Assert.Equal("NotRequired", await ScalarAsync(db, "SELECT ApprovalStatus FROM dbo.CM_Messages WHERE CommunicationMessageId=1"));
                Assert.Equal("1", await ScalarAsync(db, "SELECT CONVERT(nvarchar(10),TemplateVersion) FROM dbo.CM_Messages WHERE CommunicationMessageId=1"));
                Assert.Equal("en", await ScalarAsync(db, "SELECT LanguageCode FROM dbo.CM_Destinations WHERE CommunicationDestinationId=1"));
                await ExecuteAsync(db, """
                    INSERT dbo.CM_CommunicationConsents(OrganisationBusinessEntityId,CommunicationDestinationId,IndividualId,Purpose,Source,ConsentedAtUtc,RecordedByUserId)
                    VALUES(1,1,100,'ScheduleUpdates','Signed opt-in',SYSUTCDATETIME(),1);
                    UPDATE dbo.CM_CommunicationConsents SET RevokedAtUtc=SYSUTCDATETIME() WHERE CommunicationDestinationId=1;
                    INSERT dbo.CM_WorkerHeartbeats(InstanceId,LastSuccessfulCycleUtc,OutboundEnabled) VALUES('test-worker',SYSUTCDATETIME(),0);
                    """);
                await ApplyScriptAsync(db, "20261121_CommunicationPlatforms.sql");
                Assert.Equal("Signed opt-in", await ScalarAsync(db, "SELECT Source FROM dbo.CM_CommunicationConsents WHERE RevokedAtUtc IS NOT NULL"));
                Assert.Equal("test-worker", await ScalarAsync(db, "SELECT InstanceId FROM dbo.CM_WorkerHeartbeats"));

                var duplicateEvent = await Assert.ThrowsAsync<SqlException>(() => ExecuteAsync(db, """
                    INSERT dbo.CM_CommunicationOutbox(EventId,OrganisationBusinessEntityId,EventCode,AggregateType,AggregateId,PayloadJson,OccurredAtUtc,AvailableAtUtc,Status)
                        SELECT EventId,OrganisationBusinessEntityId,EventCode,AggregateType,AggregateId,PayloadJson,OccurredAtUtc,AvailableAtUtc,Status FROM dbo.CM_CommunicationOutbox;
                    """));
                Assert.Contains(duplicateEvent.Number, new[] { 2601, 2627 });

                await using (var transaction = (SqlTransaction)await db.BeginTransactionAsync())
                {
                    await ExecuteAsync(db, "UPDATE dbo.CM_Messages SET ApprovalStatus='Approved' WHERE CommunicationMessageId=1", transaction);
                    await ExecuteAsync(db, InsertDelivery("rolled-back"), transaction);
                    await transaction.RollbackAsync();
                }
                Assert.Equal("NotRequired", await ScalarAsync(db, "SELECT ApprovalStatus FROM dbo.CM_Messages WHERE CommunicationMessageId=1"));
                Assert.Equal(0, Convert.ToInt32(await ScalarAsync(db, "SELECT COUNT(*) FROM dbo.CM_Deliveries")));

                await ExecuteAsync(db, InsertDelivery("claim-test"));
                var duplicateDelivery = await Assert.ThrowsAsync<SqlException>(() => ExecuteAsync(db, InsertDelivery("claim-test")));
                Assert.Contains(duplicateDelivery.Number, new[] { 2601, 2627 });

                await using var other = new SqlConnection(testBuilder.ConnectionString);
                await other.OpenAsync();
                const string claim = "UPDATE dbo.CM_Deliveries SET Status='Sending', AttemptCount=AttemptCount+1 WHERE IdempotencyKey='claim-test' AND Status='Pending'";
                var claimed = await Task.WhenAll(ExecuteAsync(db, claim), ExecuteAsync(other, claim));
                Assert.Equal(1, claimed.Sum());
                Assert.Equal(1, Convert.ToInt32(await ScalarAsync(db, "SELECT AttemptCount FROM dbo.CM_Deliveries WHERE IdempotencyKey='claim-test'")));
            }
        }
        finally
        {
            // Only this run's generated database may be removed; pooling is disabled and connections are disposed.
            if (created && name.StartsWith("DHR_CommsTest_", StringComparison.Ordinal) && Guid.TryParseExact(name[14..], "N", out _))
                await ExecuteAsync(master, $"DROP DATABASE [{name}]");
        }
    }

    private static string InsertDelivery(string key) => $"""
        INSERT dbo.CM_Deliveries(CommunicationMessageId,CommunicationProviderConnectionId,CommunicationDestinationId,OrganisationBusinessEntityId,Status,IdempotencyKey,CreatedAtUtc)
            VALUES(1,1,1,1,'Pending','{key}',SYSUTCDATETIME());
        """;

    private static async Task ApplyScriptAsync(SqlConnection connection, string script)
    {
        await using var resource = typeof(HrmTeContext).Assembly.GetManifestResourceStream("HRM.Database.Scripts." + script)
            ?? throw new InvalidOperationException("Missing embedded SQL script: " + script);
        using var reader = new StreamReader(resource);
        await ExecuteAsync(connection, await reader.ReadToEndAsync());
    }

    private static async Task<int> ExecuteAsync(SqlConnection connection, string sql, SqlTransaction? transaction = null)
    {
        using var command = new SqlCommand(sql, connection, transaction) { CommandTimeout = 30 };
        return await command.ExecuteNonQueryAsync();
    }

    private static async Task<object?> ScalarAsync(SqlConnection connection, string sql)
    {
        using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
        return await command.ExecuteScalarAsync();
    }
}

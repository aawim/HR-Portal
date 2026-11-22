using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HRM.AuditLogs.Persistence;

public static class AuditSchemaBootstrapper
{
    public static async Task EnsureSchemaAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = services.GetService<IDbContextFactory<AuditDbContext>>();
            if (factory is null) return;

            await using var db = await factory.CreateDbContextAsync(cancellationToken);
            if (!db.Database.IsSqlServer()) return;

            // Ensure target database (e.g. HRMAuditLogs) exists before running DDL
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
                    cmd.CommandText = @"
IF DB_ID(@dbName) IS NULL
BEGIN
    DECLARE @sql NVARCHAR(MAX) = N'CREATE DATABASE ' + QUOTENAME(@dbName) + N';';
    EXEC sp_executesql @sql;
END";
                    cmd.Parameters.AddWithValue("@dbName", targetDbName);
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            await db.Database.ExecuteSqlRawAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'audit')
    EXEC('CREATE SCHEMA [audit]');

IF OBJECT_ID('[audit].[AuditLogs]', 'U') IS NULL
BEGIN
    CREATE TABLE [audit].[AuditLogs] (
        [AuditLogId] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_AuditLogs] PRIMARY KEY,
        [EventId] UNIQUEIDENTIFIER NOT NULL,
        [TenantId] INT NULL,
        [OrganisationId] INT NULL,
        [DivisionId] INT NULL,
        [UserId] INT NULL,
        [UserName] NVARCHAR(200) NULL,
        [ActorType] NVARCHAR(32) NOT NULL,
        [Action] NVARCHAR(64) NOT NULL,
        [Outcome] NVARCHAR(32) NOT NULL CONSTRAINT [DF_AuditLogs_Outcome] DEFAULT ('Committed'),
        [Module] NVARCHAR(64) NOT NULL,
        [EntityType] NVARCHAR(128) NOT NULL,
        [EntityId] NVARCHAR(128) NOT NULL,
        [Description] NVARCHAR(1000) NOT NULL,
        [CorrelationId] NVARCHAR(128) NULL,
        [RequestId] NVARCHAR(128) NULL,
        [Source] NVARCHAR(64) NOT NULL,
        [Reason] NVARCHAR(1000) NULL,
        [IpAddress] NVARCHAR(64) NULL,
        [Url] NVARCHAR(500) NULL,
        [OccurredAtUtc] DATETIME2 NOT NULL,
        [MetadataJson] NVARCHAR(MAX) NULL,
        [SchemaVersion] INT NOT NULL CONSTRAINT [DF_AuditLogs_SchemaVersion] DEFAULT (1),
        [RetentionUntilUtc] DATETIME2 NULL,
        [IsLegalHold] BIT NOT NULL CONSTRAINT [DF_AuditLogs_IsLegalHold] DEFAULT (0),
        [ArchivedAtUtc] DATETIME2 NULL
    );
    CREATE UNIQUE INDEX [UX_AuditLogs_EventId] ON [audit].[AuditLogs]([EventId]);
    CREATE INDEX [IX_AuditLogs_Tenant_Org_OccurredAtUtc] ON [audit].[AuditLogs]([TenantId], [OrganisationId], [OccurredAtUtc]);
    CREATE INDEX [IX_AuditLogs_Org_Entity_OccurredAtUtc] ON [audit].[AuditLogs]([OrganisationId], [EntityType], [EntityId], [OccurredAtUtc]);
    CREATE INDEX [IX_AuditLogs_Org_User_OccurredAtUtc] ON [audit].[AuditLogs]([OrganisationId], [UserId], [OccurredAtUtc]);
    CREATE INDEX [IX_AuditLogs_Org_Module_Action_OccurredAtUtc] ON [audit].[AuditLogs]([OrganisationId], [Module], [Action], [OccurredAtUtc]);
    CREATE INDEX [IX_AuditLogs_Org_Division_OccurredAtUtc] ON [audit].[AuditLogs]([OrganisationId], [DivisionId], [OccurredAtUtc]);
    CREATE INDEX [IX_AuditLogs_CorrelationId] ON [audit].[AuditLogs]([CorrelationId]);
    CREATE INDEX [IX_AuditLogs_OccurredAtUtc] ON [audit].[AuditLogs]([OccurredAtUtc]);
END;

IF COL_LENGTH('[audit].[AuditLogs]', 'UserName') IS NULL
    ALTER TABLE [audit].[AuditLogs] ADD [UserName] NVARCHAR(200) NULL;
IF COL_LENGTH('[audit].[AuditLogs]', 'Url') IS NULL
    ALTER TABLE [audit].[AuditLogs] ADD [Url] NVARCHAR(500) NULL;

IF OBJECT_ID('[audit].[AuditLogDetails]', 'U') IS NULL
BEGIN
    CREATE TABLE [audit].[AuditLogDetails] (
        [AuditLogDetailId] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_AuditLogDetails] PRIMARY KEY,
        [AuditLogId] BIGINT NOT NULL,
        [PropertyName] NVARCHAR(128) NOT NULL,
        [OldValue] NVARCHAR(MAX) NULL,
        [NewValue] NVARCHAR(MAX) NULL,
        [ValueType] NVARCHAR(64) NOT NULL,
        [IsRedacted] BIT NOT NULL CONSTRAINT [DF_AuditLogDetails_IsRedacted] DEFAULT (0),
        CONSTRAINT [FK_AuditLogDetails_AuditLogs] FOREIGN KEY ([AuditLogId]) REFERENCES [audit].[AuditLogs]([AuditLogId]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_AuditLogDetails_AuditLogId] ON [audit].[AuditLogDetails]([AuditLogId]);
    CREATE INDEX [IX_AuditLogDetails_AuditLogId_PropertyName] ON [audit].[AuditLogDetails]([AuditLogId], [PropertyName]);
END;
", cancellationToken);

            await db.Database.ExecuteSqlRawAsync(@"
CREATE OR ALTER TRIGGER [audit].[TR_AuditLogs_AppendOnly]
ON [audit].[AuditLogs]
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted) AND NOT EXISTS (SELECT 1 FROM inserted)
    BEGIN
        THROW 51001, 'Audit log records are append-only and cannot be deleted.', 1;
    END

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON i.AuditLogId = d.AuditLogId
        WHERE i.EventId <> d.EventId
           OR ISNULL(i.TenantId, -1) <> ISNULL(d.TenantId, -1)
           OR ISNULL(i.OrganisationId, -1) <> ISNULL(d.OrganisationId, -1)
           OR ISNULL(i.UserId, -1) <> ISNULL(d.UserId, -1)
           OR i.Action <> d.Action
           OR i.Outcome <> d.Outcome
           OR i.Module <> d.Module
           OR i.EntityType <> d.EntityType
           OR i.EntityId <> d.EntityId
           OR i.Description <> d.Description
           OR i.OccurredAtUtc <> d.OccurredAtUtc
    )
    BEGIN
        THROW 51002, 'Audit log records are immutable and cannot be modified.', 1;
    END

    UPDATE t
    SET t.IsLegalHold = i.IsLegalHold,
        t.ArchivedAtUtc = i.ArchivedAtUtc,
        t.RetentionUntilUtc = i.RetentionUntilUtc
    FROM [audit].[AuditLogs] t
    JOIN inserted i ON t.AuditLogId = i.AuditLogId;
END;
", cancellationToken);

            await db.Database.ExecuteSqlRawAsync(@"
CREATE OR ALTER TRIGGER [audit].[TR_AuditLogDetails_AppendOnly]
ON [audit].[AuditLogDetails]
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51003, 'Audit log detail records are immutable and append-only.', 1;
END;
", cancellationToken);
        }
        catch (Exception ex)
        {
            var logger = services.GetService<ILoggerFactory>()?.CreateLogger("HRM.AuditLogs.Bootstrap");
            logger?.LogWarning(ex, "Audit schema bootstrap skipped or failed.");
        }
    }
}

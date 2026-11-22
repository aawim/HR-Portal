-- ============================================================================
-- HRM.ActivityLogs — Standalone Activity Logging Schema
-- Dedicated Database: HRMActivityLogs (schema [activity])
-- Idempotent deployment script matching EF Core migration:
--   20261121110000_InitialActivityLogsSchema
-- ============================================================================

IF DB_ID(N'HRMActivityLogs') IS NULL
BEGIN
    CREATE DATABASE [HRMActivityLogs];
END
GO

USE [HRMActivityLogs];
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'activity')
BEGIN
    EXEC(N'CREATE SCHEMA [activity]');
END
GO

IF OBJECT_ID(N'[activity].[ActivityLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [activity].[ActivityLogs] (
        [ActivityLogId]     BIGINT           IDENTITY(1, 1) NOT NULL,
        [EventId]           UNIQUEIDENTIFIER NOT NULL,
        [TenantId]          INT              NULL,
        [OrganisationId]    INT              NULL,
        [DivisionId]        INT              NULL,
        [TeamId]            INT              NULL,
        [UserId]            INT              NULL,
        [UserName]          NVARCHAR(200)    NULL,
        [ActorType]         NVARCHAR(32)     NOT NULL,
        [IpAddress]         NVARCHAR(64)     NULL,
        [Url]               NVARCHAR(500)    NULL,
        [ActivityCategory]  NVARCHAR(64)     NOT NULL,
        [ActivityType]      NVARCHAR(128)    NOT NULL,
        [Module]            NVARCHAR(64)     NOT NULL,
        [Action]            NVARCHAR(64)     NOT NULL,
        [Description]       NVARCHAR(1000)   NOT NULL,
        [TargetEntityType]  NVARCHAR(128)    NULL,
        [TargetEntityId]    NVARCHAR(128)    NULL,
        [TargetDisplayName] NVARCHAR(256)    NULL,
        [CorrelationId]     NVARCHAR(128)    NULL,
        [SessionId]         NVARCHAR(128)    NULL,
        [Source]            NVARCHAR(128)    NOT NULL,
        [MetadataJson]      NVARCHAR(MAX)    NULL,
        [OccurredAtUtc]     DATETIME2(7)     NOT NULL,
        [CreatedAtUtc]      DATETIME2(7)     NOT NULL,
        CONSTRAINT [PK_ActivityLogs] PRIMARY KEY CLUSTERED ([ActivityLogId] ASC)
    );
END
GO

IF COL_LENGTH(N'[activity].[ActivityLogs]', N'UserName') IS NULL
    ALTER TABLE [activity].[ActivityLogs] ADD [UserName] NVARCHAR(200) NULL;
GO

IF COL_LENGTH(N'[activity].[ActivityLogs]', N'IpAddress') IS NULL
    ALTER TABLE [activity].[ActivityLogs] ADD [IpAddress] NVARCHAR(64) NULL;
GO

IF COL_LENGTH(N'[activity].[ActivityLogs]', N'Url') IS NULL
    ALTER TABLE [activity].[ActivityLogs] ADD [Url] NVARCHAR(500) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ActivityLogs_EventId' AND object_id = OBJECT_ID(N'[activity].[ActivityLogs]'))
    CREATE UNIQUE NONCLUSTERED INDEX [UX_ActivityLogs_EventId] ON [activity].[ActivityLogs] ([EventId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ActivityLogs_Tenant_Org_Occurred' AND object_id = OBJECT_ID(N'[activity].[ActivityLogs]'))
    CREATE NONCLUSTERED INDEX [IX_ActivityLogs_Tenant_Org_Occurred] ON [activity].[ActivityLogs] ([TenantId], [OrganisationId], [OccurredAtUtc]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ActivityLogs_User_Occurred' AND object_id = OBJECT_ID(N'[activity].[ActivityLogs]'))
    CREATE NONCLUSTERED INDEX [IX_ActivityLogs_User_Occurred] ON [activity].[ActivityLogs] ([UserId], [OccurredAtUtc]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ActivityLogs_Division_Occurred' AND object_id = OBJECT_ID(N'[activity].[ActivityLogs]'))
    CREATE NONCLUSTERED INDEX [IX_ActivityLogs_Division_Occurred] ON [activity].[ActivityLogs] ([DivisionId], [OccurredAtUtc]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ActivityLogs_Team_Occurred' AND object_id = OBJECT_ID(N'[activity].[ActivityLogs]'))
    CREATE NONCLUSTERED INDEX [IX_ActivityLogs_Team_Occurred] ON [activity].[ActivityLogs] ([TeamId], [OccurredAtUtc]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ActivityLogs_Category_Type_Occurred' AND object_id = OBJECT_ID(N'[activity].[ActivityLogs]'))
    CREATE NONCLUSTERED INDEX [IX_ActivityLogs_Category_Type_Occurred] ON [activity].[ActivityLogs] ([ActivityCategory], [ActivityType], [OccurredAtUtc]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ActivityLogs_Target_Occurred' AND object_id = OBJECT_ID(N'[activity].[ActivityLogs]'))
    CREATE NONCLUSTERED INDEX [IX_ActivityLogs_Target_Occurred] ON [activity].[ActivityLogs] ([TargetEntityType], [TargetEntityId], [OccurredAtUtc]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ActivityLogs_CorrelationId' AND object_id = OBJECT_ID(N'[activity].[ActivityLogs]'))
    CREATE NONCLUSTERED INDEX [IX_ActivityLogs_CorrelationId] ON [activity].[ActivityLogs] ([CorrelationId]);
GO

IF OBJECT_ID(N'[activity].[ActivityOutbox]', N'U') IS NULL
BEGIN
    CREATE TABLE [activity].[ActivityOutbox] (
        [OutboxId]         BIGINT           IDENTITY(1, 1) NOT NULL,
        [EventId]          UNIQUEIDENTIFIER NOT NULL,
        [PayloadJson]      NVARCHAR(MAX)    NOT NULL,
        [AttemptCount]     INT              NOT NULL CONSTRAINT [DF_ActivityOutbox_AttemptCount] DEFAULT (0),
        [NextAttemptAtUtc] DATETIME2(7)     NOT NULL,
        [CreatedAtUtc]     DATETIME2(7)     NOT NULL,
        [ProcessedAtUtc]   DATETIME2(7)     NULL,
        [LastError]        NVARCHAR(1000)   NULL,
        CONSTRAINT [PK_ActivityOutbox] PRIMARY KEY CLUSTERED ([OutboxId] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ActivityOutbox_EventId' AND object_id = OBJECT_ID(N'[activity].[ActivityOutbox]'))
    CREATE UNIQUE NONCLUSTERED INDEX [UX_ActivityOutbox_EventId] ON [activity].[ActivityOutbox] ([EventId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ActivityOutbox_Pending' AND object_id = OBJECT_ID(N'[activity].[ActivityOutbox]'))
    CREATE NONCLUSTERED INDEX [IX_ActivityOutbox_Pending] ON [activity].[ActivityOutbox] ([ProcessedAtUtc], [NextAttemptAtUtc]);
GO

-- Script: 20261110_ErrorLogsTable.sql
-- Description: Creates the ERR_ErrorLogs table for application diagnostic error recording.
-- Standard: Feature-initial prefix ERR_, PascalCase plural name ERR_ErrorLogs.

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ERR_ErrorLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ERR_ErrorLogs](
        [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_ERR_ErrorLogs] PRIMARY KEY ([Id]),
        [TimestampUtc] DATETIME2(7) NOT NULL,
        [TimestampLocal] DATETIME2(7) NOT NULL,
        [CriticalityLevel] INT NOT NULL,
        [ApplicationSource] NVARCHAR(100) NOT NULL,
        [Environment] NVARCHAR(50) NOT NULL,
        [MachineName] NVARCHAR(100) NOT NULL,
        [ProcessId] INT NOT NULL,
        [ThreadId] INT NOT NULL,
        [TraceId] NVARCHAR(150) NULL,
        [Username] NVARCHAR(250) NULL,
        [UserId] NVARCHAR(150) NULL,
        [RemoteIp] NVARCHAR(100) NULL,
        [PageLocation] NVARCHAR(1000) NULL,
        [HttpMethod] NVARCHAR(20) NULL,
        [StatusCode] INT NULL,
        [UserAgent] NVARCHAR(1000) NULL,
        [ExceptionType] NVARCHAR(300) NOT NULL,
        [ErrorMessage] NVARCHAR(MAX) NOT NULL,
        [StackTrace] NVARCHAR(MAX) NULL,
        [InnerExceptionType] NVARCHAR(300) NULL,
        [InnerErrorMessage] NVARCHAR(MAX) NULL,
        [InnerStackTrace] NVARCHAR(MAX) NULL,
        [FileName] NVARCHAR(500) NULL,
        [FilePath] NVARCHAR(1000) NULL,
        [LineNumber] INT NOT NULL CONSTRAINT [DF_ERR_ErrorLogs_LineNumber] DEFAULT (0),
        [ColumnNumber] INT NOT NULL CONSTRAINT [DF_ERR_ErrorLogs_ColumnNumber] DEFAULT (0),
        [ClassName] NVARCHAR(300) NULL,
        [MethodName] NVARCHAR(300) NULL,
        [Namespace] NVARCHAR(300) NULL,
        [AssemblyName] NVARCHAR(300) NULL,
        [RootCauseSummary] NVARCHAR(MAX) NULL,
        [RecommendedAction] NVARCHAR(MAX) NULL,
        [DiagnosticStepsJson] NVARCHAR(MAX) NULL,
        [SuggestedCodePattern] NVARCHAR(MAX) NULL,
        [CustomDataJson] NVARCHAR(MAX) NULL,
        [IsResolved] BIT NOT NULL CONSTRAINT [DF_ERR_ErrorLogs_IsResolved] DEFAULT (0),
        [ResolvedBy] NVARCHAR(250) NULL,
        [ResolvedAt] DATETIME2(7) NULL,
        [ResolutionNotes] NVARCHAR(MAX) NULL
    );

    CREATE NONCLUSTERED INDEX [IX_ERR_ErrorLogs_TimestampUtc] ON [dbo].[ERR_ErrorLogs]([TimestampUtc] DESC);
    CREATE NONCLUSTERED INDEX [IX_ERR_ErrorLogs_Criticality] ON [dbo].[ERR_ErrorLogs]([CriticalityLevel], [IsResolved]);
    CREATE NONCLUSTERED INDEX [IX_ERR_ErrorLogs_ApplicationSource] ON [dbo].[ERR_ErrorLogs]([ApplicationSource], [TimestampUtc] DESC);
END
GO

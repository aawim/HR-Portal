BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025120400_JobTypeOvertimeRules'
)
BEGIN
    CREATE TABLE [OT_JobTypeOvertimeRules] (
        [Id] int NOT NULL IDENTITY,
        [OrganisationId] int NOT NULL,
        [JobTypeId] int NOT NULL,
        [IsAllowed] bit NOT NULL,
        [MaximumWeeklyHours] decimal(5,2) NOT NULL,
        [MaximumDailyHours] decimal(5,2) NULL,
        [Version] int NOT NULL,
        [OperationLogId] int NOT NULL,
        CONSTRAINT [PK_OT_JobTypeOvertimeRules] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_OT_JobTypeOvertimeRules_Limits] CHECK (([IsAllowed] = 0 AND [MaximumWeeklyHours] = 0 AND [MaximumDailyHours] IS NULL) OR ([IsAllowed] = 1 AND [MaximumWeeklyHours] > 0 AND [MaximumWeeklyHours] <= 168 AND ([MaximumDailyHours] IS NULL OR ([MaximumDailyHours] > 0 AND [MaximumDailyHours] <= 24 AND [MaximumDailyHours] <= [MaximumWeeklyHours])))),
        CONSTRAINT [FK_OT_JobTypeOvertimeRules_JobTypes_JobTypeId] FOREIGN KEY ([JobTypeId]) REFERENCES [JobTypes] ([JobTypeID]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OT_JobTypeOvertimeRules_OperationLogs_OperationLogId] FOREIGN KEY ([OperationLogId]) REFERENCES [OperationLogs] ([OperationLogID]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OT_JobTypeOvertimeRules_Organisations_OrganisationId] FOREIGN KEY ([OrganisationId]) REFERENCES [Organisations] ([BusinessEntityID]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025120400_JobTypeOvertimeRules'
)
BEGIN
    CREATE INDEX [IX_OT_JobTypeOvertimeRules_JobTypeId] ON [OT_JobTypeOvertimeRules] ([JobTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025120400_JobTypeOvertimeRules'
)
BEGIN
    CREATE INDEX [IX_OT_JobTypeOvertimeRules_OperationLogId] ON [OT_JobTypeOvertimeRules] ([OperationLogId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025120400_JobTypeOvertimeRules'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OT_JobTypeOvertimeRules_OrganisationId_JobTypeId] ON [OT_JobTypeOvertimeRules] ([OrganisationId], [JobTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025120400_JobTypeOvertimeRules'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261025120400_JobTypeOvertimeRules', N'8.0.27');
END;
GO

COMMIT;
GO


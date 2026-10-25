BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025120500_StaffPositionChangeRequests'
)
BEGIN
    CREATE TABLE [SP_StaffPositionChangeRequests] (
        [RequestId] int NOT NULL,
        [OrganisationId] int NOT NULL,
        [JobId] int NOT NULL,
        [IndividualId] int NOT NULL,
        [PositionId] int NOT NULL,
        [ChangeKind] nvarchar(20) NOT NULL,
        [FromDate] date NOT NULL,
        [ToDate] date NULL,
        [OriginalJson] nvarchar(4000) NULL,
        [Reason] nvarchar(500) NOT NULL,
        [CreatedByUserId] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_SP_StaffPositionChangeRequests] PRIMARY KEY ([RequestId]),
        CONSTRAINT [CK_SP_PositionRequest_Dates] CHECK ([ToDate] IS NULL OR [ToDate] >= [FromDate]),
        CONSTRAINT [CK_SP_PositionRequest_Kind] CHECK ([ChangeKind] IN ('Assign','Change','Dates','Remove')),
        CONSTRAINT [FK_SP_StaffPositionChangeRequests_Jobs_JobId] FOREIGN KEY ([JobId]) REFERENCES [Jobs] ([JobID]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SP_StaffPositionChangeRequests_Organisations_OrganisationId] FOREIGN KEY ([OrganisationId]) REFERENCES [Organisations] ([BusinessEntityID]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SP_StaffPositionChangeRequests_Positions_PositionId] FOREIGN KEY ([PositionId]) REFERENCES [Positions] ([PositionID]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SP_StaffPositionChangeRequests_Requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [Requests] ([RequestID]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SP_StaffPositionChangeRequests_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([UserID]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025120500_StaffPositionChangeRequests'
)
BEGIN
    CREATE INDEX [IX_SP_StaffPositionChangeRequests_CreatedByUserId] ON [SP_StaffPositionChangeRequests] ([CreatedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025120500_StaffPositionChangeRequests'
)
BEGIN
    CREATE INDEX [IX_SP_StaffPositionChangeRequests_JobId] ON [SP_StaffPositionChangeRequests] ([JobId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025120500_StaffPositionChangeRequests'
)
BEGIN
    CREATE INDEX [IX_SP_StaffPositionChangeRequests_OrganisationId_JobId] ON [SP_StaffPositionChangeRequests] ([OrganisationId], [JobId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025120500_StaffPositionChangeRequests'
)
BEGIN
    CREATE INDEX [IX_SP_StaffPositionChangeRequests_PositionId] ON [SP_StaffPositionChangeRequests] ([PositionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025120500_StaffPositionChangeRequests'
)
BEGIN
    DECLARE @Types TABLE (Name nvarchar(500), Prefix nvarchar(10));
    INSERT @Types VALUES
    (N'Staff Position Assignment Request',N'SPA'),
    (N'Staff Position Removal Request',N'SPR'),
    (N'Staff Position Change Request',N'SPC'),
    (N'Staff Position Dates Request',N'SPD');
    DECLARE @Name nvarchar(500), @Prefix nvarchar(10), @Sequence int;
    WHILE EXISTS(SELECT 1 FROM @Types)
    BEGIN
        SELECT TOP(1) @Name=Name,@Prefix=Prefix FROM @Types ORDER BY Name;
        IF NOT EXISTS(SELECT 1 FROM dbo.RequestTypes WHERE TypeName=@Name)
        BEGIN
            INSERT dbo.SequenceNumberTypes(TypeName,Prefix,FormatString,IsResetAnnually,StartingNumber,NextNumber)
            VALUES(@Name,@Prefix,'{PREFIX}-{YEAR}-{NUMBER}',0,1,1);
            SET @Sequence=SCOPE_IDENTITY();
            INSERT dbo.RequestTypes(TypeName,SequenceNumberTypeID,ViewURL,IsSystemType)
            VALUES(@Name,@Sequence,'/online/staff-position-requests',1);
        END;
        DELETE FROM @Types WHERE Name=@Name;
    END;
    IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleKey='Staff.Positions.Manage')
    BEGIN
        INSERT dbo.OperationLogs(OperationLogActionID,CreatedByContextID,CreatedDate,UpdatedDate,CreatedByIP,UpdatedByIP,Remarks,UpdatedContextID)
        VALUES(18,1,SYSUTCDATETIME(),SYSUTCDATETIME(),'','','Install staff position request permission.',1);
        DECLARE @Audit int=SCOPE_IDENTITY();
        INSERT dbo.Roles(RoleKey,Name,Description,ContextID,IsSystemRole,OperationLogID)
        VALUES('Staff.Positions.Manage','Staff Positions - Manage','Submit staff position assignment, change, date and removal requests. Approval requires the configured workflow stage.',1,1,@Audit);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025120500_StaffPositionChangeRequests'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261025120500_StaffPositionChangeRequests', N'8.0.27');
END;
GO

COMMIT;
GO


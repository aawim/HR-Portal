SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.RequestNotifications', 'LeaveId') IS NULL
    ALTER TABLE dbo.RequestNotifications ADD LeaveId int NULL
        CONSTRAINT FK_RequestNotifications_Leaves REFERENCES dbo.Leaves(LeaveID);
IF COL_LENGTH('dbo.RequestNotifications', 'HandoverRevision') IS NULL
    ALTER TABLE dbo.RequestNotifications ADD HandoverRevision int NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.RequestNotifications') AND name = 'UX_RequestNotifications_HandoverRecipientRevision')
    EXEC(N'CREATE UNIQUE INDEX UX_RequestNotifications_HandoverRecipientRevision
        ON dbo.RequestNotifications(UserId, NotificationKind, LeaveId, HandoverRevision)
        WHERE LeaveId IS NOT NULL AND HandoverRevision IS NOT NULL;');
COMMIT TRANSACTION;

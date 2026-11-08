SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.RN_RequestNotifications', 'LeaveId') IS NULL
    ALTER TABLE dbo.RN_RequestNotifications ADD LeaveId int NULL
        CONSTRAINT FK_RequestNotifications_Leaves REFERENCES dbo.Leaves(LeaveID);
IF COL_LENGTH('dbo.RN_RequestNotifications', 'HandoverRevision') IS NULL
    ALTER TABLE dbo.RN_RequestNotifications ADD HandoverRevision int NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.RN_RequestNotifications') AND name = 'UX_RequestNotifications_HandoverRecipientRevision')
    EXEC(N'CREATE UNIQUE INDEX UX_RequestNotifications_HandoverRecipientRevision
        ON dbo.RN_RequestNotifications(UserId, NotificationKind, LeaveId, HandoverRevision)
        WHERE LeaveId IS NOT NULL AND HandoverRevision IS NOT NULL;');
COMMIT TRANSACTION;

SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.RN_RequestNotifications', 'NotificationKind') IS NULL
    ALTER TABLE dbo.RN_RequestNotifications ADD NotificationKind nvarchar(32) NOT NULL
        CONSTRAINT DF_RequestNotifications_NotificationKind DEFAULT N'StateChanged' WITH VALUES;
IF COL_LENGTH('dbo.RN_RequestNotifications', 'RequestStageInstanceId') IS NULL
    ALTER TABLE dbo.RN_RequestNotifications ADD RequestStageInstanceId bigint NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.RN_RequestNotifications') AND name = 'UX_RequestNotifications_ActionRecipientStage')
    EXEC(N'CREATE UNIQUE INDEX UX_RequestNotifications_ActionRecipientStage
        ON dbo.RN_RequestNotifications(UserId, RequestStageInstanceId)
        WHERE NotificationKind = ''ActionRequired'' AND RequestStageInstanceId IS NOT NULL;');
COMMIT TRANSACTION;

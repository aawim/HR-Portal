SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.CM_Messages', 'DestinationConnectionId') IS NULL
    ALTER TABLE dbo.CM_Messages ADD DestinationConnectionId bigint NULL;
IF COL_LENGTH('dbo.CM_Messages', 'DestinationExternalId') IS NULL
    ALTER TABLE dbo.CM_Messages ADD DestinationExternalId nvarchar(256) NULL;
IF COL_LENGTH('dbo.CM_Messages', 'DestinationDisplayName') IS NULL
    ALTER TABLE dbo.CM_Messages ADD DestinationDisplayName nvarchar(160) NULL;
IF COL_LENGTH('dbo.CM_AutomationRules', 'CommunicationDestinationId') IS NULL
    ALTER TABLE dbo.CM_AutomationRules ADD CommunicationDestinationId bigint NULL
        CONSTRAINT FK_CM_Rules_Destination REFERENCES dbo.CM_Destinations(CommunicationDestinationId);
IF COL_LENGTH('dbo.CM_Messages', 'CommunicationAutomationRuleId') IS NULL
    ALTER TABLE dbo.CM_Messages ADD CommunicationAutomationRuleId bigint NULL
        CONSTRAINT FK_CM_Messages_Rule REFERENCES dbo.CM_AutomationRules(CommunicationAutomationRuleId);
IF COL_LENGTH('dbo.CM_Messages', 'ApprovalStatus') IS NULL
    ALTER TABLE dbo.CM_Messages ADD ApprovalStatus nvarchar(24) NOT NULL
        CONSTRAINT DF_CM_Messages_ApprovalStatus DEFAULT N'NotRequired' WITH VALUES;
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_CM_Messages_Outbox' AND parent_object_id = OBJECT_ID('dbo.CM_Messages'))
    ALTER TABLE dbo.CM_Messages DROP CONSTRAINT UQ_CM_Messages_Outbox;
IF COL_LENGTH('dbo.CM_Messages', 'ApprovalRequestId') IS NULL
    ALTER TABLE dbo.CM_Messages ADD ApprovalRequestId int NULL CONSTRAINT FK_CM_Messages_ApprovalRequest REFERENCES dbo.Requests(RequestID);
IF COL_LENGTH('dbo.CM_Messages', 'ApprovalJobId') IS NULL
    ALTER TABLE dbo.CM_Messages ADD ApprovalJobId int NULL CONSTRAINT FK_CM_Messages_ApprovalJob REFERENCES dbo.Jobs(JobID);
IF COL_LENGTH('dbo.CM_Messages', 'CommunicationDestinationId') IS NULL
    ALTER TABLE dbo.CM_Messages ADD CommunicationDestinationId bigint NULL CONSTRAINT FK_CM_Messages_Destination REFERENCES dbo.CM_Destinations(CommunicationDestinationId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_CM_Messages_ApprovalRequest' AND object_id = OBJECT_ID('dbo.CM_Messages'))
    EXEC(N'CREATE UNIQUE INDEX UX_CM_Messages_ApprovalRequest ON dbo.CM_Messages(ApprovalRequestId) WHERE ApprovalRequestId IS NOT NULL');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_CM_Messages_Outbox_Rule' AND object_id = OBJECT_ID('dbo.CM_Messages'))
    EXEC(N'CREATE UNIQUE INDEX UX_CM_Messages_Outbox_Rule ON dbo.CM_Messages(CommunicationOutboxId, CommunicationAutomationRuleId)');
COMMIT;

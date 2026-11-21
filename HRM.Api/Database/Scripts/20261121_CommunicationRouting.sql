SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.CM_AutomationRules', 'CommunicationDestinationId') IS NULL
    ALTER TABLE dbo.CM_AutomationRules ADD CommunicationDestinationId bigint NULL
        CONSTRAINT FK_CM_Rules_Destination REFERENCES dbo.CM_Destinations(CommunicationDestinationId);
IF COL_LENGTH('dbo.CM_Messages', 'CommunicationAutomationRuleId') IS NULL
    ALTER TABLE dbo.CM_Messages ADD CommunicationAutomationRuleId bigint NULL
        CONSTRAINT FK_CM_Messages_Rule REFERENCES dbo.CM_AutomationRules(CommunicationAutomationRuleId);
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_CM_Messages_Outbox' AND parent_object_id = OBJECT_ID('dbo.CM_Messages'))
    ALTER TABLE dbo.CM_Messages DROP CONSTRAINT UQ_CM_Messages_Outbox;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_CM_Messages_Outbox_Rule' AND object_id = OBJECT_ID('dbo.CM_Messages'))
    EXEC(N'CREATE UNIQUE INDEX UX_CM_Messages_Outbox_Rule ON dbo.CM_Messages(CommunicationOutboxId, CommunicationAutomationRuleId)');
COMMIT;

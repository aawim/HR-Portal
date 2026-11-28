-- =============================================================================
-- Helpdesk Support Center Permissions (dbo.Roles)
-- Idempotent seed script for Helpdesk.Agent and Helpdesk.Manage
-- =============================================================================

DECLARE @OnlineContextId INT = (SELECT TOP 1 ContextID FROM dbo.Context WHERE ContextKey = 'Online' OR Name = 'Online');
IF @OnlineContextId IS NULL SET @OnlineContextId = 2;

DECLARE @AdminContextId INT = (SELECT TOP 1 ContextID FROM dbo.Context WHERE ContextKey = 'Admin' OR Name = 'Admin');
IF @AdminContextId IS NULL SET @AdminContextId = 1;

DECLARE @IntranetModuleId INT = (SELECT TOP 1 ModuleID FROM dbo.Modules WHERE Name = 'Intranet');
DECLARE @SettingsModuleId INT = (SELECT TOP 1 ModuleID FROM dbo.Modules WHERE Name = 'Settings');

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = N'Helpdesk.Agent')
BEGIN
    INSERT INTO dbo.Roles (RoleKey, Name, Description, ParentRoleID, IsSystemRole, ContextID, OperationLogID, ModuleID)
    VALUES (
        N'Helpdesk.Agent',
        N'Helpdesk Support Center Access',
        N'Access the Online Portal Helpdesk Support Center (/online/support-center) to communicate with users, take over escalated bot chats, and save replies to the Knowledge Base.',
        NULL,
        1,
        @OnlineContextId,
        NULL,
        @IntranetModuleId
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = N'Helpdesk.Manage')
BEGIN
    INSERT INTO dbo.Roles (RoleKey, Name, Description, ParentRoleID, IsSystemRole, ContextID, OperationLogID, ModuleID)
    VALUES (
        N'Helpdesk.Manage',
        N'Manage Helpdesk Support',
        N'Full Helpdesk Support Center access to manage live chats, resolve escalated conversations, and save articles to the Knowledge Base.',
        NULL,
        1,
        @AdminContextId,
        NULL,
        @SettingsModuleId
    );
END;

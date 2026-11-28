-- Organisational Data Isolation and Access Control Framework
-- Physical tables: AC_UserAccessScopes, AC_UserAccessScopeAudits

IF OBJECT_ID(N'dbo.AC_UserAccessScopes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AC_UserAccessScopes
    (
        UserAccessScopeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AC_UserAccessScopes PRIMARY KEY,
        OrganisationId int NOT NULL,
        UserId int NULL,
        UserGroupId int NULL,
        UnitId int NULL,
        TeamId int NULL,
        ScopeType nvarchar(50) NOT NULL,
        IncludeDescendants bit NOT NULL CONSTRAINT DF_AC_UserAccessScopes_IncludeDescendants DEFAULT (1),
        PermissionFilter nvarchar(200) NULL,
        ValidFrom datetime2 NOT NULL,
        ValidTo datetime2 NULL,
        IsActive bit NOT NULL CONSTRAINT DF_AC_UserAccessScopes_IsActive DEFAULT (1),
        GrantedByUserId int NULL,
        CreatedAtUtc datetime2 NOT NULL,
        RevokedByUserId int NULL,
        RevokedAtUtc datetime2 NULL,
        RevocationReason nvarchar(1000) NULL,
        Reason nvarchar(1000) NULL,
        OperationLogId int NULL,
        CONSTRAINT CK_AC_UserAccessScopes_Subject CHECK (([UserId] IS NOT NULL AND [UserGroupId] IS NULL) OR ([UserId] IS NULL AND [UserGroupId] IS NOT NULL)),
        CONSTRAINT CK_AC_UserAccessScopes_ScopeType CHECK ([ScopeType] IN ('Self', 'Team', 'Department', 'Division', 'DivisionAndDescendants', 'Organisation', 'Custom')),
        CONSTRAINT FK_AC_UserAccessScopes_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_AC_UserAccessScopes_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_AC_UserAccessScopes_UserGroups FOREIGN KEY (UserGroupId) REFERENCES dbo.UserGroups (UserGroupID),
        CONSTRAINT FK_AC_UserAccessScopes_Units FOREIGN KEY (UnitId) REFERENCES dbo.OrganisationStructure (OrganisationStructureID),
        CONSTRAINT FK_AC_UserAccessScopes_Teams FOREIGN KEY (TeamId) REFERENCES dbo.Teams (TeamID),
        CONSTRAINT FK_AC_UserAccessScopes_GrantedByUsers FOREIGN KEY (GrantedByUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_AC_UserAccessScopes_RevokedByUsers FOREIGN KEY (RevokedByUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_AC_UserAccessScopes_OperationLogs FOREIGN KEY (OperationLogId) REFERENCES dbo.OperationLogs (OperationLogID)
    );

    CREATE INDEX IX_AC_UserAccessScopes_Org_User_Active ON dbo.AC_UserAccessScopes (OrganisationId, UserId, IsActive);
    CREATE INDEX IX_AC_UserAccessScopes_Org_Group_Active ON dbo.AC_UserAccessScopes (OrganisationId, UserGroupId, IsActive);
    CREATE INDEX IX_AC_UserAccessScopes_Org_Unit_Active ON dbo.AC_UserAccessScopes (OrganisationId, UnitId, IsActive);
END;

IF OBJECT_ID(N'dbo.AC_UserAccessScopeAudits', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AC_UserAccessScopeAudits
    (
        UserAccessScopeAuditId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AC_UserAccessScopeAudits PRIMARY KEY,
        UserAccessScopeId int NULL,
        OrganisationId int NOT NULL,
        TargetUserId int NULL,
        TargetUserGroupId int NULL,
        Action nvarchar(100) NOT NULL,
        ScopeType nvarchar(50) NOT NULL,
        UnitId int NULL,
        TeamId int NULL,
        IncludeDescendants bit NOT NULL,
        ValidFrom datetime2 NULL,
        ValidTo datetime2 NULL,
        PerformedByUserId int NOT NULL,
        TimestampUtc datetime2 NOT NULL,
        Detail nvarchar(2000) NULL,
        OperationLogId int NULL,
        CONSTRAINT FK_AC_UserAccessScopeAudits_Scopes FOREIGN KEY (UserAccessScopeId) REFERENCES dbo.AC_UserAccessScopes (UserAccessScopeId),
        CONSTRAINT FK_AC_UserAccessScopeAudits_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_AC_UserAccessScopeAudits_TargetUsers FOREIGN KEY (TargetUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_AC_UserAccessScopeAudits_TargetUserGroups FOREIGN KEY (TargetUserGroupId) REFERENCES dbo.UserGroups (UserGroupID),
        CONSTRAINT FK_AC_UserAccessScopeAudits_Units FOREIGN KEY (UnitId) REFERENCES dbo.OrganisationStructure (OrganisationStructureID),
        CONSTRAINT FK_AC_UserAccessScopeAudits_Teams FOREIGN KEY (TeamId) REFERENCES dbo.Teams (TeamID),
        CONSTRAINT FK_AC_UserAccessScopeAudits_PerformedByUsers FOREIGN KEY (PerformedByUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_AC_UserAccessScopeAudits_OperationLogs FOREIGN KEY (OperationLogId) REFERENCES dbo.OperationLogs (OperationLogID)
    );

    CREATE INDEX IX_AC_UserAccessScopeAudits_Org_Time ON dbo.AC_UserAccessScopeAudits (OrganisationId, TimestampUtc);
    CREATE INDEX IX_AC_UserAccessScopeAudits_TargetUser_Time ON dbo.AC_UserAccessScopeAudits (TargetUserId, TimestampUtc);
END;

DECLARE @ContextId int = ISNULL((SELECT TOP (1) ContextId FROM dbo.Roles WHERE RoleKey = 'Admin'), 1);
DECLARE @ModuleId int = (SELECT TOP (1) ModuleID FROM dbo.Modules WHERE Name = 'HR');

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = 'Staff.AccessScope.Manage')
    INSERT dbo.Roles (RoleKey, Name, Description, ContextId, ModuleID, IsSystemRole)
    VALUES ('Staff.AccessScope.Manage', 'Manage organisational access scopes', 'Configure division, department, team, and custom hierarchical access scopes.', @ContextId, @ModuleId, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = 'Staff.AccessScope.Simulate')
    INSERT dbo.Roles (RoleKey, Name, Description, ContextId, ModuleID, IsSystemRole)
    VALUES ('Staff.AccessScope.Simulate', 'Simulate user effective access', 'Preview server-calculated effective organisational scopes and field permissions.', @ContextId, @ModuleId, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = 'Employee.PrivateRecord.View')
    INSERT dbo.Roles (RoleKey, Name, Description, ContextId, ModuleID, IsSystemRole)
    VALUES ('Employee.PrivateRecord.View', 'View private employee HR records', 'View confidential employee HR records within authorised organisational scopes.', @ContextId, @ModuleId, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = 'Employee.Salary.View')
    INSERT dbo.Roles (RoleKey, Name, Description, ContextId, ModuleID, IsSystemRole)
    VALUES ('Employee.Salary.View', 'View employee salary and bank details', 'View employee compensation, salary, and bank account details within authorised scopes.', @ContextId, @ModuleId, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = 'Employee.Identification.View')
    INSERT dbo.Roles (RoleKey, Name, Description, ContextId, ModuleID, IsSystemRole)
    VALUES ('Employee.Identification.View', 'View employee identification details', 'View national ID card, passport, and emergency contact details within authorised scopes.', @ContextId, @ModuleId, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = 'Employee.Disciplinary.View')
    INSERT dbo.Roles (RoleKey, Name, Description, ContextId, ModuleID, IsSystemRole)
    VALUES ('Employee.Disciplinary.View', 'View employee disciplinary records', 'View confidential disciplinary records within authorised scopes.', @ContextId, @ModuleId, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = 'Employee.Medical.View')
    INSERT dbo.Roles (RoleKey, Name, Description, ContextId, ModuleID, IsSystemRole)
    VALUES ('Employee.Medical.View', 'View employee medical information', 'View confidential medical and health records within authorised scopes.', @ContextId, @ModuleId, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = 'Directory.PublicProfile.View')
    INSERT dbo.Roles (RoleKey, Name, Description, ContextId, ModuleID, IsSystemRole)
    VALUES ('Directory.PublicProfile.View', 'View public directory profiles', 'View basic public staff directory profile information.', @ContextId, @ModuleId, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = 'Intranet.Announcement.View')
    INSERT dbo.Roles (RoleKey, Name, Description, ContextId, ModuleID, IsSystemRole)
    VALUES ('Intranet.Announcement.View', 'View intranet announcements', 'View public or audience-targeted intranet announcements without exposing private HR records.', @ContextId, @ModuleId, 1);

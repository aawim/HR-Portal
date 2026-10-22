-- Run before deploying the matching application. Preserve explicitly assigned administrators.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.Users','AdminPortalAccessEnabled') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD AdminPortalAccessEnabled bit NOT NULL
        CONSTRAINT DF_Users_AdminPortalAccessEnabled DEFAULT (0);
    ALTER TABLE dbo.Users ADD AdminPortalAccessOrganisationId int NULL;
    EXEC(N'UPDATE u SET AdminPortalAccessEnabled = 1, AdminPortalAccessOrganisationId = u.UserOrganisationId FROM dbo.Users u
      WHERE EXISTS (SELECT 1 FROM dbo.UserRoles ur JOIN dbo.Roles r ON r.RoleID=ur.RoleId
        WHERE ur.UserId=u.UserId AND ur.IsActive=1 AND r.ContextId=1
        AND ur.UserOrganisationId=u.UserOrganisationId)
      OR EXISTS (SELECT 1 FROM dbo.UserAssignedUserGroups a
        JOIN dbo.UserGroups g ON g.UserGroupId=a.UserGroupId
        JOIN dbo.UserGroupRoles gr ON gr.UserGroupId=g.UserGroupId
        JOIN dbo.Roles r ON r.RoleID=gr.RoleId
        WHERE a.UserId=u.UserId AND gr.IsActive=1 AND r.ContextId=1
        AND (g.IsGlobal=1 OR g.UserOrganisationId=u.UserOrganisationId));');
END;
COMMIT;

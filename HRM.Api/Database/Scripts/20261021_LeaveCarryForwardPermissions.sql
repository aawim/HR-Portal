-- Roles are permissions; grant them through the existing HR User Groups.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @Audit int;
INSERT dbo.OperationLogs (OperationLogActionID,CreatedByContextID,CreatedDate,UpdatedDate,
    CreatedByIP,UpdatedByIP,Remarks,UpdatedContextID)
VALUES (18,1,SYSUTCDATETIME(),SYSUTCDATETIME(),'','',
    'User-authorised configuration: add carry-forward View and Process roles to HR Staff and HR Head User Groups.',1);
SET @Audit=SCOPE_IDENTITY();
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey='Leave.CarryForward.View')
    INSERT dbo.Roles (RoleKey,Name,Description,ContextID,IsSystemRole,OperationLogID)
    VALUES ('Leave.CarryForward.View','Leave Carry Forward - View','View tenant carry-forward reviews and processing history. Requires HR membership.',1,1,@Audit);
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey='Leave.CarryForward.Process')
    INSERT dbo.Roles (RoleKey,Name,Description,ContextID,IsSystemRole,OperationLogID,ParentRoleID)
    VALUES ('Leave.CarryForward.Process','Leave Carry Forward - Process','Record HR carry-forward decisions within tenant policy. Requires View permission and HR membership.',1,1,@Audit,
        (SELECT RoleID FROM dbo.Roles WHERE RoleKey='Leave.CarryForward.View'));
INSERT dbo.UserGroupRoles (UserGroupID,RoleID,IsActive,OperationLogID)
SELECT g.UserGroupID,r.RoleID,1,@Audit
FROM dbo.UserGroups g CROSS JOIN dbo.Roles r
WHERE LTRIM(RTRIM(g.GroupName)) IN ('Human Resource: Staff','Human Resource :Head')
    AND r.RoleKey IN ('Leave.CarryForward.View','Leave.CarryForward.Process')
    AND NOT EXISTS (SELECT 1 FROM dbo.UserGroupRoles gr WHERE gr.UserGroupID=g.UserGroupID AND gr.RoleID=r.RoleID);
-- Existing assignments (including deliberately disabled ones) are preserved on rerun.
COMMIT TRANSACTION;

-- Targeted, user-authorised access repair. No unrelated permissions are granted.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserID=425 AND Username='A302409' AND BusinessEntityID=26 AND IsActive=1)
    THROW 51000, 'Ahmed Afzal account identity did not match; no changes applied.', 1;
DECLARE @Organisation int = (SELECT UserOrganisationID FROM dbo.Users WHERE UserID=425);
DECLARE @Permission int = (SELECT RoleID FROM dbo.Roles WHERE RoleKey='Developer');
IF @Organisation IS NULL OR @Permission IS NULL
    THROW 51000, 'User organisation or Developer permission is missing.', 1;
IF (SELECT COUNT(*) FROM dbo.UserGroups WHERE UserOrganisationID=@Organisation AND GroupName='Developer') > 1
    THROW 51000, 'Duplicate Developer groups need reconciliation.', 1;
DECLARE @Group int = (SELECT UserGroupID FROM dbo.UserGroups WHERE UserOrganisationID=@Organisation AND GroupName='Developer');
DECLARE @Action int = CASE WHEN @Group IS NULL THEN 17 ELSE 18 END;
DECLARE @Audit int;
INSERT dbo.OperationLogs (OperationLogActionID,CreatedByIndividualID,CreatedByUserID,CreatedByContextID,CreatedDate,
    UpdatedDate,UpdatedByIndividualID,UpdatedByUserID,CreatedByIP,UpdatedByIP,Remarks,UpdatedContextID,
    CreatedByUserOrganisationID,UpdatedByUserOrganisationID)
VALUES (@Action,NULL,NULL,1,SYSUTCDATETIME(),SYSUTCDATETIME(),NULL,NULL,'','',
    'User-authorised manual configuration: Developer User Group with Developer permission, assigned to Ahmed Afzal (UserID 425). Applied via Codex; audit timestamps are UTC.',
    1,@Organisation,@Organisation);
SET @Audit=SCOPE_IDENTITY();
IF @Group IS NULL
BEGIN
    INSERT dbo.UserGroups (UserOrganisationID,GroupName,OperationLogID,IsGlobal)
    VALUES (@Organisation,'Developer',@Audit,0);
    SET @Group=SCOPE_IDENTITY();
END;
IF EXISTS (SELECT 1 FROM dbo.UserGroupRoles WHERE UserGroupID=@Group AND RoleID=@Permission)
    UPDATE dbo.UserGroupRoles SET IsActive=1,OperationLogID=@Audit WHERE UserGroupID=@Group AND RoleID=@Permission;
ELSE
    INSERT dbo.UserGroupRoles (UserGroupID,RoleID,IsActive,OperationLogID) VALUES (@Group,@Permission,1,@Audit);
IF NOT EXISTS (SELECT 1 FROM dbo.UserAssignedUserGroups WHERE UserID=425 AND UserGroupID=@Group)
    INSERT dbo.UserAssignedUserGroups (UserID,UserGroupID,OperationLogID) VALUES (425,@Group,@Audit);
COMMIT TRANSACTION;
SELECT g.UserGroupID,g.GroupName,r.RoleKey,u.FullName
FROM dbo.UserGroups g JOIN dbo.UserGroupRoles gr ON gr.UserGroupID=g.UserGroupID AND gr.IsActive=1
JOIN dbo.Roles r ON r.RoleID=gr.RoleID JOIN dbo.UserAssignedUserGroups a ON a.UserGroupID=g.UserGroupID
JOIN dbo.Users u ON u.UserID=a.UserID WHERE g.UserGroupID=@Group AND u.UserID=425 AND r.RoleID=@Permission;

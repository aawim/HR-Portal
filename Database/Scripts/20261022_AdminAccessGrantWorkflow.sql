SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.AdminAccessGrantRequests','U') IS NULL
BEGIN
    CREATE TABLE dbo.AdminAccessGrantRequests (
        RequestId int NOT NULL PRIMARY KEY REFERENCES dbo.Requests(RequestID),
        UserId int NOT NULL REFERENCES dbo.Users(UserID),
        OrganisationId int NOT NULL REFERENCES dbo.Organisations(BusinessEntityID),
        JobId int NOT NULL REFERENCES dbo.Jobs(JobID),
        CreatedByUserId int NOT NULL REFERENCES dbo.Users(UserID),
        GroupIdsJson nvarchar(max) NOT NULL,
        PermissionIdsJson nvarchar(max) NOT NULL,
        Reason nvarchar(1000) NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        IsOpen bit NOT NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT CK_AdminAccessGroupsJson CHECK(ISJSON(GroupIdsJson)=1),
        CONSTRAINT CK_AdminAccessPermissionsJson CHECK(ISJSON(PermissionIdsJson)=1)
    );
    CREATE UNIQUE INDEX IX_AdminAccessGrantRequests_UserId ON dbo.AdminAccessGrantRequests(UserId) WHERE IsOpen=1;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.RequestTypes WHERE TypeName='Admin access grant')
    INSERT dbo.RequestTypes(TypeName,ViewURL,IsSystemType) VALUES('Admin access grant','/admin/settings/admin-access',1);
UPDATE dbo.RequestTypes SET ViewURL='/admin/settings/admin-access' WHERE TypeName='Admin access grant';
INSERT dbo.RequestTypesAllowedRequestStateTransitions(RequestTypeID,FromRequestStateID,ToRequestStateID)
SELECT r.RequestTypeID,t.f,t.t FROM dbo.RequestTypes r CROSS JOIN
    (VALUES(1,2),(1,3),(2,3),(2,4),(3,4),(1,5),(2,5),(3,5),(2,6),(3,6))t(f,t)
WHERE r.TypeName='Admin access grant' AND NOT EXISTS
    (SELECT 1 FROM dbo.RequestTypesAllowedRequestStateTransitions x WHERE x.RequestTypeID=r.RequestTypeID AND x.FromRequestStateID=t.f AND x.ToRequestStateID=t.t);
-- Configure tenant/JobType stages and User Groups in existing Request Approval Flows.
COMMIT;

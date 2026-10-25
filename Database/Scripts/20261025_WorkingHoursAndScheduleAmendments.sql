SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.WH_JobTypeWorkingHourRules','U') IS NULL
BEGIN
    CREATE TABLE dbo.WH_JobTypeWorkingHourRules (
        Id int IDENTITY PRIMARY KEY,
        OrganisationId int NOT NULL REFERENCES dbo.Organisations(BusinessEntityID),
        JobTypeId int NOT NULL REFERENCES dbo.JobTypes(JobTypeID),
        RequiredWeeklyMinutes int NOT NULL CHECK (RequiredWeeklyMinutes BETWEEN 1 AND 10080),
        WorkingDaysMask int NOT NULL CHECK (WorkingDaysMask BETWEEN 1 AND 127),
        WeekStartsOn int NOT NULL CHECK (WeekStartsOn BETWEEN 0 AND 6),
        CountDutyHours bit NOT NULL,
        EffectiveFrom date NOT NULL,
        EffectiveTo date NULL,
        Version int NOT NULL,
        OperationLogId int NOT NULL REFERENCES dbo.OperationLogs(OperationLogID),
        CONSTRAINT CK_WH_RulePeriod CHECK (EffectiveTo IS NULL OR EffectiveTo >= EffectiveFrom)
    );
    CREATE UNIQUE INDEX IX_WH_JobTypeWorkingHourRules_OrganisationId_JobTypeId_EffectiveFrom
        ON dbo.WH_JobTypeWorkingHourRules(OrganisationId,JobTypeId,EffectiveFrom);
    CREATE INDEX IX_WH_JobTypeWorkingHourRules_JobTypeId ON dbo.WH_JobTypeWorkingHourRules(JobTypeId);
    CREATE INDEX IX_WH_JobTypeWorkingHourRules_OperationLogId ON dbo.WH_JobTypeWorkingHourRules(OperationLogId);
END;
IF OBJECT_ID('dbo.SA_OrganisationScheduleAmendments','U') IS NULL
BEGIN
    CREATE TABLE dbo.SA_OrganisationScheduleAmendments (
        Id int IDENTITY PRIMARY KEY,
        OrganisationId int NOT NULL REFERENCES dbo.Organisations(BusinessEntityID),
        WorkDate date NOT NULL,
        OriginalStart time NOT NULL,
        RevisedStart time NOT NULL,
        Reason nvarchar(1000) NOT NULL,
        IsActive bit NOT NULL,
        Version int NOT NULL,
        OperationLogId int NOT NULL REFERENCES dbo.OperationLogs(OperationLogID),
        CONSTRAINT CK_SA_LaterStart CHECK (RevisedStart > OriginalStart)
    );
    CREATE UNIQUE INDEX IX_SA_OrganisationScheduleAmendments_OrganisationId_WorkDate_OriginalStart
        ON dbo.SA_OrganisationScheduleAmendments(OrganisationId,WorkDate,OriginalStart);
    CREATE INDEX IX_SA_OrganisationScheduleAmendments_OperationLogId ON dbo.SA_OrganisationScheduleAmendments(OperationLogId);
END;
IF OBJECT_ID('dbo.WH_WeeklyHourExceptionRequests','U') IS NULL
BEGIN
    CREATE TABLE dbo.WH_WeeklyHourExceptionRequests (
        RequestId int NOT NULL PRIMARY KEY REFERENCES dbo.Requests(RequestID),
        OrganisationId int NOT NULL,
        JobId int NOT NULL REFERENCES dbo.Jobs(JobID),
        IndividualId int NOT NULL,
        WeekStart date NOT NULL,
        ExcusedMinutes int NOT NULL CHECK (ExcusedMinutes BETWEEN 1 AND 10080),
        Reason nvarchar(1000) NOT NULL,
        CreatedAtUtc datetime2 NOT NULL
    );
    CREATE INDEX IX_WH_WeeklyHourExceptionRequests_OrganisationId_IndividualId_WeekStart
        ON dbo.WH_WeeklyHourExceptionRequests(OrganisationId,IndividualId,WeekStart);
    CREATE INDEX IX_WH_WeeklyHourExceptionRequests_JobId ON dbo.WH_WeeklyHourExceptionRequests(JobId);
END;
IF NOT EXISTS (SELECT 1 FROM dbo.RequestTypes WHERE TypeName='Weekly Hours Exception Request')
BEGIN
    INSERT dbo.SequenceNumberTypes(TypeName,Prefix,FormatString,IsResetAnnually,StartingNumber,NextNumber)
    VALUES('Weekly Hours Exception Requests','WHE','{PREFIX}-{YEAR}-{NUMBER}',0,1,1);
    DECLARE @Sequence int = SCOPE_IDENTITY();
    INSERT dbo.RequestTypes(TypeName,SequenceNumberTypeID,ViewURL,IsSystemType)
    VALUES('Weekly Hours Exception Request',@Sequence,'/online/attendance/attendance-request/weekly-hours-exception',1);
END;
-- Permissions are assigned through existing User Groups and remain subject to tenant permission limits.
DECLARE @Audit int;
IF EXISTS (SELECT 1 FROM (VALUES ('Attendance.WorkingHours.Configure'),('Attendance.Schedule.Amend'),('Attendance.WorkingHours.Review')) keys(RoleKey)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.RoleKey=keys.RoleKey))
BEGIN
    INSERT dbo.OperationLogs(OperationLogActionID,CreatedByContextID,CreatedDate,UpdatedDate,CreatedByIP,UpdatedByIP,Remarks,UpdatedContextID)
    VALUES(18,1,SYSUTCDATETIME(),SYSUTCDATETIME(),'','','Install working-hours and official schedule amendment permissions.',1);
    SET @Audit=SCOPE_IDENTITY();
    INSERT dbo.Roles(RoleKey,Name,Description,ContextID,IsSystemRole,OperationLogID)
    SELECT k.RoleKey,k.Name,k.Description,1,1,@Audit FROM (VALUES
        ('Attendance.WorkingHours.Configure','Working Hours - Configure','Configure tenant and Job Type weekly working-hour rules.'),
        ('Attendance.Schedule.Amend','Official Schedule - Amend','Record and withdraw temporary tenant official start-time amendments.'),
        ('Attendance.WorkingHours.Review','Working Hours - Review','Review staff weekly working hours within the current tenant.')
    ) k(RoleKey,Name,Description) WHERE NOT EXISTS(SELECT 1 FROM dbo.Roles r WHERE r.RoleKey=k.RoleKey);
    INSERT dbo.UserGroupRoles(UserGroupID,RoleID,IsActive,OperationLogID)
    SELECT g.UserGroupID,r.RoleID,1,@Audit FROM dbo.UserGroups g CROSS JOIN dbo.Roles r
    WHERE LTRIM(RTRIM(g.GroupName)) IN ('Human Resource: Staff','Human Resource :Head')
        AND r.RoleKey IN ('Attendance.WorkingHours.Configure','Attendance.Schedule.Amend','Attendance.WorkingHours.Review')
        AND NOT EXISTS(SELECT 1 FROM dbo.UserGroupRoles a WHERE a.UserGroupID=g.UserGroupID AND a.RoleID=r.RoleID);
END;
-- Extend a tenant's permission catalogue only where equivalent attendance access
-- already exists. A deliberately disabled assignment is never re-enabled.
DECLARE @TenantPermissions TABLE(UserOrganisationId int, RoleId int);
INSERT @TenantPermissions
SELECT DISTINCT allowed.UserOrganisationID, feature.RoleID
FROM dbo.UserOrganisationRoles allowed JOIN dbo.Roles existing ON existing.RoleID=allowed.RoleID
CROSS JOIN dbo.Roles feature
WHERE allowed.IsActive=1 AND (
    (existing.RoleKey IN ('Admin','Attendance.Adjust') AND feature.RoleKey IN ('Attendance.WorkingHours.Configure','Attendance.Schedule.Amend')) OR
    (existing.RoleKey IN ('Admin','Attendance.View','Attendance.Adjust') AND feature.RoleKey='Attendance.WorkingHours.Review'))
AND NOT EXISTS(SELECT 1 FROM dbo.UserOrganisationRoles a WHERE a.UserOrganisationID=allowed.UserOrganisationID AND a.RoleID=feature.RoleID);
IF EXISTS(SELECT 1 FROM @TenantPermissions)
BEGIN
    IF @Audit IS NULL
    BEGIN
        INSERT dbo.OperationLogs(OperationLogActionID,CreatedByContextID,CreatedDate,UpdatedDate,CreatedByIP,UpdatedByIP,Remarks,UpdatedContextID)
        VALUES(18,1,SYSUTCDATETIME(),SYSUTCDATETIME(),'','','Extend existing tenant attendance permission limits for working-hours features.',1);
        SET @Audit=SCOPE_IDENTITY();
    END;
    INSERT dbo.UserOrganisationRoles(UserOrganisationID,RoleID,IsActive,OperationLogID)
    SELECT UserOrganisationId,RoleId,1,@Audit FROM @TenantPermissions;
END;
COMMIT TRANSACTION;

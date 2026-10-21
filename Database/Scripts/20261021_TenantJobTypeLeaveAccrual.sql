SET XACT_ABORT ON;
BEGIN TRANSACTION;
-- Decimal values preserve every existing balance and allow 2.5-day monthly credits.
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.JobLeaveTypes') AND name='RemainingDays' AND TYPE_NAME(user_type_id)='int')
    ALTER TABLE dbo.JobLeaveTypes ALTER COLUMN RemainingDays decimal(10,2) NULL;
IF OBJECT_ID('dbo.JobTypeLeaveAccrualRules','U') IS NULL
BEGIN
    CREATE TABLE dbo.JobTypeLeaveAccrualRules (
        Id int IDENTITY PRIMARY KEY,
        OrganisationId int NOT NULL REFERENCES dbo.Organisations(BusinessEntityID),
        JobTypeId int NOT NULL REFERENCES dbo.JobTypes(JobTypeID),
        LeaveTypeId int NOT NULL REFERENCES dbo.LeaveTypes(LeaveTypeID),
        GrantDays decimal(10,2) NOT NULL,
        FirstGrantAfterMonths int NOT NULL,
        RepeatEveryMonths int NOT NULL,
        CarryForwardMode nvarchar(20) NOT NULL,
        CarryForwardLimit decimal(10,2) NULL,
        MaximumBalance decimal(10,2) NULL,
        EffectiveFrom date NOT NULL,
        IsEnabled bit NOT NULL,
        Version int NOT NULL,
        OperationLogId int NULL REFERENCES dbo.OperationLogs(OperationLogID),
        CONSTRAINT UX_JobTypeLeaveAccrualRules_TenantJobLeave UNIQUE(OrganisationId,JobTypeId,LeaveTypeId),
        CONSTRAINT CK_JobTypeLeaveAccrualRules_Schedule CHECK(GrantDays>0 AND GrantDays<=366 AND FirstGrantAfterMonths>=0 AND RepeatEveryMonths>0),
        CONSTRAINT CK_JobTypeLeaveAccrualRules_Carry CHECK(CarryForwardMode IN ('Unspecified','None','Unlimited','Capped') AND
            (CarryForwardMode<>'Capped' OR (CarryForwardLimit IS NOT NULL AND CarryForwardLimit>=0)) AND
            (IsEnabled=0 OR CarryForwardMode<>'Unspecified'))
    );
END;
IF OBJECT_ID('dbo.LeaveAccrualPostings','U') IS NULL
BEGIN
    CREATE TABLE dbo.LeaveAccrualPostings (
        Id bigint IDENTITY PRIMARY KEY,
        RuleId int NOT NULL REFERENCES dbo.JobTypeLeaveAccrualRules(Id),
        RuleVersion int NOT NULL,
        JobId int NOT NULL REFERENCES dbo.Jobs(JobID),
        JobLeaveTypeId int NOT NULL REFERENCES dbo.JobLeaveTypes(JobLeaveTypeID),
        DueDate date NOT NULL,
        GrantedDays decimal(10,2) NOT NULL,
        CarryAdjustment decimal(10,2) NOT NULL,
        PreviousBalance decimal(10,2) NOT NULL,
        NewBalance decimal(10,2) NOT NULL,
        PostedAtUtc datetime2 NOT NULL,
        OperationLogId int NOT NULL REFERENCES dbo.OperationLogs(OperationLogID),
        CONSTRAINT UX_LeaveAccrualPostings_RuleJobDue UNIQUE(RuleId,JobId,DueDate)
    );
END;
-- Supplied defaults are draft configuration for the existing NCIT tenant only.
-- Other tenants and future Job Types configure their own independent rows in Settings.
DECLARE @Tenant int=(SELECT BusinessEntityID FROM dbo.Organisations WHERE BusinessEntityID=3 AND OrganisationName='National Centre for Information Technology');
DECLARE @Leave int;
SELECT TOP(1) @Leave=l.LeaveTypeID FROM dbo.LeaveTypes l
WHERE l.Name='Annual Leave' AND (l.IsGlobal=1 OR l.OrganisationID=@Tenant)
ORDER BY (SELECT COUNT(*) FROM dbo.JobLeaveTypes b JOIN dbo.Jobs j ON j.JobID=b.JobID
          WHERE b.LeaveTypeID=l.LeaveTypeID AND j.OrganisationID=@Tenant AND b.IsValid=1) DESC,l.LeaveTypeID;
IF @Tenant IS NOT NULL AND @Leave IS NOT NULL
    INSERT dbo.JobTypeLeaveAccrualRules(OrganisationId,JobTypeId,LeaveTypeId,GrantDays,FirstGrantAfterMonths,RepeatEveryMonths,
        CarryForwardMode,CarryForwardLimit,MaximumBalance,EffectiveFrom,IsEnabled,Version,OperationLogId)
    SELECT @Tenant,j.JobTypeID,@Leave,CASE WHEN j.TypeName IN ('Contract','Temporary') THEN 2.5 ELSE 30 END,
        CASE j.TypeName WHEN 'Permanent' THEN 12 WHEN 'Political' THEN 0 ELSE 1 END,
        CASE WHEN j.TypeName IN ('Contract','Temporary') THEN 1 ELSE 12 END,
        'Unspecified',NULL,NULL,CONVERT(date,DATEADD(hour,5,SYSUTCDATETIME())),0,1,NULL
    FROM dbo.JobTypes j WHERE j.TypeName IN ('Contract','Permanent','Temporary','Political')
        AND NOT EXISTS(SELECT 1 FROM dbo.JobTypeLeaveAccrualRules r WHERE r.OrganisationId=@Tenant AND r.JobTypeId=j.JobTypeID AND r.LeaveTypeId=@Leave);
COMMIT TRANSACTION;

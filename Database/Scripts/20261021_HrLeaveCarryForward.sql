IF OBJECT_ID(N'dbo.LeaveCarryForwards', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LeaveCarryForwards (
        Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RuleId int NOT NULL REFERENCES dbo.JobTypeLeaveAccrualRules(Id),
        RuleVersion int NOT NULL,
        JobId int NOT NULL REFERENCES dbo.Jobs(JobID),
        JobLeaveTypeId int NOT NULL REFERENCES dbo.JobLeaveTypes(JobLeaveTypeID),
        RenewalDate date NOT NULL,
        PreviousBalance decimal(10,2) NOT NULL,
        CarriedDays decimal(10,2) NOT NULL CHECK (CarriedDays >= 0),
        ProcessedByUserId int NOT NULL,
        ProcessedAtUtc datetime2 NOT NULL,
        Reason nvarchar(1000) NOT NULL,
        OperationLogId int NOT NULL REFERENCES dbo.OperationLogs(OperationLogID),
        CONSTRAINT UQ_LeaveCarryForwards_Renewal UNIQUE (RuleId,JobId,RenewalDate)
    );
END;

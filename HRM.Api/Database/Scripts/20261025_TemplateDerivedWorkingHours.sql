SET XACT_ABORT ON;
IF OBJECT_ID(N'dbo.WH_JobTypeWorkingHourRules', N'U') IS NOT NULL
    ALTER TABLE dbo.WH_JobTypeWorkingHourRules ALTER COLUMN RequiredWeeklyMinutes int NULL;
IF OBJECT_ID(N'dbo.WH_OrganisationWorkingHourRequirements', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WH_OrganisationWorkingHourRequirements (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_WH_OrganisationWorkingHourRequirements PRIMARY KEY,
        OrganisationId int NOT NULL,
        JobTypeId int NULL,
        FromDate date NOT NULL,
        ToDate date NOT NULL,
        RequiredDailyMinutes int NOT NULL,
        Reason nvarchar(1000) NOT NULL,
        IsActive bit NOT NULL,
        Version int NOT NULL,
        OperationLogId int NOT NULL,
        CONSTRAINT CK_WH_OrganisationWorkingHourRequirements_Period CHECK (ToDate >= FromDate),
        CONSTRAINT CK_WH_OrganisationWorkingHourRequirements_Minutes CHECK (RequiredDailyMinutes BETWEEN 1 AND 1440),
        CONSTRAINT FK_WH_OrganisationWorkingHourRequirements_Organisations_OrganisationId FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations(BusinessEntityID),
        CONSTRAINT FK_WH_OrganisationWorkingHourRequirements_JobTypes_JobTypeId FOREIGN KEY (JobTypeId) REFERENCES dbo.JobTypes(JobTypeID),
        CONSTRAINT FK_WH_OrganisationWorkingHourRequirements_OperationLogs_OperationLogId FOREIGN KEY (OperationLogId) REFERENCES dbo.OperationLogs(OperationLogID)
    );
    CREATE UNIQUE INDEX IX_WH_OrganisationWorkingHourRequirements_OrganisationId_JobTypeId_FromDate ON dbo.WH_OrganisationWorkingHourRequirements(OrganisationId, JobTypeId, FromDate);
    CREATE INDEX IX_WH_OrganisationWorkingHourRequirements_JobTypeId ON dbo.WH_OrganisationWorkingHourRequirements(JobTypeId);
    CREATE INDEX IX_WH_OrganisationWorkingHourRequirements_OperationLogId ON dbo.WH_OrganisationWorkingHourRequirements(OperationLogId);
END;

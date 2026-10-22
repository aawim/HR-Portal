-- Additive onboarding storage. Does not create/activate any staff accounts or employment.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.StaffOnboardingRequests','U') IS NULL
BEGIN
    CREATE TABLE dbo.StaffOnboardingRequests (
        RequestId int NOT NULL PRIMARY KEY,
        IndividualId int NOT NULL,
        OrganisationId int NOT NULL,
        OrganisationStructureId int NOT NULL,
        JobTypeId int NOT NULL,
        PositionId int NULL,
        JoinedDate date NOT NULL,
        Remarks nvarchar(1000) NOT NULL,
        CreatedByUserId int NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        IsOpen bit NOT NULL,
        JobId int NULL,
        UserId int NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT FK_Onboarding_Request FOREIGN KEY (RequestId) REFERENCES dbo.Requests(RequestID),
        CONSTRAINT FK_Onboarding_Individual FOREIGN KEY (IndividualId) REFERENCES dbo.Individuals(BusinessEntityID),
        CONSTRAINT FK_Onboarding_Organisation FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations(BusinessEntityID),
        CONSTRAINT FK_Onboarding_Department FOREIGN KEY (OrganisationStructureId) REFERENCES dbo.OrganisationStructure(OrganisationStructureID),
        CONSTRAINT FK_Onboarding_JobType FOREIGN KEY (JobTypeId) REFERENCES dbo.JobTypes(JobTypeID),
        CONSTRAINT FK_Onboarding_Position FOREIGN KEY (PositionId) REFERENCES dbo.Positions(PositionID),
        CONSTRAINT FK_Onboarding_Creator FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_Onboarding_Job FOREIGN KEY (JobId) REFERENCES dbo.Jobs(JobID),
        CONSTRAINT FK_Onboarding_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserID)
    );
    CREATE UNIQUE INDEX IX_StaffOnboardingRequests_IndividualId ON dbo.StaffOnboardingRequests(IndividualId) WHERE IsOpen=1;
    CREATE INDEX IX_StaffOnboardingRequests_OrganisationId_IsOpen ON dbo.StaffOnboardingRequests(OrganisationId,IsOpen);
END;
IF NOT EXISTS(SELECT 1 FROM dbo.RequestTypes WHERE TypeName='Job Position Request')
    INSERT dbo.RequestTypes(TypeName,ViewURL,IsSystemType) VALUES('Job Position Request','/admin/hr/onboarding',1);
UPDATE dbo.RequestTypes SET ViewURL='/admin/hr/onboarding' WHERE TypeName='Job Position Request';
-- Retain all existing transitions. Add rejection and cancellation routes for onboarding's standard stages.
INSERT dbo.RequestTypesAllowedRequestStateTransitions(RequestTypeID,FromRequestStateID,ToRequestStateID)
SELECT r.RequestTypeID,t.f,t.t FROM dbo.RequestTypes r CROSS JOIN (VALUES(1,2),(1,3),(2,3),(3,4),(1,5),(2,5),(3,5),(2,6),(3,6))t(f,t)
WHERE r.TypeName='Job Position Request' AND NOT EXISTS
    (SELECT 1 FROM dbo.RequestTypesAllowedRequestStateTransitions x WHERE x.RequestTypeID=r.RequestTypeID AND x.FromRequestStateID=t.f AND x.ToRequestStateID=t.t);
-- HR must configure the tenant/JobType approval stages in existing Request Approval Flows.
-- No approver group is guessed and no Level 0 automatic approval is enabled.
COMMIT TRANSACTION;

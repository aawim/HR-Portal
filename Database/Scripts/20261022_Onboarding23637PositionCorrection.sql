-- User-confirmed correction: ONB-3-23637, Individual 820, Position 4.
-- Prepared for explicit user approval of an audited correction to approved Job 918.
-- The request was approved while the missing-position issue was being investigated.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @instance bigint;
IF NOT EXISTS (SELECT 1 FROM dbo.SO_StaffOnboardingRequests WITH (UPDLOCK,HOLDLOCK)
    WHERE RequestId=23637 AND IndividualId=820 AND OrganisationId=3 AND IsOpen=0 AND PositionId IS NULL AND JobId=918 AND UserId=535)
    THROW 51000,'The approved request changed or already has a position. No correction was applied.',1;
IF NOT EXISTS (SELECT 1 FROM dbo.Positions WHERE PositionID=4 AND Name='IT Executive - eGovernment Development'
    AND OrganisationID=3 AND IsActive=1)
    THROW 51000,'The confirmed position is unavailable. No correction was applied.',1;
SELECT @instance=WorkflowRequestInstanceID FROM dbo.WF_RequestInstances WITH(UPDLOCK,HOLDLOCK)
WHERE RequestID=23637 AND WorkflowStatus='Approved' AND ExecutionStatus='Succeeded' AND CompletedAt IS NOT NULL;
IF @instance IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.Requests WHERE RequestID=23637 AND RequestStateID=4)
    THROW 51000,'The expected approved workflow was not found. No correction was applied.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.Jobs WITH(UPDLOCK,HOLDLOCK) WHERE JobID=918 AND IndividualID=820 AND OrganisationID=3 AND JobStateID=4 AND TerminatedDate IS NULL)
    THROW 51000,'The approved employment changed. No correction was applied.',1;
IF EXISTS(SELECT 1 FROM dbo.JobPositions WITH(UPDLOCK,HOLDLOCK) WHERE JobID=918 AND JobPositionStateID=4 AND ToDate IS NULL)
    THROW 51000,'An approved position already exists for this job. No correction was applied.',1;
DECLARE @audit int,@positionAssignment int;
INSERT dbo.OperationLogs(OperationLogActionID,CreatedByContextID,CreatedDate,UpdatedDate,CreatedByIP,UpdatedByIP,Remarks,UpdatedContextID)
VALUES(122,1,SYSUTCDATETIME(),SYSUTCDATETIME(),'','',
 'Explicit user-authorised correction after onboarding approval: restore originally selected Position 4 (IT Executive - eGovernment Development) to Individual 820, approved Job 918, ONB-3-23637. Original approval decisions are preserved.',1);
SET @audit=SCOPE_IDENTITY();
INSERT dbo.JobPositions(PositionID,JobID,FromDate,JobPositionStateID,OperationLogID)
SELECT 4,918,JoinedDate,4,@audit FROM dbo.SO_StaffOnboardingRequests WHERE RequestId=23637;
SET @positionAssignment=SCOPE_IDENTITY();
INSERT dbo.JobPositionsRequests(JobPositionID,RequestID,OperationLogID) VALUES(@positionAssignment,23637,@audit);
UPDATE dbo.SO_StaffOnboardingRequests SET PositionId=4 WHERE RequestId=23637;
INSERT dbo.WF_RequestActions(WorkflowRequestInstanceID,RequestStageInstanceID,ActionCode,Comments,PerformedAt)
SELECT @instance,CurrentRequestStageInstanceID,'UpdatePosition',
    'Explicit user-authorised correction after approval: restored the originally selected IT Executive - eGovernment Development (Position 4) to approved Job 918. The missing position was caused by a form-selection issue. Original approval decisions are preserved.',SYSUTCDATETIME()
FROM dbo.WF_RequestInstances WHERE WorkflowRequestInstanceID=@instance;
INSERT dbo.SN_StaffNotifications(UserId,IndividualId,OrganisationId,Kind,Message,Url,CreatedAtUtc)
SELECT DISTINCT u.UserID,u.BusinessEntityID,3,'OnboardingUpdate',
    'ONB-3-23637: the appointment position was corrected to IT Executive - eGovernment Development. Please review the updated request.',
    '/admin/hr/onboarding?requestId=23637',SYSUTCDATETIME()
FROM dbo.Users u JOIN dbo.UserOrganisations o ON o.UserOrganisationID=u.UserOrganisationID
WHERE u.IsActive=1 AND u.LocalUserStateID=1 AND o.BusinessEntityID=3 AND
 (u.UserID=(SELECT CreatedByUserId FROM dbo.SO_StaffOnboardingRequests WHERE RequestId=23637) OR u.UserID=535);
COMMIT TRANSACTION;

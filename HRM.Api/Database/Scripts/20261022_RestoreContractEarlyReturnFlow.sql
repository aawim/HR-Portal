-- One-time compatibility repair: retain NCIT Contract's existing HR-only
-- early-return approver configuration under the dedicated request type.
-- No request instances, leave dates, balances or decisions are changed.
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRANSACTION;
DECLARE @org int = 3, @jobType int = 1, @requestType int = 105;
IF NOT EXISTS (SELECT 1 FROM dbo.JobTypes WHERE JobTypeID=@jobType AND TypeName='Contract')
    THROW 50000, 'The expected Contract Job Type was not found.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.RequestTypes WHERE RequestTypeID=@requestType AND TypeName='Early Return Request')
    THROW 50000, 'The expected Early Return Request Type was not found.', 1;
IF EXISTS (SELECT 1 FROM dbo.WF_WorkflowDefinitions d WITH (UPDLOCK,HOLDLOCK)
    JOIN dbo.WF_WorkflowJobTypes j ON j.WorkflowDefinitionID=d.WorkflowDefinitionID
    WHERE d.OrganisationBusinessEntityID=@org AND d.RequestTypeID=@requestType
      AND d.LeaveTypeId IS NULL AND d.LeaveRequestKind=0 AND d.IsActive=1 AND j.JobTypeID=@jobType AND j.IsActive=1)
BEGIN
    COMMIT;
    RETURN;
END;
DECLARE @source int, @sourceStage int;
IF (SELECT COUNT(*) FROM dbo.WF_WorkflowDefinitions d
    JOIN dbo.WF_WorkflowJobTypes j ON j.WorkflowDefinitionID=d.WorkflowDefinitionID
    WHERE d.OrganisationBusinessEntityID=@org AND d.RequestTypeID=48 AND d.LeaveRequestKind=2
      AND d.IsActive=1 AND j.JobTypeID=@jobType AND j.IsActive=1) <> 1
    THROW 50000, 'An unambiguous legacy Early Return flow is required.', 1;
SELECT @source=d.WorkflowDefinitionID FROM dbo.WF_WorkflowDefinitions d
    JOIN dbo.WF_WorkflowJobTypes j ON j.WorkflowDefinitionID=d.WorkflowDefinitionID
    WHERE d.OrganisationBusinessEntityID=@org AND d.RequestTypeID=48 AND d.LeaveRequestKind=2
      AND d.IsActive=1 AND j.JobTypeID=@jobType AND j.IsActive=1;
IF (SELECT COUNT(*) FROM dbo.WF_WorkflowStages WHERE WorkflowDefinitionID=@source AND IsActive=1) <> 1
    THROW 50000, 'The legacy flow must have exactly one HR stage.', 1;
SELECT @sourceStage=WorkflowStageID FROM dbo.WF_WorkflowStages WHERE WorkflowDefinitionID=@source AND IsActive=1;
IF NOT EXISTS (SELECT 1 FROM dbo.WF_StageApprovers a JOIN dbo.UserGroups g ON g.UserGroupID=a.ApproverReferenceID
    WHERE a.WorkflowStageID=@sourceStage AND a.ApproverType='UserGroup' AND g.GroupName='Human Resource: Staff')
    OR EXISTS (SELECT 1 FROM dbo.WF_StageApprovers WHERE WorkflowStageID=@sourceStage AND ApproverType <> 'UserGroup')
    THROW 50000, 'The expected HR-only User Group configuration was not found.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.RequestStates WHERE RequestStateID=14 AND IsProcessingState=1 AND IsFinalState=0)
    THROW 50000, 'Pending HR Approval must be a processing state.', 1;
IF (SELECT COUNT(*) FROM dbo.RequestTypesAllowedRequestStateTransitions
    WHERE RequestTypeID=@requestType AND ((FromRequestStateID IN (1,2) AND ToRequestStateID=14)
        OR (FromRequestStateID=14 AND ToRequestStateID=4))) <> 3
    THROW 50000, 'The required Early Return transitions must be configured first.', 1;
DECLARE @version int=(SELECT ISNULL(MAX(VersionNumber),0)+1 FROM dbo.WF_WorkflowDefinitions
    WHERE OrganisationBusinessEntityID=@org AND RequestTypeID=@requestType);
INSERT dbo.WF_WorkflowDefinitions
    (RequestTypeID,OrganisationBusinessEntityID,VersionNumber,Name,IsActive,CreatedAt,LeaveTypeId,LeaveRequestKind)
VALUES (@requestType,@org,@version,N'Request Type 105 / Job Type 1',1,SYSUTCDATETIME(),NULL,0);
DECLARE @definition int=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.WF_WorkflowJobTypes (WorkflowDefinitionID,JobTypeID,IsActive) VALUES (@definition,@jobType,1);
INSERT dbo.WF_WorkflowStages
    (WorkflowDefinitionID,StageOrder,StageName,RequestStateID,ApprovalMode,MinimumApprovals,IsActive)
SELECT @definition,1,StageName,14,ApprovalMode,MinimumApprovals,1
FROM dbo.WF_WorkflowStages WHERE WorkflowStageID=@sourceStage;
DECLARE @stage int=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.WF_StageApprovers (WorkflowStageID,ApproverType,ApproverReferenceID,SequenceNumber,IsRequired)
SELECT @stage,ApproverType,ApproverReferenceID,SequenceNumber,IsRequired
FROM dbo.WF_StageApprovers WHERE WorkflowStageID=@sourceStage;
COMMIT;
SELECT @definition AS RestoredWorkflowDefinitionID,@stage AS HRStageID,@source AS LegacySourceWorkflowID;

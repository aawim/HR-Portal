-- Authorized configuration: NCIT / Contract / job termination requests / HR only.
-- Existing definitions and in-flight requests are never rewritten.
SET XACT_ABORT ON;
SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRANSACTION;
DECLARE @Org int=3,@JobType int=1,@HrGroup int=3,@State int=14;
IF NOT EXISTS(SELECT 1 FROM Organisations WHERE BusinessEntityID=@Org AND OrganisationName LIKE '%Information Technology%')
    THROW 51070,'Expected NCIT organisation was not found.',1;
IF NOT EXISTS(SELECT 1 FROM JobTypes WHERE JobTypeID=@JobType AND TypeName='Contract')
    THROW 51070,'Expected Contract job type was not found.',1;
IF NOT EXISTS(SELECT 1 FROM UserGroups WHERE UserGroupID=@HrGroup AND GroupName='Human Resource: Staff' AND IsGlobal=1)
    THROW 51070,'Expected HR User Group was not found.',1;
IF NOT EXISTS(SELECT 1 FROM RequestStates WHERE RequestStateID=@State AND IsProcessingState=1 AND IsFinalState=0)
    THROW 51070,'Pending HR Approval must be a processing state.',1;
DECLARE @Targets table(Id int PRIMARY KEY);
INSERT @Targets SELECT RequestTypeID FROM RequestTypes WHERE TypeName IN
('Job Termination Request','Job Resignation Request','Job Retirement Request','Job Temporary Termination Request',
 'Job Termination Due To Promotion Request','Job Termination Due To Contract Expiration',
 'Job Termination During Probation Period Request','Job Terminated','Job Terminated other');
IF (SELECT COUNT(*) FROM @Targets)<>9 THROW 51070,'The expected termination request types were not found.',1;
DECLARE @Type int,@Definition int,@Stage int;
DECLARE types CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM @Targets;
OPEN types; FETCH NEXT FROM types INTO @Type;
WHILE @@FETCH_STATUS=0
BEGIN
    SET @Definition=NULL;
    IF (SELECT COUNT(*) FROM WF_WorkflowDefinitions d JOIN WF_WorkflowJobTypes j ON j.WorkflowDefinitionID=d.WorkflowDefinitionID
        WHERE d.RequestTypeID=@Type AND d.OrganisationBusinessEntityID=@Org AND d.IsActive=1 AND j.JobTypeID=@JobType AND j.IsActive=1)>1
        THROW 51070,'Multiple workflows already exist; resolve them through settings.',1;
    SELECT @Definition=d.WorkflowDefinitionID FROM WF_WorkflowDefinitions d JOIN WF_WorkflowJobTypes j ON j.WorkflowDefinitionID=d.WorkflowDefinitionID
        WHERE d.RequestTypeID=@Type AND d.OrganisationBusinessEntityID=@Org AND d.IsActive=1 AND j.JobTypeID=@JobType AND j.IsActive=1;
    IF @Definition IS NULL
    BEGIN
        INSERT WF_WorkflowDefinitions(RequestTypeID,OrganisationBusinessEntityID,VersionNumber,Name,IsActive,CreatedAt,LeaveRequestKind)
        SELECT @Type,@Org,COALESCE(MAX(VersionNumber),0)+1,CONCAT('Request Type ',@Type,' / Job Type ',@JobType),1,SYSUTCDATETIME(),0
        FROM WF_WorkflowDefinitions WHERE RequestTypeID=@Type AND OrganisationBusinessEntityID=@Org;
        SET @Definition=CONVERT(int,SCOPE_IDENTITY());
        INSERT WF_WorkflowJobTypes(WorkflowDefinitionID,JobTypeID,IsActive) VALUES(@Definition,@JobType,1);
        INSERT WF_WorkflowStages(WorkflowDefinitionID,StageOrder,StageName,RequestStateID,ApprovalMode,IsActive)
        VALUES(@Definition,1,'HR approval',@State,'Any',1);
        SET @Stage=CONVERT(int,SCOPE_IDENTITY());
        INSERT WF_StageApprovers(WorkflowStageID,ApproverType,ApproverReferenceID,SequenceNumber,IsRequired)
        VALUES(@Stage,'UserGroup',@HrGroup,1,1);
    END
    ELSE IF (SELECT COUNT(*) FROM WF_WorkflowStages WHERE WorkflowDefinitionID=@Definition AND IsActive=1)<>1
        OR NOT EXISTS(SELECT 1 FROM WF_WorkflowStages s JOIN WF_StageApprovers a ON a.WorkflowStageID=s.WorkflowStageID
            WHERE s.WorkflowDefinitionID=@Definition AND s.IsActive=1 AND s.RequestStateID=@State AND s.ApprovalMode='Any'
              AND a.ApproverType='UserGroup' AND a.ApproverReferenceID=@HrGroup)
        THROW 51070,'An existing workflow differs from this configuration; it was not overwritten.',1;
    INSERT RequestTypesAllowedRequestStateTransitions(RequestTypeID,FromRequestStateID,ToRequestStateID)
    SELECT @Type,p.FromState,p.ToState FROM (VALUES(1,14),(2,14),(14,4),(14,5),(14,6)) p(FromState,ToState)
    WHERE NOT EXISTS(SELECT 1 FROM RequestTypesAllowedRequestStateTransitions t WHERE t.RequestTypeID=@Type AND t.FromRequestStateID=p.FromState AND t.ToRequestStateID=p.ToState);
    FETCH NEXT FROM types INTO @Type;
END
CLOSE types; DEALLOCATE types;
COMMIT;
SELECT d.RequestTypeID,d.WorkflowDefinitionID,s.StageName,s.RequestStateID,a.ApproverReferenceID AS HrUserGroupID
FROM WF_WorkflowDefinitions d JOIN WF_WorkflowJobTypes j ON j.WorkflowDefinitionID=d.WorkflowDefinitionID
JOIN WF_WorkflowStages s ON s.WorkflowDefinitionID=d.WorkflowDefinitionID JOIN WF_StageApprovers a ON a.WorkflowStageID=s.WorkflowStageID
WHERE d.OrganisationBusinessEntityID=@Org AND j.JobTypeID=@JobType AND d.IsActive=1 AND d.RequestTypeID IN(SELECT Id FROM @Targets);

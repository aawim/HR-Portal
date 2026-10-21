-- Additive, rerunnable installation. Existing request flows and clock records are preserved.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.DutyChangeRequests','U') IS NULL
BEGIN
 CREATE TABLE dbo.DutyChangeRequests (
  DutyChangeRequestId bigint IDENTITY PRIMARY KEY, RequestId int NOT NULL,
  OrganisationId int NOT NULL, Kind nvarchar(12) NOT NULL, SourceAssignmentId bigint NOT NULL,
  SwapAssignmentId bigint NULL, SourceJobId int NOT NULL, TargetJobId int NOT NULL,
  SourceIndividualId int NOT NULL, TargetIndividualId int NOT NULL,
  SourceStart datetime2 NOT NULL, SourceEnd datetime2 NOT NULL, SwapStart datetime2 NULL, SwapEnd datetime2 NULL,
  Reason nvarchar(500) NOT NULL, SourceFingerprint nvarchar(64) NOT NULL, SwapFingerprint nvarchar(64) NULL,
  Acceptance nvarchar(16) NOT NULL, CreatedByUserId int NOT NULL,
  CreatedAtUtc datetime2 NOT NULL, AcceptedByUserId int NULL, AcceptedAtUtc datetime2 NULL,
  SourceAcceptedByUserId int NULL, SourceAcceptedAtUtc datetime2 NULL,
  IsOpen bit NOT NULL, TargetNewAssignmentId bigint NULL, SourceNewAssignmentId bigint NULL,
  CONSTRAINT FK_DutyChange_Request FOREIGN KEY(RequestId) REFERENCES dbo.Requests(RequestID),
  CONSTRAINT FK_DutyChange_Source FOREIGN KEY(SourceAssignmentId) REFERENCES dbo.WorkAssignments(WorkAssignmentID),
  CONSTRAINT FK_DutyChange_Swap FOREIGN KEY(SwapAssignmentId) REFERENCES dbo.WorkAssignments(WorkAssignmentID),
  CONSTRAINT FK_DutyChange_SourceJob FOREIGN KEY(SourceJobId) REFERENCES dbo.Jobs(JobID),
  CONSTRAINT FK_DutyChange_TargetJob FOREIGN KEY(TargetJobId) REFERENCES dbo.Jobs(JobID),
  CONSTRAINT CK_DutyChange_Kind CHECK(Kind IN ('Transfer','Swap')),
  CONSTRAINT CK_DutyChange_People CHECK(SourceIndividualId <> TargetIndividualId),
  CONSTRAINT CK_DutyChange_Swap CHECK((Kind='Swap' AND SwapAssignmentId IS NOT NULL AND SwapStart IS NOT NULL AND SwapEnd IS NOT NULL AND SwapAssignmentId <> SourceAssignmentId) OR (Kind='Transfer' AND SwapAssignmentId IS NULL AND SwapStart IS NULL AND SwapEnd IS NULL))
 );
 CREATE UNIQUE INDEX IX_DutyChangeRequests_RequestId ON dbo.DutyChangeRequests(RequestId);
 CREATE UNIQUE INDEX IX_DutyChangeRequests_SourceAssignmentId ON dbo.DutyChangeRequests(SourceAssignmentId) WHERE IsOpen=1;
 CREATE INDEX IX_DutyChangeRequests_OrganisationId_IsOpen ON dbo.DutyChangeRequests(OrganisationId,IsOpen);
 CREATE INDEX IX_DutyChangeRequests_SwapAssignmentId ON dbo.DutyChangeRequests(SwapAssignmentId) WHERE IsOpen=1;
END;
IF COL_LENGTH('dbo.DutyChangeRequests','SourceFingerprint') IS NULL
 ALTER TABLE dbo.DutyChangeRequests ADD SourceFingerprint nvarchar(64) NOT NULL CONSTRAINT DF_DutyChange_SourceFingerprint DEFAULT(''), SwapFingerprint nvarchar(64) NULL;
IF COL_LENGTH('dbo.DutyChangeRequests','SourceAcceptedByUserId') IS NULL
 ALTER TABLE dbo.DutyChangeRequests ADD SourceAcceptedByUserId int NULL, SourceAcceptedAtUtc datetime2 NULL;
DECLARE @audit int;
INSERT dbo.OperationLogs(OperationLogActionID,CreatedByContextID,CreatedDate,UpdatedDate,CreatedByIP,UpdatedByIP,Remarks,UpdatedContextID)
VALUES(18,1,SYSUTCDATETIME(),SYSUTCDATETIME(),'','','User-authorised duty roster configuration: staff acceptance followed by supervisor-only approval; tenant and JobType configurable.',1);
SET @audit=SCOPE_IDENTITY();
IF NOT EXISTS(SELECT 1 FROM dbo.RequestTypes WHERE TypeName='Duty Transfer Request')
 INSERT dbo.RequestTypes(TypeName,ViewURL,IsSystemType) VALUES('Duty Transfer Request','/online/duty-roster',1);
IF NOT EXISTS(SELECT 1 FROM dbo.RequestTypes WHERE TypeName='Duty Swap Request')
 INSERT dbo.RequestTypes(TypeName,ViewURL,IsSystemType) VALUES('Duty Swap Request','/online/duty-roster',1);
INSERT dbo.RequestTypesAllowedRequestStateTransitions(RequestTypeID,FromRequestStateID,ToRequestStateID)
SELECT r.RequestTypeID,t.f,t.t FROM dbo.RequestTypes r CROSS JOIN (VALUES(1,10),(10,3),(10,5),(10,6),(3,4),(3,5),(3,6))t(f,t)
WHERE r.TypeName IN ('Duty Transfer Request','Duty Swap Request') AND NOT EXISTS
 (SELECT 1 FROM dbo.RequestTypesAllowedRequestStateTransitions x WHERE x.RequestTypeID=r.RequestTypeID AND x.FromRequestStateID=t.f AND x.ToRequestStateID=t.t);
-- One default per configured tenant and active JobType. Never replace a tenant's custom flow.
DECLARE @org int,@jobType int,@type int,@definition int,@stage int,@group int,@version int;
DECLARE defaults CURSOR LOCAL FAST_FORWARD FOR
 SELECT DISTINCT p.OrganisationBusinessEntityID,j.JobTypeID,r.RequestTypeID
 FROM dbo.OrganisationWorkPlanningSettings p CROSS JOIN dbo.JobTypes j CROSS JOIN dbo.RequestTypes r
 WHERE p.IsActive=1 AND (j.IsActive=1 OR j.IsActive IS NULL) AND r.TypeName IN ('Duty Transfer Request','Duty Swap Request');
OPEN defaults; FETCH NEXT FROM defaults INTO @org,@jobType,@type;
WHILE @@FETCH_STATUS=0
BEGIN
 IF NOT EXISTS(SELECT 1 FROM dbo.WF_WorkflowDefinitions d JOIN dbo.WF_WorkflowJobTypes j ON j.WorkflowDefinitionID=d.WorkflowDefinitionID
   WHERE d.RequestTypeID=@type AND d.OrganisationBusinessEntityID=@org AND d.LeaveTypeId IS NULL AND d.LeaveRequestKind=0 AND d.IsActive=1 AND j.JobTypeID=@jobType AND j.IsActive=1)
 BEGIN
  SET @group=NULL;
  SELECT TOP(1) @group=g.UserGroupID FROM dbo.UserGroups g JOIN dbo.UserOrganisations u ON u.UserOrganisationID=g.UserOrganisationID
   WHERE (g.IsGlobal=1 OR u.BusinessEntityID=@org) AND g.GroupName='Supervisor (Team Leader)' ORDER BY g.IsGlobal,g.UserGroupID;
  IF @group IS NOT NULL
  BEGIN
   SELECT @version=ISNULL(MAX(VersionNumber),0)+1 FROM dbo.WF_WorkflowDefinitions WHERE RequestTypeID=@type AND OrganisationBusinessEntityID=@org;
   INSERT dbo.WF_WorkflowDefinitions(RequestTypeID,OrganisationBusinessEntityID,VersionNumber,Name,IsActive,CreatedAt,LeaveTypeId,LeaveRequestKind)
    VALUES(@type,@org,@version,'Duty change: supervisor approval',1,SYSUTCDATETIME(),NULL,0);
   SET @definition=SCOPE_IDENTITY();
   INSERT dbo.WF_WorkflowJobTypes(WorkflowDefinitionID,JobTypeID,IsActive) VALUES(@definition,@jobType,1);
   INSERT dbo.WF_WorkflowStages(WorkflowDefinitionID,StageOrder,StageName,RequestStateID,ApprovalMode,IsActive)
    VALUES(@definition,1,'Supervisor approval',3,'Any',1);
   SET @stage=SCOPE_IDENTITY();
   INSERT dbo.WF_StageApprovers(WorkflowStageID,ApproverType,ApproverReferenceID,SequenceNumber,IsRequired)
    VALUES(@stage,'SupervisorGroup',@group,1,1);
  END;
 END;
 FETCH NEXT FROM defaults INTO @org,@jobType,@type;
END;
CLOSE defaults; DEALLOCATE defaults;
COMMIT TRANSACTION;

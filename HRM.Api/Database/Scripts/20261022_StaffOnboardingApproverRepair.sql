-- Restore submitting HR officers excluded by the earlier onboarding-only rule.
-- Only open current stages and existing matching User Group memberships qualify.
-- This does not approve requests or create accounts/employment.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @restored TABLE(StageId bigint, UserId int, RequestId int);
INSERT dbo.WF_RequestApprovals(RequestStageInstanceID,ApproverUserID,StageApproverID,ApprovalSequence,Decision,AssignedAt)
OUTPUT inserted.RequestStageInstanceID,inserted.ApproverUserID,NULL INTO @restored(StageId,UserId,RequestId)
SELECT s.RequestStageInstanceID,u.UserID,MIN(a.StageApproverID),MIN(a.SequenceNumber),'Pending',SYSUTCDATETIME()
FROM dbo.SO_StaffOnboardingRequests o
JOIN dbo.Requests r ON r.RequestID=o.RequestId
JOIN dbo.RequestStates rs ON rs.RequestStateID=r.RequestStateID
JOIN dbo.WF_RequestInstances i ON i.RequestID=r.RequestID
JOIN dbo.WF_RequestStageInstances s ON s.RequestStageInstanceID=i.CurrentRequestStageInstanceID
JOIN dbo.WF_WorkflowStages ws ON ws.WorkflowStageID=s.WorkflowStageID
JOIN dbo.WF_StageApprovers a ON a.WorkflowStageID=s.WorkflowStageID AND a.ApproverType='UserGroup'
JOIN dbo.Users u ON u.UserID=o.CreatedByUserId
JOIN dbo.UserOrganisations uo ON uo.UserOrganisationID=u.UserOrganisationID AND uo.BusinessEntityID=o.OrganisationId
JOIN dbo.UserAssignedUserGroups ug ON ug.UserID=u.UserID AND ug.UserGroupID=a.ApproverReferenceID
JOIN dbo.UserGroups g ON g.UserGroupID=ug.UserGroupID
JOIN dbo.UserOrganisations go ON go.UserOrganisationID=g.UserOrganisationID
WHERE o.IsOpen=1 AND rs.IsFinalState=0 AND u.IsActive=1 AND u.LocalUserStateID=1
 AND u.BusinessEntityID<>o.IndividualId AND (g.IsGlobal=1 OR go.BusinessEntityID=o.OrganisationId)
 AND i.WorkflowStatus='InProgress' AND i.CompletedAt IS NULL
 AND s.StageStatus='Pending' AND s.CompletedAt IS NULL AND ws.RequestStateID=r.RequestStateID
 AND NOT EXISTS(SELECT 1 FROM dbo.WF_RequestApprovals old WITH(UPDLOCK,HOLDLOCK)
     WHERE old.RequestStageInstanceID=s.RequestStageInstanceID AND old.ApproverUserID=u.UserID)
GROUP BY s.RequestStageInstanceID,u.UserID;
UPDATE x SET RequestId=i.RequestID FROM @restored x JOIN dbo.WF_RequestStageInstances s ON s.RequestStageInstanceID=x.StageId
 JOIN dbo.WF_RequestInstances i ON i.WorkflowRequestInstanceID=s.WorkflowRequestInstanceID;
INSERT dbo.RN_RequestNotifications(UserId,RequestId,NotificationKind,RequestStageInstanceId,FromStateId,ToStateId,ReferenceNumber,CreatedAtUtc)
SELECT x.UserId,x.RequestId,'ActionRequired',x.StageId,r.RequestStateID,r.RequestStateID,r.ReferenceNumber,SYSUTCDATETIME()
FROM @restored x JOIN dbo.Requests r ON r.RequestID=x.RequestId
JOIN dbo.WF_RequestStageInstances s ON s.RequestStageInstanceID=x.StageId
JOIN dbo.WF_WorkflowStages ws ON ws.WorkflowStageID=s.WorkflowStageID
JOIN dbo.WF_RequestApprovals own ON own.RequestStageInstanceID=x.StageId AND own.ApproverUserID=x.UserId
WHERE NOT EXISTS(SELECT 1 FROM dbo.RN_RequestNotifications n WHERE n.UserId=x.UserId AND n.RequestStageInstanceId=x.StageId AND n.NotificationKind='ActionRequired')
 AND (ws.ApprovalMode<>'Sequential' OR NOT EXISTS(SELECT 1 FROM dbo.WF_RequestApprovals a
     WHERE a.RequestStageInstanceID=x.StageId AND a.Decision='Pending' AND a.ApprovalSequence<own.ApprovalSequence));
INSERT dbo.WF_RequestActions(WorkflowRequestInstanceID,RequestStageInstanceID,ActionCode,Comments,PerformedAt)
SELECT i.WorkflowRequestInstanceID,x.StageId,'AssignmentRepair','Restored eligible submitting HR officer from the configured User Group; the individual cannot approve their own appointment.',SYSUTCDATETIME()
FROM @restored x JOIN dbo.WF_RequestStageInstances s ON s.RequestStageInstanceID=x.StageId
JOIN dbo.WF_RequestInstances i ON i.WorkflowRequestInstanceID=s.WorkflowRequestInstanceID;
SELECT COUNT(*) RestoredApproverAssignments FROM @restored;
COMMIT TRANSACTION;

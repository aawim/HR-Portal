-- Approved correction: type 66 -> canonical type 1. Preserve amounts and history.
-- Executed in an EF/deployment transaction; no application workers are started.
SET XACT_ABORT ON;
IF @@TRANCOUNT=0 THROW 51000,'Annual Leave reconciliation requires a transaction.',1;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HRM.AnnualLeaveCatalogueReconciliation',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
IF @lock<0 THROW 51000,'Could not lock Annual Leave reconciliation.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.LT_LeaveTypes WHERE LeaveTypeID=66 AND Name=N'Annual Leave' AND OrganisationID=3) RETURN;
IF EXISTS(SELECT 1 FROM dbo.LT_TypeMigrationAudits WHERE SourceEntity=N'AnnualLeaveDuplicate' AND SourceId=66 AND LeaveTypeId=1)
 AND NOT EXISTS(SELECT 1 FROM dbo.LT_LeaveTypes WHERE LeaveTypeID=66 AND IsActive=1) RETURN;
IF NOT EXISTS(SELECT 1 FROM dbo.LT_LeaveTypes WITH(UPDLOCK,HOLDLOCK) WHERE LeaveTypeID=1 AND Code=N'AL01' AND Name=N'Annual Leave' AND IsGlobal=1 AND IsActive=1 AND RequestTypeID=34)
 OR NOT EXISTS(SELECT 1 FROM dbo.LT_LeaveTypes WITH(UPDLOCK,HOLDLOCK) WHERE LeaveTypeID=66 AND Code=N'LT-66' AND RequestTypeID=34 AND IsActive=1)
 THROW 51000,'Annual Leave identities changed; no correction applied.',1;

SELECT * INTO #AnnualBalances FROM dbo.JobLeaveTypes WITH(UPDLOCK,HOLDLOCK) WHERE LeaveTypeID=66 OR JobLeaveTypeID=9678;
SELECT * INTO #AnnualLeaves FROM dbo.Leaves WITH(UPDLOCK,HOLDLOCK) WHERE LeaveTypeID=66;
SELECT r.* INTO #AnnualReasons FROM dbo.LeaveReasons r WITH(UPDLOCK,HOLDLOCK) JOIN dbo.LeaveTypeReasonTypes t ON t.LeaveTypeReasonTypeID=r.LeaveTypeReasonTypeID WHERE t.LeaveTypeID=66;
IF (SELECT COUNT(*) FROM #AnnualBalances)<>4
 OR (SELECT COUNT(*) FROM #AnnualBalances WHERE LeaveTypeID=66 AND JobLeaveTypeID IN(9683,9684,9685) AND RemainingDays=30 AND IsValid=1)<>3
 OR NOT EXISTS(SELECT 1 FROM #AnnualBalances WHERE JobLeaveTypeID=9683 AND JobID=906)
 OR NOT EXISTS(SELECT 1 FROM #AnnualBalances WHERE JobLeaveTypeID=9684 AND JobID=918)
 OR NOT EXISTS(SELECT 1 FROM #AnnualBalances WHERE JobLeaveTypeID=9685 AND JobID=917)
 OR NOT EXISTS(SELECT 1 FROM #AnnualBalances WHERE JobLeaveTypeID=9678 AND JobID=906 AND LeaveTypeID=1 AND RemainingDays=0 AND IsValid=1)
 OR (SELECT COUNT(*) FROM #AnnualLeaves)<>7 OR EXISTS(SELECT 1 FROM #AnnualLeaves WHERE LeaveStateID NOT IN(5,6))
 OR EXISTS(SELECT 1 FROM #AnnualLeaves WHERE LeaveID NOT IN(7143,7158,7173,7174,7175,7176,7179))
 OR (SELECT COUNT(*) FROM #AnnualReasons)<>1
 THROW 51000,'Approved balance, leave or reason scope changed; review before correcting.',1;
IF EXISTS(SELECT 1 FROM dbo.LeaveSetLeaveTypes WHERE LeaveTypeID=66)
 OR EXISTS(SELECT 1 FROM dbo.LA_JobTypeLeaveAccrualRules WHERE LeaveTypeId=66)
 OR EXISTS(SELECT 1 FROM dbo.LT_LeavePolicies WHERE LeaveTypeID=66)
 OR EXISTS(SELECT 1 FROM dbo.WF_WorkflowDefinitions WHERE LeaveTypeId=66)
 THROW 51000,'New configuration references exist outside the approved correction.',1;
IF (SELECT COUNT(*) FROM dbo.LH_LeaveHandoverRuleTypes WHERE LeaveTypeId=66)<>1
 OR NOT EXISTS(SELECT 1 FROM dbo.LH_LeaveHandoverRuleTypes WHERE Id=2 AND RuleId=1 AND LeaveTypeId=66 AND IsActive=1)
 OR NOT EXISTS(SELECT 1 FROM dbo.LH_LeaveHandoverRuleTypes WHERE RuleId=1 AND LeaveTypeId=1 AND IsActive=1)
 THROW 51000,'Annual Leave handover configuration changed; review before correcting.',1;

SELECT t.LeaveTypeReasonTypeID AS OldId,MIN(c.LeaveTypeReasonTypeID) AS NewId INTO #AnnualReasonMap
FROM dbo.LeaveTypeReasonTypes t JOIN dbo.LeaveTypeReasonTypes c ON c.LeaveTypeID=1 AND LTRIM(RTRIM(c.ReasonTypeName))=LTRIM(RTRIM(t.ReasonTypeName))
WHERE t.LeaveTypeID=66 GROUP BY t.LeaveTypeReasonTypeID;
IF EXISTS(SELECT 1 FROM #AnnualReasons r WHERE NOT EXISTS(SELECT 1 FROM #AnnualReasonMap m WHERE m.OldId=r.LeaveTypeReasonTypeID))
 THROW 51000,'No canonical equivalent exists for a saved leave reason.',1;

INSERT dbo.LT_TypeMigrationAudits(SourceEntity,SourceId,LeaveTypeId,SnapshotJson,MigratedAtUtc)
SELECT N'AnnualLeaveDuplicate',t.LeaveTypeID,1,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),SYSUTCDATETIME() FROM dbo.LT_LeaveTypes t WHERE t.LeaveTypeID=66;
INSERT dbo.LT_TypeMigrationAudits(SourceEntity,SourceId,LeaveTypeId,SnapshotJson,MigratedAtUtc)
SELECT N'AnnualMerge:JobLeaveTypes',t.JobLeaveTypeID,1,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),SYSUTCDATETIME() FROM #AnnualBalances t;
INSERT dbo.LT_TypeMigrationAudits(SourceEntity,SourceId,LeaveTypeId,SnapshotJson,MigratedAtUtc)
SELECT N'AnnualMerge:Leaves',t.LeaveID,1,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),SYSUTCDATETIME() FROM #AnnualLeaves t;
INSERT dbo.LT_TypeMigrationAudits(SourceEntity,SourceId,LeaveTypeId,SnapshotJson,MigratedAtUtc)
SELECT N'AnnualMerge:LeaveReasons',t.LeaveID,1,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),SYSUTCDATETIME() FROM #AnnualReasons t;
INSERT dbo.LT_TypeMigrationAudits(SourceEntity,SourceId,LeaveTypeId,SnapshotJson,MigratedAtUtc)
SELECT N'AnnualMerge:HandoverTypes',t.Id,1,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),SYSUTCDATETIME() FROM dbo.LH_LeaveHandoverRuleTypes t WHERE t.Id=2;

UPDATE dbo.JobLeaveTypes SET IsValid=0 WHERE JobLeaveTypeID=9678;
UPDATE dbo.JobLeaveTypes SET LeaveTypeID=1 WHERE LeaveTypeID=66;
UPDATE dbo.Leaves SET LeaveTypeID=1 WHERE LeaveTypeID=66;
UPDATE r SET LeaveTypeReasonTypeID=m.NewId FROM dbo.LeaveReasons r JOIN #AnnualReasonMap m ON m.OldId=r.LeaveTypeReasonTypeID;
UPDATE dbo.LH_LeaveHandoverRuleTypes SET IsActive=0 WHERE Id=2;
UPDATE dbo.LT_LeaveTypes SET IsActive=0,UpdatedDate=SYSUTCDATETIME() WHERE LeaveTypeID=66;

IF EXISTS(SELECT JobLeaveTypeID,JobID,RemainingDays,EffectiveFromDate,EffectiveToDate FROM #AnnualBalances
 EXCEPT SELECT JobLeaveTypeID,JobID,RemainingDays,EffectiveFromDate,EffectiveToDate FROM dbo.JobLeaveTypes)
 OR EXISTS(SELECT LeaveID,JobID,Duration,LeaveStateID,FromDate,ToDate FROM #AnnualLeaves
 EXCEPT SELECT LeaveID,JobID,Duration,LeaveStateID,FromDate,ToDate FROM dbo.Leaves)
 OR EXISTS(SELECT LeaveID,Reason FROM #AnnualReasons EXCEPT SELECT LeaveID,Reason FROM dbo.LeaveReasons)
 OR EXISTS(SELECT 1 FROM dbo.JobLeaveTypes WHERE LeaveTypeID=66)
 OR EXISTS(SELECT 1 FROM dbo.Leaves WHERE LeaveTypeID=66)
 OR (SELECT COUNT(*) FROM dbo.LT_LeaveTypes WHERE IsActive=1 AND Name=N'Annual Leave' AND (IsGlobal=1 OR OrganisationID=3))<>1
 THROW 51000,'Annual Leave preservation verification failed; roll back the correction.',1;

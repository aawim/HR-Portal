-- Reviewed NCIT annual-leave routing repair. No balance, application or workflow state is changed.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS(SELECT 1 FROM dbo.RequestTypes WHERE RequestTypeID=34 AND TypeName=N'Annual Leave Request') THROW 51000,'Expected Annual Leave Request type 34 was not found.',1;
IF EXISTS(SELECT 1 FROM dbo.LT_LeaveTypes WHERE LeaveTypeID=66 AND OrganisationID=3 AND Name=N'Annual Leave' AND RequestTypeID=1)
BEGIN
 IF NOT EXISTS(SELECT 1 FROM dbo.LeaveRequests l JOIN dbo.Leaves v ON v.LeaveID=l.LeaveID JOIN dbo.Requests r ON r.RequestID=l.RequestID WHERE v.LeaveTypeID=66 AND r.RequestTypeID=34)
  OR EXISTS(SELECT 1 FROM dbo.LeaveRequests l JOIN dbo.Leaves v ON v.LeaveID=l.LeaveID JOIN dbo.Requests r ON r.RequestID=l.RequestID WHERE v.LeaveTypeID=66 AND r.RequestTypeID<>34)
  THROW 51000,'Annual leave history does not uniquely confirm request type 34. No repair applied.',1;
 INSERT dbo.OperationLogs(OperationLogActionID,CreatedByContextID,CreatedDate,UpdatedDate,CreatedByIP,UpdatedByIP,Remarks,UpdatedContextID)
 VALUES(160,1,SYSUTCDATETIME(),SYSUTCDATETIME(),'','','Reviewed routing correction in NCIT: Annual Leave type 66 RequestTypeID 1 (User Registration) -> 34 (Annual Leave Request), confirmed against all existing type-66 leave requests. No balances or requests changed.',1);
 DECLARE @typeAudit int=CONVERT(int,SCOPE_IDENTITY());
 UPDATE dbo.LT_LeaveTypes SET RequestTypeID=34,OperationLogID=@typeAudit,UpdatedDate=SYSUTCDATETIME() WHERE LeaveTypeID=66 AND OrganisationID=3 AND RequestTypeID=1;
END;
IF EXISTS(SELECT 1 FROM dbo.LT_LeavePolicies WHERE LeavePolicyID=1 AND LeaveTypeID=1 AND OrganisationID=3 AND RequestTypeID=0)
BEGIN
 IF NOT EXISTS(SELECT 1 FROM dbo.LT_LeaveTypes WHERE LeaveTypeID=1 AND Name=N'Annual Leave' AND RequestTypeID=34) THROW 51000,'The canonical annual request link could not be confirmed. No repair applied.',1;
 INSERT dbo.OperationLogs(OperationLogActionID,CreatedByContextID,CreatedDate,UpdatedDate,CreatedByIP,UpdatedByIP,Remarks,UpdatedContextID)
 VALUES(160,1,SYSUTCDATETIME(),SYSUTCDATETIME(),'','','Reviewed routing correction in NCIT: Annual Leave policy 1 RequestTypeID 0 (missing) -> 34 (Annual Leave Request), confirmed against its canonical type 1. Policy rules, balances, requests and workflow history unchanged.',1);
 DECLARE @policyAudit int=CONVERT(int,SCOPE_IDENTITY());
 UPDATE dbo.LT_LeavePolicies SET RequestTypeID=34,OperationLogID=@policyAudit,UpdatedDate=SYSUTCDATETIME() WHERE LeavePolicyID=1 AND LeaveTypeID=1 AND OrganisationID=3 AND RequestTypeID=0;
END;
COMMIT TRANSACTION;

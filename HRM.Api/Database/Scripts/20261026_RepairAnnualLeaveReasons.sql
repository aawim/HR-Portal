-- Restore the organisation annual type's reason choices from its explicitly related global annual type.
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRANSACTION;
IF NOT EXISTS(SELECT 1 FROM dbo.LT_LeaveTypes WHERE LeaveTypeID=1 AND Name=N'Annual Leave' AND IsGlobal=1 AND RequestTypeID=34)
 OR NOT EXISTS(SELECT 1 FROM dbo.LT_LeaveTypes WHERE LeaveTypeID=66 AND OrganisationID=3 AND Name=N'Annual Leave' AND RequestTypeID=34)
 THROW 51000,'The reviewed global and NCIT annual type relationship could not be confirmed. No reasons changed.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.LeaveTypeReasonTypes WHERE LeaveTypeID=1)
 THROW 51000,'No global annual leave reasons were found.',1;
IF EXISTS(SELECT LTRIM(RTRIM(ReasonTypeName)) FROM dbo.LeaveTypeReasonTypes WHERE LeaveTypeID=1 GROUP BY LTRIM(RTRIM(ReasonTypeName)) HAVING COUNT(*)>1)
 THROW 51000,'Duplicate source reason names require review. No reasons changed.',1;
DECLARE @changed int=0;
-- Reuse the generated fallback row only when no historical leave refers to it.
IF NOT EXISTS(SELECT 1 FROM dbo.LeaveTypeReasonTypes WHERE LeaveTypeID=66 AND ReasonTypeName=N'Other')
 AND EXISTS(SELECT 1 FROM dbo.LeaveTypeReasonTypes WHERE LeaveTypeID=1 AND ReasonTypeName=N'Other')
BEGIN
 UPDATE r SET ReasonTypeName=N'Other' FROM dbo.LeaveTypeReasonTypes r
 WHERE r.LeaveTypeID=66 AND r.ReasonTypeName=N'General'
 AND NOT EXISTS(SELECT 1 FROM dbo.LeaveReasons used WHERE used.LeaveTypeReasonTypeID=r.LeaveTypeReasonTypeID);
 SET @changed=@@ROWCOUNT;
END;
INSERT dbo.LeaveTypeReasonTypes(LeaveTypeID,ReasonTypeName)
SELECT 66,s.ReasonTypeName FROM dbo.LeaveTypeReasonTypes s WHERE s.LeaveTypeID=1
 AND NOT EXISTS(SELECT 1 FROM dbo.LeaveTypeReasonTypes t WHERE t.LeaveTypeID=66 AND LTRIM(RTRIM(t.ReasonTypeName))=LTRIM(RTRIM(s.ReasonTypeName)));
SET @changed=@changed+@@ROWCOUNT;
IF @changed>0
 INSERT dbo.OperationLogs(OperationLogActionID,CreatedByContextID,CreatedDate,UpdatedDate,CreatedByIP,UpdatedByIP,Remarks,UpdatedContextID)
 VALUES(160,1,SYSUTCDATETIME(),SYSUTCDATETIME(),'','','NCIT Annual Leave type 66 reason choices aligned with global annual type 1: Other, Holiday, Family and Personal. Only unused fallback reason renamed; missing choices copied. Historical leave reasons and balances unchanged.',1);
COMMIT TRANSACTION;

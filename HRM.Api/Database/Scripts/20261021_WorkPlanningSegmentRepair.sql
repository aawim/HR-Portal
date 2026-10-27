-- Preview by default. Apply only after reviewing the dated snapshots and creating
-- an operation log for the approved repair. Does not update attendance or plans.
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @TenantId int = 3;
DECLARE @Apply bit = 0;
DECLARE @RepairOperationLogId int = NULL;
IF @Apply = 1
BEGIN
    IF @RepairOperationLogId IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.OperationLogs WHERE OperationLogId = @RepairOperationLogId)
        THROW 50001, 'An operation log for this approved repair is required.', 1;
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
    BEGIN TRANSACTION;
END;

SELECT a.WorkAssignmentId, p.WorkPlanId, p.WorkDate, p.IndividualId, p.JobId,
       s.WorkPlanSegmentId, s.WorkTemplateSegmentId, s.WorkSegmentTypeId,
       s.Name, s.Description, s.SequenceNumber, s.StartDateTime, s.EndDateTime,
       s.GraceBeforeMinutes, s.GraceAfterMinutes, s.IsMandatory, s.RequiresAttendance,
       s.RequiresLocationValidation, s.RequiresDeviceValidation
INTO #Candidates
FROM dbo.WorkAssignments a
JOIN dbo.WorkPlans p ON p.WorkPlanId = a.WorkPlanId
JOIN dbo.WorkPlanSegments s ON s.WorkPlanId = p.WorkPlanId AND s.IsValid = 1 AND s.SchedulePurpose = a.SchedulePurpose
JOIN dbo.WorkTemplateSegments ts ON ts.WorkTemplateSegmentId = s.WorkTemplateSegmentId AND ts.WorkTemplateId = a.WorkTemplateId
WHERE a.IsValid = 1 AND p.IsValid = 1 AND p.IsFinalized = 0 AND p.OrganisationBusinessEntityId = @TenantId
  AND NOT EXISTS (SELECT 1 FROM dbo.WorkAssignmentSegments old WHERE old.WorkAssignmentId = a.WorkAssignmentId)
  AND NOT EXISTS (SELECT 1 FROM dbo.WorkAssignments other WHERE other.WorkPlanId = a.WorkPlanId AND other.IsValid = 1
      AND other.WorkTemplateId = a.WorkTemplateId AND other.SchedulePurpose = a.SchedulePurpose AND other.WorkAssignmentId <> a.WorkAssignmentId);

SELECT WorkAssignmentId, WorkPlanId, WorkDate, IndividualId, JobId, COUNT(*) AS MissingSegments
FROM #Candidates GROUP BY WorkAssignmentId, WorkPlanId, WorkDate, IndividualId, JobId ORDER BY WorkDate, WorkAssignmentId;
SELECT COUNT(DISTINCT WorkAssignmentId) AS AssignmentsToRepair, COUNT(*) AS SegmentsToInsert FROM #Candidates;
IF @Apply = 0 RETURN;
IF EXISTS (
    SELECT 1 FROM #Candidates c JOIN dbo.WorkAssignments a ON a.WorkAssignmentId = c.WorkAssignmentId
    JOIN dbo.WorkPlans p ON p.WorkPlanId = a.WorkPlanId
    WHERE a.IsValid = 0 OR p.IsValid = 0 OR p.IsFinalized = 1 OR p.OrganisationBusinessEntityId <> @TenantId
      OR EXISTS (SELECT 1 FROM dbo.WorkAssignmentSegments old WHERE old.WorkAssignmentId = a.WorkAssignmentId)
)
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50002, 'Candidate data changed. Preview again before applying.', 1;
END;

INSERT dbo.WorkAssignmentSegments
    (WorkAssignmentId, WorkTemplateSegmentId, WorkSegmentTypeId, Name, Description, SequenceNumber,
     StartDateTime, EndDateTime, GraceBeforeMinutes, GraceAfterMinutes, IsMandatory, RequiresAttendance,
     RequiresLocationValidation, RequiresDeviceValidation, IsValid, OperationLogId)
SELECT WorkAssignmentId, WorkTemplateSegmentId, WorkSegmentTypeId, Name, Description, SequenceNumber,
       StartDateTime, EndDateTime, GraceBeforeMinutes, GraceAfterMinutes, IsMandatory, RequiresAttendance,
       RequiresLocationValidation, RequiresDeviceValidation, 1, @RepairOperationLogId
FROM #Candidates;
SELECT @@ROWCOUNT AS SegmentsInserted;
COMMIT TRANSACTION;

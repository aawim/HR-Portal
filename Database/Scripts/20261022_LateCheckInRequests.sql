SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.LateCheckInRequests','U') IS NULL
BEGIN
    CREATE TABLE dbo.LateCheckInRequests (
        RequestId int NOT NULL PRIMARY KEY REFERENCES dbo.Requests(RequestID),
        JobId int NOT NULL REFERENCES dbo.Jobs(JobID),
        OrganisationId int NOT NULL,
        IndividualId int NOT NULL,
        CreatedByUserId int NOT NULL,
        AttendanceLogId int NOT NULL REFERENCES dbo.AttendanceLogs(AttendanceLogID),
        WorkAssignmentId bigint NOT NULL,
        ScheduleName nvarchar(250) NOT NULL,
        ScheduledStart datetime2 NOT NULL,
        ActualCheckIn datetime2 NOT NULL,
        LateMinutes int NOT NULL CHECK (LateMinutes > 0),
        Reason nvarchar(1000) NOT NULL,
        CreatedAtUtc datetime2 NOT NULL
    );
    CREATE INDEX IX_LateCheckInRequests_OrganisationId_IndividualId ON dbo.LateCheckInRequests(OrganisationId,IndividualId);
    CREATE INDEX IX_LateCheckInRequests_AttendanceLogId_WorkAssignmentId ON dbo.LateCheckInRequests(AttendanceLogId,WorkAssignmentId);
END;
IF NOT EXISTS (SELECT 1 FROM dbo.RequestTypes WHERE TypeName = 'Late Check In Request')
BEGIN
    INSERT dbo.SequenceNumberTypes(TypeName,Prefix,FormatString,IsResetAnnually,StartingNumber,NextNumber)
    VALUES ('Late Check In Requests','LCI','{PREFIX}-{YEAR}-{NUMBER}',0,1,1);
    DECLARE @Sequence int = SCOPE_IDENTITY();
    INSERT dbo.RequestTypes(TypeName,SequenceNumberTypeID,ViewURL,IsSystemType)
    VALUES ('Late Check In Request',@Sequence,'/online/attendance/attendance-request/late-check-in',1);
END;
COMMIT;

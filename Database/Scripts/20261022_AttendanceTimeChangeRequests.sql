SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.LC_LateCheckInRequests','Kind') IS NULL
BEGIN
    ALTER TABLE dbo.LC_LateCheckInRequests ADD Kind nvarchar(20) NOT NULL CONSTRAINT DF_LateCheckInRequests_Kind DEFAULT 'Explanation',
        ScheduledEnd datetime2 NULL, RequestedStart datetime2 NULL, RequestedEnd datetime2 NULL, AppliedAtUtc datetime2 NULL;
    ALTER TABLE dbo.LC_LateCheckInRequests ALTER COLUMN AttendanceLogId int NULL;
    ALTER TABLE dbo.LC_LateCheckInRequests ALTER COLUMN ActualCheckIn datetime2 NULL;
END;
IF NOT EXISTS (SELECT 1 FROM dbo.RequestTypes WHERE TypeName='Early Check Out Request')
BEGIN
    INSERT dbo.SequenceNumberTypes(TypeName,Prefix,FormatString,IsResetAnnually,StartingNumber,NextNumber)
    VALUES ('Early Check Out Requests','ECO','{PREFIX}-{YEAR}-{NUMBER}',0,1,1);
    DECLARE @Sequence int=SCOPE_IDENTITY();
    INSERT dbo.RequestTypes(TypeName,SequenceNumberTypeID,ViewURL,IsSystemType)
    VALUES ('Early Check Out Request',@Sequence,'/online/attendance/attendance-request/early-check-out',1);
END;
COMMIT;

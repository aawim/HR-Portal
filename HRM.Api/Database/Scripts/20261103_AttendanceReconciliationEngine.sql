-- =============================================================================
-- Migration: 20261103_AttendanceReconciliationEngine.sql
-- Purpose:
--   1. Seed new AttendanceResolutionStatuses (Conflict = 12, PartiallyResolved = 13).
--   2. Add multi-schedule classification, policy version, match reason, and manual
--      override columns to AttendanceLogResolutions.
--   3. Replace UX_AttendanceLogResolutions_ActiveAttendanceLog with:
--      - UX_AttendanceLogResolutions_ActivePrimaryPerLog
--      - UX_AttendanceLogResolutions_ActiveSegmentPerLog
--      so a single physical clock event can have one primary classification
--      (Official schedule priority) and additional policy-permitted secondary
--      duty/break associations.
--   4. Create AR_AttendanceReconciliationRuns, AR_AttendanceObligations, and
--      AR_AttendanceManualOverrides under the AR_ feature prefix.
-- =============================================================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

-- 1. Ensure AttendanceResolutionStatuses rows exist
IF OBJECT_ID('dbo.AttendanceResolutionStatuses', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.AttendanceResolutionStatuses WHERE AttendanceResolutionStatusID = 11)
        INSERT INTO dbo.AttendanceResolutionStatuses (AttendanceResolutionStatusID, Name, Description)
        VALUES (11, N'DuplicatePunch', N'Clock event suppressed as a duplicate or noisy punch within the configured threshold.');

    IF NOT EXISTS (SELECT 1 FROM dbo.AttendanceResolutionStatuses WHERE AttendanceResolutionStatusID = 12)
        INSERT INTO dbo.AttendanceResolutionStatuses (AttendanceResolutionStatusID, Name, Description)
        VALUES (12, N'Conflict', N'Clock event has an unresolved or ambiguous schedule conflict requiring review.');

    IF NOT EXISTS (SELECT 1 FROM dbo.AttendanceResolutionStatuses WHERE AttendanceResolutionStatusID = 13)
        INSERT INTO dbo.AttendanceResolutionStatuses (AttendanceResolutionStatusID, Name, Description)
        VALUES (13, N'PartiallyResolved', N'Clock event resolved partially while another obligation remains open or incomplete.');
END;

-- 2. Create AR_AttendanceReconciliationRuns
IF OBJECT_ID('dbo.AR_AttendanceReconciliationRuns', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AR_AttendanceReconciliationRuns
    (
        AttendanceReconciliationRunID BIGINT IDENTITY(1,1) NOT NULL,
        OrganisationID INT NOT NULL,
        IndividualID INT NULL,
        JobID INT NULL,
        FromWorkDate DATE NOT NULL,
        ToWorkDate DATE NOT NULL,
        TriggerSource NVARCHAR(50) NOT NULL,
        PolicyVersion NVARCHAR(40) NOT NULL,
        InitiatedByUserID INT NULL,
        OperationLogID INT NULL,
        StartedAtUtc DATETIME2(0) NOT NULL,
        CompletedAtUtc DATETIME2(0) NULL,
        TotalDaysEvaluated INT NOT NULL CONSTRAINT DF_AR_AttendanceReconciliationRuns_TotalDays DEFAULT (0),
        TotalEventsProcessed INT NOT NULL CONSTRAINT DF_AR_AttendanceReconciliationRuns_TotalEvents DEFAULT (0),
        ConflictCount INT NOT NULL CONSTRAINT DF_AR_AttendanceReconciliationRuns_ConflictCount DEFAULT (0),
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_AR_AttendanceReconciliationRuns_Status DEFAULT (N'Completed'),
        SummaryMessage NVARCHAR(1000) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT PK_AR_AttendanceReconciliationRuns PRIMARY KEY CLUSTERED (AttendanceReconciliationRunID),
        CONSTRAINT FK_AR_AttendanceReconciliationRuns_Organisations FOREIGN KEY (OrganisationID) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_AR_AttendanceReconciliationRuns_Individuals FOREIGN KEY (IndividualID) REFERENCES dbo.BE_Individuals (BusinessEntityID),
        CONSTRAINT FK_AR_AttendanceReconciliationRuns_Jobs FOREIGN KEY (JobID) REFERENCES dbo.Jobs (JobID),
        CONSTRAINT FK_AR_AttendanceReconciliationRuns_Users FOREIGN KEY (InitiatedByUserID) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_AR_AttendanceReconciliationRuns_OperationLogs FOREIGN KEY (OperationLogID) REFERENCES dbo.OperationLogs (OperationLogID)
    );

    CREATE INDEX IX_AR_AttendanceReconciliationRuns_OrgIndividualDate
        ON dbo.AR_AttendanceReconciliationRuns (OrganisationID, IndividualID, FromWorkDate, ToWorkDate);
END;

-- 3. Create AR_AttendanceObligations
IF OBJECT_ID('dbo.AR_AttendanceObligations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AR_AttendanceObligations
    (
        AttendanceObligationID BIGINT IDENTITY(1,1) NOT NULL,
        OrganisationID INT NOT NULL,
        IndividualID INT NOT NULL,
        JobID INT NOT NULL,
        WorkDate DATE NOT NULL,
        WorkPlanID BIGINT NULL,
        WorkAssignmentID BIGINT NULL,
        WorkPlanSegmentID INT NULL,
        ObligationType NVARCHAR(30) NOT NULL,
        ScheduleName NVARCHAR(250) NOT NULL,
        Priority INT NOT NULL CONSTRAINT DF_AR_AttendanceObligations_Priority DEFAULT (1),
        ScheduledStart DATETIME2(0) NOT NULL,
        ScheduledEnd DATETIME2(0) NOT NULL,
        GraceBeforeMinutes INT NOT NULL CONSTRAINT DF_AR_AttendanceObligations_GraceBefore DEFAULT (0),
        GraceAfterMinutes INT NOT NULL CONSTRAINT DF_AR_AttendanceObligations_GraceAfter DEFAULT (0),
        RequiresAttendance BIT NOT NULL CONSTRAINT DF_AR_AttendanceObligations_ReqAttendance DEFAULT (1),
        RequiresCheckOut BIT NOT NULL CONSTRAINT DF_AR_AttendanceObligations_ReqCheckOut DEFAULT (1),
        ObligationState NVARCHAR(30) NOT NULL CONSTRAINT DF_AR_AttendanceObligations_State DEFAULT (N'NotStarted'),
        ResolvedStartAttendanceLogID INT NULL,
        ResolvedEndAttendanceLogID INT NULL,
        ActualStartDateTime DATETIME2(0) NULL,
        ActualEndDateTime DATETIME2(0) NULL,
        PlannedMinutes INT NOT NULL CONSTRAINT DF_AR_AttendanceObligations_PlannedMinutes DEFAULT (0),
        ActualPresenceMinutes INT NOT NULL CONSTRAINT DF_AR_AttendanceObligations_ActualMinutes DEFAULT (0),
        CreditedWorkedMinutes INT NOT NULL CONSTRAINT DF_AR_AttendanceObligations_CreditedMinutes DEFAULT (0),
        BreakDeductedMinutes INT NOT NULL CONSTRAINT DF_AR_AttendanceObligations_BreakMinutes DEFAULT (0),
        LateMinutes INT NOT NULL CONSTRAINT DF_AR_AttendanceObligations_LateMinutes DEFAULT (0),
        EarlyDepartureMinutes INT NOT NULL CONSTRAINT DF_AR_AttendanceObligations_EarlyMinutes DEFAULT (0),
        OvertimeCandidateMinutes INT NOT NULL CONSTRAINT DF_AR_AttendanceObligations_OtMinutes DEFAULT (0),
        OverlapWithOfficialMinutes INT NOT NULL CONSTRAINT DF_AR_AttendanceObligations_OverlapMinutes DEFAULT (0),
        OutsideOfficialMinutes INT NOT NULL CONSTRAINT DF_AR_AttendanceObligations_OutsideMinutes DEFAULT (0),
        StatusReason NVARCHAR(500) NULL,
        PolicyVersion NVARCHAR(40) NOT NULL CONSTRAINT DF_AR_AttendanceObligations_PolicyVersion DEFAULT (N'v2.0-OfficialPriority'),
        ReconciliationRunID BIGINT NULL,
        IsValid BIT NOT NULL CONSTRAINT DF_AR_AttendanceObligations_IsValid DEFAULT (1),
        UpdatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_AR_AttendanceObligations_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AR_AttendanceObligations PRIMARY KEY CLUSTERED (AttendanceObligationID),
        CONSTRAINT FK_AR_AttendanceObligations_Organisations FOREIGN KEY (OrganisationID) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_AR_AttendanceObligations_Individuals FOREIGN KEY (IndividualID) REFERENCES dbo.BE_Individuals (BusinessEntityID),
        CONSTRAINT FK_AR_AttendanceObligations_Jobs FOREIGN KEY (JobID) REFERENCES dbo.Jobs (JobID),
        CONSTRAINT FK_AR_AttendanceObligations_WorkPlans FOREIGN KEY (WorkPlanID) REFERENCES dbo.WorkPlans (WorkPlanID),
        CONSTRAINT FK_AR_AttendanceObligations_WorkAssignments FOREIGN KEY (WorkAssignmentID) REFERENCES dbo.WorkAssignments (WorkAssignmentID),
        CONSTRAINT FK_AR_AttendanceObligations_WorkPlanSegments FOREIGN KEY (WorkPlanSegmentID) REFERENCES dbo.WorkPlanSegments (WorkPlanSegmentID),
        CONSTRAINT FK_AR_AttendanceObligations_StartLog FOREIGN KEY (ResolvedStartAttendanceLogID) REFERENCES dbo.AttendanceLogs (AttendanceLogID),
        CONSTRAINT FK_AR_AttendanceObligations_EndLog FOREIGN KEY (ResolvedEndAttendanceLogID) REFERENCES dbo.AttendanceLogs (AttendanceLogID),
        CONSTRAINT FK_AR_AttendanceObligations_Run FOREIGN KEY (ReconciliationRunID) REFERENCES dbo.AR_AttendanceReconciliationRuns (AttendanceReconciliationRunID)
    );

    CREATE INDEX IX_AR_AttendanceObligations_EmployeeWorkDate
        ON dbo.AR_AttendanceObligations (IndividualID, JobID, WorkDate, IsValid);
END;

-- 4. Create AR_AttendanceManualOverrides
IF OBJECT_ID('dbo.AR_AttendanceManualOverrides', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AR_AttendanceManualOverrides
    (
        AttendanceManualOverrideID BIGINT IDENTITY(1,1) NOT NULL,
        OrganisationID INT NOT NULL,
        IndividualID INT NOT NULL,
        JobID INT NOT NULL,
        WorkDate DATE NOT NULL,
        AttendanceLogID INT NULL,
        WorkPlanID BIGINT NULL,
        WorkAssignmentID BIGINT NULL,
        WorkPlanSegmentID INT NULL,
        OverrideAction NVARCHAR(50) NOT NULL,
        TargetSchedulePurpose NVARCHAR(30) NOT NULL,
        TargetClockTypeID INT NULL,
        Reason NVARCHAR(1000) NOT NULL,
        CreatedByUserID INT NOT NULL,
        CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_AR_AttendanceManualOverrides_CreatedAt DEFAULT (SYSUTCDATETIME()),
        SupersededByOverrideID BIGINT NULL,
        OperationLogID INT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_AR_AttendanceManualOverrides_IsActive DEFAULT (1),
        CONSTRAINT PK_AR_AttendanceManualOverrides PRIMARY KEY CLUSTERED (AttendanceManualOverrideID),
        CONSTRAINT FK_AR_AttendanceManualOverrides_Organisations FOREIGN KEY (OrganisationID) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_AR_AttendanceManualOverrides_Individuals FOREIGN KEY (IndividualID) REFERENCES dbo.BE_Individuals (BusinessEntityID),
        CONSTRAINT FK_AR_AttendanceManualOverrides_Jobs FOREIGN KEY (JobID) REFERENCES dbo.Jobs (JobID),
        CONSTRAINT FK_AR_AttendanceManualOverrides_AttendanceLogs FOREIGN KEY (AttendanceLogID) REFERENCES dbo.AttendanceLogs (AttendanceLogID),
        CONSTRAINT FK_AR_AttendanceManualOverrides_WorkPlans FOREIGN KEY (WorkPlanID) REFERENCES dbo.WorkPlans (WorkPlanID),
        CONSTRAINT FK_AR_AttendanceManualOverrides_WorkAssignments FOREIGN KEY (WorkAssignmentID) REFERENCES dbo.WorkAssignments (WorkAssignmentID),
        CONSTRAINT FK_AR_AttendanceManualOverrides_WorkPlanSegments FOREIGN KEY (WorkPlanSegmentID) REFERENCES dbo.WorkPlanSegments (WorkPlanSegmentID),
        CONSTRAINT FK_AR_AttendanceManualOverrides_Users FOREIGN KEY (CreatedByUserID) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_AR_AttendanceManualOverrides_OperationLogs FOREIGN KEY (OperationLogID) REFERENCES dbo.OperationLogs (OperationLogID)
    );

    CREATE INDEX IX_AR_AttendanceManualOverrides_EmployeeWorkDate
        ON dbo.AR_AttendanceManualOverrides (IndividualID, JobID, WorkDate, IsActive);
END;

-- 5. Add multi-schedule & reconciliation audit columns to AttendanceLogResolutions
IF OBJECT_ID('dbo.AttendanceLogResolutions', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.AttendanceLogResolutions', 'IsPrimaryClassification') IS NULL
        ALTER TABLE dbo.AttendanceLogResolutions
            ADD IsPrimaryClassification BIT NOT NULL
                CONSTRAINT DF_AttendanceLogResolutions_IsPrimaryClassification DEFAULT (1);

    IF COL_LENGTH('dbo.AttendanceLogResolutions', 'SchedulePurpose') IS NULL
        ALTER TABLE dbo.AttendanceLogResolutions
            ADD SchedulePurpose NVARCHAR(30) NULL;

    IF COL_LENGTH('dbo.AttendanceLogResolutions', 'LogicalEventType') IS NULL
        ALTER TABLE dbo.AttendanceLogResolutions
            ADD LogicalEventType NVARCHAR(50) NULL;

    IF COL_LENGTH('dbo.AttendanceLogResolutions', 'MatchMethod') IS NULL
        ALTER TABLE dbo.AttendanceLogResolutions
            ADD MatchMethod NVARCHAR(50) NULL;

    IF COL_LENGTH('dbo.AttendanceLogResolutions', 'MatchReason') IS NULL
        ALTER TABLE dbo.AttendanceLogResolutions
            ADD MatchReason NVARCHAR(500) NULL;

    IF COL_LENGTH('dbo.AttendanceLogResolutions', 'ConfidenceState') IS NULL
        ALTER TABLE dbo.AttendanceLogResolutions
            ADD ConfidenceState NVARCHAR(30) NOT NULL
                CONSTRAINT DF_AttendanceLogResolutions_ConfidenceState DEFAULT (N'Deterministic');

    IF COL_LENGTH('dbo.AttendanceLogResolutions', 'PolicyVersion') IS NULL
        ALTER TABLE dbo.AttendanceLogResolutions
            ADD PolicyVersion NVARCHAR(40) NOT NULL
                CONSTRAINT DF_AttendanceLogResolutions_PolicyVersion DEFAULT (N'v2.0-OfficialPriority');

    IF COL_LENGTH('dbo.AttendanceLogResolutions', 'ReconciliationRunID') IS NULL
        ALTER TABLE dbo.AttendanceLogResolutions
            ADD ReconciliationRunID BIGINT NULL;

    IF COL_LENGTH('dbo.AttendanceLogResolutions', 'AttendanceObligationID') IS NULL
        ALTER TABLE dbo.AttendanceLogResolutions
            ADD AttendanceObligationID BIGINT NULL;

    IF COL_LENGTH('dbo.AttendanceLogResolutions', 'OverriddenByUserID') IS NULL
        ALTER TABLE dbo.AttendanceLogResolutions
            ADD OverriddenByUserID INT NULL;

    IF COL_LENGTH('dbo.AttendanceLogResolutions', 'OverrideReason') IS NULL
        ALTER TABLE dbo.AttendanceLogResolutions
            ADD OverrideReason NVARCHAR(1000) NULL;

    IF COL_LENGTH('dbo.AttendanceLogResolutions', 'OverriddenAtUtc') IS NULL
        ALTER TABLE dbo.AttendanceLogResolutions
            ADD OverriddenAtUtc DATETIME2(0) NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.AttendanceLogResolutions')
          AND name = 'UX_AttendanceLogResolutions_ActiveAttendanceLog'
    )
    BEGIN
        DROP INDEX UX_AttendanceLogResolutions_ActiveAttendanceLog ON dbo.AttendanceLogResolutions;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.AttendanceLogResolutions')
          AND name = 'UX_AttendanceLogResolutions_ActivePrimaryPerLog'
    )
    BEGIN
        EXEC(N'CREATE UNIQUE INDEX UX_AttendanceLogResolutions_ActivePrimaryPerLog
            ON dbo.AttendanceLogResolutions (AttendanceLogID)
            WHERE [IsValid] = 1 AND [IsPrimaryClassification] = 1;');
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.AttendanceLogResolutions')
          AND name = 'UX_AttendanceLogResolutions_ActiveSegmentPerLog'
    )
    BEGIN
        EXEC(N'CREATE UNIQUE INDEX UX_AttendanceLogResolutions_ActiveSegmentPerLog
            ON dbo.AttendanceLogResolutions (AttendanceLogID, WorkPlanSegmentID)
            WHERE [IsValid] = 1 AND [WorkPlanSegmentID] IS NOT NULL;');
    END;
END;

IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20261103140000_AttendanceReconciliationEngine')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20261103140000_AttendanceReconciliationEngine', N'8.0.27');
END;

COMMIT TRANSACTION;

-- OC_ Organisation Event Manager / Calendar tables. Additive and idempotent; no data is altered.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.OC_OrganisationEvents', N'U') IS NULL
CREATE TABLE dbo.OC_OrganisationEvents (
    OrganisationEventId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OC_OrganisationEvents PRIMARY KEY,
    OrganisationBusinessEntityId int NOT NULL CONSTRAINT FK_OC_Events_Org REFERENCES dbo.Organisations(BusinessEntityID),
    EventCode nvarchar(40) NULL,
    Title nvarchar(200) NOT NULL,
    Category nvarchar(40) NOT NULL,
    Description nvarchar(max) NULL,
    StartDateTime datetime2 NOT NULL,
    EndDateTime datetime2 NOT NULL,
    TimeZoneId nvarchar(64) NOT NULL,
    IsAllDay bit NOT NULL,
    VenueName nvarchar(200) NULL,
    VenueAddress nvarchar(400) NULL,
    MeetingUrl nvarchar(500) NULL,
    OrganisingStructureId int NULL CONSTRAINT FK_OC_Events_Structure REFERENCES dbo.OrganisationStructure(OrganisationStructureID),
    ResponsibleIndividualId int NULL CONSTRAINT FK_OC_Events_Responsible REFERENCES dbo.Staffs(IndividualID),
    ContactInformation nvarchar(300) NULL,
    DressCode nvarchar(200) NULL,
    AttendanceInstructions nvarchar(max) NULL,
    RequiredItems nvarchar(max) NULL,
    AdditionalNotes nvarchar(max) NULL,
    AttendanceRequirement nvarchar(24) NOT NULL,
    CalendarImpactType nvarchar(32) NOT NULL,
    EffectiveWorkStartTime time NULL,
    EffectiveWorkEndTime time NULL,
    AffectsAttendanceExpectation bit NOT NULL,
    RequiresLeaveRecalculation bit NOT NULL,
    Status nvarchar(20) NOT NULL,
    Visibility nvarchar(20) NOT NULL,
    NotifyStaff bit NOT NULL,
    NotifyBeforeMinutes int NULL,
    PublishDate datetime2 NULL,
    CreatedBy int NOT NULL,
    CreatedDate datetime2 NOT NULL,
    UpdatedBy int NULL,
    UpdatedDate datetime2 NULL,
    ApprovedBy int NULL,
    ApprovedDate datetime2 NULL,
    PublishedBy int NULL,
    IsValid bit NOT NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT CK_OC_Events_Dates CHECK (EndDateTime >= StartDateTime),
    CONSTRAINT CK_OC_Events_Status CHECK (Status IN ('Draft','PendingApproval','Published','Cancelled','Completed')),
    CONSTRAINT CK_OC_Events_Attendance CHECK (AttendanceRequirement IN ('InformationOnly','Optional','Required','RequiredSelected')),
    CONSTRAINT CK_OC_Events_Impact CHECK (CalendarImpactType IN ('None','MarkAsNonWorkingDay','MarkAsWorkingDay','OverrideWorkingHours','ShortenedWorkingDay','ExtendedWorkingDay')),
    CONSTRAINT CK_OC_Events_ImpactHours CHECK (CalendarImpactType NOT IN ('MarkAsWorkingDay','OverrideWorkingHours','ShortenedWorkingDay','ExtendedWorkingDay')
        OR (EffectiveWorkStartTime IS NOT NULL AND EffectiveWorkEndTime IS NOT NULL AND EffectiveWorkEndTime > EffectiveWorkStartTime))
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OC_OrganisationEvents_Org_Status_Start')
    CREATE INDEX IX_OC_OrganisationEvents_Org_Status_Start ON dbo.OC_OrganisationEvents(OrganisationBusinessEntityId, Status, StartDateTime);

IF OBJECT_ID(N'dbo.OC_OrganisationEventAudiences', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OC_OrganisationEventAudiences (
        OrganisationEventAudienceId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OC_EventAudiences PRIMARY KEY,
        OrganisationEventId int NOT NULL CONSTRAINT FK_OC_EventAudiences_Event REFERENCES dbo.OC_OrganisationEvents(OrganisationEventId),
        AudienceType nvarchar(24) NOT NULL,
        TargetEntityId int NULL,
        IsIncluded bit NOT NULL,
        CONSTRAINT CK_OC_EventAudiences_Type CHECK (AudienceType IN ('Organisation','Department','Team','Individual','EmploymentCategory')),
        CONSTRAINT CK_OC_EventAudiences_Target CHECK (AudienceType = 'Organisation' OR TargetEntityId IS NOT NULL)
    );
    CREATE UNIQUE INDEX UX_OC_EventAudiences ON dbo.OC_OrganisationEventAudiences(OrganisationEventId, AudienceType, TargetEntityId);
END;

IF OBJECT_ID(N'dbo.OC_OrganisationEventRecurrences', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OC_OrganisationEventRecurrences (
        OrganisationEventRecurrenceId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OC_EventRecurrences PRIMARY KEY,
        OrganisationEventId int NOT NULL CONSTRAINT FK_OC_EventRecurrences_Event REFERENCES dbo.OC_OrganisationEvents(OrganisationEventId),
        RecurrenceType nvarchar(12) NOT NULL,
        Interval int NOT NULL,
        WeekdayMask int NOT NULL,
        EffectiveFrom datetime2 NOT NULL,
        EffectiveTo datetime2 NULL,
        MaxOccurrences int NULL,
        CONSTRAINT CK_OC_EventRecurrences_Type CHECK (RecurrenceType IN ('Daily','Weekly','Monthly','Yearly')),
        CONSTRAINT CK_OC_EventRecurrences_Interval CHECK (Interval >= 1)
    );
    CREATE UNIQUE INDEX UX_OC_EventRecurrences_Event ON dbo.OC_OrganisationEventRecurrences(OrganisationEventId);
END;

IF OBJECT_ID(N'dbo.OC_OrganisationEventOccurrenceExceptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OC_OrganisationEventOccurrenceExceptions (
        OrganisationEventOccurrenceExceptionId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OC_EventOccurrenceExceptions PRIMARY KEY,
        OrganisationEventRecurrenceId int NOT NULL CONSTRAINT FK_OC_OccExc_Recurrence REFERENCES dbo.OC_OrganisationEventRecurrences(OrganisationEventRecurrenceId),
        OccurrenceDate date NOT NULL,
        IsCancelled bit NOT NULL,
        OverrideStartDateTime datetime2 NULL,
        OverrideEndDateTime datetime2 NULL,
        OverrideVenueName nvarchar(200) NULL,
        Reason nvarchar(400) NULL,
        CreatedBy int NOT NULL,
        CreatedDate datetime2 NOT NULL
    );
    CREATE UNIQUE INDEX UX_OC_OccExc ON dbo.OC_OrganisationEventOccurrenceExceptions(OrganisationEventRecurrenceId, OccurrenceDate);
END;

IF OBJECT_ID(N'dbo.OC_CalendarExceptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OC_CalendarExceptions (
        OrganisationCalendarExceptionId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OC_CalendarExceptions PRIMARY KEY,
        OrganisationBusinessEntityId int NOT NULL CONSTRAINT FK_OC_CalExc_Org REFERENCES dbo.Organisations(BusinessEntityID),
        OrganisationEventId int NULL CONSTRAINT FK_OC_CalExc_Event REFERENCES dbo.OC_OrganisationEvents(OrganisationEventId),
        EffectiveDate date NOT NULL,
        EffectiveEndDate date NOT NULL,
        TargetScope nvarchar(16) NOT NULL,
        TargetEntityId int NULL,
        CalendarImpactType nvarchar(32) NOT NULL,
        Priority int NOT NULL,
        Reason nvarchar(400) NOT NULL,
        IsApproved bit NOT NULL,
        IsActive bit NOT NULL,
        ApprovedBy int NULL,
        ApprovedDate datetime2 NULL,
        CreatedDate datetime2 NOT NULL,
        CreatedBy int NOT NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT CK_OC_CalExc_Dates CHECK (EffectiveEndDate >= EffectiveDate),
        CONSTRAINT CK_OC_CalExc_Scope CHECK (TargetScope IN ('Organisation','Department','Team','Individual')),
        CONSTRAINT CK_OC_CalExc_Target CHECK (TargetScope = 'Organisation' OR TargetEntityId IS NOT NULL),
        CONSTRAINT CK_OC_CalExc_Impact CHECK (CalendarImpactType IN ('MarkAsNonWorkingDay','MarkAsWorkingDay','OverrideWorkingHours','ShortenedWorkingDay','ExtendedWorkingDay'))
    );
    CREATE INDEX IX_OC_CalExc_Org_Dates ON dbo.OC_CalendarExceptions(OrganisationBusinessEntityId, EffectiveDate, EffectiveEndDate, IsActive);
END;

IF OBJECT_ID(N'dbo.OC_CalendarExceptionSegments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OC_CalendarExceptionSegments (
        OrganisationCalendarExceptionSegmentId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OC_CalExcSegments PRIMARY KEY,
        OrganisationCalendarExceptionId int NOT NULL CONSTRAINT FK_OC_CalExcSeg_Exc REFERENCES dbo.OC_CalendarExceptions(OrganisationCalendarExceptionId),
        StartTime time NOT NULL,
        EndTime time NOT NULL,
        CONSTRAINT CK_OC_CalExcSeg_Times CHECK (EndTime > StartTime)
    );
    CREATE INDEX IX_OC_CalExcSeg_Exc ON dbo.OC_CalendarExceptionSegments(OrganisationCalendarExceptionId);
END;

IF OBJECT_ID(N'dbo.OC_CalendarImpactAssessments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OC_CalendarImpactAssessments (
        CalendarImpactAssessmentId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OC_ImpactAssessments PRIMARY KEY,
        OrganisationCalendarExceptionId int NOT NULL CONSTRAINT FK_OC_Impact_Exc REFERENCES dbo.OC_CalendarExceptions(OrganisationCalendarExceptionId),
        SourceRecordType nvarchar(16) NOT NULL,
        SourceRecordId int NOT NULL,
        IndividualId int NOT NULL CONSTRAINT FK_OC_Impact_Staff REFERENCES dbo.Staffs(IndividualID),
        AssessmentType nvarchar(32) NOT NULL,
        PreviousValue nvarchar(500) NULL,
        ProposedValue nvarchar(500) NULL,
        ImpactDetails nvarchar(1000) NULL,
        Status nvarchar(16) NOT NULL,
        IsLockedPeriod bit NOT NULL,
        AssessedDate datetime2 NOT NULL,
        ReviewedBy int NULL,
        ReviewedDate datetime2 NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT CK_OC_Impact_Source CHECK (SourceRecordType IN ('Leave','LeavePlan','WorkPlan','Schedule')),
        CONSTRAINT CK_OC_Impact_Status CHECK (Status IN ('Pending','Reviewed','Adjusted','Dismissed','Blocked'))
    );
    CREATE UNIQUE INDEX UX_OC_Impact_Source ON dbo.OC_CalendarImpactAssessments(OrganisationCalendarExceptionId, SourceRecordType, SourceRecordId, AssessmentType);
END;

IF OBJECT_ID(N'dbo.OC_EventNotifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OC_EventNotifications (
        OrganisationEventNotificationId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_OC_EventNotifications PRIMARY KEY,
        OrganisationEventId int NOT NULL CONSTRAINT FK_OC_EventNotif_Event REFERENCES dbo.OC_OrganisationEvents(OrganisationEventId),
        OccurrenceDate date NULL,
        IndividualId int NOT NULL CONSTRAINT FK_OC_EventNotif_Staff REFERENCES dbo.Staffs(IndividualID),
        Reason nvarchar(32) NOT NULL,
        StaffNotificationId bigint NULL,
        CreatedAtUtc datetime2 NOT NULL
    );
    CREATE UNIQUE INDEX UX_OC_EventNotif_Dedup ON dbo.OC_EventNotifications(OrganisationEventId, OccurrenceDate, IndividualId, Reason);
END;

IF OBJECT_ID(N'dbo.OC_SupervisorTasks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OC_SupervisorTasks (
        SupervisorCalendarTaskId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OC_SupervisorTasks PRIMARY KEY,
        OrganisationBusinessEntityId int NOT NULL CONSTRAINT FK_OC_SupTask_Org REFERENCES dbo.Organisations(BusinessEntityID),
        TeamId int NULL CONSTRAINT FK_OC_SupTask_Team REFERENCES dbo.Teams(TeamID),
        OwnerIndividualId int NOT NULL CONSTRAINT FK_OC_SupTask_Owner REFERENCES dbo.Staffs(IndividualID),
        Title nvarchar(200) NOT NULL,
        Description nvarchar(1000) NULL,
        TaskCategory nvarchar(40) NOT NULL,
        StartDateTime datetime2 NULL,
        DueDateTime datetime2 NOT NULL,
        Priority nvarchar(10) NOT NULL,
        Status nvarchar(16) NOT NULL,
        ReminderMinutes int NULL,
        SourceModule nvarchar(40) NULL,
        SourceRecordId int NULL,
        CreatedBy int NOT NULL,
        CreatedDate datetime2 NOT NULL,
        UpdatedDate datetime2 NOT NULL,
        IsDeleted bit NOT NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT CK_OC_SupTask_Status CHECK (Status IN ('NotStarted','InProgress','Completed','Cancelled')),
        CONSTRAINT CK_OC_SupTask_Priority CHECK (Priority IN ('Low','Normal','High','Critical'))
    );
    CREATE INDEX IX_OC_SupTask_Org_Owner_Due ON dbo.OC_SupervisorTasks(OrganisationBusinessEntityId, OwnerIndividualId, DueDateTime);
END;

IF OBJECT_ID(N'dbo.OC_SupervisorTaskAssignees', N'U') IS NULL
CREATE TABLE dbo.OC_SupervisorTaskAssignees (
    SupervisorCalendarTaskId int NOT NULL CONSTRAINT FK_OC_SupTaskAssignee_Task REFERENCES dbo.OC_SupervisorTasks(SupervisorCalendarTaskId),
    IndividualId int NOT NULL CONSTRAINT FK_OC_SupTaskAssignee_Staff REFERENCES dbo.Staffs(IndividualID),
    CONSTRAINT PK_OC_SupTaskAssignees PRIMARY KEY (SupervisorCalendarTaskId, IndividualId)
);

IF OBJECT_ID(N'dbo.OC_EventAudits', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OC_EventAudits (
        OrganisationEventAuditId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_OC_EventAudits PRIMARY KEY,
        OrganisationBusinessEntityId int NOT NULL CONSTRAINT FK_OC_EventAudits_Org REFERENCES dbo.Organisations(BusinessEntityID),
        OrganisationEventId int NOT NULL CONSTRAINT FK_OC_EventAudits_Event REFERENCES dbo.OC_OrganisationEvents(OrganisationEventId),
        Action nvarchar(32) NOT NULL,
        ActorIndividualId int NOT NULL,
        PreviousValue nvarchar(2000) NULL,
        NewValue nvarchar(2000) NULL,
        Reason nvarchar(400) NULL,
        CreatedAtUtc datetime2 NOT NULL
    );
    CREATE INDEX IX_OC_EventAudits_Event ON dbo.OC_EventAudits(OrganisationEventId, CreatedAtUtc);
END;

COMMIT;

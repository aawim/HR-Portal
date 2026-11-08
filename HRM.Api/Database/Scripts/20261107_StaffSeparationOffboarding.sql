-- 20261107_StaffSeparationOffboarding.sql
-- Feature Prefix: SEP_ (Enterprise Staff Separation, Offboarding and Clearance Management)

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SEP_SeparationTypes')
BEGIN
    CREATE TABLE dbo.SEP_SeparationTypes (
        SeparationTypeId INT IDENTITY(1,1) NOT NULL,
        OrganisationId INT NULL,
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        Category NVARCHAR(50) NOT NULL,
        DefaultNoticeDays INT NOT NULL CONSTRAINT DF_SEP_SeparationTypes_DefaultNoticeDays DEFAULT (30),
        RequiresClearance BIT NOT NULL CONSTRAINT DF_SEP_SeparationTypes_RequiresClearance DEFAULT (1),
        RequiresExitInterview BIT NOT NULL CONSTRAINT DF_SEP_SeparationTypes_RequiresExitInterview DEFAULT (1),
        AllowsNoticeWaiver BIT NOT NULL CONSTRAINT DF_SEP_SeparationTypes_AllowsNoticeWaiver DEFAULT (1),
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_SEP_SeparationTypes_IsActive DEFAULT (1),
        SortOrder INT NOT NULL CONSTRAINT DF_SEP_SeparationTypes_SortOrder DEFAULT (0),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SEP_SeparationTypes_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SEP_SeparationTypes_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_SEP_SeparationTypes PRIMARY KEY CLUSTERED (SeparationTypeId),
        CONSTRAINT FK_SEP_SeparationTypes_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID) ON DELETE SET NULL
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_SEP_SeparationTypes_Code_Org ON dbo.SEP_SeparationTypes (Code, OrganisationId);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SEP_ClearanceTemplates')
BEGIN
    CREATE TABLE dbo.SEP_ClearanceTemplates (
        ClearanceTemplateId INT IDENTITY(1,1) NOT NULL,
        OrganisationId INT NOT NULL,
        SeparationTypeId INT NULL,
        JobTypeId INT NULL,
        TemplateName NVARCHAR(150) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsDefault BIT NOT NULL CONSTRAINT DF_SEP_ClearanceTemplates_IsDefault DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_SEP_ClearanceTemplates_IsActive DEFAULT (1),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SEP_ClearanceTemplates_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SEP_ClearanceTemplates_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_SEP_ClearanceTemplates PRIMARY KEY CLUSTERED (ClearanceTemplateId),
        CONSTRAINT FK_SEP_ClearanceTemplates_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_SEP_ClearanceTemplates_SeparationTypes FOREIGN KEY (SeparationTypeId) REFERENCES dbo.SEP_SeparationTypes (SeparationTypeId) ON DELETE SET NULL,
        CONSTRAINT FK_SEP_ClearanceTemplates_JobTypes FOREIGN KEY (JobTypeId) REFERENCES dbo.JobTypes (JobTypeID) ON DELETE SET NULL
    );

    CREATE INDEX IX_SEP_ClearanceTemplates_Org_Type ON dbo.SEP_ClearanceTemplates (OrganisationId, SeparationTypeId, JobTypeId);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SEP_ClearanceTemplateTasks')
BEGIN
    CREATE TABLE dbo.SEP_ClearanceTemplateTasks (
        ClearanceTemplateTaskId INT IDENTITY(1,1) NOT NULL,
        ClearanceTemplateId INT NOT NULL,
        Domain NVARCHAR(50) NOT NULL,
        TaskName NVARCHAR(200) NOT NULL,
        TaskDescription NVARCHAR(500) NULL,
        AssignedOrganisationStructureId INT NULL,
        AssignedRoleId INT NULL,
        IsMandatory BIT NOT NULL CONSTRAINT DF_SEP_ClearanceTemplateTasks_IsMandatory DEFAULT (1),
        SequenceOrder INT NOT NULL CONSTRAINT DF_SEP_ClearanceTemplateTasks_SequenceOrder DEFAULT (0),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SEP_ClearanceTemplateTasks_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_SEP_ClearanceTemplateTasks PRIMARY KEY CLUSTERED (ClearanceTemplateTaskId),
        CONSTRAINT FK_SEP_ClearanceTemplateTasks_Templates FOREIGN KEY (ClearanceTemplateId) REFERENCES dbo.SEP_ClearanceTemplates (ClearanceTemplateId) ON DELETE CASCADE,
        CONSTRAINT FK_SEP_ClearanceTemplateTasks_OrgStructures FOREIGN KEY (AssignedOrganisationStructureId) REFERENCES dbo.OrganisationStructure (OrganisationStructureID) ON DELETE SET NULL
    );

    CREATE INDEX IX_SEP_ClearanceTemplateTasks_Template ON dbo.SEP_ClearanceTemplateTasks (ClearanceTemplateId, SequenceOrder);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SEP_StaffSeparations')
BEGIN
    CREATE TABLE dbo.SEP_StaffSeparations (
        StaffSeparationId INT IDENTITY(1,1) NOT NULL,
        OrganisationId INT NOT NULL,
        IndividualId INT NOT NULL,
        JobId INT NOT NULL,
        JobPositionId INT NULL,
        SeparationTypeId INT NOT NULL,
        RequestId INT NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_SEP_StaffSeparations_Status DEFAULT ('Draft'),
        NoticeDate DATE NOT NULL,
        NoticePeriodDays INT NOT NULL CONSTRAINT DF_SEP_StaffSeparations_NoticePeriodDays DEFAULT (30),
        NoticeShortfallDays INT NOT NULL CONSTRAINT DF_SEP_StaffSeparations_NoticeShortfallDays DEFAULT (0),
        NoticeShortfallRecoverableAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SEP_StaffSeparations_ShortfallAmount DEFAULT (0),
        LastWorkingDate DATE NOT NULL,
        EffectiveSeparationDate DATE NOT NULL,
        ReasonCategory NVARCHAR(100) NOT NULL,
        ReasonDetails NVARCHAR(2000) NULL,
        EmployeeComments NVARCHAR(2000) NULL,
        HandoverRequired BIT NOT NULL CONSTRAINT DF_SEP_StaffSeparations_HandoverRequired DEFAULT (1),
        ClearanceRequired BIT NOT NULL CONSTRAINT DF_SEP_StaffSeparations_ClearanceRequired DEFAULT (1),
        ExitInterviewRequired BIT NOT NULL CONSTRAINT DF_SEP_StaffSeparations_ExitInterviewRequired DEFAULT (0),
        EligibleForRehire BIT NOT NULL CONSTRAINT DF_SEP_StaffSeparations_EligibleForRehire DEFAULT (1),
        RehireEligibilityNotes NVARCHAR(500) NULL,
        InitiatedByUserId INT NOT NULL,
        InitiatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SEP_StaffSeparations_InitiatedAtUtc DEFAULT (SYSUTCDATETIME()),
        SupervisorIndividualId INT NULL,
        SupervisorApprovalStatus NVARCHAR(50) NULL,
        SupervisorApprovalComments NVARCHAR(1000) NULL,
        SupervisorApprovedAtUtc DATETIME2 NULL,
        HodIndividualId INT NULL,
        HodApprovalStatus NVARCHAR(50) NULL,
        HodApprovalComments NVARCHAR(1000) NULL,
        HodApprovedAtUtc DATETIME2 NULL,
        FinalApprovedByUserId INT NULL,
        FinalApprovedAtUtc DATETIME2 NULL,
        FinalApprovalComments NVARCHAR(1000) NULL,
        ExecutedByUserId INT NULL,
        ExecutedAtUtc DATETIME2 NULL,
        ExecutionNotes NVARCHAR(1000) NULL,
        HasUnresolvedObligations BIT NOT NULL CONSTRAINT DF_SEP_StaffSeparations_HasUnresolvedObligations DEFAULT (0),
        UnresolvedObligationsDescription NVARCHAR(1000) NULL,
        PersonalEmail NVARCHAR(200) NULL,
        PersonalContactNumber NVARCHAR(50) NULL,
        ForwardingAddress NVARCHAR(500) NULL,
        EmployeeExitComments NVARCHAR(2000) NULL,
        EmployeeAcknowledgedAtUtc DATETIME2 NULL,
        FullNameSnapshot NVARCHAR(200) NOT NULL,
        EmployeeNumberSnapshot NVARCHAR(50) NOT NULL,
        PositionTitleSnapshot NVARCHAR(150) NOT NULL,
        DepartmentNameSnapshot NVARCHAR(150) NOT NULL,
        JobProfileSnapshotJson NVARCHAR(MAX) NULL,
        EmployeeProfileSnapshotJson NVARCHAR(MAX) NULL,
        RowVersion ROWVERSION NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SEP_StaffSeparations_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SEP_StaffSeparations_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_SEP_StaffSeparations PRIMARY KEY CLUSTERED (StaffSeparationId),
        CONSTRAINT FK_SEP_StaffSeparations_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_SEP_StaffSeparations_Individuals FOREIGN KEY (IndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID),
        CONSTRAINT FK_SEP_StaffSeparations_Jobs FOREIGN KEY (JobId) REFERENCES dbo.Jobs (JobID),
        CONSTRAINT FK_SEP_StaffSeparations_JobPositions FOREIGN KEY (JobPositionId) REFERENCES dbo.JobPositions (JobPositionID),
        CONSTRAINT FK_SEP_StaffSeparations_SeparationTypes FOREIGN KEY (SeparationTypeId) REFERENCES dbo.SEP_SeparationTypes (SeparationTypeId),
        CONSTRAINT FK_SEP_StaffSeparations_Requests FOREIGN KEY (RequestId) REFERENCES dbo.Requests (RequestID) ON DELETE SET NULL,
        CONSTRAINT FK_SEP_StaffSeparations_Initiators FOREIGN KEY (InitiatedByUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_SEP_StaffSeparations_FinalApprovers FOREIGN KEY (FinalApprovedByUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_SEP_StaffSeparations_Executors FOREIGN KEY (ExecutedByUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_SEP_StaffSeparations_Supervisors FOREIGN KEY (SupervisorIndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID),
        CONSTRAINT FK_SEP_StaffSeparations_Hods FOREIGN KEY (HodIndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_SEP_StaffSeparations_ActiveJob ON dbo.SEP_StaffSeparations (JobId)
    WHERE Status NOT IN ('Cancelled', 'Rejected', 'Terminated');

    CREATE INDEX IX_SEP_StaffSeparations_Org_Status ON dbo.SEP_StaffSeparations (OrganisationId, Status);
    CREATE INDEX IX_SEP_StaffSeparations_Individual ON dbo.SEP_StaffSeparations (IndividualId, EffectiveSeparationDate);
    CREATE INDEX IX_SEP_StaffSeparations_LastWorkingDate ON dbo.SEP_StaffSeparations (LastWorkingDate);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SEP_ClearanceTasks')
BEGIN
    CREATE TABLE dbo.SEP_ClearanceTasks (
        ClearanceTaskId INT IDENTITY(1,1) NOT NULL,
        StaffSeparationId INT NOT NULL,
        Domain NVARCHAR(50) NOT NULL,
        TaskName NVARCHAR(200) NOT NULL,
        TaskDescription NVARCHAR(500) NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_SEP_ClearanceTasks_Status DEFAULT ('Pending'),
        AssignedOrganisationStructureId INT NULL,
        AssignedToRoleId INT NULL,
        AssignedToUserId INT NULL,
        IsMandatory BIT NOT NULL CONSTRAINT DF_SEP_ClearanceTasks_IsMandatory DEFAULT (1),
        SequenceOrder INT NOT NULL CONSTRAINT DF_SEP_ClearanceTasks_SequenceOrder DEFAULT (0),
        RequiresSignoff BIT NOT NULL CONSTRAINT DF_SEP_ClearanceTasks_RequiresSignoff DEFAULT (1),
        SignoffNotes NVARCHAR(1000) NULL,
        SignedOffByUserId INT NULL,
        SignedOffAtUtc DATETIME2 NULL,
        HasException BIT NOT NULL CONSTRAINT DF_SEP_ClearanceTasks_HasException DEFAULT (0),
        ExceptionReason NVARCHAR(1000) NULL,
        ExceptionApprovedByUserId INT NULL,
        RowVersion ROWVERSION NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SEP_ClearanceTasks_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SEP_ClearanceTasks_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_SEP_ClearanceTasks PRIMARY KEY CLUSTERED (ClearanceTaskId),
        CONSTRAINT FK_SEP_ClearanceTasks_Separations FOREIGN KEY (StaffSeparationId) REFERENCES dbo.SEP_StaffSeparations (StaffSeparationId) ON DELETE CASCADE,
        CONSTRAINT FK_SEP_ClearanceTasks_AssignedUsers FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_SEP_ClearanceTasks_SignoffUsers FOREIGN KEY (SignedOffByUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_SEP_ClearanceTasks_ExceptionApprovers FOREIGN KEY (ExceptionApprovedByUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_SEP_ClearanceTasks_OrgStructures FOREIGN KEY (AssignedOrganisationStructureId) REFERENCES dbo.OrganisationStructure (OrganisationStructureID) ON DELETE SET NULL
    );

    CREATE INDEX IX_SEP_ClearanceTasks_Separation_Domain ON dbo.SEP_ClearanceTasks (StaffSeparationId, Domain, Status);
    CREATE INDEX IX_SEP_ClearanceTasks_AssignedUser ON dbo.SEP_ClearanceTasks (AssignedToUserId, Status);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SEP_WorkHandovers')
BEGIN
    CREATE TABLE dbo.SEP_WorkHandovers (
        WorkHandoverId INT IDENTITY(1,1) NOT NULL,
        StaffSeparationId INT NOT NULL,
        SupervisorIndividualId INT NOT NULL,
        HodIndividualId INT NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_SEP_WorkHandovers_Status DEFAULT ('Draft'),
        EmployeeNotes NVARCHAR(2000) NULL,
        SupervisorComments NVARCHAR(1000) NULL,
        SupervisorDecisionDate DATETIME2 NULL,
        HodComments NVARCHAR(1000) NULL,
        HodDecisionDate DATETIME2 NULL,
        IsFullyCompleted BIT NOT NULL CONSTRAINT DF_SEP_WorkHandovers_IsFullyCompleted DEFAULT (0),
        RowVersion ROWVERSION NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SEP_WorkHandovers_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SEP_WorkHandovers_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_SEP_WorkHandovers PRIMARY KEY CLUSTERED (WorkHandoverId),
        CONSTRAINT FK_SEP_WorkHandovers_Separations FOREIGN KEY (StaffSeparationId) REFERENCES dbo.SEP_StaffSeparations (StaffSeparationId) ON DELETE CASCADE,
        CONSTRAINT FK_SEP_WorkHandovers_Supervisors FOREIGN KEY (SupervisorIndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID),
        CONSTRAINT FK_SEP_WorkHandovers_Hods FOREIGN KEY (HodIndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_SEP_WorkHandovers_SeparationId ON dbo.SEP_WorkHandovers (StaffSeparationId);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SEP_WorkHandoverItems')
BEGIN
    CREATE TABLE dbo.SEP_WorkHandoverItems (
        WorkHandoverItemId INT IDENTITY(1,1) NOT NULL,
        WorkHandoverId INT NOT NULL,
        ItemType NVARCHAR(50) NOT NULL,
        ItemTitle NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        TaskOrAssignmentId BIGINT NULL,
        RecipientIndividualId INT NOT NULL,
        TargetCompletionDate DATE NOT NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_SEP_WorkHandoverItems_Status DEFAULT ('Pending'),
        RecipientAcknowledgedAtUtc DATETIME2 NULL,
        VerifiedBySupervisorAtUtc DATETIME2 NULL,
        Remarks NVARCHAR(500) NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SEP_WorkHandoverItems_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_SEP_WorkHandoverItems PRIMARY KEY CLUSTERED (WorkHandoverItemId),
        CONSTRAINT FK_SEP_WorkHandoverItems_WorkHandovers FOREIGN KEY (WorkHandoverId) REFERENCES dbo.SEP_WorkHandovers (WorkHandoverId) ON DELETE CASCADE,
        CONSTRAINT FK_SEP_WorkHandoverItems_Recipients FOREIGN KEY (RecipientIndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID)
    );

    CREATE INDEX IX_SEP_WorkHandoverItems_WorkHandover ON dbo.SEP_WorkHandoverItems (WorkHandoverId, Status);
    CREATE INDEX IX_SEP_WorkHandoverItems_Recipient ON dbo.SEP_WorkHandoverItems (RecipientIndividualId, Status);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SEP_AssetClearances')
BEGIN
    CREATE TABLE dbo.SEP_AssetClearances (
        SeparationAssetClearanceId INT IDENTITY(1,1) NOT NULL,
        StaffSeparationId INT NOT NULL,
        AssetCustodyId INT NOT NULL,
        AssetItemId INT NOT NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_SEP_AssetClearances_Status DEFAULT ('PendingReturn'),
        PhysicalCondition NVARCHAR(50) NULL,
        ConditionNotes NVARCHAR(500) NULL,
        DisposalAction NVARCHAR(50) NOT NULL CONSTRAINT DF_SEP_AssetClearances_DisposalAction DEFAULT ('Restock'),
        AssessedDamageCost DECIMAL(18,2) NOT NULL CONSTRAINT DF_SEP_AssetClearances_DamageCost DEFAULT (0),
        DeductionFromFinalSettlement BIT NOT NULL CONSTRAINT DF_SEP_AssetClearances_Deduction DEFAULT (0),
        InspectedByUserId INT NULL,
        InspectedAtUtc DATETIME2 NULL,
        ReturnedAtUtc DATETIME2 NULL,
        Remarks NVARCHAR(500) NULL,
        RowVersion ROWVERSION NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SEP_AssetClearances_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_SEP_AssetClearances PRIMARY KEY CLUSTERED (SeparationAssetClearanceId),
        CONSTRAINT FK_SEP_AssetClearances_Separations FOREIGN KEY (StaffSeparationId) REFERENCES dbo.SEP_StaffSeparations (StaffSeparationId) ON DELETE CASCADE,
        CONSTRAINT FK_SEP_AssetClearances_AssetCustodies FOREIGN KEY (AssetCustodyId) REFERENCES dbo.AM_AssetCustodies (AssetCustodyId),
        CONSTRAINT FK_SEP_AssetClearances_AssetItems FOREIGN KEY (AssetItemId) REFERENCES dbo.AM_AssetItems (AssetItemId),
        CONSTRAINT FK_SEP_AssetClearances_Inspectors FOREIGN KEY (InspectedByUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_SEP_AssetClearances_Separation_Custody ON dbo.SEP_AssetClearances (StaffSeparationId, AssetCustodyId);
    CREATE INDEX IX_SEP_AssetClearances_AssetItem ON dbo.SEP_AssetClearances (AssetItemId, Status);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SEP_FinanceItems')
BEGIN
    CREATE TABLE dbo.SEP_FinanceItems (
        SeparationFinanceItemId INT IDENTITY(1,1) NOT NULL,
        StaffSeparationId INT NOT NULL,
        Category NVARCHAR(50) NOT NULL,
        Description NVARCHAR(250) NOT NULL,
        IsCredit BIT NOT NULL CONSTRAINT DF_SEP_FinanceItems_IsCredit DEFAULT (1),
        Amount DECIMAL(18,2) NOT NULL,
        Currency NVARCHAR(10) NOT NULL CONSTRAINT DF_SEP_FinanceItems_Currency DEFAULT ('MVR'),
        ReferenceType NVARCHAR(50) NULL,
        ReferenceId BIGINT NULL,
        IsSettled BIT NOT NULL CONSTRAINT DF_SEP_FinanceItems_IsSettled DEFAULT (0),
        SettledAtUtc DATETIME2 NULL,
        AddedByUserId INT NOT NULL,
        Remarks NVARCHAR(500) NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SEP_FinanceItems_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_SEP_FinanceItems PRIMARY KEY CLUSTERED (SeparationFinanceItemId),
        CONSTRAINT FK_SEP_FinanceItems_Separations FOREIGN KEY (StaffSeparationId) REFERENCES dbo.SEP_StaffSeparations (StaffSeparationId) ON DELETE CASCADE,
        CONSTRAINT FK_SEP_FinanceItems_AddedByUsers FOREIGN KEY (AddedByUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE INDEX IX_SEP_FinanceItems_Separation ON dbo.SEP_FinanceItems (StaffSeparationId, Category, IsSettled);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SEP_AccessRevocations')
BEGIN
    CREATE TABLE dbo.SEP_AccessRevocations (
        SeparationAccessRevocationId INT IDENTITY(1,1) NOT NULL,
        StaffSeparationId INT NOT NULL,
        SystemType NVARCHAR(50) NOT NULL,
        SystemName NVARCHAR(150) NOT NULL,
        AccountIdentifier NVARCHAR(150) NOT NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_SEP_AccessRevocations_Status DEFAULT ('PendingRevocation'),
        IsImmediateSuspension BIT NOT NULL CONSTRAINT DF_SEP_AccessRevocations_Immediate DEFAULT (0),
        RevokedByUserId INT NULL,
        RevokedAtUtc DATETIME2 NULL,
        PhysicalCardNumber NVARCHAR(50) NULL,
        PhysicalCardReturned BIT NOT NULL CONSTRAINT DF_SEP_AccessRevocations_CardReturned DEFAULT (0),
        Notes NVARCHAR(500) NULL,
        RowVersion ROWVERSION NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SEP_AccessRevocations_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_SEP_AccessRevocations PRIMARY KEY CLUSTERED (SeparationAccessRevocationId),
        CONSTRAINT FK_SEP_AccessRevocations_Separations FOREIGN KEY (StaffSeparationId) REFERENCES dbo.SEP_StaffSeparations (StaffSeparationId) ON DELETE CASCADE,
        CONSTRAINT FK_SEP_AccessRevocations_Revokers FOREIGN KEY (RevokedByUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE INDEX IX_SEP_AccessRevocations_Separation ON dbo.SEP_AccessRevocations (StaffSeparationId, SystemType, Status);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SEP_SeparationDocuments')
BEGIN
    CREATE TABLE dbo.SEP_SeparationDocuments (
        SeparationDocumentId BIGINT IDENTITY(1,1) NOT NULL,
        StaffSeparationId INT NOT NULL,
        DocumentNumber NVARCHAR(100) NOT NULL,
        DocumentVariant NVARCHAR(50) NOT NULL,
        Title NVARCHAR(250) NOT NULL,
        PdfBytes VARBINARY(MAX) NULL,
        ContentType NVARCHAR(100) NOT NULL CONSTRAINT DF_SEP_SeparationDocuments_ContentType DEFAULT ('application/pdf'),
        Sha256Hash NVARCHAR(64) NOT NULL,
        GeneratedByUserId INT NOT NULL,
        GeneratedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SEP_SeparationDocuments_GeneratedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_SEP_SeparationDocuments PRIMARY KEY CLUSTERED (SeparationDocumentId),
        CONSTRAINT FK_SEP_SeparationDocuments_Separations FOREIGN KEY (StaffSeparationId) REFERENCES dbo.SEP_StaffSeparations (StaffSeparationId) ON DELETE CASCADE,
        CONSTRAINT FK_SEP_SeparationDocuments_Generators FOREIGN KEY (GeneratedByUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE INDEX IX_SEP_SeparationDocuments_SeparationId ON dbo.SEP_SeparationDocuments (StaffSeparationId, DocumentVariant);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SEP_SeparationEvents')
BEGIN
    CREATE TABLE dbo.SEP_SeparationEvents (
        SeparationEventId BIGINT IDENTITY(1,1) NOT NULL,
        StaffSeparationId INT NOT NULL,
        EventType NVARCHAR(50) NOT NULL,
        EventTitle NVARCHAR(200) NOT NULL,
        DetailsJson NVARCHAR(MAX) NULL,
        ActorUserId INT NOT NULL,
        ActorName NVARCHAR(150) NOT NULL,
        OccurredAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SEP_SeparationEvents_OccurredAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_SEP_SeparationEvents PRIMARY KEY CLUSTERED (SeparationEventId),
        CONSTRAINT FK_SEP_SeparationEvents_Separations FOREIGN KEY (StaffSeparationId) REFERENCES dbo.SEP_StaffSeparations (StaffSeparationId) ON DELETE CASCADE,
        CONSTRAINT FK_SEP_SeparationEvents_Actors FOREIGN KEY (ActorUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE INDEX IX_SEP_SeparationEvents_SeparationId ON dbo.SEP_SeparationEvents (StaffSeparationId, OccurredAtUtc);
END;

-- Seed standard system separation types if empty
IF NOT EXISTS (SELECT 1 FROM dbo.SEP_SeparationTypes)
BEGIN
    INSERT INTO dbo.SEP_SeparationTypes (OrganisationId, Code, Name, Category, DefaultNoticeDays, RequiresClearance, RequiresExitInterview, AllowsNoticeWaiver, Description, IsActive, SortOrder, CreatedAtUtc, UpdatedAtUtc)
    VALUES
    (NULL, 'RESIGNATION', 'Voluntary Resignation', 'Resignation', 30, 1, 1, 1, 'Standard voluntary employee resignation with notice period', 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, 'RETIREMENT', 'Age/Service Retirement', 'Retirement', 60, 1, 1, 1, 'Formal statutory or voluntary retirement on reaching qualifying age', 1, 2, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, 'CONTRACT_EXPIRY', 'End of Fixed-Term Contract', 'ContractExpiry', 30, 1, 0, 0, 'Natural expiration of fixed-term employment contract', 1, 3, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, 'TERMINATION_INITIATED', 'Employer-Initiated Termination', 'EmployerInitiated', 30, 1, 1, 0, 'Authorized organization-initiated contract termination', 1, 4, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, 'MUTUAL_SEPARATION', 'Mutual Separation Agreement', 'MutualSeparation', 14, 1, 1, 1, 'Bilateral agreed release under mutually executed terms', 1, 5, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, 'MEDICAL_INVALIDATION', 'Medical Discharge', 'MedicalDischarge', 0, 1, 0, 1, 'Medical board certified incapacity for ongoing service', 1, 6, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, 'PROBATION_TERMINATION', 'End of Probationary Period', 'ProbationTermination', 7, 1, 1, 0, 'Separation due to unconfirmed probation status', 1, 7, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

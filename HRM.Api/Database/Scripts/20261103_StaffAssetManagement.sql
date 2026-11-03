-- Staff Asset Assignment, Acknowledgement & Custody Management (AM_* tables, seed data, request type, and state transitions)
-- Idempotent deployment script matching EF Core migration 20261103014000_StaffAssetManagement.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.AM_AssetCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetCategories
    (
        AssetCategoryId int IDENTITY(1,1) NOT NULL,
        OrganisationId int NULL,
        Code nvarchar(50) NOT NULL,
        Name nvarchar(150) NOT NULL,
        Description nvarchar(1000) NOT NULL,
        RequiresSerialNumber bit NOT NULL,
        RequiresReturn bit NOT NULL,
        IsSecurityItem bit NOT NULL,
        IsActive bit NOT NULL,
        OperationLogId int NULL,
        CreatedAtUtc datetime2 NOT NULL,
        CONSTRAINT PK_AM_AssetCategories PRIMARY KEY (AssetCategoryId),
        CONSTRAINT FK_AM_AssetCategories_Organisations_OrganisationId FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_AM_AssetCategories_OperationLogs_OperationLogId FOREIGN KEY (OperationLogId) REFERENCES dbo.OperationLogs (OperationLogID)
    );

    CREATE UNIQUE INDEX IX_AM_AssetCategories_OrganisationId_Code ON dbo.AM_AssetCategories (OrganisationId, Code);
    CREATE UNIQUE INDEX IX_AM_AssetCategories_OrganisationId_Name ON dbo.AM_AssetCategories (OrganisationId, Name);
END;

IF OBJECT_ID(N'dbo.AM_AssetConditions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetConditions
    (
        AssetConditionId int IDENTITY(1,1) NOT NULL,
        Code nvarchar(40) NOT NULL,
        Name nvarchar(100) NOT NULL,
        Description nvarchar(500) NOT NULL,
        RequiresMaintenanceOnReturn bit NOT NULL,
        SortOrder int NOT NULL,
        IsActive bit NOT NULL,
        CONSTRAINT PK_AM_AssetConditions PRIMARY KEY (AssetConditionId)
    );

    CREATE UNIQUE INDEX IX_AM_AssetConditions_Code ON dbo.AM_AssetConditions (Code);
    CREATE UNIQUE INDEX IX_AM_AssetConditions_Name ON dbo.AM_AssetConditions (Name);
END;

IF OBJECT_ID(N'dbo.AM_AssetItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetItems
    (
        AssetItemId int IDENTITY(1,1) NOT NULL,
        CategoryId int NOT NULL,
        AssetCode nvarchar(100) NOT NULL,
        SerialNumber nvarchar(120) NULL,
        Name nvarchar(200) NOT NULL,
        Brand nvarchar(120) NULL,
        Model nvarchar(120) NULL,
        OwnerOrganisationId int NOT NULL,
        OwnerStructureId int NULL,
        Location nvarchar(200) NULL,
        Status nvarchar(30) NOT NULL,
        CurrentConditionId int NOT NULL,
        WarrantyExpiryDate date NULL,
        DefaultAccessories nvarchar(500) NULL,
        Notes nvarchar(1000) NULL,
        IsActive bit NOT NULL,
        OperationLogId int NULL,
        CreatedAtUtc datetime2 NOT NULL,
        UpdatedAtUtc datetime2 NOT NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_AM_AssetItems PRIMARY KEY (AssetItemId),
        CONSTRAINT CK_AM_AssetItems_Status CHECK ([Status] IN ('Available','Reserved','PendingHandover','Assigned','ReturnPending','Maintenance','Lost','Disposed')),
        CONSTRAINT FK_AM_AssetItems_AM_AssetCategories_CategoryId FOREIGN KEY (CategoryId) REFERENCES dbo.AM_AssetCategories (AssetCategoryId),
        CONSTRAINT FK_AM_AssetItems_Organisations_OwnerOrganisationId FOREIGN KEY (OwnerOrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_AM_AssetItems_OrganisationStructure_OwnerStructureId FOREIGN KEY (OwnerStructureId) REFERENCES dbo.OrganisationStructure (OrganisationStructureID),
        CONSTRAINT FK_AM_AssetItems_AM_AssetConditions_CurrentConditionId FOREIGN KEY (CurrentConditionId) REFERENCES dbo.AM_AssetConditions (AssetConditionId),
        CONSTRAINT FK_AM_AssetItems_OperationLogs_OperationLogId FOREIGN KEY (OperationLogId) REFERENCES dbo.OperationLogs (OperationLogID)
    );

    CREATE UNIQUE INDEX UX_AM_AssetItems_AssetCode ON dbo.AM_AssetItems (AssetCode);
    CREATE UNIQUE INDEX UX_AM_AssetItems_SerialNumber ON dbo.AM_AssetItems (OwnerOrganisationId, SerialNumber) WHERE [SerialNumber] IS NOT NULL AND [SerialNumber] <> '';
    CREATE INDEX IX_AM_AssetItems_OwnerOrganisationId_Status_CategoryId ON dbo.AM_AssetItems (OwnerOrganisationId, Status, CategoryId);
END;

IF OBJECT_ID(N'dbo.AM_AssetIssuances', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetIssuances
    (
        AssetIssuanceId int IDENTITY(1,1) NOT NULL,
        RequestId int NULL,
        ReferenceNumber nvarchar(60) NOT NULL,
        IndividualId int NOT NULL,
        JobId int NOT NULL,
        OrganisationId int NOT NULL,
        StructureId int NULL,
        Reason nvarchar(1000) NOT NULL,
        Status nvarchar(40) NOT NULL,
        CreatedByUserId int NOT NULL,
        IssueDate date NOT NULL,
        SubmittedAtUtc datetime2 NULL,
        FinalizedAtUtc datetime2 NULL,
        FinalizedByUserId int NULL,
        FinalizationIdempotencyKey nvarchar(80) NULL,
        EmployeeNameSnapshot nvarchar(250) NOT NULL,
        StaffNumberSnapshot nvarchar(60) NOT NULL,
        PositionTitleSnapshot nvarchar(250) NOT NULL,
        DepartmentNameSnapshot nvarchar(250) NOT NULL,
        OrganisationNameSnapshot nvarchar(250) NOT NULL,
        OperationLogId int NULL,
        CreatedAtUtc datetime2 NOT NULL,
        UpdatedAtUtc datetime2 NOT NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_AM_AssetIssuances PRIMARY KEY (AssetIssuanceId),
        CONSTRAINT CK_AM_AssetIssuances_Status CHECK ([Status] IN ('Draft','AwaitingHandover','PendingStaffAcknowledgement','PendingHRApproval','ReturnedForCorrection','Completed','PartiallyCompleted','Cancelled')),
        CONSTRAINT FK_AM_AssetIssuances_Requests_RequestId FOREIGN KEY (RequestId) REFERENCES dbo.Requests (RequestID),
        CONSTRAINT FK_AM_AssetIssuances_BE_Individuals_IndividualId FOREIGN KEY (IndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID),
        CONSTRAINT FK_AM_AssetIssuances_Jobs_JobId FOREIGN KEY (JobId) REFERENCES dbo.Jobs (JobID),
        CONSTRAINT FK_AM_AssetIssuances_Organisations_OrganisationId FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_AM_AssetIssuances_OrganisationStructure_StructureId FOREIGN KEY (StructureId) REFERENCES dbo.OrganisationStructure (OrganisationStructureID),
        CONSTRAINT FK_AM_AssetIssuances_Users_CreatedByUserId FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_AM_AssetIssuances_Users_FinalizedByUserId FOREIGN KEY (FinalizedByUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_AM_AssetIssuances_OperationLogs_OperationLogId FOREIGN KEY (OperationLogId) REFERENCES dbo.OperationLogs (OperationLogID)
    );

    CREATE UNIQUE INDEX IX_AM_AssetIssuances_ReferenceNumber ON dbo.AM_AssetIssuances (ReferenceNumber);
    CREATE UNIQUE INDEX IX_AM_AssetIssuances_RequestId ON dbo.AM_AssetIssuances (RequestId) WHERE [RequestId] IS NOT NULL;
    CREATE INDEX IX_AM_AssetIssuances_Staff_Status ON dbo.AM_AssetIssuances (IndividualId, Status);
    CREATE INDEX IX_AM_AssetIssuances_OrganisationId_Status_IssueDate ON dbo.AM_AssetIssuances (OrganisationId, Status, IssueDate);
END;

IF OBJECT_ID(N'dbo.AM_AssetIssuanceItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetIssuanceItems
    (
        AssetIssuanceItemId int IDENTITY(1,1) NOT NULL,
        IssuanceId int NOT NULL,
        AssetItemId int NOT NULL,
        ConditionAtIssueId int NOT NULL,
        Accessories nvarchar(500) NULL,
        HandedOverAtUtc datetime2 NULL,
        HandedOverByUserId int NULL,
        HandoverNotes nvarchar(1000) NULL,
        Status nvarchar(40) NOT NULL,
        IsActiveReservation bit NOT NULL,
        ReplacedFromIssuanceItemId int NULL,
        Version int NOT NULL,
        ExpectedReturnDate date NULL,
        AssetCodeSnapshot nvarchar(100) NOT NULL,
        SerialNumberSnapshot nvarchar(120) NULL,
        CategoryNameSnapshot nvarchar(150) NOT NULL,
        ItemNameSnapshot nvarchar(200) NOT NULL,
        BrandSnapshot nvarchar(120) NULL,
        ModelSnapshot nvarchar(120) NULL,
        ConditionNameSnapshot nvarchar(100) NOT NULL,
        IsSecurityItemSnapshot bit NOT NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_AM_AssetIssuanceItems PRIMARY KEY (AssetIssuanceItemId),
        CONSTRAINT CK_AM_AssetIssuanceItems_Status CHECK ([Status] IN ('Proposed','Reserved','HandedOver','Accepted','Rejected','HRApproved','ReturnedForCorrection','Closed')),
        CONSTRAINT CK_AM_AssetIssuanceItems_Version CHECK ([Version] >= 1),
        CONSTRAINT FK_AM_AssetIssuanceItems_AM_AssetIssuances_IssuanceId FOREIGN KEY (IssuanceId) REFERENCES dbo.AM_AssetIssuances (AssetIssuanceId),
        CONSTRAINT FK_AM_AssetIssuanceItems_AM_AssetItems_AssetItemId FOREIGN KEY (AssetItemId) REFERENCES dbo.AM_AssetItems (AssetItemId),
        CONSTRAINT FK_AM_AssetIssuanceItems_AM_AssetConditions_ConditionAtIssueId FOREIGN KEY (ConditionAtIssueId) REFERENCES dbo.AM_AssetConditions (AssetConditionId),
        CONSTRAINT FK_AM_AssetIssuanceItems_Users_HandedOverByUserId FOREIGN KEY (HandedOverByUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_AM_AssetIssuanceItems_AM_AssetIssuanceItems_ReplacedFromIssuanceItemId FOREIGN KEY (ReplacedFromIssuanceItemId) REFERENCES dbo.AM_AssetIssuanceItems (AssetIssuanceItemId)
    );

    CREATE INDEX IX_AM_AssetIssuanceItems_Asset ON dbo.AM_AssetIssuanceItems (AssetItemId);
    CREATE UNIQUE INDEX UX_AM_AssetIssuanceItems_ActiveReservation ON dbo.AM_AssetIssuanceItems (AssetItemId) WHERE [IsActiveReservation] = 1;
    CREATE INDEX IX_AM_AssetIssuanceItems_IssuanceId_Status ON dbo.AM_AssetIssuanceItems (IssuanceId, Status);
END;

IF OBJECT_ID(N'dbo.AM_AssetAcknowledgements', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetAcknowledgements
    (
        AcknowledgementId int IDENTITY(1,1) NOT NULL,
        IssuanceItemId int NOT NULL,
        IndividualId int NOT NULL,
        Decision nvarchar(20) NOT NULL,
        RejectionReasonCode nvarchar(50) NULL,
        Remarks nvarchar(1000) NULL,
        AcknowledgementStatement nvarchar(1000) NOT NULL,
        EvidenceAttachmentId int NULL,
        ConfirmedByUserId int NOT NULL,
        ConfirmedAtUtc datetime2 NOT NULL,
        ItemVersion int NOT NULL,
        CONSTRAINT PK_AM_AssetAcknowledgements PRIMARY KEY (AcknowledgementId),
        CONSTRAINT CK_AM_AssetAcknowledgements_Decision CHECK ([Decision] IN ('Accepted','Rejected') AND ([Decision] = 'Accepted' OR ([Remarks] IS NOT NULL AND LEN(LTRIM(RTRIM([Remarks]))) > 0))),
        CONSTRAINT FK_AM_AssetAcknowledgements_AM_AssetIssuanceItems_IssuanceItemId FOREIGN KEY (IssuanceItemId) REFERENCES dbo.AM_AssetIssuanceItems (AssetIssuanceItemId),
        CONSTRAINT FK_AM_AssetAcknowledgements_BE_Individuals_IndividualId FOREIGN KEY (IndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID),
        CONSTRAINT FK_AM_AssetAcknowledgements_Users_ConfirmedByUserId FOREIGN KEY (ConfirmedByUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE INDEX IX_AM_AssetAcknowledgements_IssuanceItemId_ItemVersion ON dbo.AM_AssetAcknowledgements (IssuanceItemId, ItemVersion);
    CREATE INDEX IX_AM_AssetAcknowledgements_IndividualId_ConfirmedAtUtc ON dbo.AM_AssetAcknowledgements (IndividualId, ConfirmedAtUtc);
END;

IF OBJECT_ID(N'dbo.AM_AssetHrDecisions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetHrDecisions
    (
        HrDecisionId int IDENTITY(1,1) NOT NULL,
        IssuanceItemId int NOT NULL,
        Decision nvarchar(30) NOT NULL,
        Comments nvarchar(1000) NULL,
        DecidedByUserId int NOT NULL,
        DecidedAtUtc datetime2 NOT NULL,
        ItemVersion int NOT NULL,
        IdempotencyKey nvarchar(80) NOT NULL,
        CONSTRAINT PK_AM_AssetHrDecisions PRIMARY KEY (HrDecisionId),
        CONSTRAINT CK_AM_AssetHrDecisions_Decision CHECK ([Decision] IN ('HRApproved','ReturnedForCorrection','Closed')),
        CONSTRAINT FK_AM_AssetHrDecisions_AM_AssetIssuanceItems_IssuanceItemId FOREIGN KEY (IssuanceItemId) REFERENCES dbo.AM_AssetIssuanceItems (AssetIssuanceItemId),
        CONSTRAINT FK_AM_AssetHrDecisions_Users_DecidedByUserId FOREIGN KEY (DecidedByUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE UNIQUE INDEX IX_AM_AssetHrDecisions_IssuanceItemId_ItemVersion_IdempotencyKey ON dbo.AM_AssetHrDecisions (IssuanceItemId, ItemVersion, IdempotencyKey);
END;

IF OBJECT_ID(N'dbo.AM_AssetCustodies', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetCustodies
    (
        AssetCustodyId int IDENTITY(1,1) NOT NULL,
        AssetItemId int NOT NULL,
        IndividualId int NOT NULL,
        JobId int NOT NULL,
        OrganisationId int NOT NULL,
        StructureId int NULL,
        IssuanceItemId int NOT NULL,
        CustodyFromUtc datetime2 NOT NULL,
        CustodyToUtc datetime2 NULL,
        ExpectedReturnDate date NULL,
        Status nvarchar(30) NOT NULL,
        ClosedReason nvarchar(500) NULL,
        ClosedByUserId int NULL,
        IdempotencyKey nvarchar(80) NOT NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_AM_AssetCustodies PRIMARY KEY (AssetCustodyId),
        CONSTRAINT CK_AM_AssetCustodies_Status CHECK ([Status] IN ('Active','ReturnPending','Returned','Lost','Transferred','Closed')),
        CONSTRAINT CK_AM_AssetCustodies_Dates CHECK ([CustodyToUtc] IS NULL OR [CustodyToUtc] >= [CustodyFromUtc]),
        CONSTRAINT FK_AM_AssetCustodies_AM_AssetItems_AssetItemId FOREIGN KEY (AssetItemId) REFERENCES dbo.AM_AssetItems (AssetItemId),
        CONSTRAINT FK_AM_AssetCustodies_BE_Individuals_IndividualId FOREIGN KEY (IndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID),
        CONSTRAINT FK_AM_AssetCustodies_Jobs_JobId FOREIGN KEY (JobId) REFERENCES dbo.Jobs (JobID),
        CONSTRAINT FK_AM_AssetCustodies_Organisations_OrganisationId FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_AM_AssetCustodies_OrganisationStructure_StructureId FOREIGN KEY (StructureId) REFERENCES dbo.OrganisationStructure (OrganisationStructureID),
        CONSTRAINT FK_AM_AssetCustodies_AM_AssetIssuanceItems_IssuanceItemId FOREIGN KEY (IssuanceItemId) REFERENCES dbo.AM_AssetIssuanceItems (AssetIssuanceItemId),
        CONSTRAINT FK_AM_AssetCustodies_Users_ClosedByUserId FOREIGN KEY (ClosedByUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE UNIQUE INDEX UX_AM_AssetCustodies_Active ON dbo.AM_AssetCustodies (AssetItemId) WHERE [CustodyToUtc] IS NULL;
    CREATE UNIQUE INDEX UX_AM_AssetCustodies_IssuanceItem ON dbo.AM_AssetCustodies (IssuanceItemId);
    CREATE UNIQUE INDEX UX_AM_AssetCustodies_Idempotency ON dbo.AM_AssetCustodies (IdempotencyKey);
    CREATE INDEX IX_AM_AssetCustodies_IndividualId_Status ON dbo.AM_AssetCustodies (IndividualId, Status);
    CREATE INDEX IX_AM_AssetCustodies_OrganisationId_Status ON dbo.AM_AssetCustodies (OrganisationId, Status);
END;

IF OBJECT_ID(N'dbo.AM_AssetReturns', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetReturns
    (
        AssetReturnId int IDENTITY(1,1) NOT NULL,
        ReferenceNumber nvarchar(60) NOT NULL,
        OrganisationId int NOT NULL,
        IndividualId int NOT NULL,
        JobId int NULL,
        StaffTransferId int NULL,
        ReturnType nvarchar(30) NOT NULL,
        RequestedByUserId int NOT NULL,
        ReturnedToUserId int NULL,
        ReturnDate datetime2 NOT NULL,
        CompletedAtUtc datetime2 NULL,
        Status nvarchar(30) NOT NULL,
        Notes nvarchar(1000) NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_AM_AssetReturns PRIMARY KEY (AssetReturnId),
        CONSTRAINT CK_AM_AssetReturns_Status CHECK ([Status] IN ('ReturnPending','Inspected','Closed','Cancelled') AND [ReturnType] IN ('StandardReturn','DamageReport','LossReport','TransferReturn','TerminationClearance')),
        CONSTRAINT FK_AM_AssetReturns_Organisations_OrganisationId FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_AM_AssetReturns_BE_Individuals_IndividualId FOREIGN KEY (IndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID),
        CONSTRAINT FK_AM_AssetReturns_Jobs_JobId FOREIGN KEY (JobId) REFERENCES dbo.Jobs (JobID),
        CONSTRAINT FK_AM_AssetReturns_ST_StaffTransfers_StaffTransferId FOREIGN KEY (StaffTransferId) REFERENCES dbo.ST_StaffTransfers (StaffTransferId),
        CONSTRAINT FK_AM_AssetReturns_Users_RequestedByUserId FOREIGN KEY (RequestedByUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_AM_AssetReturns_Users_ReturnedToUserId FOREIGN KEY (ReturnedToUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE UNIQUE INDEX IX_AM_AssetReturns_ReferenceNumber ON dbo.AM_AssetReturns (ReferenceNumber);
    CREATE INDEX IX_AM_AssetReturns_OrganisationId_Status_ReturnDate ON dbo.AM_AssetReturns (OrganisationId, Status, ReturnDate);
    CREATE INDEX IX_AM_AssetReturns_IndividualId_Status ON dbo.AM_AssetReturns (IndividualId, Status);
END;

IF OBJECT_ID(N'dbo.AM_AssetReturnItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetReturnItems
    (
        AssetReturnItemId int IDENTITY(1,1) NOT NULL,
        AssetReturnId int NOT NULL,
        AssetCustodyId int NOT NULL,
        AssetItemId int NOT NULL,
        ConditionAtReturnId int NULL,
        AccessoryChecklist nvarchar(500) NULL,
        MissingAccessories nvarchar(500) NULL,
        DamageOrLossEvidence nvarchar(1000) NULL,
        InspectionResult nvarchar(40) NOT NULL,
        PostReturnDisposition nvarchar(30) NULL,
        ReceivedByUserId int NULL,
        ReceivedAtUtc datetime2 NULL,
        Status nvarchar(30) NOT NULL,
        CONSTRAINT PK_AM_AssetReturnItems PRIMARY KEY (AssetReturnItemId),
        CONSTRAINT CK_AM_AssetReturnItems_Status CHECK ([Status] IN ('ReturnPending','Inspected','Closed') AND [InspectionResult] IN ('Pending','GoodAvailable','DamagedMaintenance','LostConfirmed','Disposed','ReplacementRequired')),
        CONSTRAINT FK_AM_AssetReturnItems_AM_AssetReturns_AssetReturnId FOREIGN KEY (AssetReturnId) REFERENCES dbo.AM_AssetReturns (AssetReturnId),
        CONSTRAINT FK_AM_AssetReturnItems_AM_AssetCustodies_AssetCustodyId FOREIGN KEY (AssetCustodyId) REFERENCES dbo.AM_AssetCustodies (AssetCustodyId),
        CONSTRAINT FK_AM_AssetReturnItems_AM_AssetItems_AssetItemId FOREIGN KEY (AssetItemId) REFERENCES dbo.AM_AssetItems (AssetItemId),
        CONSTRAINT FK_AM_AssetReturnItems_AM_AssetConditions_ConditionAtReturnId FOREIGN KEY (ConditionAtReturnId) REFERENCES dbo.AM_AssetConditions (AssetConditionId),
        CONSTRAINT FK_AM_AssetReturnItems_Users_ReceivedByUserId FOREIGN KEY (ReceivedByUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE UNIQUE INDEX IX_AM_AssetReturnItems_AssetReturnId_AssetCustodyId ON dbo.AM_AssetReturnItems (AssetReturnId, AssetCustodyId);
    CREATE INDEX IX_AM_AssetReturnItems_AssetCustodyId_Status ON dbo.AM_AssetReturnItems (AssetCustodyId, Status);
END;

IF OBJECT_ID(N'dbo.AM_AssetTransferDispositions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetTransferDispositions
    (
        AssetTransferDispositionId int IDENTITY(1,1) NOT NULL,
        StaffTransferId int NULL,
        AssetCustodyId int NOT NULL,
        AssetItemId int NOT NULL,
        IndividualId int NOT NULL,
        FromJobId int NOT NULL,
        ToJobId int NULL,
        FromOrganisationId int NOT NULL,
        ToOrganisationId int NOT NULL,
        FromStructureId int NULL,
        ToStructureId int NULL,
        Decision nvarchar(30) NOT NULL,
        Status nvarchar(30) NOT NULL,
        DecidedByUserId int NOT NULL,
        ApprovedByUserId int NULL,
        DecidedAtUtc datetime2 NOT NULL,
        ExecutedAtUtc datetime2 NULL,
        Remarks nvarchar(1000) NULL,
        CONSTRAINT PK_AM_AssetTransferDispositions PRIMARY KEY (AssetTransferDispositionId),
        CONSTRAINT CK_AM_AssetTransferDispositions_Decision CHECK ([Decision] IN ('Retain','Return','ChangeOwningDepartment','Replace') AND [Status] IN ('Pending','Approved','Executed','Cancelled')),
        CONSTRAINT FK_AM_AssetTransferDispositions_ST_StaffTransfers_StaffTransferId FOREIGN KEY (StaffTransferId) REFERENCES dbo.ST_StaffTransfers (StaffTransferId),
        CONSTRAINT FK_AM_AssetTransferDispositions_AM_AssetCustodies_AssetCustodyId FOREIGN KEY (AssetCustodyId) REFERENCES dbo.AM_AssetCustodies (AssetCustodyId),
        CONSTRAINT FK_AM_AssetTransferDispositions_AM_AssetItems_AssetItemId FOREIGN KEY (AssetItemId) REFERENCES dbo.AM_AssetItems (AssetItemId),
        CONSTRAINT FK_AM_AssetTransferDispositions_BE_Individuals_IndividualId FOREIGN KEY (IndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID),
        CONSTRAINT FK_AM_AssetTransferDispositions_Jobs_FromJobId FOREIGN KEY (FromJobId) REFERENCES dbo.Jobs (JobID),
        CONSTRAINT FK_AM_AssetTransferDispositions_Jobs_ToJobId FOREIGN KEY (ToJobId) REFERENCES dbo.Jobs (JobID),
        CONSTRAINT FK_AM_AssetTransferDispositions_Organisations_FromOrganisationId FOREIGN KEY (FromOrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_AM_AssetTransferDispositions_Organisations_ToOrganisationId FOREIGN KEY (ToOrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_AM_AssetTransferDispositions_OrganisationStructure_FromStructureId FOREIGN KEY (FromStructureId) REFERENCES dbo.OrganisationStructure (OrganisationStructureID),
        CONSTRAINT FK_AM_AssetTransferDispositions_OrganisationStructure_ToStructureId FOREIGN KEY (ToStructureId) REFERENCES dbo.OrganisationStructure (OrganisationStructureID),
        CONSTRAINT FK_AM_AssetTransferDispositions_Users_DecidedByUserId FOREIGN KEY (DecidedByUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_AM_AssetTransferDispositions_Users_ApprovedByUserId FOREIGN KEY (ApprovedByUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE INDEX IX_AM_AssetTransferDispositions_StaffTransferId_AssetCustodyId ON dbo.AM_AssetTransferDispositions (StaffTransferId, AssetCustodyId);
    CREATE INDEX IX_AM_AssetTransferDispositions_IndividualId_Status ON dbo.AM_AssetTransferDispositions (IndividualId, Status);
END;

IF OBJECT_ID(N'dbo.AM_AssetEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetEvents
    (
        AssetEventId bigint IDENTITY(1,1) NOT NULL,
        OrganisationId int NOT NULL,
        AssetItemId int NULL,
        RelatedEntityType nvarchar(50) NOT NULL,
        RelatedEntityId int NOT NULL,
        EventType nvarchar(50) NOT NULL,
        FromStatus nvarchar(40) NULL,
        ToStatus nvarchar(40) NULL,
        ActorUserId int NOT NULL,
        AtUtc datetime2 NOT NULL,
        MetadataJson nvarchar(4000) NULL,
        CONSTRAINT PK_AM_AssetEvents PRIMARY KEY (AssetEventId),
        CONSTRAINT FK_AM_AssetEvents_Organisations_OrganisationId FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_AM_AssetEvents_AM_AssetItems_AssetItemId FOREIGN KEY (AssetItemId) REFERENCES dbo.AM_AssetItems (AssetItemId),
        CONSTRAINT FK_AM_AssetEvents_Users_ActorUserId FOREIGN KEY (ActorUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE INDEX IX_AM_AssetEvents_AssetItemId_AtUtc ON dbo.AM_AssetEvents (AssetItemId, AtUtc);
    CREATE INDEX IX_AM_AssetEvents_RelatedEntityType_RelatedEntityId_AtUtc ON dbo.AM_AssetEvents (RelatedEntityType, RelatedEntityId, AtUtc);
    CREATE INDEX IX_AM_AssetEvents_OrganisationId_AtUtc ON dbo.AM_AssetEvents (OrganisationId, AtUtc);
END;

IF OBJECT_ID(N'dbo.AM_AssetAttachments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetAttachments
    (
        AttachmentId int IDENTITY(1,1) NOT NULL,
        OrganisationId int NOT NULL,
        EntityType nvarchar(50) NOT NULL,
        EntityId int NOT NULL,
        StorageKey nvarchar(200) NOT NULL,
        FileName nvarchar(255) NOT NULL,
        ContentType nvarchar(100) NOT NULL,
        FileSizeBytes bigint NOT NULL,
        Sha256 nvarchar(64) NOT NULL,
        FileBytes varbinary(max) NULL,
        UploadedByUserId int NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        AccessScope nvarchar(30) NOT NULL,
        CONSTRAINT PK_AM_AssetAttachments PRIMARY KEY (AttachmentId),
        CONSTRAINT CK_AM_AssetAttachments_Scope CHECK ([AccessScope] IN ('EmployeeAndHr','HrOnly','RestrictedSecurity')),
        CONSTRAINT FK_AM_AssetAttachments_Organisations_OrganisationId FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_AM_AssetAttachments_Users_UploadedByUserId FOREIGN KEY (UploadedByUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE UNIQUE INDEX IX_AM_AssetAttachments_StorageKey ON dbo.AM_AssetAttachments (StorageKey);
    CREATE INDEX IX_AM_AssetAttachments_EntityType_EntityId ON dbo.AM_AssetAttachments (EntityType, EntityId);
END;

IF OBJECT_ID(N'dbo.AM_AssetDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetDocuments
    (
        AssetDocumentId bigint IDENTITY(1,1) NOT NULL,
        AssetIssuanceId int NOT NULL,
        DocumentNumber nvarchar(60) NOT NULL,
        DocumentVariant nvarchar(30) NOT NULL,
        DocumentVersion int NOT NULL,
        SnapshotJson nvarchar(max) NOT NULL,
        Sha256Hash nvarchar(64) NOT NULL,
        PdfBytes varbinary(max) NULL,
        ContentType nvarchar(60) NOT NULL,
        GeneratedByUserId int NOT NULL,
        GeneratedAtUtc datetime2 NOT NULL,
        CONSTRAINT PK_AM_AssetDocuments PRIMARY KEY (AssetDocumentId),
        CONSTRAINT CK_AM_AssetDocuments_Variant CHECK ([DocumentVariant] IN ('StandardHandoverRecord','RestrictedSecurityRecord')),
        CONSTRAINT FK_AM_AssetDocuments_AM_AssetIssuances_AssetIssuanceId FOREIGN KEY (AssetIssuanceId) REFERENCES dbo.AM_AssetIssuances (AssetIssuanceId),
        CONSTRAINT FK_AM_AssetDocuments_Users_GeneratedByUserId FOREIGN KEY (GeneratedByUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE UNIQUE INDEX IX_AM_AssetDocuments_DocumentNumber ON dbo.AM_AssetDocuments (DocumentNumber);
    CREATE INDEX IX_AM_AssetDocuments_AssetIssuanceId_DocumentVariant_DocumentVersion ON dbo.AM_AssetDocuments (AssetIssuanceId, DocumentVariant, DocumentVersion);
END;

-- Seed default conditions
IF NOT EXISTS (SELECT 1 FROM dbo.AM_AssetConditions WHERE Code = N'NEW')
    INSERT INTO dbo.AM_AssetConditions (Code, Name, Description, RequiresMaintenanceOnReturn, SortOrder, IsActive)
    VALUES
        (N'NEW', N'New', N'Brand new, unused equipment in original factory condition.', 0, 1, 1),
        (N'GOOD', N'Good', N'Fully functional equipment in good working order.', 0, 2, 1),
        (N'FAIR', N'Fair', N'Functional with minor cosmetic wear.', 0, 3, 1),
        (N'DAMAGED', N'Damaged', N'Damaged or faulty; requires maintenance or repair.', 1, 4, 1),
        (N'UNSERVICEABLE', N'Unserviceable', N'Non-functional beyond normal use; candidate for repair or disposal.', 1, 5, 1);

-- Seed default categories
IF NOT EXISTS (SELECT 1 FROM dbo.AM_AssetCategories WHERE OrganisationId IS NULL AND Code = N'LAPTOP')
    INSERT INTO dbo.AM_AssetCategories (OrganisationId, Code, Name, Description, RequiresSerialNumber, RequiresReturn, IsSecurityItem, IsActive, CreatedAtUtc)
    VALUES
        (NULL, N'LAPTOP', N'Laptops & Notebooks', N'Portable computers issued to staff.', 1, 1, 0, 1, SYSUTCDATETIME()),
        (NULL, N'MONITOR', N'Displays & Monitors', N'External monitors and docking displays.', 1, 1, 0, 1, SYSUTCDATETIME()),
        (NULL, N'PHONE', N'Mobile Phones & Tablets', N'Official handsets, SIM-enabled tablets and communication devices.', 1, 1, 0, 1, SYSUTCDATETIME()),
        (NULL, N'ACCESS_CARD', N'Security & Access Cards', N'Smart cards, building badges, and cryptographic tokens.', 1, 1, 1, 1, SYSUTCDATETIME()),
        (NULL, N'EQUIPMENT', N'Specialized Office & Field Equipment', N'Peripherals, radios, cameras, and technical instruments.', 1, 1, 0, 1, SYSUTCDATETIME());

-- Seed RequestType for workflow integration
IF NOT EXISTS(SELECT 1 FROM dbo.RequestTypes WHERE TypeName = N'Staff Asset Issuance Request')
BEGIN
    INSERT dbo.SequenceNumberTypes(TypeName,Prefix,FormatString,IsResetAnnually,StartingNumber,NextNumber)
    VALUES(N'Staff Asset Issuances',N'SAI',N'{PREFIX}-{YEAR}-{NUMBER}',0,1,1);
    DECLARE @AssetSequence int = CONVERT(int, SCOPE_IDENTITY());
    INSERT dbo.RequestTypes(TypeName,SequenceNumberTypeID,ViewURL,IsSystemType)
    VALUES(N'Staff Asset Issuance Request',@AssetSequence,N'/admin/hr/items/asset-custody',1);
END;

INSERT dbo.RequestTypesAllowedRequestStateTransitions(RequestTypeID,FromRequestStateID,ToRequestStateID)
SELECT t.RequestTypeID, f.RequestStateID, s.RequestStateID
FROM dbo.RequestTypes t CROSS JOIN dbo.RequestStates f CROSS JOIN dbo.RequestStates s
WHERE t.TypeName = N'Staff Asset Issuance Request'
  AND (f.RequestStateID=1 OR f.IsProcessingState=1 AND f.IsFinalState=0)
  AND (s.IsProcessingState=1 AND s.IsFinalState=0 OR s.RequestStateID IN (4,5,6))
  AND f.RequestStateID<>s.RequestStateID
  AND NOT EXISTS(SELECT 1 FROM dbo.RequestTypesAllowedRequestStateTransitions x
      WHERE x.RequestTypeID=t.RequestTypeID AND x.FromRequestStateID=f.RequestStateID AND x.ToRequestStateID=s.RequestStateID);

IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20261103014000_StaffAssetManagement')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20261103014000_StaffAssetManagement', N'8.0.27');
END;

COMMIT TRANSACTION;

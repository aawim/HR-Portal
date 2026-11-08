-- Asset Return, Maintenance, Upgrade and Replacement Management (AM_* lifecycle tables)
-- Idempotent deployment script matching EF Core migration 20261103093000_AssetLifecycleManagement.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- 1. Service Requests
IF OBJECT_ID(N'dbo.AM_AssetServiceRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetServiceRequests
    (
        AssetServiceRequestId int IDENTITY(1,1) NOT NULL,
        RequestId int NULL,
        ReferenceNumber nvarchar(60) NOT NULL,
        IndividualId int NOT NULL,
        JobId int NOT NULL,
        OrganisationId int NOT NULL,
        StructureId int NULL,
        StaffTransferId int NULL,
        RequestType nvarchar(40) NOT NULL,
        Priority nvarchar(20) NOT NULL CONSTRAINT DF_AM_AssetServiceRequests_Priority DEFAULT ('Standard'),
        Title nvarchar(250) NOT NULL,
        Description nvarchar(max) NOT NULL,
        Status nvarchar(40) NOT NULL,
        RequestedByUserId int NOT NULL,
        AssignedTechnicianUserId int NULL,
        EstimatedCost decimal(18,2) NULL,
        ActualCost decimal(18,2) NULL,
        SubmittedAtUtc datetime2 NOT NULL,
        CompletedAtUtc datetime2 NULL,
        ResolutionSummary nvarchar(1000) NULL,
        OperationLogId int NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_AM_AssetServiceRequests PRIMARY KEY (AssetServiceRequestId),
        CONSTRAINT FK_AM_AssetServiceRequests_Requests_RequestId FOREIGN KEY (RequestId) REFERENCES dbo.Requests (RequestID),
        CONSTRAINT FK_AM_AssetServiceRequests_Individuals_IndividualId FOREIGN KEY (IndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID),
        CONSTRAINT FK_AM_AssetServiceRequests_Jobs_JobId FOREIGN KEY (JobId) REFERENCES dbo.Jobs (JobID),
        CONSTRAINT FK_AM_AssetServiceRequests_Organisations_OrganisationId FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_AM_AssetServiceRequests_OrganisationStructures_StructureId FOREIGN KEY (StructureId) REFERENCES dbo.OrganisationStructures (OrganisationStructureID),
        CONSTRAINT FK_AM_AssetServiceRequests_ST_StaffTransfers_StaffTransferId FOREIGN KEY (StaffTransferId) REFERENCES dbo.ST_StaffTransfers (StaffTransferId),
        CONSTRAINT FK_AM_AssetServiceRequests_Users_RequestedByUserId FOREIGN KEY (RequestedByUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_AM_AssetServiceRequests_Users_AssignedTechnicianUserId FOREIGN KEY (AssignedTechnicianUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_AM_AssetServiceRequests_OperationLogs_OperationLogId FOREIGN KEY (OperationLogId) REFERENCES dbo.OperationLogs (OperationLogID)
    );

    CREATE UNIQUE INDEX UX_AM_AssetServiceRequests_ReferenceNumber ON dbo.AM_AssetServiceRequests (ReferenceNumber);
    CREATE INDEX IX_AM_AssetServiceRequests_OrganisationId_Status_RequestType ON dbo.AM_AssetServiceRequests (OrganisationId, Status, RequestType);
    CREATE INDEX IX_AM_AssetServiceRequests_IndividualId_JobId ON dbo.AM_AssetServiceRequests (IndividualId, JobId);
END;

-- 2. Service Request Items
IF OBJECT_ID(N'dbo.AM_AssetServiceRequestItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetServiceRequestItems
    (
        AssetServiceRequestItemId int IDENTITY(1,1) NOT NULL,
        AssetServiceRequestId int NOT NULL,
        AssetCustodyId int NULL,
        AssetItemId int NOT NULL,
        ItemRequestType nvarchar(40) NOT NULL,
        ReportedFaultOrRequirement nvarchar(1000) NOT NULL,
        TargetComponentCategory nvarchar(60) NULL,
        TargetSpecification nvarchar(500) NULL,
        Status nvarchar(40) NOT NULL,
        StaffAcknowledged bit NOT NULL CONSTRAINT DF_AM_AssetServiceRequestItems_StaffAcknowledged DEFAULT (0),
        StaffDecision nvarchar(30) NULL,
        StaffRemarks nvarchar(1000) NULL,
        StaffConfirmedAtUtc datetime2 NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_AM_AssetServiceRequestItems PRIMARY KEY (AssetServiceRequestItemId),
        CONSTRAINT FK_AM_AssetServiceRequestItems_AssetServiceRequests_AssetServiceRequestId FOREIGN KEY (AssetServiceRequestId) REFERENCES dbo.AM_AssetServiceRequests (AssetServiceRequestId),
        CONSTRAINT FK_AM_AssetServiceRequestItems_AM_AssetItems_AssetItemId FOREIGN KEY (AssetItemId) REFERENCES dbo.AM_AssetItems (AssetItemId),
        CONSTRAINT FK_AM_AssetServiceRequestItems_AM_AssetCustodies_AssetCustodyId FOREIGN KEY (AssetCustodyId) REFERENCES dbo.AM_AssetCustodies (AssetCustodyId)
    );

    CREATE INDEX IX_AM_AssetServiceRequestItems_AssetServiceRequestId_AssetItemId ON dbo.AM_AssetServiceRequestItems (AssetServiceRequestId, AssetItemId);
    CREATE INDEX IX_AM_AssetServiceRequestItems_AssetCustodyId ON dbo.AM_AssetServiceRequestItems (AssetCustodyId);
END;

-- 3. Maintenance Jobs (Work Orders)
IF OBJECT_ID(N'dbo.AM_AssetMaintenanceJobs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetMaintenanceJobs
    (
        AssetMaintenanceJobId int IDENTITY(1,1) NOT NULL,
        WorkOrderNumber nvarchar(60) NOT NULL,
        AssetServiceRequestId int NULL,
        AssetItemId int NOT NULL,
        JobType nvarchar(40) NOT NULL,
        Priority nvarchar(20) NOT NULL CONSTRAINT DF_AM_AssetMaintenanceJobs_Priority DEFAULT ('Standard'),
        AssignedTechnicianUserId int NOT NULL,
        Status nvarchar(40) NOT NULL,
        StartedAtUtc datetime2 NULL,
        CompletedAtUtc datetime2 NULL,
        DiagnosticSummary nvarchar(max) NULL,
        WorkPerformedSummary nvarchar(max) NULL,
        HardwareTestSummary nvarchar(max) NULL,
        DataSanitizationMethod nvarchar(100) NULL,
        DataSanitizationVerified bit NOT NULL CONSTRAINT DF_AM_AssetMaintenanceJobs_DataSanitizationVerified DEFAULT (0),
        VendorWarrantyClaimNumber nvarchar(100) NULL,
        PartsCost decimal(18,2) NOT NULL CONSTRAINT DF_AM_AssetMaintenanceJobs_PartsCost DEFAULT (0),
        LabourCost decimal(18,2) NOT NULL CONSTRAINT DF_AM_AssetMaintenanceJobs_LabourCost DEFAULT (0),
        TotalCost decimal(18,2) NOT NULL CONSTRAINT DF_AM_AssetMaintenanceJobs_TotalCost DEFAULT (0),
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_AM_AssetMaintenanceJobs PRIMARY KEY (AssetMaintenanceJobId),
        CONSTRAINT FK_AM_AssetMaintenanceJobs_AM_AssetServiceRequests_AssetServiceRequestId FOREIGN KEY (AssetServiceRequestId) REFERENCES dbo.AM_AssetServiceRequests (AssetServiceRequestId),
        CONSTRAINT FK_AM_AssetMaintenanceJobs_AM_AssetItems_AssetItemId FOREIGN KEY (AssetItemId) REFERENCES dbo.AM_AssetItems (AssetItemId),
        CONSTRAINT FK_AM_AssetMaintenanceJobs_Users_AssignedTechnicianUserId FOREIGN KEY (AssignedTechnicianUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE UNIQUE INDEX UX_AM_AssetMaintenanceJobs_WorkOrderNumber ON dbo.AM_AssetMaintenanceJobs (WorkOrderNumber);
    CREATE INDEX IX_AM_AssetMaintenanceJobs_AssetItemId_Status ON dbo.AM_AssetMaintenanceJobs (AssetItemId, Status);
    CREATE INDEX IX_AM_AssetMaintenanceJobs_AssignedTechnicianUserId_Status ON dbo.AM_AssetMaintenanceJobs (AssignedTechnicianUserId, Status);
END;

-- 4. Maintenance Actions
IF OBJECT_ID(N'dbo.AM_AssetMaintenanceActions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetMaintenanceActions
    (
        AssetMaintenanceActionId bigint IDENTITY(1,1) NOT NULL,
        AssetMaintenanceJobId int NOT NULL,
        ActionType nvarchar(50) NOT NULL,
        TechnicianUserId int NOT NULL,
        ActionAtUtc datetime2 NOT NULL,
        DurationMinutes int NOT NULL CONSTRAINT DF_AM_AssetMaintenanceActions_DurationMinutes DEFAULT (0),
        Remarks nvarchar(1000) NOT NULL,
        MetadataJson nvarchar(max) NULL,
        CONSTRAINT PK_AM_AssetMaintenanceActions PRIMARY KEY (AssetMaintenanceActionId),
        CONSTRAINT FK_AM_AssetMaintenanceActions_AM_AssetMaintenanceJobs_AssetMaintenanceJobId FOREIGN KEY (AssetMaintenanceJobId) REFERENCES dbo.AM_AssetMaintenanceJobs (AssetMaintenanceJobId),
        CONSTRAINT FK_AM_AssetMaintenanceActions_Users_TechnicianUserId FOREIGN KEY (TechnicianUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE INDEX IX_AM_AssetMaintenanceActions_AssetMaintenanceJobId_ActionAtUtc ON dbo.AM_AssetMaintenanceActions (AssetMaintenanceJobId, ActionAtUtc);
END;

-- 5. Asset Components (Modular hardware: RAM, SSD, Battery, etc.)
IF OBJECT_ID(N'dbo.AM_AssetComponents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetComponents
    (
        AssetComponentId int IDENTITY(1,1) NOT NULL,
        OwnerOrganisationId int NOT NULL,
        AssetItemId int NULL,
        ComponentType nvarchar(40) NOT NULL,
        Brand nvarchar(100) NULL,
        Model nvarchar(100) NULL,
        PartNumber nvarchar(100) NULL,
        SerialNumber nvarchar(120) NULL,
        Specification nvarchar(250) NOT NULL,
        CapacityOrRating nvarchar(100) NULL,
        SlotOrPortLocation nvarchar(60) NULL,
        Status nvarchar(30) NOT NULL,
        Cost decimal(18,2) NULL,
        InstalledAtUtc datetime2 NULL,
        RemovedAtUtc datetime2 NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_AM_AssetComponents PRIMARY KEY (AssetComponentId),
        CONSTRAINT FK_AM_AssetComponents_Organisations_OwnerOrganisationId FOREIGN KEY (OwnerOrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_AM_AssetComponents_AM_AssetItems_AssetItemId FOREIGN KEY (AssetItemId) REFERENCES dbo.AM_AssetItems (AssetItemId)
    );

    CREATE INDEX IX_AM_AssetComponents_AssetItemId_ComponentType_Status ON dbo.AM_AssetComponents (AssetItemId, ComponentType, Status);
    CREATE INDEX IX_AM_AssetComponents_SerialNumber ON dbo.AM_AssetComponents (SerialNumber) WHERE SerialNumber IS NOT NULL AND SerialNumber <> '';
END;

-- 6. Asset Component Transactions
IF OBJECT_ID(N'dbo.AM_AssetComponentTransactions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetComponentTransactions
    (
        AssetComponentTransactionId bigint IDENTITY(1,1) NOT NULL,
        AssetComponentId int NOT NULL,
        AssetItemId int NOT NULL,
        AssetMaintenanceJobId int NULL,
        TransactionType nvarchar(40) NOT NULL,
        FromStatus nvarchar(30) NULL,
        ToStatus nvarchar(30) NOT NULL,
        TechnicianUserId int NOT NULL,
        TransactedAtUtc datetime2 NOT NULL,
        Notes nvarchar(1000) NULL,
        CONSTRAINT PK_AM_AssetComponentTransactions PRIMARY KEY (AssetComponentTransactionId),
        CONSTRAINT FK_AM_AssetComponentTransactions_AM_AssetComponents_AssetComponentId FOREIGN KEY (AssetComponentId) REFERENCES dbo.AM_AssetComponents (AssetComponentId),
        CONSTRAINT FK_AM_AssetComponentTransactions_AM_AssetItems_AssetItemId FOREIGN KEY (AssetItemId) REFERENCES dbo.AM_AssetItems (AssetItemId),
        CONSTRAINT FK_AM_AssetComponentTransactions_AM_AssetMaintenanceJobs_AssetMaintenanceJobId FOREIGN KEY (AssetMaintenanceJobId) REFERENCES dbo.AM_AssetMaintenanceJobs (AssetMaintenanceJobId),
        CONSTRAINT FK_AM_AssetComponentTransactions_Users_TechnicianUserId FOREIGN KEY (TechnicianUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE INDEX IX_AM_AssetComponentTransactions_AssetComponentId_TransactedAtUtc ON dbo.AM_AssetComponentTransactions (AssetComponentId, TransactedAtUtc);
END;

-- 7. Service Parts Inventory
IF OBJECT_ID(N'dbo.AM_AssetServiceParts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetServiceParts
    (
        AssetServicePartId int IDENTITY(1,1) NOT NULL,
        OrganisationId int NOT NULL,
        PartCode nvarchar(100) NOT NULL,
        Name nvarchar(150) NOT NULL,
        Category nvarchar(50) NOT NULL,
        Specification nvarchar(250) NOT NULL,
        CompatibleModels nvarchar(250) NULL,
        QuantityOnHand int NOT NULL CONSTRAINT DF_AM_AssetServiceParts_QuantityOnHand DEFAULT (0),
        QuantityReserved int NOT NULL CONSTRAINT DF_AM_AssetServiceParts_QuantityReserved DEFAULT (0),
        UnitCost decimal(18,2) NOT NULL CONSTRAINT DF_AM_AssetServiceParts_UnitCost DEFAULT (0),
        ReorderLevel int NOT NULL CONSTRAINT DF_AM_AssetServiceParts_ReorderLevel DEFAULT (5),
        LocationBin nvarchar(100) NULL,
        IsActive bit NOT NULL CONSTRAINT DF_AM_AssetServiceParts_IsActive DEFAULT (1),
        CONSTRAINT PK_AM_AssetServiceParts PRIMARY KEY (AssetServicePartId),
        CONSTRAINT FK_AM_AssetServiceParts_Organisations_OrganisationId FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID)
    );

    CREATE UNIQUE INDEX UX_AM_AssetServiceParts_Organisation_PartCode ON dbo.AM_AssetServiceParts (OrganisationId, PartCode);
    CREATE INDEX IX_AM_AssetServiceParts_OrganisationId_Category_IsActive ON dbo.AM_AssetServiceParts (OrganisationId, Category, IsActive);
END;

-- 8. Replacement Links
IF OBJECT_ID(N'dbo.AM_AssetReplacementLinks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetReplacementLinks
    (
        AssetReplacementLinkId int IDENTITY(1,1) NOT NULL,
        AssetServiceRequestId int NULL,
        OriginalAssetItemId int NOT NULL,
        OriginalAssetCustodyId int NULL,
        ReplacementAssetItemId int NOT NULL,
        ReplacementAssetCustodyId int NULL,
        ReplacementType nvarchar(40) NOT NULL,
        ReplacementReason nvarchar(40) NOT NULL,
        OriginalAssetDisposition nvarchar(60) NOT NULL,
        StaffAcknowledged bit NOT NULL CONSTRAINT DF_AM_AssetReplacementLinks_StaffAcknowledged DEFAULT (0),
        StaffDecision nvarchar(30) NULL,
        StaffRemarks nvarchar(1000) NULL,
        StaffConfirmedAtUtc datetime2 NULL,
        RecordedByUserId int NOT NULL,
        RecordedAtUtc datetime2 NOT NULL,
        CONSTRAINT PK_AM_AssetReplacementLinks PRIMARY KEY (AssetReplacementLinkId),
        CONSTRAINT FK_AM_AssetReplacementLinks_AM_AssetServiceRequests_AssetServiceRequestId FOREIGN KEY (AssetServiceRequestId) REFERENCES dbo.AM_AssetServiceRequests (AssetServiceRequestId),
        CONSTRAINT FK_AM_AssetReplacementLinks_AM_AssetItems_OriginalAssetItemId FOREIGN KEY (OriginalAssetItemId) REFERENCES dbo.AM_AssetItems (AssetItemId),
        CONSTRAINT FK_AM_AssetReplacementLinks_AM_AssetCustodies_OriginalAssetCustodyId FOREIGN KEY (OriginalAssetCustodyId) REFERENCES dbo.AM_AssetCustodies (AssetCustodyId),
        CONSTRAINT FK_AM_AssetReplacementLinks_AM_AssetItems_ReplacementAssetItemId FOREIGN KEY (ReplacementAssetItemId) REFERENCES dbo.AM_AssetItems (AssetItemId),
        CONSTRAINT FK_AM_AssetReplacementLinks_AM_AssetCustodies_ReplacementAssetCustodyId FOREIGN KEY (ReplacementAssetCustodyId) REFERENCES dbo.AM_AssetCustodies (AssetCustodyId),
        CONSTRAINT FK_AM_AssetReplacementLinks_Users_RecordedByUserId FOREIGN KEY (RecordedByUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE INDEX IX_AM_AssetReplacementLinks_AssetServiceRequestId_OriginalAssetItemId ON dbo.AM_AssetReplacementLinks (AssetServiceRequestId, OriginalAssetItemId);
    CREATE INDEX IX_AM_AssetReplacementLinks_ReplacementAssetItemId ON dbo.AM_AssetReplacementLinks (ReplacementAssetItemId);
END;

-- 9. Temporary Loaner Equipment
IF OBJECT_ID(N'dbo.AM_AssetTemporaryHandovers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetTemporaryHandovers
    (
        AssetTemporaryHandoverId int IDENTITY(1,1) NOT NULL,
        AssetServiceRequestId int NULL,
        PrimaryAssetItemId int NOT NULL,
        LoanerAssetItemId int NOT NULL,
        IndividualId int NOT NULL,
        JobId int NOT NULL,
        OrganisationId int NOT NULL,
        HandedOverAtUtc datetime2 NOT NULL,
        ExpectedReturnDate date NOT NULL,
        ActualReturnedAtUtc datetime2 NULL,
        HandedOverByUserId int NOT NULL,
        ReceivedBackByUserId int NULL,
        ConditionOutId int NOT NULL,
        ConditionInId int NULL,
        Status nvarchar(40) NOT NULL,
        StaffAcknowledgementConfirmed bit NOT NULL CONSTRAINT DF_AM_AssetTemporaryHandovers_StaffAcknowledgementConfirmed DEFAULT (0),
        StaffConfirmedAtUtc datetime2 NULL,
        Notes nvarchar(1000) NULL,
        CONSTRAINT PK_AM_AssetTemporaryHandovers PRIMARY KEY (AssetTemporaryHandoverId),
        CONSTRAINT FK_AM_AssetTemporaryHandovers_AM_AssetServiceRequests_AssetServiceRequestId FOREIGN KEY (AssetServiceRequestId) REFERENCES dbo.AM_AssetServiceRequests (AssetServiceRequestId),
        CONSTRAINT FK_AM_AssetTemporaryHandovers_AM_AssetItems_PrimaryAssetItemId FOREIGN KEY (PrimaryAssetItemId) REFERENCES dbo.AM_AssetItems (AssetItemId),
        CONSTRAINT FK_AM_AssetTemporaryHandovers_AM_AssetItems_LoanerAssetItemId FOREIGN KEY (LoanerAssetItemId) REFERENCES dbo.AM_AssetItems (AssetItemId),
        CONSTRAINT FK_AM_AssetTemporaryHandovers_Individuals_IndividualId FOREIGN KEY (IndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID),
        CONSTRAINT FK_AM_AssetTemporaryHandovers_Jobs_JobId FOREIGN KEY (JobId) REFERENCES dbo.Jobs (JobID),
        CONSTRAINT FK_AM_AssetTemporaryHandovers_Organisations_OrganisationId FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_AM_AssetTemporaryHandovers_Users_HandedOverByUserId FOREIGN KEY (HandedOverByUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_AM_AssetTemporaryHandovers_Users_ReceivedBackByUserId FOREIGN KEY (ReceivedBackByUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_AM_AssetTemporaryHandovers_AM_AssetConditions_ConditionOutId FOREIGN KEY (ConditionOutId) REFERENCES dbo.AM_AssetConditions (AssetConditionId),
        CONSTRAINT FK_AM_AssetTemporaryHandovers_AM_AssetConditions_ConditionInId FOREIGN KEY (ConditionInId) REFERENCES dbo.AM_AssetConditions (AssetConditionId)
    );

    CREATE INDEX IX_AM_AssetTemporaryHandovers_PrimaryAssetItemId_Status ON dbo.AM_AssetTemporaryHandovers (PrimaryAssetItemId, Status);
    CREATE INDEX IX_AM_AssetTemporaryHandovers_LoanerAssetItemId_Status ON dbo.AM_AssetTemporaryHandovers (LoanerAssetItemId, Status);
    CREATE INDEX IX_AM_AssetTemporaryHandovers_IndividualId_Status ON dbo.AM_AssetTemporaryHandovers (IndividualId, Status);
END;

-- 10. Technical and Condition Inspections
IF OBJECT_ID(N'dbo.AM_AssetInspections', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetInspections
    (
        AssetInspectionId int IDENTITY(1,1) NOT NULL,
        AssetItemId int NOT NULL,
        AssetCustodyId int NULL,
        AssetServiceRequestId int NULL,
        AssetReturnItemId int NULL,
        InspectionStage nvarchar(40) NOT NULL,
        InspectorUserId int NOT NULL,
        InspectedAtUtc datetime2 NOT NULL,
        OverallConditionId int NOT NULL,
        PhysicalDamageReport nvarchar(1000) NULL,
        FunctionalTestSummary nvarchar(1000) NULL,
        AccessoriesChecklist nvarchar(500) NULL,
        DataSanitizationVerified bit NOT NULL CONSTRAINT DF_AM_AssetInspections_DataSanitizationVerified DEFAULT (0),
        InspectorDecision nvarchar(40) NOT NULL,
        Notes nvarchar(1000) NULL,
        CONSTRAINT PK_AM_AssetInspections PRIMARY KEY (AssetInspectionId),
        CONSTRAINT FK_AM_AssetInspections_AM_AssetItems_AssetItemId FOREIGN KEY (AssetItemId) REFERENCES dbo.AM_AssetItems (AssetItemId),
        CONSTRAINT FK_AM_AssetInspections_AM_AssetCustodies_AssetCustodyId FOREIGN KEY (AssetCustodyId) REFERENCES dbo.AM_AssetCustodies (AssetCustodyId),
        CONSTRAINT FK_AM_AssetInspections_AM_AssetServiceRequests_AssetServiceRequestId FOREIGN KEY (AssetServiceRequestId) REFERENCES dbo.AM_AssetServiceRequests (AssetServiceRequestId),
        CONSTRAINT FK_AM_AssetInspections_AM_AssetReturnItems_AssetReturnItemId FOREIGN KEY (AssetReturnItemId) REFERENCES dbo.AM_AssetReturnItems (AssetReturnItemId),
        CONSTRAINT FK_AM_AssetInspections_Users_InspectorUserId FOREIGN KEY (InspectorUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_AM_AssetInspections_AM_AssetConditions_OverallConditionId FOREIGN KEY (OverallConditionId) REFERENCES dbo.AM_AssetConditions (AssetConditionId)
    );

    CREATE INDEX IX_AM_AssetInspections_AssetItemId_InspectionStage ON dbo.AM_AssetInspections (AssetItemId, InspectionStage);
    CREATE INDEX IX_AM_AssetInspections_AssetServiceRequestId ON dbo.AM_AssetInspections (AssetServiceRequestId);
END;

-- 11. Service Documents (Printable versioned snapshots with SHA-256)
IF OBJECT_ID(N'dbo.AM_AssetServiceDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetServiceDocuments
    (
        AssetServiceDocumentId bigint IDENTITY(1,1) NOT NULL,
        DocumentType nvarchar(40) NOT NULL,
        DocumentNumber nvarchar(60) NOT NULL,
        DocumentVersion int NOT NULL CONSTRAINT DF_AM_AssetServiceDocuments_DocumentVersion DEFAULT (1),
        AssetServiceRequestId int NULL,
        AssetMaintenanceJobId int NULL,
        AssetItemId int NULL,
        IndividualId int NULL,
        SnapshotJson nvarchar(max) NOT NULL,
        Sha256Hash nvarchar(64) NOT NULL,
        GeneratedByUserId int NOT NULL,
        GeneratedAtUtc datetime2 NOT NULL,
        IsRestrictedSecurityVariant bit NOT NULL CONSTRAINT DF_AM_AssetServiceDocuments_IsRestrictedSecurityVariant DEFAULT (0),
        CONSTRAINT PK_AM_AssetServiceDocuments PRIMARY KEY (AssetServiceDocumentId),
        CONSTRAINT FK_AM_AssetServiceDocuments_AM_AssetServiceRequests_AssetServiceRequestId FOREIGN KEY (AssetServiceRequestId) REFERENCES dbo.AM_AssetServiceRequests (AssetServiceRequestId),
        CONSTRAINT FK_AM_AssetServiceDocuments_AM_AssetMaintenanceJobs_AssetMaintenanceJobId FOREIGN KEY (AssetMaintenanceJobId) REFERENCES dbo.AM_AssetMaintenanceJobs (AssetMaintenanceJobId),
        CONSTRAINT FK_AM_AssetServiceDocuments_AM_AssetItems_AssetItemId FOREIGN KEY (AssetItemId) REFERENCES dbo.AM_AssetItems (AssetItemId),
        CONSTRAINT FK_AM_AssetServiceDocuments_Individuals_IndividualId FOREIGN KEY (IndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID),
        CONSTRAINT FK_AM_AssetServiceDocuments_Users_GeneratedByUserId FOREIGN KEY (GeneratedByUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE UNIQUE INDEX UX_AM_AssetServiceDocuments_DocumentNumber ON dbo.AM_AssetServiceDocuments (DocumentNumber);
    CREATE INDEX IX_AM_AssetServiceDocuments_AssetServiceRequestId_DocumentType ON dbo.AM_AssetServiceDocuments (AssetServiceRequestId, DocumentType);
    CREATE INDEX IX_AM_AssetServiceDocuments_IndividualId_DocumentType ON dbo.AM_AssetServiceDocuments (IndividualId, DocumentType);
END;

-- 12. Asset Configuration Snapshots (Hardware specification audit timeline)
IF OBJECT_ID(N'dbo.AM_AssetConfigurationSnapshots', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AM_AssetConfigurationSnapshots
    (
        AssetConfigurationSnapshotId bigint IDENTITY(1,1) NOT NULL,
        AssetItemId int NOT NULL,
        TriggerEvent nvarchar(50) NOT NULL,
        ProcessorSpec nvarchar(150) NULL,
        RamSummary nvarchar(150) NOT NULL,
        StorageSummary nvarchar(250) NOT NULL,
        GraphicsSpec nvarchar(150) NULL,
        BatteryHealthPercentage int NULL,
        OsVersion nvarchar(100) NULL,
        ComponentsJson nvarchar(max) NOT NULL,
        CapturedAtUtc datetime2 NOT NULL,
        CapturedByUserId int NOT NULL,
        CONSTRAINT PK_AM_AssetConfigurationSnapshots PRIMARY KEY (AssetConfigurationSnapshotId),
        CONSTRAINT FK_AM_AssetConfigurationSnapshots_AM_AssetItems_AssetItemId FOREIGN KEY (AssetItemId) REFERENCES dbo.AM_AssetItems (AssetItemId),
        CONSTRAINT FK_AM_AssetConfigurationSnapshots_Users_CapturedByUserId FOREIGN KEY (CapturedByUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE INDEX IX_AM_AssetConfigurationSnapshots_AssetItemId_CapturedAtUtc ON dbo.AM_AssetConfigurationSnapshots (AssetItemId, CapturedAtUtc);
END;

COMMIT TRANSACTION;

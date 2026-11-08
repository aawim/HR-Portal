-- External Staff Transfer & Clearance Integration Migration Script
-- Idempotent deployment extending ST_StaffTransfers and introducing Clearance & Asset Decision tables.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- 1. Extend ST_StaffTransfers with External Transfer columns
IF COL_LENGTH(N'dbo.ST_StaffTransfers', N'IsExternalTransfer') IS NULL
BEGIN
    ALTER TABLE dbo.ST_StaffTransfers
    ADD IsExternalTransfer bit NOT NULL CONSTRAINT DF_ST_StaffTransfers_IsExternalTransfer DEFAULT (0),
        IsLegalEmployerChanged bit NOT NULL CONSTRAINT DF_ST_StaffTransfers_IsLegalEmployerChanged DEFAULT (0),
        SourceEmployerId int NULL,
        DestinationEmployerId int NULL,
        SecondmentEndDate date NULL,
        ServiceContinuityTreatment nvarchar(50) NOT NULL CONSTRAINT DF_ST_StaffTransfers_ServiceContinuity DEFAULT ('RecognizedContinuous'),
        LeaveBalanceTreatment nvarchar(50) NOT NULL CONSTRAINT DF_ST_StaffTransfers_LeaveBalance DEFAULT ('TransferFull'),
        SourceHrReleaseUserId int NULL,
        SourceHrReleasedAt datetime2 NULL,
        AppointingAuthorityRef nvarchar(100) NULL;

    ALTER TABLE dbo.ST_StaffTransfers
    ADD CONSTRAINT FK_ST_StaffTransfers_SourceEmployer FOREIGN KEY (SourceEmployerId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_ST_StaffTransfers_DestinationEmployer FOREIGN KEY (DestinationEmployerId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_ST_StaffTransfers_SourceHrReleaseUser FOREIGN KEY (SourceHrReleaseUserId) REFERENCES dbo.Users (UserID);
END;

-- 2. Clearance Tasks Table
IF OBJECT_ID(N'dbo.ST_StaffTransferClearanceTasks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ST_StaffTransferClearanceTasks
    (
        ClearanceTaskId int IDENTITY(1,1) NOT NULL,
        StaffTransferId int NOT NULL,
        ClearanceDomain nvarchar(50) NOT NULL,
        TaskTitle nvarchar(250) NOT NULL,
        TaskDescription nvarchar(1000) NULL,
        AssignedToUserId int NULL,
        AssignedToOrganisationStructureId int NULL,
        Status nvarchar(40) NOT NULL CONSTRAINT DF_ST_ClearanceTasks_Status DEFAULT ('Pending'),
        IsMandatoryForFinalApproval bit NOT NULL CONSTRAINT DF_ST_ClearanceTasks_MandatoryApproval DEFAULT (0),
        IsMandatoryForActivation bit NOT NULL CONSTRAINT DF_ST_ClearanceTasks_MandatoryActivation DEFAULT (1),
        ClearedByUserId int NULL,
        ClearedAtUtc datetime2 NULL,
        WaiverReason nvarchar(1000) NULL,
        Remarks nvarchar(1000) NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_ST_StaffTransferClearanceTasks PRIMARY KEY (ClearanceTaskId),
        CONSTRAINT FK_ST_ClearanceTasks_StaffTransfers FOREIGN KEY (StaffTransferId) REFERENCES dbo.ST_StaffTransfers (StaffTransferId) ON DELETE CASCADE,
        CONSTRAINT FK_ST_ClearanceTasks_AssignedUser FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_ST_ClearanceTasks_AssignedStructure FOREIGN KEY (AssignedToOrganisationStructureId) REFERENCES dbo.OrganisationStructure (OrganisationStructureID),
        CONSTRAINT FK_ST_ClearanceTasks_ClearedUser FOREIGN KEY (ClearedByUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE INDEX IX_ST_StaffTransferClearanceTasks_Transfer_Domain ON dbo.ST_StaffTransferClearanceTasks (StaffTransferId, ClearanceDomain, Status);
END;

-- 3. Asset Decisions Table (Integrated with AM_AssetCustodies)
IF OBJECT_ID(N'dbo.ST_StaffTransferAssetDecisions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ST_StaffTransferAssetDecisions
    (
        TransferAssetDecisionId int IDENTITY(1,1) NOT NULL,
        StaffTransferId int NOT NULL,
        AssetCustodyId int NOT NULL,
        AssetItemId int NOT NULL,
        AssetDecision nvarchar(50) NOT NULL,
        DestinationAccepted bit NOT NULL CONSTRAINT DF_ST_AssetDecisions_DestAccepted DEFAULT (0),
        HandoverConditionId int NULL,
        ReturnServiceRequestId int NULL,
        VerifiedByUserId int NULL,
        VerifiedAtUtc datetime2 NULL,
        Notes nvarchar(1000) NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_ST_StaffTransferAssetDecisions PRIMARY KEY (TransferAssetDecisionId),
        CONSTRAINT FK_ST_AssetDecisions_StaffTransfers FOREIGN KEY (StaffTransferId) REFERENCES dbo.ST_StaffTransfers (StaffTransferId) ON DELETE CASCADE,
        CONSTRAINT FK_ST_AssetDecisions_AssetCustodies FOREIGN KEY (AssetCustodyId) REFERENCES dbo.AM_AssetCustodies (AssetCustodyId),
        CONSTRAINT FK_ST_AssetDecisions_AssetItems FOREIGN KEY (AssetItemId) REFERENCES dbo.AM_AssetItems (AssetItemId),
        CONSTRAINT FK_ST_AssetDecisions_AssetConditions FOREIGN KEY (HandoverConditionId) REFERENCES dbo.AM_AssetConditions (AssetConditionId),
        CONSTRAINT FK_ST_AssetDecisions_ServiceRequests FOREIGN KEY (ReturnServiceRequestId) REFERENCES dbo.AM_AssetServiceRequests (AssetServiceRequestId),
        CONSTRAINT FK_ST_AssetDecisions_VerifiedUser FOREIGN KEY (VerifiedByUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE INDEX IX_ST_StaffTransferAssetDecisions_Transfer_Custody ON dbo.ST_StaffTransferAssetDecisions (StaffTransferId, AssetCustodyId);
END;

-- 4. Financial Settlement Table
IF OBJECT_ID(N'dbo.ST_StaffTransferFinanceSettlements', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ST_StaffTransferFinanceSettlements
    (
        FinanceSettlementId int IDENTITY(1,1) NOT NULL,
        StaffTransferId int NOT NULL,
        LastWorkingDateAtSource date NOT NULL,
        LastPayrollCutoffDate date NOT NULL,
        OutstandingAdvanceDeduction decimal(18,2) NOT NULL CONSTRAINT DF_ST_Finance_Advance DEFAULT (0.00),
        UnencashedLeaveDays decimal(9,2) NOT NULL CONSTRAINT DF_ST_Finance_LeaveDays DEFAULT (0.00),
        LeaveEncashmentAmount decimal(18,2) NOT NULL CONSTRAINT DF_ST_Finance_LeaveAmount DEFAULT (0.00),
        GratuitySeveranceAmount decimal(18,2) NOT NULL CONSTRAINT DF_ST_Finance_Gratuity DEFAULT (0.00),
        SettlementMode nvarchar(50) NOT NULL CONSTRAINT DF_ST_Finance_Mode DEFAULT ('InterCompanyTransfer'),
        FinanceOfficerUserId int NULL,
        IsCleared bit NOT NULL CONSTRAINT DF_ST_Finance_Cleared DEFAULT (0),
        ClearedAtUtc datetime2 NULL,
        Remarks nvarchar(1000) NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_ST_StaffTransferFinanceSettlements PRIMARY KEY (FinanceSettlementId),
        CONSTRAINT UQ_ST_StaffTransferFinanceSettlements_StaffTransferId UNIQUE (StaffTransferId),
        CONSTRAINT FK_ST_Finance_StaffTransfers FOREIGN KEY (StaffTransferId) REFERENCES dbo.ST_StaffTransfers (StaffTransferId) ON DELETE CASCADE,
        CONSTRAINT FK_ST_Finance_FinanceOfficerUser FOREIGN KEY (FinanceOfficerUserId) REFERENCES dbo.Users (UserID)
    );
END;

-- 5. Employment Transition Executions (Audit & Idempotency)
IF OBJECT_ID(N'dbo.ST_StaffTransferEmploymentExecutions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ST_StaffTransferEmploymentExecutions
    (
        ExecutionId bigint IDENTITY(1,1) NOT NULL,
        StaffTransferId int NOT NULL,
        SourceJobId int NOT NULL,
        DestinationJobId int NULL,
        DestinationJobPositionId int NULL,
        EffectiveDate date NOT NULL,
        IsNewContractCreated bit NOT NULL,
        IdempotencyKey nvarchar(120) NOT NULL,
        ExecutedAtUtc datetime2 NOT NULL,
        ExecutionDetailsJson nvarchar(max) NULL,
        Success bit NOT NULL,
        ErrorMessage nvarchar(max) NULL,
        CONSTRAINT PK_ST_StaffTransferEmploymentExecutions PRIMARY KEY (ExecutionId),
        CONSTRAINT UQ_ST_StaffTransferEmploymentExecutions_Key UNIQUE (IdempotencyKey),
        CONSTRAINT FK_ST_Executions_StaffTransfers FOREIGN KEY (StaffTransferId) REFERENCES dbo.ST_StaffTransfers (StaffTransferId) ON DELETE CASCADE,
        CONSTRAINT FK_ST_Executions_SourceJob FOREIGN KEY (SourceJobId) REFERENCES dbo.Jobs (JobID),
        CONSTRAINT FK_ST_Executions_DestinationJob FOREIGN KEY (DestinationJobId) REFERENCES dbo.Jobs (JobID),
        CONSTRAINT FK_ST_Executions_DestinationJobPosition FOREIGN KEY (DestinationJobPositionId) REFERENCES dbo.JobPositions (JobPositionID)
    );

    CREATE INDEX IX_ST_StaffTransferEmploymentExecutions_Transfer ON dbo.ST_StaffTransferEmploymentExecutions (StaffTransferId, ExecutedAtUtc);
END;

COMMIT TRANSACTION;

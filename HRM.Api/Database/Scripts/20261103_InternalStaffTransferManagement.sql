-- Internal Staff Transfer Management (ST_* tables, request type, and state transitions)
-- Idempotent deployment script matching EF Core migration 20261103001800_InternalStaffTransferManagement.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.ST_StaffTransfers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ST_StaffTransfers
    (
        StaffTransferId int IDENTITY(1,1) NOT NULL,
        RequestId int NULL,
        IndividualId int NOT NULL,
        CurrentJobId int NOT NULL,
        FromOrganisationId int NOT NULL,
        FromOrganisationStructureId int NOT NULL,
        ToOrganisationId int NOT NULL,
        ToOrganisationStructureId int NOT NULL,
        TransferTypeId int NOT NULL,
        TransferReason nvarchar(1000) NOT NULL,
        RequestedByUserId int NOT NULL,
        RequestedDate datetime2 NOT NULL,
        EffectiveDate date NOT NULL,
        ReportingDate date NOT NULL,
        Status nvarchar(40) NOT NULL,
        IsOpen bit NOT NULL,
        CurrentVersion int NOT NULL,
        ConfirmedVersion int NULL,
        MaterialStateFingerprint nvarchar(64) NULL,
        FullNameSnapshot nvarchar(250) NOT NULL,
        EmployeeNumberSnapshot nvarchar(50) NOT NULL,
        ProtectedIdCardSnapshot nvarchar(100) NULL,
        CurrentEmploymentStatusSnapshot nvarchar(100) NOT NULL,
        EmploymentCategorySnapshot nvarchar(100) NOT NULL,
        DateJoinedSnapshot date NOT NULL,
        OfficialEmailSnapshot nvarchar(128) NULL,
        OfficialPhoneSnapshot nvarchar(50) NULL,
        CurrentSupervisorIndividualId int NULL,
        CurrentHodIndividualId int NULL,
        StaffSnapshotJson nvarchar(4000) NULL,
        FinalApprovedByUserId int NULL,
        FinalApprovedAt datetime2 NULL,
        FinalDecisionRemarks nvarchar(2000) NULL,
        ActivatedAt datetime2 NULL,
        OperationLogId int NULL,
        CreatedDate datetime2 NOT NULL,
        UpdatedDate datetime2 NOT NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_ST_StaffTransfers PRIMARY KEY (StaffTransferId),
        CONSTRAINT CK_ST_StaffTransfers_Status CHECK ([Status] IN ('Draft','Submitted','PendingStaffConfirmation','CorrectionRequested','PendingReceivingApproval','Rejected','Approved','Scheduled','ActivationInProgress','Active','ActivationFailed','Cancelled')),
        CONSTRAINT CK_ST_StaffTransfers_TransferType CHECK ([TransferTypeId] BETWEEN 1 AND 5),
        CONSTRAINT CK_ST_StaffTransfers_Versions CHECK ([CurrentVersion] >= 1 AND ([ConfirmedVersion] IS NULL OR ([ConfirmedVersion] >= 1 AND [ConfirmedVersion] <= [CurrentVersion]))),
        CONSTRAINT FK_ST_StaffTransfers_Requests_RequestId FOREIGN KEY (RequestId) REFERENCES dbo.Requests (RequestID),
        CONSTRAINT FK_ST_StaffTransfers_BE_Individuals_IndividualId FOREIGN KEY (IndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID),
        CONSTRAINT FK_ST_StaffTransfers_Jobs_CurrentJobId FOREIGN KEY (CurrentJobId) REFERENCES dbo.Jobs (JobID),
        CONSTRAINT FK_ST_StaffTransfers_Organisations_FromOrganisationId FOREIGN KEY (FromOrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_ST_StaffTransfers_OrganisationStructure_FromOrganisationStructureId FOREIGN KEY (FromOrganisationStructureId) REFERENCES dbo.OrganisationStructure (OrganisationStructureID),
        CONSTRAINT FK_ST_StaffTransfers_Organisations_ToOrganisationId FOREIGN KEY (ToOrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_ST_StaffTransfers_OrganisationStructure_ToOrganisationStructureId FOREIGN KEY (ToOrganisationStructureId) REFERENCES dbo.OrganisationStructure (OrganisationStructureID),
        CONSTRAINT FK_ST_StaffTransfers_Users_RequestedByUserId FOREIGN KEY (RequestedByUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_ST_StaffTransfers_Users_FinalApprovedByUserId FOREIGN KEY (FinalApprovedByUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_ST_StaffTransfers_BE_Individuals_CurrentSupervisorIndividualId FOREIGN KEY (CurrentSupervisorIndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID),
        CONSTRAINT FK_ST_StaffTransfers_BE_Individuals_CurrentHodIndividualId FOREIGN KEY (CurrentHodIndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID),
        CONSTRAINT FK_ST_StaffTransfers_OperationLogs_OperationLogId FOREIGN KEY (OperationLogId) REFERENCES dbo.OperationLogs (OperationLogID)
    );

    CREATE UNIQUE INDEX IX_ST_StaffTransfers_RequestId ON dbo.ST_StaffTransfers (RequestId) WHERE [RequestId] IS NOT NULL;
    CREATE UNIQUE INDEX IX_ST_StaffTransfers_CurrentJobId ON dbo.ST_StaffTransfers (CurrentJobId) WHERE [IsOpen] = 1;
    CREATE INDEX IX_ST_StaffTransfers_FromOrganisationId_Status_EffectiveDate ON dbo.ST_StaffTransfers (FromOrganisationId, Status, EffectiveDate);
    CREATE INDEX IX_ST_StaffTransfers_ToOrganisationId_Status_EffectiveDate ON dbo.ST_StaffTransfers (ToOrganisationId, Status, EffectiveDate);
    CREATE INDEX IX_ST_StaffTransfers_IndividualId_EffectiveDate ON dbo.ST_StaffTransfers (IndividualId, EffectiveDate);
END;

IF OBJECT_ID(N'dbo.ST_StaffTransferJobDetails', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ST_StaffTransferJobDetails
    (
        StaffTransferJobDetailId int IDENTITY(1,1) NOT NULL,
        StaffTransferId int NOT NULL,
        ExistingJobId int NOT NULL,
        ExistingJobPositionId int NULL,
        ExistingPositionId int NULL,
        ExistingPositionTitleSnapshot nvarchar(250) NOT NULL,
        ExistingPositionFrameworkId int NULL,
        ExistingPositionMatrixId int NULL,
        ExistingJobMatrixId int NULL,
        ExistingJobMatrixStepId int NULL,
        ExistingGradeLevelSnapshot nvarchar(150) NULL,
        ExistingBasicSalarySnapshot decimal(18,2) NULL,
        ExistingAllowanceSnapshot decimal(18,2) NULL,
        DestinationOrganisationId int NOT NULL,
        DestinationOrganisationStructureId int NOT NULL,
        DestinationJobTypeId int NULL,
        DestinationPositionId int NULL,
        DestinationPositionTitleSnapshot nvarchar(250) NULL,
        DestinationPositionFrameworkId int NULL,
        DestinationPositionMatrixId int NULL,
        DestinationJobMatrixId int NULL,
        DestinationJobMatrixStepId int NULL,
        DestinationGradeLevelSnapshot nvarchar(150) NULL,
        TransferAppointmentMode nvarchar(40) NOT NULL,
        HasSalaryBenefitChange bit NOT NULL,
        ProposedBasicSalary decimal(18,2) NULL,
        ProposedAllowance decimal(18,2) NULL,
        SalaryBenefitChangeNotes nvarchar(1000) NULL,
        NewReportingSupervisorIndividualId int NULL,
        ResultingJobId int NULL,
        ResultingJobPositionId int NULL,
        CONSTRAINT PK_ST_StaffTransferJobDetails PRIMARY KEY (StaffTransferJobDetailId),
        CONSTRAINT CK_ST_StaffTransferJobDetails_Mode CHECK ([TransferAppointmentMode] IN ('OrganisationalReassignment','SameJobPositionChange','NewEmploymentAppointment')),
        CONSTRAINT FK_ST_StaffTransferJobDetails_ST_StaffTransfers_StaffTransferId FOREIGN KEY (StaffTransferId) REFERENCES dbo.ST_StaffTransfers (StaffTransferId),
        CONSTRAINT FK_ST_StaffTransferJobDetails_Jobs_ExistingJobId FOREIGN KEY (ExistingJobId) REFERENCES dbo.Jobs (JobID),
        CONSTRAINT FK_ST_StaffTransferJobDetails_JobPositions_ExistingJobPositionId FOREIGN KEY (ExistingJobPositionId) REFERENCES dbo.JobPositions (JobPositionID),
        CONSTRAINT FK_ST_StaffTransferJobDetails_Positions_ExistingPositionId FOREIGN KEY (ExistingPositionId) REFERENCES dbo.Positions (PositionID),
        CONSTRAINT FK_ST_StaffTransferJobDetails_Organisations_DestinationOrganisationId FOREIGN KEY (DestinationOrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_ST_StaffTransferJobDetails_OrganisationStructure_DestinationOrganisationStructureId FOREIGN KEY (DestinationOrganisationStructureId) REFERENCES dbo.OrganisationStructure (OrganisationStructureID),
        CONSTRAINT FK_ST_StaffTransferJobDetails_JobTypes_DestinationJobTypeId FOREIGN KEY (DestinationJobTypeId) REFERENCES dbo.JobTypes (JobTypeID),
        CONSTRAINT FK_ST_StaffTransferJobDetails_Positions_DestinationPositionId FOREIGN KEY (DestinationPositionId) REFERENCES dbo.Positions (PositionID),
        CONSTRAINT FK_ST_StaffTransferJobDetails_BE_Individuals_NewReportingSupervisorIndividualId FOREIGN KEY (NewReportingSupervisorIndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID),
        CONSTRAINT FK_ST_StaffTransferJobDetails_Jobs_ResultingJobId FOREIGN KEY (ResultingJobId) REFERENCES dbo.Jobs (JobID),
        CONSTRAINT FK_ST_StaffTransferJobDetails_JobPositions_ResultingJobPositionId FOREIGN KEY (ResultingJobPositionId) REFERENCES dbo.JobPositions (JobPositionID)
    );

    CREATE UNIQUE INDEX IX_ST_StaffTransferJobDetails_StaffTransferId ON dbo.ST_StaffTransferJobDetails (StaffTransferId);
    CREATE INDEX IX_ST_StaffTransferJobDetails_DestinationPositionMatrixId ON dbo.ST_StaffTransferJobDetails (DestinationPositionMatrixId);
END;

IF OBJECT_ID(N'dbo.ST_StaffTransferWorkDetails', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ST_StaffTransferWorkDetails
    (
        StaffTransferWorkDetailId int IDENTITY(1,1) NOT NULL,
        StaffTransferId int NOT NULL,
        CurrentLocationId int NULL,
        CurrentLocationNameSnapshot nvarchar(250) NULL,
        ProposedLocationId int NULL,
        ProposedLocationName nvarchar(250) NOT NULL,
        CurrentPlanningProviderId int NULL,
        ProposedPlanningProviderId int NULL,
        CurrentWorkTemplateId int NULL,
        ProposedWorkTemplateId int NULL,
        CurrentScheduleMode nvarchar(30) NULL,
        ProposedScheduleMode nvarchar(30) NOT NULL,
        WorkingDaysMask int NOT NULL,
        StandardStartTime time NULL,
        StandardEndTime time NULL,
        WeeklyWorkingHours decimal(6,2) NOT NULL,
        RequiredAttendanceMethod nvarchar(50) NOT NULL,
        AttendanceDeviceId int NULL,
        AttendanceDeviceLocationNotes nvarchar(250) NULL,
        IsOvertimeEligible bit NOT NULL,
        WorkArrangementMode nvarchar(30) NOT NULL,
        AttendanceVerificationSupervisorIndividualId int NULL,
        ScheduleEffectiveDate date NOT NULL,
        ReconcileFutureWorkPlans bit NOT NULL,
        WorkNotes nvarchar(1000) NULL,
        CONSTRAINT PK_ST_StaffTransferWorkDetails PRIMARY KEY (StaffTransferWorkDetailId),
        CONSTRAINT CK_ST_StaffTransferWorkDetails_Hours CHECK ([WeeklyWorkingHours] > 0 AND [WeeklyWorkingHours] <= 168 AND [WorkingDaysMask] BETWEEN 0 AND 127),
        CONSTRAINT FK_ST_StaffTransferWorkDetails_ST_StaffTransfers_StaffTransferId FOREIGN KEY (StaffTransferId) REFERENCES dbo.ST_StaffTransfers (StaffTransferId),
        CONSTRAINT FK_ST_StaffTransferWorkDetails_Locations_CurrentLocationId FOREIGN KEY (CurrentLocationId) REFERENCES dbo.Locations (LocationID),
        CONSTRAINT FK_ST_StaffTransferWorkDetails_Locations_ProposedLocationId FOREIGN KEY (ProposedLocationId) REFERENCES dbo.Locations (LocationID),
        CONSTRAINT FK_ST_StaffTransferWorkDetails_PlanningProviders_CurrentPlanningProviderId FOREIGN KEY (CurrentPlanningProviderId) REFERENCES dbo.PlanningProviders (PlanningProviderID),
        CONSTRAINT FK_ST_StaffTransferWorkDetails_PlanningProviders_ProposedPlanningProviderId FOREIGN KEY (ProposedPlanningProviderId) REFERENCES dbo.PlanningProviders (PlanningProviderID),
        CONSTRAINT FK_ST_StaffTransferWorkDetails_WorkTemplates_CurrentWorkTemplateId FOREIGN KEY (CurrentWorkTemplateId) REFERENCES dbo.WorkTemplates (WorkTemplateID),
        CONSTRAINT FK_ST_StaffTransferWorkDetails_WorkTemplates_ProposedWorkTemplateId FOREIGN KEY (ProposedWorkTemplateId) REFERENCES dbo.WorkTemplates (WorkTemplateID),
        CONSTRAINT FK_ST_StaffTransferWorkDetails_AttendanceDevices_AttendanceDeviceId FOREIGN KEY (AttendanceDeviceId) REFERENCES dbo.AttendanceDevices (AttendanceDeviceID),
        CONSTRAINT FK_ST_StaffTransferWorkDetails_BE_Individuals_AttendanceVerificationSupervisorIndividualId FOREIGN KEY (AttendanceVerificationSupervisorIndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID)
    );

    CREATE UNIQUE INDEX IX_ST_StaffTransferWorkDetails_StaffTransferId ON dbo.ST_StaffTransferWorkDetails (StaffTransferId);
END;

IF OBJECT_ID(N'dbo.ST_StaffTransferAccessChanges', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ST_StaffTransferAccessChanges
    (
        StaffTransferAccessChangeId int IDENTITY(1,1) NOT NULL,
        StaffTransferId int NOT NULL,
        AccessAreaCode nvarchar(60) NOT NULL,
        AccessAreaName nvarchar(150) NOT NULL,
        CurrentScopeSummary nvarchar(500) NULL,
        ProposedScopeSummary nvarchar(500) NULL,
        TransitionAction nvarchar(30) NOT NULL,
        RoleId int NULL,
        UserGroupId int NULL,
        RemainsActiveInOldDepartmentWarning bit NOT NULL,
        RequiresSeparateSecurityAuth bit NOT NULL,
        ProvisioningStatus nvarchar(30) NOT NULL,
        AppliedAtUtc datetime2 NULL,
        AppliedByUserId int NULL,
        Remarks nvarchar(500) NULL,
        CONSTRAINT PK_ST_StaffTransferAccessChanges PRIMARY KEY (StaffTransferAccessChangeId),
        CONSTRAINT CK_ST_StaffTransferAccessChanges_Action CHECK ([TransitionAction] IN ('Retain','Revoke','Grant','RevokeOldGrantNew','ReviewAndAssign','NoChange') AND [ProvisioningStatus] IN ('Pending','Applied','RequiresManualAuth','Revoked','Failed')),
        CONSTRAINT FK_ST_StaffTransferAccessChanges_ST_StaffTransfers_StaffTransferId FOREIGN KEY (StaffTransferId) REFERENCES dbo.ST_StaffTransfers (StaffTransferId),
        CONSTRAINT FK_ST_StaffTransferAccessChanges_Roles_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.Roles (RoleID),
        CONSTRAINT FK_ST_StaffTransferAccessChanges_UserGroups_UserGroupId FOREIGN KEY (UserGroupId) REFERENCES dbo.UserGroups (UserGroupID),
        CONSTRAINT FK_ST_StaffTransferAccessChanges_Users_AppliedByUserId FOREIGN KEY (AppliedByUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE INDEX IX_ST_StaffTransferAccessChanges_StaffTransferId_AccessAreaCode ON dbo.ST_StaffTransferAccessChanges (StaffTransferId, AccessAreaCode);
END;

IF OBJECT_ID(N'dbo.ST_StaffTransferDoorAccess', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ST_StaffTransferDoorAccess
    (
        StaffTransferDoorAccessId int IDENTITY(1,1) NOT NULL,
        StaffTransferId int NOT NULL,
        BuildingName nvarchar(150) NOT NULL,
        FloorName nvarchar(100) NULL,
        DepartmentOfficeArea nvarchar(150) NULL,
        RestrictedRoomName nvarchar(150) NULL,
        IncludesServerRoom bit NOT NULL,
        StaffEntranceName nvarchar(150) NULL,
        AttendanceDeviceId int NULL,
        AccessCardReference nvarchar(100) NULL,
        ExistingAccessGroups nvarchar(500) NULL,
        ProposedAccessGroups nvarchar(500) NULL,
        TransitionAction nvarchar(30) NOT NULL,
        AccessStartDate date NOT NULL,
        PreviousAccessExpiryDate date NULL,
        SecurityOfficerUserId int NULL,
        ConnectorReference nvarchar(150) NULL,
        ProvisioningState nvarchar(30) NOT NULL,
        ProvisionedAtUtc datetime2 NULL,
        ProvisioningNotes nvarchar(1000) NULL,
        CONSTRAINT PK_ST_StaffTransferDoorAccess PRIMARY KEY (StaffTransferDoorAccessId),
        CONSTRAINT CK_ST_StaffTransferDoorAccess_State CHECK ([TransitionAction] IN ('RetainCommon','RevokeOld','GrantNew','Replace') AND [ProvisioningState] IN ('Pending','TaskGenerated','InProgress','Provisioned','Revoked','Verified','Failed')),
        CONSTRAINT FK_ST_StaffTransferDoorAccess_ST_StaffTransfers_StaffTransferId FOREIGN KEY (StaffTransferId) REFERENCES dbo.ST_StaffTransfers (StaffTransferId),
        CONSTRAINT FK_ST_StaffTransferDoorAccess_AttendanceDevices_AttendanceDeviceId FOREIGN KEY (AttendanceDeviceId) REFERENCES dbo.AttendanceDevices (AttendanceDeviceID),
        CONSTRAINT FK_ST_StaffTransferDoorAccess_Users_SecurityOfficerUserId FOREIGN KEY (SecurityOfficerUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE INDEX IX_ST_StaffTransferDoorAccess_StaffTransferId_ProvisioningState ON dbo.ST_StaffTransferDoorAccess (StaffTransferId, ProvisioningState);
END;

IF OBJECT_ID(N'dbo.ST_StaffTransferReportingDetails', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ST_StaffTransferReportingDetails
    (
        StaffTransferReportingDetailId int IDENTITY(1,1) NOT NULL,
        StaffTransferId int NOT NULL,
        ApprovedTransferEffectiveDate date NOT NULL,
        ReportingDate date NOT NULL,
        ReportingTime time NULL,
        ReportingLocation nvarchar(250) NOT NULL,
        ReportingToIndividualId int NULL,
        ReportingToNameSnapshot nvarchar(250) NULL,
        HandoverRequired bit NOT NULL,
        HandoverDeadline date NULL,
        OutstandingAssignmentsSummary nvarchar(2000) NULL,
        AssetsToReturnSummary nvarchar(2000) NULL,
        AssetsToReceiveSummary nvarchar(2000) NULL,
        OutstandingApprovalsRoutingRule nvarchar(40) NOT NULL,
        OutstandingApprovalsSummary nvarchar(2000) NULL,
        SpecialInstructions nvarchar(2000) NULL,
        CONSTRAINT PK_ST_StaffTransferReportingDetails PRIMARY KEY (StaffTransferReportingDetailId),
        CONSTRAINT CK_ST_StaffTransferReportingDetails_Routing CHECK ([OutstandingApprovalsRoutingRule] IN ('RetainHistoricalReassignOpen','ReassignAllToSuccessor','EscalateToSendingHod','ManualReview')),
        CONSTRAINT FK_ST_StaffTransferReportingDetails_ST_StaffTransfers_StaffTransferId FOREIGN KEY (StaffTransferId) REFERENCES dbo.ST_StaffTransfers (StaffTransferId),
        CONSTRAINT FK_ST_StaffTransferReportingDetails_BE_Individuals_ReportingToIndividualId FOREIGN KEY (ReportingToIndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID)
    );

    CREATE UNIQUE INDEX IX_ST_StaffTransferReportingDetails_StaffTransferId ON dbo.ST_StaffTransferReportingDetails (StaffTransferId);
END;

IF OBJECT_ID(N'dbo.ST_StaffTransferComments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ST_StaffTransferComments
    (
        StaffTransferCommentId int IDENTITY(1,1) NOT NULL,
        StaffTransferId int NOT NULL,
        AuthorUserId int NOT NULL,
        AuthorIndividualId int NULL,
        ParticipantRole nvarchar(40) NOT NULL,
        CommentKind nvarchar(30) NOT NULL,
        RequestVersion int NOT NULL,
        CommentText nvarchar(2000) NOT NULL,
        IsVisibleToEmployee bit NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        CONSTRAINT PK_ST_StaffTransferComments PRIMARY KEY (StaffTransferCommentId),
        CONSTRAINT CK_ST_StaffTransferComments_Kind CHECK ([CommentKind] IN ('AdvisoryComment','Clarification','CorrectionRequest','ApprovalRemark','SystemAudit')),
        CONSTRAINT FK_ST_StaffTransferComments_ST_StaffTransfers_StaffTransferId FOREIGN KEY (StaffTransferId) REFERENCES dbo.ST_StaffTransfers (StaffTransferId),
        CONSTRAINT FK_ST_StaffTransferComments_Users_AuthorUserId FOREIGN KEY (AuthorUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_ST_StaffTransferComments_BE_Individuals_AuthorIndividualId FOREIGN KEY (AuthorIndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID)
    );

    CREATE INDEX IX_ST_StaffTransferComments_StaffTransferId_CreatedAtUtc ON dbo.ST_StaffTransferComments (StaffTransferId, CreatedAtUtc);
END;

IF OBJECT_ID(N'dbo.ST_StaffTransferConfirmations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ST_StaffTransferConfirmations
    (
        StaffTransferConfirmationId int IDENTITY(1,1) NOT NULL,
        StaffTransferId int NOT NULL,
        ConfirmedVersion int NOT NULL,
        ResponseKind nvarchar(30) NOT NULL,
        ConfirmedByUserId int NOT NULL,
        ConfirmedByIndividualId int NOT NULL,
        ConfirmationStatement nvarchar(1000) NOT NULL,
        EmployeeRemarks nvarchar(2000) NULL,
        ReviewedSnapshotJson nvarchar(max) NOT NULL,
        IsInvalidatedByMaterialEdit bit NOT NULL,
        InvalidatedAtUtc datetime2 NULL,
        ConfirmedAtUtc datetime2 NOT NULL,
        CONSTRAINT PK_ST_StaffTransferConfirmations PRIMARY KEY (StaffTransferConfirmationId),
        CONSTRAINT CK_ST_StaffTransferConfirmations_Response CHECK ([ResponseKind] IN ('Confirmed','CorrectionRequested','ClarificationSubmitted') AND [ConfirmedVersion] >= 1),
        CONSTRAINT FK_ST_StaffTransferConfirmations_ST_StaffTransfers_StaffTransferId FOREIGN KEY (StaffTransferId) REFERENCES dbo.ST_StaffTransfers (StaffTransferId),
        CONSTRAINT FK_ST_StaffTransferConfirmations_Users_ConfirmedByUserId FOREIGN KEY (ConfirmedByUserId) REFERENCES dbo.Users (UserID),
        CONSTRAINT FK_ST_StaffTransferConfirmations_BE_Individuals_ConfirmedByIndividualId FOREIGN KEY (ConfirmedByIndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID)
    );

    CREATE INDEX IX_ST_StaffTransferConfirmations_StaffTransferId_ConfirmedVersion ON dbo.ST_StaffTransferConfirmations (StaffTransferId, ConfirmedVersion);
END;

IF OBJECT_ID(N'dbo.ST_StaffTransferActivationLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ST_StaffTransferActivationLogs
    (
        StaffTransferActivationLogId bigint IDENTITY(1,1) NOT NULL,
        StaffTransferId int NOT NULL,
        ExecutionAttempt int NOT NULL,
        StepCode nvarchar(50) NOT NULL,
        StepStatus nvarchar(25) NOT NULL,
        IdempotencyKey nvarchar(120) NOT NULL,
        DetailsJson nvarchar(4000) NULL,
        ErrorMessage nvarchar(2000) NULL,
        ExecutedByUserId int NULL,
        ExecutedAtUtc datetime2 NOT NULL,
        CONSTRAINT PK_ST_StaffTransferActivationLogs PRIMARY KEY (StaffTransferActivationLogId),
        CONSTRAINT CK_ST_StaffTransferActivationLogs_Status CHECK ([StepStatus] IN ('Pending','Succeeded','Skipped','Failed') AND [ExecutionAttempt] >= 1),
        CONSTRAINT FK_ST_StaffTransferActivationLogs_ST_StaffTransfers_StaffTransferId FOREIGN KEY (StaffTransferId) REFERENCES dbo.ST_StaffTransfers (StaffTransferId),
        CONSTRAINT FK_ST_StaffTransferActivationLogs_Users_ExecutedByUserId FOREIGN KEY (ExecutedByUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE UNIQUE INDEX IX_ST_StaffTransferActivationLogs_StaffTransferId_StepCode_IdempotencyKey
        ON dbo.ST_StaffTransferActivationLogs (StaffTransferId, StepCode, IdempotencyKey)
        WHERE [StepStatus] = 'Succeeded';
END;

IF OBJECT_ID(N'dbo.ST_StaffTransferDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ST_StaffTransferDocuments
    (
        StaffTransferDocumentId bigint IDENTITY(1,1) NOT NULL,
        StaffTransferId int NOT NULL,
        DocumentNumber nvarchar(60) NOT NULL,
        DocumentVariant nvarchar(30) NOT NULL,
        RequestVersion int NOT NULL,
        ConfirmedVersion int NULL,
        SnapshotJson nvarchar(max) NOT NULL,
        Sha256Hash nvarchar(64) NOT NULL,
        PdfBytes varbinary(max) NULL,
        ContentType nvarchar(60) NOT NULL,
        GeneratedByUserId int NOT NULL,
        GeneratedAtUtc datetime2 NOT NULL,
        CONSTRAINT PK_ST_StaffTransferDocuments PRIMARY KEY (StaffTransferDocumentId),
        CONSTRAINT CK_ST_StaffTransferDocuments_Variant CHECK ([DocumentVariant] IN ('EmployeeNotice','RestrictedAdminRecord')),
        CONSTRAINT FK_ST_StaffTransferDocuments_ST_StaffTransfers_StaffTransferId FOREIGN KEY (StaffTransferId) REFERENCES dbo.ST_StaffTransfers (StaffTransferId),
        CONSTRAINT FK_ST_StaffTransferDocuments_Users_GeneratedByUserId FOREIGN KEY (GeneratedByUserId) REFERENCES dbo.Users (UserID)
    );

    CREATE UNIQUE INDEX IX_ST_StaffTransferDocuments_DocumentNumber ON dbo.ST_StaffTransferDocuments (DocumentNumber);
    CREATE INDEX IX_ST_StaffTransferDocuments_StaffTransferId_DocumentVariant_RequestVersion ON dbo.ST_StaffTransferDocuments (StaffTransferId, DocumentVariant, RequestVersion);
END;

IF OBJECT_ID(N'dbo.ST_JobOrganisationHistories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ST_JobOrganisationHistories
    (
        JobOrganisationHistoryId bigint IDENTITY(1,1) NOT NULL,
        JobId int NOT NULL,
        IndividualId int NOT NULL,
        OrganisationId int NOT NULL,
        OrganisationStructureId int NOT NULL,
        EffectiveFrom date NOT NULL,
        EffectiveTo date NULL,
        StaffTransferId int NULL,
        ChangeReason nvarchar(500) NOT NULL,
        OperationLogId int NULL,
        CreatedAtUtc datetime2 NOT NULL,
        CONSTRAINT PK_ST_JobOrganisationHistories PRIMARY KEY (JobOrganisationHistoryId),
        CONSTRAINT CK_ST_JobOrganisationHistories_Dates CHECK ([EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]),
        CONSTRAINT FK_ST_JobOrganisationHistories_Jobs_JobId FOREIGN KEY (JobId) REFERENCES dbo.Jobs (JobID),
        CONSTRAINT FK_ST_JobOrganisationHistories_BE_Individuals_IndividualId FOREIGN KEY (IndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID),
        CONSTRAINT FK_ST_JobOrganisationHistories_Organisations_OrganisationId FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_ST_JobOrganisationHistories_OrganisationStructure_OrganisationStructureId FOREIGN KEY (OrganisationStructureId) REFERENCES dbo.OrganisationStructure (OrganisationStructureID),
        CONSTRAINT FK_ST_JobOrganisationHistories_ST_StaffTransfers_StaffTransferId FOREIGN KEY (StaffTransferId) REFERENCES dbo.ST_StaffTransfers (StaffTransferId),
        CONSTRAINT FK_ST_JobOrganisationHistories_OperationLogs_OperationLogId FOREIGN KEY (OperationLogId) REFERENCES dbo.OperationLogs (OperationLogID)
    );

    CREATE INDEX IX_ST_JobOrganisationHistories_JobId_EffectiveFrom ON dbo.ST_JobOrganisationHistories (JobId, EffectiveFrom);
    CREATE INDEX IX_ST_JobOrganisationHistories_OrganisationId_OrganisationStructureId_EffectiveFrom ON dbo.ST_JobOrganisationHistories (OrganisationId, OrganisationStructureId, EffectiveFrom);
END;

IF NOT EXISTS(SELECT 1 FROM dbo.RequestTypes WHERE TypeName=N'Internal Staff Transfer Request')
BEGIN
    INSERT dbo.SequenceNumberTypes(TypeName,Prefix,FormatString,IsResetAnnually,StartingNumber,NextNumber)
    VALUES(N'Staff Transfers',N'STR',N'{PREFIX}-{YEAR}-{NUMBER}',0,1,1);
    DECLARE @TransferSequence int=CONVERT(int,SCOPE_IDENTITY());
    INSERT dbo.RequestTypes(TypeName,SequenceNumberTypeID,ViewURL,IsSystemType)
    VALUES(N'Internal Staff Transfer Request',@TransferSequence,N'/admin/hr/staff-transfers',1);
END;

INSERT dbo.RequestTypesAllowedRequestStateTransitions(RequestTypeID,FromRequestStateID,ToRequestStateID)
SELECT t.RequestTypeID, f.RequestStateID, s.RequestStateID
FROM dbo.RequestTypes t CROSS JOIN dbo.RequestStates f CROSS JOIN dbo.RequestStates s
WHERE t.TypeName = N'Internal Staff Transfer Request'
  AND (f.RequestStateID=1 OR f.IsProcessingState=1 AND f.IsFinalState=0)
  AND (s.IsProcessingState=1 AND s.IsFinalState=0 OR s.RequestStateID IN (4,5,6))
  AND f.RequestStateID<>s.RequestStateID
  AND NOT EXISTS(SELECT 1 FROM dbo.RequestTypesAllowedRequestStateTransitions x
      WHERE x.RequestTypeID=t.RequestTypeID AND x.FromRequestStateID=f.RequestStateID AND x.ToRequestStateID=s.RequestStateID);

IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20261103001800_InternalStaffTransferManagement')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20261103001800_InternalStaffTransferManagement', N'8.0.27');
END;

COMMIT TRANSACTION;

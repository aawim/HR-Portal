using System;
using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    [DbContext(typeof(HrmTeContext))]
    [Migration("20261107140000_EnterpriseStaffSeparationManagement")]
    public sealed class EnterpriseStaffSeparationManagement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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
                        CONSTRAINT FK_SEP_SeparationTypes_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (OrganisationBusinessEntityID) ON DELETE SET NULL
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
                        CONSTRAINT FK_SEP_ClearanceTemplates_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (OrganisationBusinessEntityID),
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
                        CONSTRAINT FK_SEP_ClearanceTemplateTasks_OrgStructure FOREIGN KEY (AssignedOrganisationStructureId) REFERENCES dbo.OrganisationStructures (OrganisationStructureID) ON DELETE SET NULL,
                        CONSTRAINT FK_SEP_ClearanceTemplateTasks_Roles FOREIGN KEY (AssignedRoleId) REFERENCES dbo.Roles (RoleID) ON DELETE SET NULL
                    );

                    CREATE INDEX IX_SEP_ClearanceTemplateTasks_TemplateId ON dbo.SEP_ClearanceTemplateTasks (ClearanceTemplateId, SequenceOrder);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SEP_StaffSeparations')
                BEGIN
                    CREATE TABLE dbo.SEP_StaffSeparations (
                        StaffSeparationId INT IDENTITY(1,1) NOT NULL,
                        RequestId INT NULL,
                        IndividualId INT NOT NULL,
                        JobId INT NOT NULL,
                        OrganisationId INT NOT NULL,
                        OrganisationStructureId INT NOT NULL,
                        SeparationTypeId INT NOT NULL,
                        ReasonCategory NVARCHAR(50) NOT NULL,
                        ReasonDetails NVARCHAR(MAX) NOT NULL,
                        InitiationSource NVARCHAR(50) NOT NULL CONSTRAINT DF_SEP_StaffSeparations_InitiationSource DEFAULT ('EmployeeSelf'),
                        RequestedNoticeDate DATE NOT NULL,
                        ContractualNoticeDays INT NOT NULL,
                        NoticeWaiverRequested BIT NOT NULL CONSTRAINT DF_SEP_StaffSeparations_NoticeWaiverRequested DEFAULT (0),
                        NoticeWaiverApproved BIT NOT NULL CONSTRAINT DF_SEP_StaffSeparations_NoticeWaiverApproved DEFAULT (0),
                        NoticeWaiverDays INT NOT NULL CONSTRAINT DF_SEP_StaffSeparations_NoticeWaiverDays DEFAULT (0),
                        NoticeShortfallDays INT NOT NULL CONSTRAINT DF_SEP_StaffSeparations_NoticeShortfallDays DEFAULT (0),
                        NoticeShortfallPenaltyAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SEP_StaffSeparations_NoticeShortfallPenaltyAmount DEFAULT (0),
                        LastWorkingDate DATE NOT NULL,
                        EffectiveSeparationDate DATE NOT NULL,
                        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_SEP_StaffSeparations_Status DEFAULT ('Draft'),
                        EmployeeAcknowledgementStatus NVARCHAR(50) NOT NULL CONSTRAINT DF_SEP_StaffSeparations_EmployeeAcknowledgementStatus DEFAULT ('Pending'),
                        EmployeeAcknowledgedAtUtc DATETIME2 NULL,
                        EmployeeExitComments NVARCHAR(MAX) NULL,
                        ExitInterviewCompleted BIT NOT NULL CONSTRAINT DF_SEP_StaffSeparations_ExitInterviewCompleted DEFAULT (0),
                        ExitInterviewDate DATE NULL,
                        ExitInterviewNotes NVARCHAR(MAX) NULL,
                        FullNameSnapshot NVARCHAR(250) NOT NULL,
                        EmployeeNumberSnapshot NVARCHAR(50) NOT NULL,
                        ProtectedIdCardSnapshot NVARCHAR(100) NULL,
                        PositionTitleSnapshot NVARCHAR(200) NOT NULL,
                        OrganisationNameSnapshot NVARCHAR(250) NOT NULL,
                        DepartmentNameSnapshot NVARCHAR(250) NOT NULL,
                        DateJoinedSnapshot DATE NOT NULL,
                        BasicSalarySnapshot DECIMAL(18,2) NULL,
                        SupervisorIndividualId INT NULL,
                        SupervisorNameSnapshot NVARCHAR(250) NULL,
                        OfficialEmailSnapshot NVARCHAR(128) NULL,
                        PersonalContactNumber NVARCHAR(50) NULL,
                        PersonalEmail NVARCHAR(128) NULL,
                        ForwardingAddress NVARCHAR(500) NULL,
                        HrAdminReviewerUserId INT NULL,
                        HrAdminReviewedAtUtc DATETIME2 NULL,
                        HrReviewRemarks NVARCHAR(MAX) NULL,
                        TerminationExecutedAtUtc DATETIME2 NULL,
                        ExecutedByUserId INT NULL,
                        HasUnresolvedObligations BIT NOT NULL CONSTRAINT DF_SEP_StaffSeparations_HasUnresolvedObligations DEFAULT (0),
                        UnresolvedObligationsSummary NVARCHAR(MAX) NULL,
                        CreatedByUserId INT NOT NULL,
                        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SEP_StaffSeparations_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SEP_StaffSeparations_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        RowVersion ROWVERSION NOT NULL,
                        CONSTRAINT PK_SEP_StaffSeparations PRIMARY KEY CLUSTERED (StaffSeparationId),
                        CONSTRAINT FK_SEP_StaffSeparations_Requests FOREIGN KEY (RequestId) REFERENCES dbo.Requests (RequestID) ON DELETE SET NULL,
                        CONSTRAINT FK_SEP_StaffSeparations_Individuals FOREIGN KEY (IndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID),
                        CONSTRAINT FK_SEP_StaffSeparations_Jobs FOREIGN KEY (JobId) REFERENCES dbo.Jobs (JobID),
                        CONSTRAINT FK_SEP_StaffSeparations_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (OrganisationBusinessEntityID),
                        CONSTRAINT FK_SEP_StaffSeparations_OrgStructures FOREIGN KEY (OrganisationStructureId) REFERENCES dbo.OrganisationStructures (OrganisationStructureID),
                        CONSTRAINT FK_SEP_StaffSeparations_Types FOREIGN KEY (SeparationTypeId) REFERENCES dbo.SEP_SeparationTypes (SeparationTypeId),
                        CONSTRAINT FK_SEP_StaffSeparations_Supervisors FOREIGN KEY (SupervisorIndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID),
                        CONSTRAINT FK_SEP_StaffSeparations_HrReviewer FOREIGN KEY (HrAdminReviewerUserId) REFERENCES dbo.Users (UserID),
                        CONSTRAINT FK_SEP_StaffSeparations_Executor FOREIGN KEY (ExecutedByUserId) REFERENCES dbo.Users (UserID),
                        CONSTRAINT FK_SEP_StaffSeparations_Creator FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (UserID)
                    );

                    CREATE INDEX IX_SEP_StaffSeparations_Individual_Job ON dbo.SEP_StaffSeparations (IndividualId, JobId, Status);
                    CREATE INDEX IX_SEP_StaffSeparations_Organisation_Status ON dbo.SEP_StaffSeparations (OrganisationId, Status, EffectiveSeparationDate);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SEP_ClearanceTasks')
                BEGIN
                    CREATE TABLE dbo.SEP_ClearanceTasks (
                        ClearanceTaskId INT IDENTITY(1,1) NOT NULL,
                        StaffSeparationId INT NOT NULL,
                        Domain NVARCHAR(50) NOT NULL,
                        TaskName NVARCHAR(200) NOT NULL,
                        TaskDescription NVARCHAR(1000) NULL,
                        AssignedOrganisationStructureId INT NULL,
                        AssignedToUserId INT NULL,
                        AssignedToRoleId INT NULL,
                        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_SEP_ClearanceTasks_Status DEFAULT ('Pending'),
                        IsMandatory BIT NOT NULL CONSTRAINT DF_SEP_ClearanceTasks_IsMandatory DEFAULT (1),
                        SequenceOrder INT NOT NULL CONSTRAINT DF_SEP_ClearanceTasks_SequenceOrder DEFAULT (0),
                        ClearedByUserId INT NULL,
                        ClearedAtUtc DATETIME2 NULL,
                        Remarks NVARCHAR(MAX) NULL,
                        WaiverReason NVARCHAR(MAX) NULL,
                        OutstandingObligation BIT NOT NULL CONSTRAINT DF_SEP_ClearanceTasks_OutstandingObligation DEFAULT (0),
                        OutstandingObligationAmount DECIMAL(18,2) NULL,
                        OutstandingObligationDetails NVARCHAR(MAX) NULL,
                        RowVersion ROWVERSION NOT NULL,
                        CONSTRAINT PK_SEP_ClearanceTasks PRIMARY KEY CLUSTERED (ClearanceTaskId),
                        CONSTRAINT FK_SEP_ClearanceTasks_Separations FOREIGN KEY (StaffSeparationId) REFERENCES dbo.SEP_StaffSeparations (StaffSeparationId) ON DELETE CASCADE,
                        CONSTRAINT FK_SEP_ClearanceTasks_Structures FOREIGN KEY (AssignedOrganisationStructureId) REFERENCES dbo.OrganisationStructures (OrganisationStructureID) ON DELETE SET NULL,
                        CONSTRAINT FK_SEP_ClearanceTasks_AssignedUsers FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users (UserID) ON DELETE SET NULL,
                        CONSTRAINT FK_SEP_ClearanceTasks_Roles FOREIGN KEY (AssignedToRoleId) REFERENCES dbo.Roles (RoleID) ON DELETE SET NULL,
                        CONSTRAINT FK_SEP_ClearanceTasks_ClearedUsers FOREIGN KEY (ClearedByUserId) REFERENCES dbo.Users (UserID) ON DELETE SET NULL
                    );

                    CREATE INDEX IX_SEP_ClearanceTasks_Separation_Domain ON dbo.SEP_ClearanceTasks (StaffSeparationId, Domain, Status);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SEP_WorkHandovers')
                BEGIN
                    CREATE TABLE dbo.SEP_WorkHandovers (
                        WorkHandoverId INT IDENTITY(1,1) NOT NULL,
                        StaffSeparationId INT NOT NULL,
                        SupervisorIndividualId INT NOT NULL,
                        HodIndividualId INT NULL,
                        SupervisorStatus NVARCHAR(50) NOT NULL CONSTRAINT DF_SEP_WorkHandovers_SupervisorStatus DEFAULT ('Pending'),
                        SupervisorReviewedAtUtc DATETIME2 NULL,
                        SupervisorComments NVARCHAR(MAX) NULL,
                        HodStatus NVARCHAR(50) NOT NULL CONSTRAINT DF_SEP_WorkHandovers_HodStatus DEFAULT ('Pending'),
                        HodReviewedAtUtc DATETIME2 NULL,
                        HodComments NVARCHAR(MAX) NULL,
                        AllItemsCompleted BIT NOT NULL CONSTRAINT DF_SEP_WorkHandovers_AllItemsCompleted DEFAULT (0),
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
                        StaffSeparationId INT NOT NULL,
                        ItemType NVARCHAR(50) NOT NULL,
                        ItemTitle NVARCHAR(250) NOT NULL,
                        Details NVARCHAR(MAX) NULL,
                        RecipientIndividualId INT NOT NULL,
                        TargetCompletionDate DATE NOT NULL,
                        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_SEP_WorkHandoverItems_Status DEFAULT ('Pending'),
                        VerifiedByIndividualId INT NULL,
                        VerifiedAtUtc DATETIME2 NULL,
                        HandoverNotes NVARCHAR(MAX) NULL,
                        CONSTRAINT PK_SEP_WorkHandoverItems PRIMARY KEY CLUSTERED (WorkHandoverItemId),
                        CONSTRAINT FK_SEP_WorkHandoverItems_Handovers FOREIGN KEY (WorkHandoverId) REFERENCES dbo.SEP_WorkHandovers (WorkHandoverId) ON DELETE CASCADE,
                        CONSTRAINT FK_SEP_WorkHandoverItems_Separations FOREIGN KEY (StaffSeparationId) REFERENCES dbo.SEP_StaffSeparations (StaffSeparationId),
                        CONSTRAINT FK_SEP_WorkHandoverItems_Recipients FOREIGN KEY (RecipientIndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID),
                        CONSTRAINT FK_SEP_WorkHandoverItems_Verifiers FOREIGN KEY (VerifiedByIndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID)
                    );

                    CREATE INDEX IX_SEP_WorkHandoverItems_HandoverId ON dbo.SEP_WorkHandoverItems (WorkHandoverId, Status);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SEP_AssetClearances')
                BEGIN
                    CREATE TABLE dbo.SEP_AssetClearances (
                        SeparationAssetClearanceId INT IDENTITY(1,1) NOT NULL,
                        StaffSeparationId INT NOT NULL,
                        AssetCustodyId INT NULL,
                        AssetItemId INT NOT NULL,
                        AssetReturnId INT NULL,
                        AssetReturnItemId INT NULL,
                        ConditionOnReturnId INT NULL,
                        DisposalAction NVARCHAR(50) NOT NULL CONSTRAINT DF_SEP_AssetClearances_DisposalAction DEFAULT ('PendingReturn'),
                        AssessedDamageCost DECIMAL(18,2) NULL,
                        DeductionFromFinalSettlement BIT NOT NULL CONSTRAINT DF_SEP_AssetClearances_Deduction DEFAULT (0),
                        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_SEP_AssetClearances_Status DEFAULT ('PendingReturn'),
                        VerifiedByUserId INT NULL,
                        VerifiedAtUtc DATETIME2 NULL,
                        Remarks NVARCHAR(MAX) NULL,
                        CONSTRAINT PK_SEP_AssetClearances PRIMARY KEY CLUSTERED (SeparationAssetClearanceId),
                        CONSTRAINT FK_SEP_AssetClearances_Separations FOREIGN KEY (StaffSeparationId) REFERENCES dbo.SEP_StaffSeparations (StaffSeparationId) ON DELETE CASCADE,
                        CONSTRAINT FK_SEP_AssetClearances_Custodies FOREIGN KEY (AssetCustodyId) REFERENCES dbo.AM_AssetCustodies (AssetCustodyId) ON DELETE SET NULL,
                        CONSTRAINT FK_SEP_AssetClearances_Items FOREIGN KEY (AssetItemId) REFERENCES dbo.AM_AssetItems (AssetItemId),
                        CONSTRAINT FK_SEP_AssetClearances_Returns FOREIGN KEY (AssetReturnId) REFERENCES dbo.AM_AssetReturns (AssetReturnId) ON DELETE SET NULL,
                        CONSTRAINT FK_SEP_AssetClearances_Conditions FOREIGN KEY (ConditionOnReturnId) REFERENCES dbo.AM_AssetConditions (AssetConditionId) ON DELETE SET NULL,
                        CONSTRAINT FK_SEP_AssetClearances_Verifiers FOREIGN KEY (VerifiedByUserId) REFERENCES dbo.Users (UserID) ON DELETE SET NULL
                    );

                    CREATE INDEX IX_SEP_AssetClearances_Separation_Status ON dbo.SEP_AssetClearances (StaffSeparationId, Status);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SEP_FinanceItems')
                BEGIN
                    CREATE TABLE dbo.SEP_FinanceItems (
                        SeparationFinanceItemId INT IDENTITY(1,1) NOT NULL,
                        StaffSeparationId INT NOT NULL,
                        Category NVARCHAR(50) NOT NULL,
                        Description NVARCHAR(250) NOT NULL,
                        IsCredit BIT NOT NULL,
                        Amount DECIMAL(18,2) NOT NULL CONSTRAINT CK_SEP_FinanceItems_Amount CHECK (Amount >= 0),
                        IsCleared BIT NOT NULL CONSTRAINT DF_SEP_FinanceItems_IsCleared DEFAULT (0),
                        PayrollBatchReference NVARCHAR(100) NULL,
                        Remarks NVARCHAR(MAX) NULL,
                        VerifiedByUserId INT NULL,
                        VerifiedAtUtc DATETIME2 NULL,
                        CONSTRAINT PK_SEP_FinanceItems PRIMARY KEY CLUSTERED (SeparationFinanceItemId),
                        CONSTRAINT FK_SEP_FinanceItems_Separations FOREIGN KEY (StaffSeparationId) REFERENCES dbo.SEP_StaffSeparations (StaffSeparationId) ON DELETE CASCADE,
                        CONSTRAINT FK_SEP_FinanceItems_Verifiers FOREIGN KEY (VerifiedByUserId) REFERENCES dbo.Users (UserID) ON DELETE SET NULL
                    );

                    CREATE INDEX IX_SEP_FinanceItems_Separation_Category ON dbo.SEP_FinanceItems (StaffSeparationId, Category, IsCleared);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SEP_AccessRevocations')
                BEGIN
                    CREATE TABLE dbo.SEP_AccessRevocations (
                        SeparationAccessRevocationId INT IDENTITY(1,1) NOT NULL,
                        StaffSeparationId INT NOT NULL,
                        SystemType NVARCHAR(50) NOT NULL,
                        SystemName NVARCHAR(150) NOT NULL,
                        AccountIdentifier NVARCHAR(150) NOT NULL,
                        DoorBuildingName NVARCHAR(150) NULL,
                        DoorCardNumber NVARCHAR(100) NULL,
                        RevocationAction NVARCHAR(50) NOT NULL,
                        ScheduledRevocationDate DATE NOT NULL,
                        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_SEP_AccessRevocations_Status DEFAULT ('Pending'),
                        RevokedAtUtc DATETIME2 NULL,
                        RevokedByUserId INT NULL,
                        ExecutionNotes NVARCHAR(MAX) NULL,
                        CONSTRAINT PK_SEP_AccessRevocations PRIMARY KEY CLUSTERED (SeparationAccessRevocationId),
                        CONSTRAINT FK_SEP_AccessRevocations_Separations FOREIGN KEY (StaffSeparationId) REFERENCES dbo.SEP_StaffSeparations (StaffSeparationId) ON DELETE CASCADE,
                        CONSTRAINT FK_SEP_AccessRevocations_Verifiers FOREIGN KEY (RevokedByUserId) REFERENCES dbo.Users (UserID) ON DELETE SET NULL
                    );

                    CREATE INDEX IX_SEP_AccessRevocations_Separation_Status ON dbo.SEP_AccessRevocations (StaffSeparationId, Status);
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
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS dbo.SEP_SeparationEvents;
                DROP TABLE IF EXISTS dbo.SEP_SeparationDocuments;
                DROP TABLE IF EXISTS dbo.SEP_AccessRevocations;
                DROP TABLE IF EXISTS dbo.SEP_FinanceItems;
                DROP TABLE IF EXISTS dbo.SEP_AssetClearances;
                DROP TABLE IF EXISTS dbo.SEP_WorkHandoverItems;
                DROP TABLE IF EXISTS dbo.SEP_WorkHandovers;
                DROP TABLE IF EXISTS dbo.SEP_ClearanceTasks;
                DROP TABLE IF EXISTS dbo.SEP_StaffSeparations;
                DROP TABLE IF EXISTS dbo.SEP_ClearanceTemplateTasks;
                DROP TABLE IF EXISTS dbo.SEP_ClearanceTemplates;
                DROP TABLE IF EXISTS dbo.SEP_SeparationTypes;
            ");
        }
    }
}

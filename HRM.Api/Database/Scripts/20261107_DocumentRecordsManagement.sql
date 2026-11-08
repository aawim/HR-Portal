-- 20261107_DocumentRecordsManagement.sql
-- Feature Prefix: DMS_ (Enterprise Document & Records Management System)

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DMS_Categories')
BEGIN
    CREATE TABLE dbo.DMS_Categories (
        CategoryId INT IDENTITY(1,1) NOT NULL,
        OrganisationId INT NULL,
        Code NVARCHAR(60) NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        Description NVARCHAR(500) NULL,
        TargetDomain NVARCHAR(50) NOT NULL CONSTRAINT DF_DMS_Categories_TargetDomain DEFAULT ('General'),
        DisplayOrder INT NOT NULL CONSTRAINT DF_DMS_Categories_DisplayOrder DEFAULT (0),
        IsSystemCategory BIT NOT NULL CONSTRAINT DF_DMS_Categories_IsSystem DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_DMS_Categories_IsActive DEFAULT (1),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DMS_Categories_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedByUserId INT NULL,
        UpdatedAtUtc DATETIME2 NULL,
        CONSTRAINT PK_DMS_Categories PRIMARY KEY CLUSTERED (CategoryId ASC),
        CONSTRAINT FK_DMS_Categories_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_DMS_Categories_Org_Code ON dbo.DMS_Categories (OrganisationId, Code);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DMS_RetentionPolicies')
BEGIN
    CREATE TABLE dbo.DMS_RetentionPolicies (
        RetentionPolicyId INT IDENTITY(1,1) NOT NULL,
        OrganisationId INT NULL,
        Code NVARCHAR(60) NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        Description NVARCHAR(500) NULL,
        RetentionMonths INT NULL,
        TriggerEvent NVARCHAR(40) NOT NULL CONSTRAINT DF_DMS_RetentionPolicies_Trigger DEFAULT ('CreationDate'),
        ArchiveAfterMonths INT NULL,
        DispositionAction NVARCHAR(40) NOT NULL CONSTRAINT DF_DMS_RetentionPolicies_Disposition DEFAULT ('PermanentKeep'),
        RequiresApprovalToDispose BIT NOT NULL CONSTRAINT DF_DMS_RetentionPolicies_ReqApp DEFAULT (1),
        PolicyReferenceNumber NVARCHAR(100) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_DMS_RetentionPolicies_IsActive DEFAULT (1),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DMS_RetentionPolicies_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedByUserId INT NULL,
        UpdatedAtUtc DATETIME2 NULL,
        CONSTRAINT PK_DMS_RetentionPolicies PRIMARY KEY CLUSTERED (RetentionPolicyId ASC),
        CONSTRAINT FK_DMS_RetentionPolicies_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_DMS_RetentionPolicies_Org_Code ON dbo.DMS_RetentionPolicies (OrganisationId, Code);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DMS_DocumentTypes')
BEGIN
    CREATE TABLE dbo.DMS_DocumentTypes (
        DmsDocumentTypeId INT IDENTITY(1,1) NOT NULL,
        CategoryId INT NOT NULL,
        LegacyDocumentTypeId INT NULL,
        OrganisationId INT NULL,
        Code NVARCHAR(60) NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        NameDhivehi NVARCHAR(150) NULL,
        Description NVARCHAR(500) NULL,
        DefaultClassification NVARCHAR(40) NOT NULL CONSTRAINT DF_DMS_DocumentTypes_Class DEFAULT ('Internal'),
        DefaultAccessScope NVARCHAR(40) NOT NULL CONSTRAINT DF_DMS_DocumentTypes_Scope DEFAULT ('EmployeeAndHr'),
        PermittedExtensions NVARCHAR(200) NOT NULL CONSTRAINT DF_DMS_DocumentTypes_Ext DEFAULT ('.pdf,.png,.jpg,.jpeg,.docx,.xlsx'),
        MaxFileSizeMb INT NOT NULL CONSTRAINT DF_DMS_DocumentTypes_MaxMb DEFAULT (15),
        RequiredMetadataJson NVARCHAR(MAX) NULL,
        RequiresApproval BIT NOT NULL CONSTRAINT DF_DMS_DocumentTypes_ReqApproval DEFAULT (0),
        RequiresVerification BIT NOT NULL CONSTRAINT DF_DMS_DocumentTypes_ReqVerify DEFAULT (0),
        RequiresSignature BIT NOT NULL CONSTRAINT DF_DMS_DocumentTypes_ReqSign DEFAULT (0),
        IsOfficialRecordWhenApproved BIT NOT NULL CONSTRAINT DF_DMS_DocumentTypes_Official DEFAULT (0),
        AllowEmployeeUpload BIT NOT NULL CONSTRAINT DF_DMS_DocumentTypes_EmpUpload DEFAULT (1),
        AllowEmployeeRequest BIT NOT NULL CONSTRAINT DF_DMS_DocumentTypes_EmpRequest DEFAULT (0),
        RetentionPolicyId INT NULL,
        IsSystemType BIT NOT NULL CONSTRAINT DF_DMS_DocumentTypes_IsSystem DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_DMS_DocumentTypes_IsActive DEFAULT (1),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DMS_DocumentTypes_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        CONSTRAINT PK_DMS_DocumentTypes PRIMARY KEY CLUSTERED (DmsDocumentTypeId ASC),
        CONSTRAINT FK_DMS_DocumentTypes_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.DMS_Categories (CategoryId),
        CONSTRAINT FK_DMS_DocumentTypes_LegacyDocumentTypes FOREIGN KEY (LegacyDocumentTypeId) REFERENCES dbo.DocumentTypes (DocumentTypeID),
        CONSTRAINT FK_DMS_DocumentTypes_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_DMS_DocumentTypes_RetentionPolicies FOREIGN KEY (RetentionPolicyId) REFERENCES dbo.DMS_RetentionPolicies (RetentionPolicyId)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_DMS_DocumentTypes_Org_Code ON dbo.DMS_DocumentTypes (OrganisationId, Code);
    CREATE NONCLUSTERED INDEX IX_DMS_DocumentTypes_Category ON dbo.DMS_DocumentTypes (CategoryId, IsActive);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DMS_StoredObjects')
BEGIN
    CREATE TABLE dbo.DMS_StoredObjects (
        StoredObjectId BIGINT IDENTITY(1,1) NOT NULL,
        StorageKey NVARCHAR(80) NOT NULL,
        OrganisationId INT NOT NULL,
        ContentType NVARCHAR(120) NOT NULL,
        FileExtension NVARCHAR(20) NOT NULL,
        FileSizeBytes BIGINT NOT NULL,
        Sha256Hash NVARCHAR(64) NOT NULL,
        StorageProvider NVARCHAR(50) NOT NULL CONSTRAINT DF_DMS_StoredObjects_Provider DEFAULT ('SqlServerPrivateStore'),
        BinaryContent VARBINARY(MAX) NOT NULL,
        MalwareScanStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_DMS_StoredObjects_Scan DEFAULT ('Clean'),
        MalwareScanDetails NVARCHAR(500) NULL,
        ScannedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DMS_StoredObjects_ScannedAt DEFAULT (SYSUTCDATETIME()),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DMS_StoredObjects_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedByUserId INT NOT NULL,
        CONSTRAINT PK_DMS_StoredObjects PRIMARY KEY CLUSTERED (StoredObjectId ASC),
        CONSTRAINT UQ_DMS_StoredObjects_StorageKey UNIQUE (StorageKey),
        CONSTRAINT FK_DMS_StoredObjects_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID)
    );

    CREATE NONCLUSTERED INDEX IX_DMS_StoredObjects_Org_Hash ON dbo.DMS_StoredObjects (OrganisationId, Sha256Hash);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DMS_Documents')
BEGIN
    CREATE TABLE dbo.DMS_Documents (
        DocumentId BIGINT IDENTITY(1,1) NOT NULL,
        LegacyDocumentId INT NULL,
        DocumentNumber NVARCHAR(60) NOT NULL,
        OrganisationId INT NOT NULL,
        DepartmentStructureId INT NULL,
        SubjectIndividualId INT NULL,
        SubjectJobId INT NULL,
        CategoryId INT NOT NULL,
        DmsDocumentTypeId INT NOT NULL,
        Title NVARCHAR(250) NOT NULL,
        Description NVARCHAR(1000) NULL,
        ReferenceCode NVARCHAR(100) NULL,
        Classification NVARCHAR(40) NOT NULL CONSTRAINT DF_DMS_Documents_Class DEFAULT ('Internal'),
        AccessScope NVARCHAR(40) NOT NULL CONSTRAINT DF_DMS_Documents_Scope DEFAULT ('EmployeeAndHr'),
        Status NVARCHAR(40) NOT NULL CONSTRAINT DF_DMS_Documents_Status DEFAULT ('Active'),
        CurrentVersionNumber INT NOT NULL CONSTRAINT DF_DMS_Documents_Ver DEFAULT (1),
        CurrentVersionId BIGINT NULL,
        IsOfficialRecord BIT NOT NULL CONSTRAINT DF_DMS_Documents_Official DEFAULT (0),
        IsImmutable BIT NOT NULL CONSTRAINT DF_DMS_Documents_Immutable DEFAULT (0),
        IsLockedForEdit BIT NOT NULL CONSTRAINT DF_DMS_Documents_Locked DEFAULT (0),
        CheckedOutByUserId INT NULL,
        CheckedOutByUserName NVARCHAR(200) NULL,
        CheckedOutAtUtc DATETIME2 NULL,
        CheckOutNote NVARCHAR(500) NULL,
        DocumentDate DATETIME2 NULL,
        EffectiveFromDate DATETIME2 NULL,
        ExpiryDate DATETIME2 NULL,
        Tags NVARCHAR(500) NULL,
        CustomMetadataJson NVARCHAR(MAX) NULL,
        LinkedRequestId INT NULL,
        IsVerified BIT NOT NULL CONSTRAINT DF_DMS_Documents_Verified DEFAULT (0),
        VerifiedByUserId INT NULL,
        VerifiedByUserName NVARCHAR(200) NULL,
        VerifiedAtUtc DATETIME2 NULL,
        VerificationNotes NVARCHAR(500) NULL,
        ApprovedByUserId INT NULL,
        ApprovedByUserName NVARCHAR(200) NULL,
        ApprovedAtUtc DATETIME2 NULL,
        SupersededByDocumentId BIGINT NULL,
        RevokedReason NVARCHAR(500) NULL,
        RevokedAtUtc DATETIME2 NULL,
        RevokedByUserId INT NULL,
        RetentionPolicyId INT NULL,
        RetentionDueDateUtc DATETIME2 NULL,
        ArchivedAtUtc DATETIME2 NULL,
        ArchivedByUserId INT NULL,
        DisposedAtUtc DATETIME2 NULL,
        DisposedByUserId INT NULL,
        DisposalCertificateRef NVARCHAR(100) NULL,
        HasActiveLegalHold BIT NOT NULL CONSTRAINT DF_DMS_Documents_LegalHold DEFAULT (0),
        IsDeleted BIT NOT NULL CONSTRAINT DF_DMS_Documents_Deleted DEFAULT (0),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DMS_Documents_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedByUserId INT NOT NULL,
        CreatedByUserName NVARCHAR(200) NOT NULL,
        UpdatedAtUtc DATETIME2 NULL,
        UpdatedByUserId INT NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT PK_DMS_Documents PRIMARY KEY CLUSTERED (DocumentId ASC),
        CONSTRAINT UQ_DMS_Documents_DocumentNumber UNIQUE (DocumentNumber),
        CONSTRAINT FK_DMS_Documents_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_DMS_Documents_Individuals FOREIGN KEY (SubjectIndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID),
        CONSTRAINT FK_DMS_Documents_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.DMS_Categories (CategoryId),
        CONSTRAINT FK_DMS_Documents_DocumentTypes FOREIGN KEY (DmsDocumentTypeId) REFERENCES dbo.DMS_DocumentTypes (DmsDocumentTypeId),
        CONSTRAINT FK_DMS_Documents_RetentionPolicies FOREIGN KEY (RetentionPolicyId) REFERENCES dbo.DMS_RetentionPolicies (RetentionPolicyId),
        CONSTRAINT FK_DMS_Documents_Requests FOREIGN KEY (LinkedRequestId) REFERENCES dbo.Requests (RequestID)
    );

    CREATE NONCLUSTERED INDEX IX_DMS_Documents_Org_Status ON dbo.DMS_Documents (OrganisationId, Status, IsDeleted);
    CREATE NONCLUSTERED INDEX IX_DMS_Documents_Org_Individual ON dbo.DMS_Documents (OrganisationId, SubjectIndividualId, IsDeleted);
    CREATE NONCLUSTERED INDEX IX_DMS_Documents_Org_Category_Type ON dbo.DMS_Documents (OrganisationId, CategoryId, DmsDocumentTypeId);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DMS_DocumentVersions')
BEGIN
    CREATE TABLE dbo.DMS_DocumentVersions (
        DocumentVersionId BIGINT IDENTITY(1,1) NOT NULL,
        DocumentId BIGINT NOT NULL,
        VersionNumber INT NOT NULL,
        VersionLabel NVARCHAR(30) NOT NULL,
        StoredObjectId BIGINT NOT NULL,
        OriginalFileName NVARCHAR(260) NOT NULL,
        ContentType NVARCHAR(120) NOT NULL,
        FileExtension NVARCHAR(20) NOT NULL,
        FileSizeBytes BIGINT NOT NULL,
        Sha256Hash NVARCHAR(64) NOT NULL,
        ChangeComments NVARCHAR(1000) NULL,
        ContentSnapshotJson NVARCHAR(MAX) NULL,
        IsCurrentVersion BIT NOT NULL CONSTRAINT DF_DMS_DocumentVersions_Current DEFAULT (1),
        IsOfficialSnapshot BIT NOT NULL CONSTRAINT DF_DMS_DocumentVersions_Official DEFAULT (0),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DMS_DocumentVersions_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedByUserId INT NOT NULL,
        CreatedByUserName NVARCHAR(200) NOT NULL,
        CONSTRAINT PK_DMS_DocumentVersions PRIMARY KEY CLUSTERED (DocumentVersionId ASC),
        CONSTRAINT FK_DMS_DocumentVersions_Documents FOREIGN KEY (DocumentId) REFERENCES dbo.DMS_Documents (DocumentId),
        CONSTRAINT FK_DMS_DocumentVersions_StoredObjects FOREIGN KEY (StoredObjectId) REFERENCES dbo.DMS_StoredObjects (StoredObjectId)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_DMS_DocumentVersions_Doc_Ver ON dbo.DMS_DocumentVersions (DocumentId, VersionNumber);
    CREATE NONCLUSTERED INDEX IX_DMS_DocumentVersions_Doc_Current ON dbo.DMS_DocumentVersions (DocumentId, IsCurrentVersion);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DMS_DocumentLinks')
BEGIN
    CREATE TABLE dbo.DMS_DocumentLinks (
        DocumentLinkId BIGINT IDENTITY(1,1) NOT NULL,
        DocumentId BIGINT NOT NULL,
        OrganisationId INT NOT NULL,
        TargetEntityType NVARCHAR(50) NOT NULL,
        TargetEntityId BIGINT NOT NULL,
        TargetReferenceCode NVARCHAR(100) NULL,
        TargetDisplayName NVARCHAR(250) NULL,
        LinkRelationship NVARCHAR(50) NOT NULL CONSTRAINT DF_DMS_DocumentLinks_Rel DEFAULT ('Primary'),
        Notes NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_DMS_DocumentLinks_Active DEFAULT (1),
        LinkedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DMS_DocumentLinks_LinkedAt DEFAULT (SYSUTCDATETIME()),
        LinkedByUserId INT NOT NULL,
        LinkedByUserName NVARCHAR(200) NOT NULL,
        CONSTRAINT PK_DMS_DocumentLinks PRIMARY KEY CLUSTERED (DocumentLinkId ASC),
        CONSTRAINT FK_DMS_DocumentLinks_Documents FOREIGN KEY (DocumentId) REFERENCES dbo.DMS_Documents (DocumentId),
        CONSTRAINT FK_DMS_DocumentLinks_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID)
    );

    CREATE NONCLUSTERED INDEX IX_DMS_DocumentLinks_Target ON dbo.DMS_DocumentLinks (OrganisationId, TargetEntityType, TargetEntityId, IsActive);
    CREATE NONCLUSTERED INDEX IX_DMS_DocumentLinks_Doc ON dbo.DMS_DocumentLinks (DocumentId, TargetEntityType, TargetEntityId);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DMS_DocumentPermissions')
BEGIN
    CREATE TABLE dbo.DMS_DocumentPermissions (
        DocumentPermissionId BIGINT IDENTITY(1,1) NOT NULL,
        DocumentId BIGINT NOT NULL,
        OrganisationId INT NOT NULL,
        PrincipalType NVARCHAR(40) NOT NULL,
        PrincipalId INT NULL,
        PrincipalName NVARCHAR(200) NULL,
        PermissionLevel NVARCHAR(40) NOT NULL,
        ExpiresAtUtc DATETIME2 NULL,
        GrantReason NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_DMS_DocumentPermissions_Active DEFAULT (1),
        GrantedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DMS_DocumentPermissions_GrantedAt DEFAULT (SYSUTCDATETIME()),
        GrantedByUserId INT NOT NULL,
        GrantedByUserName NVARCHAR(200) NOT NULL,
        CONSTRAINT PK_DMS_DocumentPermissions PRIMARY KEY CLUSTERED (DocumentPermissionId ASC),
        CONSTRAINT FK_DMS_DocumentPermissions_Documents FOREIGN KEY (DocumentId) REFERENCES dbo.DMS_Documents (DocumentId)
    );

    CREATE NONCLUSTERED INDEX IX_DMS_DocumentPermissions_Doc ON dbo.DMS_DocumentPermissions (DocumentId, IsActive);
    CREATE NONCLUSTERED INDEX IX_DMS_DocumentPermissions_Principal ON dbo.DMS_DocumentPermissions (OrganisationId, PrincipalType, PrincipalId);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DMS_DocumentEvents')
BEGIN
    CREATE TABLE dbo.DMS_DocumentEvents (
        DocumentEventId BIGINT IDENTITY(1,1) NOT NULL,
        DocumentId BIGINT NOT NULL,
        DocumentVersionId BIGINT NULL,
        OrganisationId INT NOT NULL,
        EventType NVARCHAR(50) NOT NULL,
        EventDescription NVARCHAR(1000) NOT NULL,
        PreviousStateJson NVARCHAR(MAX) NULL,
        NewStateJson NVARCHAR(MAX) NULL,
        PerformedByUserId INT NOT NULL,
        PerformedByUserName NVARCHAR(200) NOT NULL,
        IpAddress NVARCHAR(60) NULL,
        OccurredAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DMS_DocumentEvents_OccurredAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_DMS_DocumentEvents PRIMARY KEY CLUSTERED (DocumentEventId ASC),
        CONSTRAINT FK_DMS_DocumentEvents_Documents FOREIGN KEY (DocumentId) REFERENCES dbo.DMS_Documents (DocumentId)
    );

    CREATE NONCLUSTERED INDEX IX_DMS_DocumentEvents_Doc ON dbo.DMS_DocumentEvents (DocumentId, OccurredAtUtc DESC);
    CREATE NONCLUSTERED INDEX IX_DMS_DocumentEvents_Org ON dbo.DMS_DocumentEvents (OrganisationId, OccurredAtUtc DESC);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DMS_DocumentTemplates')
BEGIN
    CREATE TABLE dbo.DMS_DocumentTemplates (
        DocumentTemplateId INT IDENTITY(1,1) NOT NULL,
        OrganisationId INT NULL,
        CategoryId INT NULL,
        DmsDocumentTypeId INT NULL,
        Code NVARCHAR(60) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL,
        VersionNumber INT NOT NULL CONSTRAINT DF_DMS_DocumentTemplates_Ver DEFAULT (1),
        DocumentTitlePattern NVARCHAR(250) NOT NULL,
        HeaderText NVARCHAR(1000) NULL,
        BodyTemplate NVARCHAR(MAX) NOT NULL,
        FooterText NVARCHAR(1000) NULL,
        SignatoryName NVARCHAR(200) NULL,
        SignatoryTitle NVARCHAR(200) NULL,
        IncludeOrganisationLetterhead BIT NOT NULL CONSTRAINT DF_DMS_DocumentTemplates_Letterhead DEFAULT (1),
        IncludePositionHistoryTable BIT NOT NULL CONSTRAINT DF_DMS_DocumentTemplates_PosTable DEFAULT (0),
        IncludeQrVerification BIT NOT NULL CONSTRAINT DF_DMS_DocumentTemplates_Qr DEFAULT (1),
        IsEmployeeRequestable BIT NOT NULL CONSTRAINT DF_DMS_DocumentTemplates_EmpReq DEFAULT (1),
        IsActive BIT NOT NULL CONSTRAINT DF_DMS_DocumentTemplates_Active DEFAULT (1),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DMS_DocumentTemplates_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedByUserId INT NULL,
        UpdatedAtUtc DATETIME2 NULL,
        CONSTRAINT PK_DMS_DocumentTemplates PRIMARY KEY CLUSTERED (DocumentTemplateId ASC),
        CONSTRAINT FK_DMS_DocumentTemplates_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_DMS_DocumentTemplates_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.DMS_Categories (CategoryId),
        CONSTRAINT FK_DMS_DocumentTemplates_DocumentTypes FOREIGN KEY (DmsDocumentTypeId) REFERENCES dbo.DMS_DocumentTypes (DmsDocumentTypeId)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_DMS_DocumentTemplates_Org_Code_Ver ON dbo.DMS_DocumentTemplates (OrganisationId, Code, VersionNumber);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DMS_DocumentRequests')
BEGIN
    CREATE TABLE dbo.DMS_DocumentRequests (
        DocumentRequestId INT IDENTITY(1,1) NOT NULL,
        RequestId INT NOT NULL,
        OrganisationId INT NOT NULL,
        ApplicantIndividualId INT NOT NULL,
        SubjectJobId INT NULL,
        DmsDocumentTypeId INT NULL,
        DocumentTemplateId INT NULL,
        RequestKind NVARCHAR(60) NOT NULL,
        Purpose NVARCHAR(200) NOT NULL,
        AddressedTo NVARCHAR(200) NULL CONSTRAINT DF_DMS_DocumentRequests_AddressedTo DEFAULT ('To Whom It May Concern'),
        EmployeeNotes NVARCHAR(1000) NULL,
        IncludePositionHistory BIT NOT NULL CONSTRAINT DF_DMS_DocumentRequests_PosHist DEFAULT (0),
        IncludeSalaryDetails BIT NOT NULL CONSTRAINT DF_DMS_DocumentRequests_Salary DEFAULT (0),
        Status NVARCHAR(40) NOT NULL CONSTRAINT DF_DMS_DocumentRequests_Status DEFAULT ('Submitted'),
        ResolvedSnapshotJson NVARCHAR(MAX) NULL,
        GeneratedBodyText NVARCHAR(MAX) NULL,
        IssuedDocumentId BIGINT NULL,
        IssuedDocumentNumber NVARCHAR(60) NULL,
        VerificationToken NVARCHAR(64) NULL,
        ReviewedByUserId INT NULL,
        ReviewedByUserName NVARCHAR(200) NULL,
        ReviewedAtUtc DATETIME2 NULL,
        ReviewComments NVARCHAR(1000) NULL,
        ApprovedByUserId INT NULL,
        ApprovedByUserName NVARCHAR(200) NULL,
        ApprovedAtUtc DATETIME2 NULL,
        IssuedAtUtc DATETIME2 NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DMS_DocumentRequests_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedByUserId INT NOT NULL,
        UpdatedAtUtc DATETIME2 NULL,
        CONSTRAINT PK_DMS_DocumentRequests PRIMARY KEY CLUSTERED (DocumentRequestId ASC),
        CONSTRAINT FK_DMS_DocumentRequests_Requests FOREIGN KEY (RequestId) REFERENCES dbo.Requests (RequestID),
        CONSTRAINT FK_DMS_DocumentRequests_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT FK_DMS_DocumentRequests_Individuals FOREIGN KEY (ApplicantIndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID),
        CONSTRAINT FK_DMS_DocumentRequests_DocumentTypes FOREIGN KEY (DmsDocumentTypeId) REFERENCES dbo.DMS_DocumentTypes (DmsDocumentTypeId),
        CONSTRAINT FK_DMS_DocumentRequests_Templates FOREIGN KEY (DocumentTemplateId) REFERENCES dbo.DMS_DocumentTemplates (DocumentTemplateId),
        CONSTRAINT FK_DMS_DocumentRequests_IssuedDocuments FOREIGN KEY (IssuedDocumentId) REFERENCES dbo.DMS_Documents (DocumentId)
    );

    CREATE NONCLUSTERED INDEX IX_DMS_DocumentRequests_Request ON dbo.DMS_DocumentRequests (RequestId);
    CREATE NONCLUSTERED INDEX IX_DMS_DocumentRequests_Org_Status ON dbo.DMS_DocumentRequests (OrganisationId, Status);
    CREATE NONCLUSTERED INDEX IX_DMS_DocumentRequests_Applicant ON dbo.DMS_DocumentRequests (ApplicantIndividualId, Status);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DMS_LegalHolds')
BEGIN
    CREATE TABLE dbo.DMS_LegalHolds (
        LegalHoldId INT IDENTITY(1,1) NOT NULL,
        DocumentId BIGINT NOT NULL,
        OrganisationId INT NOT NULL,
        HoldReference NVARCHAR(80) NOT NULL,
        CaseOrMatterName NVARCHAR(200) NOT NULL,
        HoldReason NVARCHAR(1000) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_DMS_LegalHolds_Active DEFAULT (1),
        PlacedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DMS_LegalHolds_PlacedAt DEFAULT (SYSUTCDATETIME()),
        PlacedByUserId INT NOT NULL,
        PlacedByUserName NVARCHAR(200) NOT NULL,
        ReleasedAtUtc DATETIME2 NULL,
        ReleasedByUserId INT NULL,
        ReleasedByUserName NVARCHAR(200) NULL,
        ReleaseNotes NVARCHAR(1000) NULL,
        CONSTRAINT PK_DMS_LegalHolds PRIMARY KEY CLUSTERED (LegalHoldId ASC),
        CONSTRAINT FK_DMS_LegalHolds_Documents FOREIGN KEY (DocumentId) REFERENCES dbo.DMS_Documents (DocumentId),
        CONSTRAINT FK_DMS_LegalHolds_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID)
    );

    CREATE NONCLUSTERED INDEX IX_DMS_LegalHolds_Doc ON dbo.DMS_LegalHolds (DocumentId, IsActive);
    CREATE NONCLUSTERED INDEX IX_DMS_LegalHolds_Org ON dbo.DMS_LegalHolds (OrganisationId, IsActive);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DMS_DocumentSignatures')
BEGIN
    CREATE TABLE dbo.DMS_DocumentSignatures (
        DocumentSignatureId BIGINT IDENTITY(1,1) NOT NULL,
        DocumentId BIGINT NOT NULL,
        DocumentVersionId BIGINT NOT NULL,
        OrganisationId INT NOT NULL,
        SignerUserId INT NOT NULL,
        SignerName NVARCHAR(200) NOT NULL,
        SignerTitle NVARCHAR(200) NOT NULL,
        SignatureRole NVARCHAR(50) NOT NULL,
        SignatureStatus NVARCHAR(30) NOT NULL,
        ContentSha256AtSigning NVARCHAR(64) NOT NULL,
        SignatureDigest NVARCHAR(128) NOT NULL,
        Remarks NVARCHAR(500) NULL,
        SignedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DMS_DocumentSignatures_SignedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_DMS_DocumentSignatures PRIMARY KEY CLUSTERED (DocumentSignatureId ASC),
        CONSTRAINT FK_DMS_DocumentSignatures_Documents FOREIGN KEY (DocumentId) REFERENCES dbo.DMS_Documents (DocumentId),
        CONSTRAINT FK_DMS_DocumentSignatures_Versions FOREIGN KEY (DocumentVersionId) REFERENCES dbo.DMS_DocumentVersions (DocumentVersionId)
    );

    CREATE NONCLUSTERED INDEX IX_DMS_DocumentSignatures_Doc_Ver ON dbo.DMS_DocumentSignatures (DocumentId, DocumentVersionId);
    CREATE NONCLUSTERED INDEX IX_DMS_DocumentSignatures_Org ON dbo.DMS_DocumentSignatures (OrganisationId, SignedAtUtc DESC);
END;

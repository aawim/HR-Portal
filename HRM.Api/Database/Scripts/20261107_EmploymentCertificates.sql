-- 20261107_EmploymentCertificates.sql
-- Feature Prefix: EC_ (Employment and Work Experience Certificate Management)

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EC_CertificateRequests')
BEGIN
    CREATE TABLE dbo.EC_CertificateRequests (
        CertificateRequestId INT IDENTITY(1,1) NOT NULL,
        RequestId INT NOT NULL,
        IndividualId INT NOT NULL,
        OrganisationId INT NOT NULL,
        CertificateType NVARCHAR(50) NOT NULL, -- 'StandardService', 'DetailedExperience'
        Purpose NVARCHAR(100) NOT NULL,
        PurposeDetails NVARCHAR(500) NULL,
        RequestScope NVARCHAR(50) NOT NULL CONSTRAINT DF_EC_CertificateRequests_RequestScope DEFAULT ('FullCareer'),
        SpecificPeriodStart DATETIME2 NULL,
        SpecificPeriodEnd DATETIME2 NULL,
        SelectedPositionIdsJson NVARCHAR(MAX) NULL,
        SelectedProjectIdsJson NVARCHAR(MAX) NULL,
        IncludeResponsibilities BIT NOT NULL CONSTRAINT DF_EC_CertificateRequests_IncludeResp DEFAULT (1),
        IncludeProjects BIT NOT NULL CONSTRAINT DF_EC_CertificateRequests_IncludeProjects DEFAULT (0),
        IncludeSalary BIT NOT NULL CONSTRAINT DF_EC_CertificateRequests_IncludeSalary DEFAULT (0),
        AddressedTo NVARCHAR(200) NULL CONSTRAINT DF_EC_CertificateRequests_AddressedTo DEFAULT ('To Whom It May Concern'),
        EmployeeComments NVARCHAR(1000) NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_EC_CertificateRequests_Status DEFAULT ('Submitted'),
        ReviewerComments NVARCHAR(1000) NULL,
        ReturnedReason NVARCHAR(1000) NULL,
        RejectionReason NVARCHAR(1000) NULL,
        ReviewedByUserId INT NULL,
        ReviewedAt DATETIME2 NULL,
        ApprovedByUserId INT NULL,
        ApprovedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_EC_CertificateRequests_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedByUserId INT NOT NULL,
        UpdatedAt DATETIME2 NULL,
        UpdatedByUserId INT NULL,
        CONSTRAINT PK_EC_CertificateRequests PRIMARY KEY CLUSTERED (CertificateRequestId ASC),
        CONSTRAINT FK_EC_CertificateRequests_Requests FOREIGN KEY (RequestId) REFERENCES dbo.Requests (RequestId),
        CONSTRAINT FK_EC_CertificateRequests_Individuals FOREIGN KEY (IndividualId) REFERENCES dbo.Individuals (BusinessEntityID),
        CONSTRAINT FK_EC_CertificateRequests_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID)
    );

    CREATE NONCLUSTERED INDEX IX_EC_CertificateRequests_IndividualId ON dbo.EC_CertificateRequests (IndividualId, Status);
    CREATE NONCLUSTERED INDEX IX_EC_CertificateRequests_OrganisationId ON dbo.EC_CertificateRequests (OrganisationId, Status);
    CREATE NONCLUSTERED INDEX IX_EC_CertificateRequests_RequestId ON dbo.EC_CertificateRequests (RequestId);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EC_IssuedCertificates')
BEGIN
    CREATE TABLE dbo.EC_IssuedCertificates (
        IssuedCertificateId INT IDENTITY(1,1) NOT NULL,
        CertificateRequestId INT NOT NULL,
        CertificateNumber NVARCHAR(50) NOT NULL,
        VerificationToken NVARCHAR(64) NOT NULL,
        IndividualId INT NOT NULL,
        OrganisationId INT NOT NULL,
        CertificateType NVARCHAR(50) NOT NULL,
        IssueDate DATETIME2 NOT NULL,
        IssuingOfficerUserId INT NOT NULL,
        IssuingOfficerName NVARCHAR(200) NOT NULL,
        IssuingOfficerTitle NVARCHAR(200) NOT NULL,
        SignatoryName NVARCHAR(200) NOT NULL,
        SignatoryTitle NVARCHAR(200) NOT NULL,
        SnapshotJson NVARCHAR(MAX) NOT NULL,
        Sha256Hash NVARCHAR(64) NOT NULL,
        PdfContent VARBINARY(MAX) NULL,
        PdfGeneratedAt DATETIME2 NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_EC_IssuedCertificates_Status DEFAULT ('Active'), -- 'Active', 'Revoked', 'Superseded'
        RevokedReason NVARCHAR(500) NULL,
        RevokedAt DATETIME2 NULL,
        RevokedByUserId INT NULL,
        SupersededByCertificateId INT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_EC_IssuedCertificates_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_EC_IssuedCertificates PRIMARY KEY CLUSTERED (IssuedCertificateId ASC),
        CONSTRAINT FK_EC_IssuedCertificates_Requests FOREIGN KEY (CertificateRequestId) REFERENCES dbo.EC_CertificateRequests (CertificateRequestId),
        CONSTRAINT FK_EC_IssuedCertificates_Individuals FOREIGN KEY (IndividualId) REFERENCES dbo.Individuals (BusinessEntityID),
        CONSTRAINT FK_EC_IssuedCertificates_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
        CONSTRAINT UQ_EC_IssuedCertificates_CertificateNumber UNIQUE (CertificateNumber),
        CONSTRAINT UQ_EC_IssuedCertificates_VerificationToken UNIQUE (VerificationToken)
    );

    CREATE NONCLUSTERED INDEX IX_EC_IssuedCertificates_IndividualId ON dbo.EC_IssuedCertificates (IndividualId, Status);
    CREATE NONCLUSTERED INDEX IX_EC_IssuedCertificates_OrganisationId ON dbo.EC_IssuedCertificates (OrganisationId, Status);
    CREATE NONCLUSTERED INDEX IX_EC_IssuedCertificates_VerificationToken ON dbo.EC_IssuedCertificates (VerificationToken);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EC_CertificateTemplates')
BEGIN
    CREATE TABLE dbo.EC_CertificateTemplates (
        CertificateTemplateId INT IDENTITY(1,1) NOT NULL,
        OrganisationId INT NOT NULL,
        CertificateType NVARCHAR(50) NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        HeaderLetterheadHtml NVARCHAR(MAX) NULL,
        BodyIntroduction NVARCHAR(1000) NULL,
        FooterNotes NVARCHAR(1000) NULL,
        DefaultSignatoryName NVARCHAR(200) NULL,
        DefaultSignatoryTitle NVARCHAR(200) NULL,
        ShowLetterhead BIT NOT NULL CONSTRAINT DF_EC_CertificateTemplates_ShowLetterhead DEFAULT (1),
        ShowQrCode BIT NOT NULL CONSTRAINT DF_EC_CertificateTemplates_ShowQrCode DEFAULT (1),
        IsActive BIT NOT NULL CONSTRAINT DF_EC_CertificateTemplates_IsActive DEFAULT (1),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_EC_CertificateTemplates_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT PK_EC_CertificateTemplates PRIMARY KEY CLUSTERED (CertificateTemplateId ASC),
        CONSTRAINT FK_EC_CertificateTemplates_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID)
    );

    CREATE NONCLUSTERED INDEX IX_EC_CertificateTemplates_Org_Type ON dbo.EC_CertificateTemplates (OrganisationId, CertificateType);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EC_CertificateAuditLogs')
BEGIN
    CREATE TABLE dbo.EC_CertificateAuditLogs (
        CertificateAuditLogId INT IDENTITY(1,1) NOT NULL,
        CertificateRequestId INT NULL,
        IssuedCertificateId INT NULL,
        OrganisationId INT NOT NULL,
        Action NVARCHAR(50) NOT NULL,
        PerformedByUserId INT NOT NULL,
        PerformedByUserName NVARCHAR(200) NOT NULL,
        IpAddress NVARCHAR(50) NULL,
        Details NVARCHAR(1000) NULL,
        Timestamp DATETIME2 NOT NULL CONSTRAINT DF_EC_CertificateAuditLogs_Timestamp DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_EC_CertificateAuditLogs PRIMARY KEY CLUSTERED (CertificateAuditLogId ASC)
    );

    CREATE NONCLUSTERED INDEX IX_EC_CertificateAuditLogs_RequestId ON dbo.EC_CertificateAuditLogs (CertificateRequestId);
    CREATE NONCLUSTERED INDEX IX_EC_CertificateAuditLogs_IssuedId ON dbo.EC_CertificateAuditLogs (IssuedCertificateId);
    CREATE NONCLUSTERED INDEX IX_EC_CertificateAuditLogs_Org ON dbo.EC_CertificateAuditLogs (OrganisationId, Timestamp DESC);
END;

-- Ensure RequestType row exists for EMPLOYMENT_CERTIFICATE_REQUEST (ID 120 or by TypeName)
IF NOT EXISTS (SELECT * FROM dbo.RequestTypes WHERE TypeName = 'Employment Certificate Request')
BEGIN
    INSERT INTO dbo.RequestTypes (RequestTypeId, TypeName, Descriptions, IsActive)
    VALUES (120, 'Employment Certificate Request', 'Official Employment & Work Experience Certificate Request', 1);
END;

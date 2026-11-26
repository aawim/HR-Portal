-- Document Signatures (e-Sign) tables: DS_UserSignatures, DS_DocumentSignatures
-- Idempotent deployment script for SQL Server

IF OBJECT_ID(N'dbo.DS_UserSignatures', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DS_UserSignatures (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DS_UserSignatures PRIMARY KEY,
        UserId INT NOT NULL,
        IndividualId INT NOT NULL,
        SignatureDataUrl NVARCHAR(MAX) NOT NULL,
        ContentSha256 NVARCHAR(64) NOT NULL,
        PinHash NVARCHAR(128) NULL,
        PinSalt NVARCHAR(64) NULL,
        PinFailedAttempts INT NOT NULL CONSTRAINT DF_DS_UserSignatures_PinFailedAttempts DEFAULT (0),
        PinLockedUntilUtc DATETIME2 NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DS_UserSignatures_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DS_UserSignatures_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
        IsActive BIT NOT NULL CONSTRAINT DF_DS_UserSignatures_IsActive DEFAULT (1)
    );
    CREATE UNIQUE INDEX IX_DS_UserSignatures_UserId ON dbo.DS_UserSignatures (UserId);
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.DS_UserSignatures', N'PinHash') IS NULL
        ALTER TABLE dbo.DS_UserSignatures ADD PinHash NVARCHAR(128) NULL, PinSalt NVARCHAR(64) NULL, PinFailedAttempts INT NOT NULL CONSTRAINT DF_DS_UserSignatures_PinFailedAttempts DEFAULT (0), PinLockedUntilUtc DATETIME2 NULL;
END
GO

IF OBJECT_ID(N'dbo.DS_DocumentSignatures', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DS_DocumentSignatures (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DS_DocumentSignatures PRIMARY KEY,
        OrganisationId INT NOT NULL,
        DocumentType NVARCHAR(80) NOT NULL,
        DocumentId NVARCHAR(80) NOT NULL,
        SlotKey NVARCHAR(80) NOT NULL,
        SlotLabel NVARCHAR(150) NOT NULL,
        SignerUserId INT NOT NULL,
        SignerIndividualId INT NOT NULL,
        SignerName NVARCHAR(250) NOT NULL,
        SignerPosition NVARCHAR(250) NULL,
        SignatureImageDataUrl NVARCHAR(MAX) NOT NULL,
        DocumentContentHash NVARCHAR(64) NOT NULL,
        VerificationCode NVARCHAR(40) NOT NULL,
        SignedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DS_DocumentSignatures_SignedAtUtc DEFAULT SYSUTCDATETIME(),
        IpAddress NVARCHAR(64) NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_DS_DocumentSignatures_Status DEFAULT ('Signed'),
        RevokedAtUtc DATETIME2 NULL,
        RevokedByUserId INT NULL,
        RevokeReason NVARCHAR(500) NULL,
        OperationLogId INT NULL,
        CONSTRAINT FK_DS_DocumentSignatures_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID)
    );
    CREATE UNIQUE INDEX IX_DS_DocumentSignatures_VerificationCode ON dbo.DS_DocumentSignatures (VerificationCode);
    CREATE INDEX IX_DS_DocumentSignatures_DocSlot ON dbo.DS_DocumentSignatures (OrganisationId, DocumentType, DocumentId, SlotKey);
END
GO

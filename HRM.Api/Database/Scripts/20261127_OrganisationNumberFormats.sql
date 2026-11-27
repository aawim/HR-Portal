-- Per-organisation number formatting configuration (Requests, Documents, Certificates, Assets, Recruitment)
-- Follows HRM table naming convention: NF_ (Number Formatting)
IF OBJECT_ID(N'dbo.NF_OrganisationNumberFormats', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NF_OrganisationNumberFormats
    (
        NumberFormatId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NF_OrganisationNumberFormats PRIMARY KEY,
        OrganisationId        INT NOT NULL,
        Category              NVARCHAR(40) NOT NULL,
        SequenceNumberTypeId  INT NULL,
        Code                  NVARCHAR(60) NOT NULL,
        Name                  NVARCHAR(150) NOT NULL,
        Prefix                NVARCHAR(30) NOT NULL,
        FormatString          NVARCHAR(150) NOT NULL,
        PaddingLength         INT NOT NULL CONSTRAINT DF_NF_OrgNumFmt_Pad DEFAULT (4),
        StartingNumber        INT NOT NULL CONSTRAINT DF_NF_OrgNumFmt_Start DEFAULT (1),
        NextNumber            INT NOT NULL CONSTRAINT DF_NF_OrgNumFmt_Next DEFAULT (1),
        IsResetAnnually       BIT NOT NULL CONSTRAINT DF_NF_OrgNumFmt_Reset DEFAULT (1),
        LastResetYear         INT NULL,
        IsActive              BIT NOT NULL CONSTRAINT DF_NF_OrgNumFmt_Active DEFAULT (1),
        UpdatedAtUtc          DATETIME2 NOT NULL CONSTRAINT DF_NF_OrgNumFmt_Updated DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_NF_OrganisationNumberFormats_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations(BusinessEntityID),
        CONSTRAINT FK_NF_OrganisationNumberFormats_SequenceNumberTypes FOREIGN KEY (SequenceNumberTypeId) REFERENCES dbo.SequenceNumberTypes(SequenceNumberTypeID)
    );

    CREATE UNIQUE INDEX UX_NF_OrganisationNumberFormats_Org_Code
        ON dbo.NF_OrganisationNumberFormats (OrganisationId, Code)
        WHERE IsActive = 1;

    CREATE INDEX IX_NF_OrganisationNumberFormats_Org_Seq
        ON dbo.NF_OrganisationNumberFormats (OrganisationId, SequenceNumberTypeId)
        WHERE IsActive = 1;
END
GO

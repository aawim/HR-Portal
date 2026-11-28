-- Helpdesk Chatbot Global & Tenant-Wise Settings (Prefix: HD_)
SET NOCOUNT ON;

IF OBJECT_ID('dbo.HD_HelpdeskSettings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HD_HelpdeskSettings
    (
        Id              INT          NOT NULL CONSTRAINT PK_HD_HelpdeskSettings PRIMARY KEY,
        IsEnabled       BIT          NOT NULL CONSTRAINT DF_HD_HelpdeskSettings_IsEnabled DEFAULT (1),
        ScopeMode       NVARCHAR(24) NOT NULL CONSTRAINT DF_HD_HelpdeskSettings_ScopeMode DEFAULT ('Global'),
        UpdatedByUserId INT          NULL,
        UpdatedAtUtc    DATETIME2(7) NOT NULL CONSTRAINT DF_HD_HelpdeskSettings_UpdatedAtUtc DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.HD_HelpdeskSettings WHERE Id = 1)
BEGIN
    INSERT INTO dbo.HD_HelpdeskSettings (Id, IsEnabled, ScopeMode, UpdatedAtUtc)
    VALUES (1, 1, 'Global', SYSUTCDATETIME());
END
GO

IF OBJECT_ID('dbo.HD_HelpdeskOrganisationAccess', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HD_HelpdeskOrganisationAccess
    (
        OrganisationId  INT          NOT NULL CONSTRAINT PK_HD_HelpdeskOrganisationAccess PRIMARY KEY,
        IsEnabled       BIT          NOT NULL CONSTRAINT DF_HD_HelpdeskOrganisationAccess_IsEnabled DEFAULT (1),
        UpdatedByUserId INT          NULL,
        UpdatedAtUtc    DATETIME2(7) NOT NULL CONSTRAINT DF_HD_HelpdeskOrganisationAccess_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_HD_HelpdeskOrganisationAccess_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations(BusinessEntityID)
    );
END
GO

SELECT Id, IsEnabled, ScopeMode FROM dbo.HD_HelpdeskSettings WHERE Id = 1;
GO

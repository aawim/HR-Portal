-- CM_ Communication Hub foundation. Additive, disabled by default, and safe to re-run.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.CM_CommunicationOutbox', N'U') IS NULL
CREATE TABLE dbo.CM_CommunicationOutbox (
    CommunicationOutboxId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CM_CommunicationOutbox PRIMARY KEY,
    EventId uniqueidentifier NOT NULL CONSTRAINT UQ_CM_CommunicationOutbox_Event UNIQUE,
    OrganisationBusinessEntityId int NOT NULL CONSTRAINT FK_CM_Outbox_Organisation REFERENCES dbo.Organisations(BusinessEntityID),
    EventCode nvarchar(120) NOT NULL, AggregateType nvarchar(80) NOT NULL, AggregateId nvarchar(80) NOT NULL,
    PayloadJson nvarchar(max) NOT NULL, OccurredAtUtc datetime2 NOT NULL, AvailableAtUtc datetime2 NOT NULL,
    ProcessedAtUtc datetime2 NULL, Status nvarchar(16) NOT NULL, AttemptCount int NOT NULL CONSTRAINT DF_CM_Outbox_Attempts DEFAULT 0,
    LeaseExpiresAtUtc datetime2 NULL,
    CONSTRAINT CK_CM_Outbox_Status CHECK (Status IN ('Pending','Processing','Processed','Failed'))
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CM_Outbox_Status_Available')
    CREATE INDEX IX_CM_Outbox_Status_Available ON dbo.CM_CommunicationOutbox(Status, AvailableAtUtc);

IF OBJECT_ID(N'dbo.CM_ProviderConnections', N'U') IS NULL
CREATE TABLE dbo.CM_ProviderConnections (
    CommunicationProviderConnectionId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CM_ProviderConnections PRIMARY KEY,
    OrganisationBusinessEntityId int NOT NULL CONSTRAINT FK_CM_ProviderConnections_Organisation REFERENCES dbo.Organisations(BusinessEntityID),
    ProviderCode nvarchar(32) NOT NULL, ConnectionName nvarchar(120) NOT NULL, CredentialReference nvarchar(256) NOT NULL,
    ConfigurationJson nvarchar(max) NULL, IsEnabled bit NOT NULL CONSTRAINT DF_CM_ProviderConnections_Enabled DEFAULT 0,
    CreatedAtUtc datetime2 NOT NULL,
    CONSTRAINT UQ_CM_ProviderConnections UNIQUE (OrganisationBusinessEntityId, ProviderCode, ConnectionName)
);

IF OBJECT_ID(N'dbo.CM_Destinations', N'U') IS NULL
CREATE TABLE dbo.CM_Destinations (
    CommunicationDestinationId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CM_Destinations PRIMARY KEY,
    CommunicationProviderConnectionId bigint NOT NULL CONSTRAINT FK_CM_Destinations_Connection REFERENCES dbo.CM_ProviderConnections(CommunicationProviderConnectionId),
    OrganisationBusinessEntityId int NOT NULL CONSTRAINT FK_CM_Destinations_Organisation REFERENCES dbo.Organisations(BusinessEntityID),
    DestinationType nvarchar(32) NOT NULL, ExternalDestinationId nvarchar(256) NOT NULL, DisplayName nvarchar(160) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_CM_Destinations_Active DEFAULT 0,
    CONSTRAINT UQ_CM_Destinations UNIQUE (CommunicationProviderConnectionId, ExternalDestinationId)
);

IF OBJECT_ID(N'dbo.CM_AutomationRules', N'U') IS NULL
CREATE TABLE dbo.CM_AutomationRules (
    CommunicationAutomationRuleId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CM_AutomationRules PRIMARY KEY,
    OrganisationBusinessEntityId int NOT NULL CONSTRAINT FK_CM_AutomationRules_Organisation REFERENCES dbo.Organisations(BusinessEntityID),
    EventCode nvarchar(120) NOT NULL, RuleName nvarchar(160) NOT NULL, TemplateBody nvarchar(4000) NOT NULL,
    RequiresApproval bit NOT NULL CONSTRAINT DF_CM_AutomationRules_Approval DEFAULT 1,
    IsEnabled bit NOT NULL CONSTRAINT DF_CM_AutomationRules_Enabled DEFAULT 0
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CM_AutomationRules_Organisation_Event')
    CREATE INDEX IX_CM_AutomationRules_Organisation_Event ON dbo.CM_AutomationRules(OrganisationBusinessEntityId, EventCode, IsEnabled);

COMMIT;

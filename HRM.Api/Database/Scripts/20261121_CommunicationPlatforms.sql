SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.CM_WorkerHeartbeats','U') IS NULL
CREATE TABLE dbo.CM_WorkerHeartbeats (
 InstanceId nvarchar(100) NOT NULL CONSTRAINT PK_CM_WorkerHeartbeats PRIMARY KEY,
 LastSuccessfulCycleUtc datetime2 NOT NULL, OutboundEnabled bit NOT NULL
);
IF COL_LENGTH('dbo.CM_Destinations','IndividualId') IS NULL ALTER TABLE dbo.CM_Destinations ADD IndividualId int NULL;
IF COL_LENGTH('dbo.CM_Destinations','LanguageCode') IS NULL ALTER TABLE dbo.CM_Destinations ADD LanguageCode nvarchar(12) NOT NULL CONSTRAINT DF_CM_Destinations_Language DEFAULT N'en' WITH VALUES;
IF COL_LENGTH('dbo.CM_AutomationRules','DhivehiTemplateBody') IS NULL ALTER TABLE dbo.CM_AutomationRules ADD DhivehiTemplateBody nvarchar(4000) NULL;
IF COL_LENGTH('dbo.CM_AutomationRules','TemplateVersion') IS NULL ALTER TABLE dbo.CM_AutomationRules ADD TemplateVersion int NOT NULL CONSTRAINT DF_CM_Rules_TemplateVersion DEFAULT 1 WITH VALUES;
IF COL_LENGTH('dbo.CM_Messages','TemplateVersion') IS NULL ALTER TABLE dbo.CM_Messages ADD TemplateVersion int NOT NULL CONSTRAINT DF_CM_Messages_TemplateVersion DEFAULT 1 WITH VALUES;
IF COL_LENGTH('dbo.CM_Messages','ProviderConfigurationJson') IS NULL ALTER TABLE dbo.CM_Messages ADD ProviderConfigurationJson nvarchar(max) NULL;
IF OBJECT_ID('dbo.CM_CommunicationConsents','U') IS NULL
CREATE TABLE dbo.CM_CommunicationConsents (
 CommunicationConsentId bigint IDENTITY NOT NULL CONSTRAINT PK_CM_CommunicationConsents PRIMARY KEY,
 OrganisationBusinessEntityId int NOT NULL CONSTRAINT FK_CM_Consents_Organisation REFERENCES dbo.Organisations(BusinessEntityID),
 CommunicationDestinationId bigint NOT NULL CONSTRAINT FK_CM_Consents_Destination REFERENCES dbo.CM_Destinations(CommunicationDestinationId),
 IndividualId int NOT NULL, Purpose nvarchar(40) NOT NULL, Source nvarchar(400) NOT NULL,
 ConsentedAtUtc datetime2 NOT NULL, RevokedAtUtc datetime2 NULL, RecordedByUserId int NOT NULL
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.CM_CommunicationConsents') AND name='IX_CM_Consents_DestinationIndividualPurposeRevoked')
CREATE INDEX IX_CM_Consents_DestinationIndividualPurposeRevoked ON dbo.CM_CommunicationConsents(CommunicationDestinationId,IndividualId,Purpose,RevokedAtUtc);
COMMIT;

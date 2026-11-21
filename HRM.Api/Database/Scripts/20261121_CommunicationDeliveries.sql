-- CM_ delivery records. Additive and safe to re-run.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.CM_Messages', N'U') IS NULL
CREATE TABLE dbo.CM_Messages (
    CommunicationMessageId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CM_Messages PRIMARY KEY,
    CommunicationOutboxId bigint NOT NULL CONSTRAINT FK_CM_Messages_Outbox REFERENCES dbo.CM_CommunicationOutbox(CommunicationOutboxId),
    OrganisationBusinessEntityId int NOT NULL CONSTRAINT FK_CM_Messages_Organisation REFERENCES dbo.Organisations(BusinessEntityID),
    Subject nvarchar(300) NOT NULL, RenderedBody nvarchar(4000) NOT NULL, CreatedAtUtc datetime2 NOT NULL,
    CONSTRAINT UQ_CM_Messages_Outbox UNIQUE (CommunicationOutboxId)
);

IF OBJECT_ID(N'dbo.CM_Deliveries', N'U') IS NULL
CREATE TABLE dbo.CM_Deliveries (
    CommunicationDeliveryId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CM_Deliveries PRIMARY KEY,
    CommunicationMessageId bigint NOT NULL CONSTRAINT FK_CM_Deliveries_Message REFERENCES dbo.CM_Messages(CommunicationMessageId),
    CommunicationProviderConnectionId bigint NOT NULL CONSTRAINT FK_CM_Deliveries_Connection REFERENCES dbo.CM_ProviderConnections(CommunicationProviderConnectionId),
    CommunicationDestinationId bigint NOT NULL CONSTRAINT FK_CM_Deliveries_Destination REFERENCES dbo.CM_Destinations(CommunicationDestinationId),
    OrganisationBusinessEntityId int NOT NULL CONSTRAINT FK_CM_Deliveries_Organisation REFERENCES dbo.Organisations(BusinessEntityID),
    Status nvarchar(16) NOT NULL, IdempotencyKey nvarchar(120) NOT NULL, ProviderMessageId nvarchar(256) NULL,
    AttemptCount int NOT NULL CONSTRAINT DF_CM_Deliveries_Attempts DEFAULT 0, NextRetryAtUtc datetime2 NULL, SentAtUtc datetime2 NULL,
    LastErrorCode nvarchar(128) NULL, CreatedAtUtc datetime2 NOT NULL,
    CONSTRAINT UQ_CM_Deliveries_Idempotency UNIQUE (IdempotencyKey),
    CONSTRAINT CK_CM_Deliveries_Status CHECK (Status IN ('Pending','Sending','Sent','Failed'))
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CM_Deliveries_Status_Retry')
    CREATE INDEX IX_CM_Deliveries_Status_Retry ON dbo.CM_Deliveries(Status, NextRetryAtUtc);

IF OBJECT_ID(N'dbo.CM_DeliveryAttempts', N'U') IS NULL
CREATE TABLE dbo.CM_DeliveryAttempts (
    CommunicationDeliveryAttemptId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CM_DeliveryAttempts PRIMARY KEY,
    CommunicationDeliveryId bigint NOT NULL CONSTRAINT FK_CM_DeliveryAttempts_Delivery REFERENCES dbo.CM_Deliveries(CommunicationDeliveryId),
    AttemptNumber int NOT NULL, StartedAtUtc datetime2 NOT NULL, CompletedAtUtc datetime2 NOT NULL,
    ResultStatus nvarchar(16) NOT NULL, HttpStatusCode int NULL, SanitizedErrorCode nvarchar(128) NULL,
    CONSTRAINT UQ_CM_DeliveryAttempts UNIQUE (CommunicationDeliveryId, AttemptNumber)
);

COMMIT;

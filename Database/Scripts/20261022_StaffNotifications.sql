SET XACT_ABORT ON;
IF OBJECT_ID('dbo.StaffNotifications','U') IS NULL
BEGIN
 CREATE TABLE dbo.StaffNotifications (
  StaffNotificationId bigint IDENTITY PRIMARY KEY, UserId int NOT NULL, IndividualId int NOT NULL, OrganisationId int NOT NULL,
  Kind nvarchar(48) NOT NULL, Message nvarchar(500) NOT NULL, Url nvarchar(250) NOT NULL,
  CreatedAtUtc datetime2 NOT NULL, ReadAtUtc datetime2 NULL,
  CONSTRAINT FK_StaffNotifications_Users FOREIGN KEY(UserId) REFERENCES dbo.Users(UserID)
 );
 CREATE INDEX IX_StaffNotifications_UserId_OrganisationId_ReadAtUtc_CreatedAtUtc ON dbo.StaffNotifications(UserId,OrganisationId,ReadAtUtc,CreatedAtUtc);
END;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.RequestNotifications') AND name='UX_RequestNotifications_DutyAcceptanceRecipient')
 CREATE UNIQUE INDEX UX_RequestNotifications_DutyAcceptanceRecipient ON dbo.RequestNotifications(UserId,RequestId) WHERE NotificationKind='DutyAcceptance';

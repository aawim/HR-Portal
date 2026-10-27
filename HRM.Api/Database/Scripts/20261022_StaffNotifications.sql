SET XACT_ABORT ON;
IF OBJECT_ID('dbo.SN_StaffNotifications','U') IS NULL
BEGIN
 CREATE TABLE dbo.SN_StaffNotifications (
  StaffNotificationId bigint IDENTITY PRIMARY KEY, UserId int NOT NULL, IndividualId int NOT NULL, OrganisationId int NOT NULL,
  Kind nvarchar(48) NOT NULL, Message nvarchar(500) NOT NULL, Url nvarchar(250) NOT NULL,
  CreatedAtUtc datetime2 NOT NULL, ReadAtUtc datetime2 NULL,
  CONSTRAINT FK_StaffNotifications_Users FOREIGN KEY(UserId) REFERENCES dbo.Users(UserID)
 );
 CREATE INDEX IX_StaffNotifications_UserId_OrganisationId_ReadAtUtc_CreatedAtUtc ON dbo.SN_StaffNotifications(UserId,OrganisationId,ReadAtUtc,CreatedAtUtc);
END;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.RN_RequestNotifications') AND name='UX_RequestNotifications_DutyAcceptanceRecipient')
 CREATE UNIQUE INDEX UX_RequestNotifications_DutyAcceptanceRecipient ON dbo.RN_RequestNotifications(UserId,RequestId) WHERE NotificationKind='DutyAcceptance';

SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.RN_RequestNotifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RN_RequestNotifications (
        RequestNotificationId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_RequestNotifications PRIMARY KEY,
        UserId int NOT NULL,
        RequestId int NOT NULL,
        FromStateId int NOT NULL,
        ToStateId int NOT NULL,
        ReferenceNumber nvarchar(512) NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        ReadAtUtc datetime2 NULL,
        CONSTRAINT FK_RequestNotifications_Users FOREIGN KEY(UserId) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_RequestNotifications_Requests FOREIGN KEY(RequestId) REFERENCES dbo.Requests(RequestID),
        CONSTRAINT FK_RequestNotifications_FromState FOREIGN KEY(FromStateId) REFERENCES dbo.RequestStates(RequestStateID),
        CONSTRAINT FK_RequestNotifications_ToState FOREIGN KEY(ToStateId) REFERENCES dbo.RequestStates(RequestStateID)
    );
    CREATE INDEX IX_RequestNotifications_UserId_ReadAtUtc_CreatedAtUtc
        ON dbo.RN_RequestNotifications(UserId, ReadAtUtc, CreatedAtUtc);
END;
COMMIT TRANSACTION;

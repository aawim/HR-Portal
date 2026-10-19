-- Personal tasks are owned by the authenticated Users.UserID.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.UserTodos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserTodos (
        UserTodoId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserTodos PRIMARY KEY,
        UserId int NOT NULL,
        Title nvarchar(200) NOT NULL,
        Notes nvarchar(1000) NULL,
        Priority nvarchar(10) NOT NULL,
        DueDate date NULL,
        IsCompleted bit NOT NULL,
        IsDeleted bit NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        UpdatedAtUtc datetime2 NOT NULL,
        CompletedAtUtc datetime2 NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT FK_UserTodos_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users(UserID),
        CONSTRAINT CK_UserTodos_Priority CHECK (Priority IN ('Normal','Medium','High'))
    );
    CREATE INDEX IX_UserTodos_UserId_IsDeleted_IsCompleted_DueDate ON dbo.UserTodos(UserId,IsDeleted,IsCompleted,DueDate);
END;
COMMIT;

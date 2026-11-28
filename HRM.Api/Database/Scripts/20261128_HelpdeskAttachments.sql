-- =============================================================================
-- Helpdesk Chat Attachments (Images, Screenshots, Documents)
-- Physical table: dbo.HD_HelpdeskAttachments
-- =============================================================================

IF OBJECT_ID(N'dbo.HD_HelpdeskAttachments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HD_HelpdeskAttachments (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HD_HelpdeskAttachments PRIMARY KEY,
        TicketId BIGINT NOT NULL,
        MessageId BIGINT NOT NULL,
        UploadedByUserId INT NOT NULL,
        FileName NVARCHAR(255) NOT NULL,
        ContentType NVARCHAR(120) NOT NULL,
        FileSizeBytes BIGINT NOT NULL,
        Content VARBINARY(MAX) NOT NULL,
        IsImage BIT NOT NULL CONSTRAINT DF_HD_HelpdeskAttachments_IsImage DEFAULT (0),
        UploadedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_HD_HelpdeskAttachments_UploadedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_HD_HelpdeskAttachments_Ticket FOREIGN KEY (TicketId) REFERENCES dbo.HD_HelpdeskTickets(Id) ON DELETE CASCADE,
        CONSTRAINT FK_HD_HelpdeskAttachments_Message FOREIGN KEY (MessageId) REFERENCES dbo.HD_HelpdeskMessages(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_HD_HelpdeskAttachments_User FOREIGN KEY (UploadedByUserId) REFERENCES dbo.Users(UserID) ON DELETE NO ACTION
    );

    CREATE INDEX IX_HD_HelpdeskAttachments_Ticket_Message
        ON dbo.HD_HelpdeskAttachments (TicketId, MessageId);
END;

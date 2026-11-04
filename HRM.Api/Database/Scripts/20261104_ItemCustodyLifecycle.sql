-- Migration: Staff item custody lifecycle (issue → accept → replace/upgrade → return with HR condition check) and history.
-- Existing IM_ItemStaffRequests rows are preserved and mapped to the new Status column.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF COL_LENGTH(N'dbo.IM_ItemStaffRequests', N'Status') IS NULL
    ALTER TABLE dbo.IM_ItemStaffRequests ADD Status nvarchar(30) NOT NULL CONSTRAINT DF_IM_ItemStaffRequests_Status DEFAULT(N'Submitted');
IF COL_LENGTH(N'dbo.IM_ItemStaffRequests', N'ReplacesRequestId') IS NULL
    ALTER TABLE dbo.IM_ItemStaffRequests ADD ReplacesRequestId int NULL;
IF COL_LENGTH(N'dbo.IM_ItemStaffRequests', N'AcceptedAtUtc') IS NULL
    ALTER TABLE dbo.IM_ItemStaffRequests ADD AcceptedAtUtc datetime2 NULL;
IF COL_LENGTH(N'dbo.IM_ItemStaffRequests', N'ReturnRequestedAtUtc') IS NULL
    ALTER TABLE dbo.IM_ItemStaffRequests ADD ReturnRequestedAtUtc datetime2 NULL;
IF COL_LENGTH(N'dbo.IM_ItemStaffRequests', N'ReturnCondition') IS NULL
    ALTER TABLE dbo.IM_ItemStaffRequests ADD ReturnCondition nvarchar(20) NULL;
IF COL_LENGTH(N'dbo.IM_ItemStaffRequests', N'ReturnNotes') IS NULL
    ALTER TABLE dbo.IM_ItemStaffRequests ADD ReturnNotes nvarchar(1000) NULL;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_IM_ItemStaffRequests_Values')
    ALTER TABLE dbo.IM_ItemStaffRequests DROP CONSTRAINT CK_IM_ItemStaffRequests_Values;
ALTER TABLE dbo.IM_ItemStaffRequests ADD CONSTRAINT CK_IM_ItemStaffRequests_Values
    CHECK ([Quantity] > 0 AND [Purpose] IN ('Job related','Common') AND [Kind] IN ('Issue','Requisition','Replacement','Upgrade'));

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_IM_ItemStaffRequests_IM_ItemStaffRequests_ReplacesRequestId')
    ALTER TABLE dbo.IM_ItemStaffRequests ADD CONSTRAINT FK_IM_ItemStaffRequests_IM_ItemStaffRequests_ReplacesRequestId
        FOREIGN KEY (ReplacesRequestId) REFERENCES dbo.IM_ItemStaffRequests(RequestId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_IM_ItemStaffRequests_ReplacesRequestId')
    CREATE INDEX IX_IM_ItemStaffRequests_ReplacesRequestId ON dbo.IM_ItemStaffRequests(ReplacesRequestId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_IM_ItemStaffRequests_OrganisationId_Status')
    CREATE INDEX IX_IM_ItemStaffRequests_OrganisationId_Status ON dbo.IM_ItemStaffRequests(OrganisationId, Status);

-- Map legacy rows that were driven by the generic request workflow.
UPDATE r SET Status = CASE
        WHEN r.ReturnedAtUtc IS NOT NULL THEN N'Returned'
        WHEN r.IssuedAtUtc IS NOT NULL AND r.Purpose = N'Job related' THEN N'InUse'
        WHEN r.IssuedAtUtc IS NOT NULL THEN N'Received'
        WHEN RTRIM(s.StateName) LIKE N'%Reject%' THEN N'Rejected'
        WHEN RTRIM(s.StateName) LIKE N'%Cancel%' OR RTRIM(s.StateName) LIKE N'%Withdraw%' THEN N'Cancelled'
        ELSE N'Submitted' END,
    AcceptedAtUtc = COALESCE(r.AcceptedAtUtc, r.IssuedAtUtc)
FROM dbo.IM_ItemStaffRequests r
JOIN dbo.Requests q ON q.RequestID = r.RequestId
JOIN dbo.RequestStates s ON s.RequestStateID = q.RequestStateID
WHERE r.Status = N'Submitted';

IF OBJECT_ID(N'dbo.IM_ItemStaffRequestEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IM_ItemStaffRequestEvents(
        ItemStaffRequestEventId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_IM_ItemStaffRequestEvents PRIMARY KEY,
        RequestId int NOT NULL,
        Action nvarchar(40) NOT NULL,
        FromStatus nvarchar(30) NULL,
        ToStatus nvarchar(30) NOT NULL,
        ActorUserId int NOT NULL,
        ActorIndividualId int NOT NULL,
        Condition nvarchar(20) NULL,
        Comment nvarchar(1000) NULL,
        CreatedAtUtc datetime2 NOT NULL,
        CONSTRAINT FK_IM_ItemStaffRequestEvents_IM_ItemStaffRequests_RequestId FOREIGN KEY (RequestId) REFERENCES dbo.IM_ItemStaffRequests(RequestId)
    );
    CREATE INDEX IX_IM_ItemStaffRequestEvents_RequestId_CreatedAtUtc ON dbo.IM_ItemStaffRequestEvents(RequestId, CreatedAtUtc);

    -- Seed history for legacy rows so every record has at least its creation entry.
    INSERT dbo.IM_ItemStaffRequestEvents(RequestId, Action, FromStatus, ToStatus, ActorUserId, ActorIndividualId, Comment, CreatedAtUtc)
    SELECT RequestId, N'Migrated', NULL, Status, 0, IndividualId, N'Record migrated to custody lifecycle.', SYSUTCDATETIME()
    FROM dbo.IM_ItemStaffRequests;
END;

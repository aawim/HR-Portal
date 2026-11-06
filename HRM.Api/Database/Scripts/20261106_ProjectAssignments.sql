-- Workspace Hub: staff-to-project and staff-to-task assignment support. Idempotent; preserves existing data.
IF COL_LENGTH(N'dbo.PR_ProjectMembers', N'ExpectedEndDate') IS NULL ALTER TABLE dbo.PR_ProjectMembers ADD ExpectedEndDate datetime2 NULL;
IF COL_LENGTH(N'dbo.PR_ProjectMembers', N'Remarks') IS NULL ALTER TABLE dbo.PR_ProjectMembers ADD Remarks nvarchar(1000) NOT NULL CONSTRAINT DF_PR_ProjectMembers_Remarks DEFAULT(N'');
IF COL_LENGTH(N'dbo.PR_ProjectMembers', N'AssignedByStaffId') IS NULL ALTER TABLE dbo.PR_ProjectMembers ADD AssignedByStaffId int NULL;
IF COL_LENGTH(N'dbo.PR_ProjectMembers', N'UpdatedAt') IS NULL ALTER TABLE dbo.PR_ProjectMembers ADD UpdatedAt datetime2 NULL;
IF COL_LENGTH(N'dbo.PR_ProjectMembers', N'UpdatedByStaffId') IS NULL ALTER TABLE dbo.PR_ProjectMembers ADD UpdatedByStaffId int NULL;
IF COL_LENGTH(N'dbo.PR_ProjectMembers', N'RemovedAt') IS NULL ALTER TABLE dbo.PR_ProjectMembers ADD RemovedAt datetime2 NULL;
IF COL_LENGTH(N'dbo.PR_ProjectMembers', N'RemovedByStaffId') IS NULL ALTER TABLE dbo.PR_ProjectMembers ADD RemovedByStaffId int NULL;
IF COL_LENGTH(N'dbo.PR_ProjectTasks', N'AssignedAt') IS NULL ALTER TABLE dbo.PR_ProjectTasks ADD AssignedAt datetime2 NULL;
IF COL_LENGTH(N'dbo.PR_ProjectTasks', N'AssignedByStaffId') IS NULL ALTER TABLE dbo.PR_ProjectTasks ADD AssignedByStaffId int NULL;
IF OBJECT_ID(N'dbo.PR_ProjectAssignmentHistories', N'U') IS NULL
CREATE TABLE dbo.PR_ProjectAssignmentHistories(
    HistoryId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PR_ProjectAssignmentHistories PRIMARY KEY,
    ProjectId int NOT NULL,
    TaskId int NULL,
    StaffId int NULL,
    PreviousStaffId int NULL,
    Action nvarchar(30) NOT NULL,
    Details nvarchar(1000) NOT NULL CONSTRAINT DF_PR_ProjectAssignmentHistories_Details DEFAULT(N''),
    PerformedByStaffId int NOT NULL,
    PerformedAt datetime2 NOT NULL CONSTRAINT DF_PR_ProjectAssignmentHistories_PerformedAt DEFAULT(SYSUTCDATETIME()),
    OperationLogId int NULL
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PR_ProjectAssignmentHistories_ProjectId_PerformedAt')
CREATE INDEX IX_PR_ProjectAssignmentHistories_ProjectId_PerformedAt ON dbo.PR_ProjectAssignmentHistories(ProjectId, PerformedAt);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PR_ProjectAssignmentHistories_StaffId')
CREATE INDEX IX_PR_ProjectAssignmentHistories_StaffId ON dbo.PR_ProjectAssignmentHistories(StaffId);
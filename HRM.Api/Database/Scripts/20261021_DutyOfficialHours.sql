SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.WorkAssignments','SchedulePurpose') IS NULL
    ALTER TABLE dbo.WorkAssignments ADD SchedulePurpose nvarchar(20) NOT NULL
        CONSTRAINT DF_WorkAssignments_SchedulePurpose DEFAULT 'Official';
IF COL_LENGTH('dbo.WorkPlanSegments','SchedulePurpose') IS NULL
    ALTER TABLE dbo.WorkPlanSegments ADD SchedulePurpose nvarchar(20) NOT NULL
        CONSTRAINT DF_WorkPlanSegments_SchedulePurpose DEFAULT 'Official';
-- Preserve existing generated records; only new plans receive duty classification.
COMMIT TRANSACTION;

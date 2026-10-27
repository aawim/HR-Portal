SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.JobWorkTemplateAssignments', 'ScheduleMode') IS NULL
    ALTER TABLE dbo.JobWorkTemplateAssignments ADD ScheduleMode nvarchar(20) NOT NULL CONSTRAINT DF_JWTA_ScheduleMode DEFAULT 'Standard';
IF COL_LENGTH('dbo.JobWorkTemplateAssignments', 'WorkingDaysMask') IS NULL
    ALTER TABLE dbo.JobWorkTemplateAssignments ADD WorkingDaysMask int NOT NULL CONSTRAINT DF_JWTA_WorkingDaysMask DEFAULT 31;
IF COL_LENGTH('dbo.JobWorkTemplateAssignments', 'RotationAnchorDate') IS NULL
    ALTER TABLE dbo.JobWorkTemplateAssignments ADD RotationAnchorDate date NULL;
IF COL_LENGTH('dbo.JobWorkTemplateAssignments', 'RotationTemplateIds') IS NULL
    ALTER TABLE dbo.JobWorkTemplateAssignments ADD RotationTemplateIds nvarchar(2000) NULL;
COMMIT TRANSACTION;

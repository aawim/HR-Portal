SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.JobWorkTemplateAssignments','SchedulePurpose') IS NULL
BEGIN
    ALTER TABLE dbo.JobWorkTemplateAssignments ADD SchedulePurpose nvarchar(20) NOT NULL
        CONSTRAINT DF_JobWorkTemplateAssignments_Purpose DEFAULT 'Official';
    EXEC(N'ALTER TABLE dbo.JobWorkTemplateAssignments ADD CONSTRAINT CK_JobWorkTemplateAssignments_Purpose
        CHECK (SchedulePurpose IN (''Official'',''Duty''))');
END;
COMMIT TRANSACTION;

-- Reuse existing Calendars, BusinessEntityCalendars, Schedules,
-- CalendarSchedules and BusinessEntitySchedules. No schema changes required.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM dbo.ScheduleTypes WITH (UPDLOCK, HOLDLOCK) WHERE Name = 'Planned leave')
    INSERT dbo.ScheduleTypes (Name) VALUES ('Planned leave');
COMMIT;

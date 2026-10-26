-- Additive compatibility repair. Existing records and table object IDs are preserved.
IF OBJECT_ID(N'dbo.DayOfWeek',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.DayOfWeeks',N'U') IS NULL
    EXEC sys.sp_rename N'dbo.DayOfWeek',N'DayOfWeeks';
IF OBJECT_ID(N'dbo.DayOfWeek',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.DayOfWeeks',N'U') IS NOT NULL
    THROW 51000,'Both day-of-week tables exist. Resolve the collision without discarding records.',1;
IF OBJECT_ID(N'dbo.Teams',N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Teams',N'IsSuperVisor') IS NULL
    ALTER TABLE dbo.Teams ADD IsSuperVisor bit NULL;

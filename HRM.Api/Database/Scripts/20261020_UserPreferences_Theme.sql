SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.UserPreferences', 'ThemeMode') IS NULL
    ALTER TABLE dbo.UserPreferences ADD ThemeMode nvarchar(16) NOT NULL
        CONSTRAINT DF_UserPreferences_ThemeMode DEFAULT N'dark' WITH VALUES;
IF COL_LENGTH('dbo.UserPreferences', 'UpdatedAtUtc') IS NULL
    ALTER TABLE dbo.UserPreferences ADD UpdatedAtUtc datetime2 NULL;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_UserPreferences_ThemeMode' AND parent_object_id = OBJECT_ID('dbo.UserPreferences'))
    EXEC(N'ALTER TABLE dbo.UserPreferences WITH CHECK ADD CONSTRAINT CK_UserPreferences_ThemeMode CHECK (ThemeMode IN (N''dark'', N''light''));');
COMMIT TRANSACTION;

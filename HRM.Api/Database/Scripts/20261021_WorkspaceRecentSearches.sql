IF COL_LENGTH('dbo.UserPreferences','RecentWorkspaceSearchesJson') IS NULL
 ALTER TABLE dbo.UserPreferences ADD RecentWorkspaceSearchesJson nvarchar(max) NULL;

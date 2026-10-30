-- SQLCMD deployment from the repository root. Same SQL and migration identity as EF.
:On Error exit
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRANSACTION;
IF NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20261029195048_SoftwareProfessionRequirements')
 THROW 51000,'Expected migration baseline is missing.',1;
GO
:r HRM.Api/Database/Scripts/20261030_ReconcileAnnualLeaveCatalogue.sql
GO
IF NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20261030090000_ReconcileAnnualLeaveCatalogue')
 INSERT dbo.__EFMigrationsHistory(MigrationId,ProductVersion) VALUES(N'20261030090000_ReconcileAnnualLeaveCatalogue',N'8.0.27');
COMMIT TRANSACTION;
GO

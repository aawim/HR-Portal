SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.LeavePolicies', 'RequireWorkHandover') IS NULL
    ALTER TABLE dbo.LeavePolicies ADD RequireWorkHandover bit NOT NULL CONSTRAINT DF_LeavePolicies_RequireWorkHandover DEFAULT (0);
IF COL_LENGTH('dbo.LeavePolicies', 'WorkHandoverAfterDays') IS NULL
    ALTER TABLE dbo.LeavePolicies ADD WorkHandoverAfterDays int NOT NULL CONSTRAINT DF_LeavePolicies_WorkHandoverAfterDays DEFAULT (2);
COMMIT;

-- Explicit independent consent from both staff. Existing source consent is never inferred.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.DC_DutyChangeRequests','SourceAcceptedByUserId') IS NULL
 ALTER TABLE dbo.DC_DutyChangeRequests ADD SourceAcceptedByUserId int NULL, SourceAcceptedAtUtc datetime2 NULL;
COMMIT TRANSACTION;

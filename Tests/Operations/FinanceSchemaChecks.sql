-- SQL Server integration check. Test rows always roll back; no employee records are changed.
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @Org int,@Cycle int,@Job int,@Person int,@Batch bigint,@Entry bigint;
    SELECT TOP(1) @Org=p.OrganisationID,@Cycle=c.RequestID,@Job=j.JobID,@Person=j.IndividualID
    FROM dbo.PayrollCycles c JOIN dbo.OrganisationPayrollPeriods p ON p.OrganisationPayrollPeriodID=c.OrganisationPayrollPeriodID
    JOIN dbo.Jobs j ON j.OrganisationID=p.OrganisationID;
    IF @Cycle IS NULL THROW 51001,'A payroll cycle and employment record are required for this rollback-only integration check.',1;
    INSERT dbo.AD_PayrollBatches(OrganisationId,Month,PayrollCycleRequestId,ClientBatchId,Reference,PostedByUserId,PostedAtUtc,SelectionJson,Fingerprint)
    VALUES(@Org,'20000101',@Cycle,NEWID(),'__CODEX_FINANCE_LOCK_TEST__',0,SYSUTCDATETIME(),'[]',REPLICATE('0',64));
    SET @Batch=SCOPE_IDENTITY();
    INSERT dbo.AD_PayrollEntries(BatchId,OrganisationId,JobId,IndividualId,Month,SourceKey,Kind,Description,CurrencyTypeId,Amount)
    VALUES(@Batch,@Org,@Job,@Person,'20000101',CONVERT(varchar(36),NEWID()),'Allowance','Rollback-only lock test',1,100);
    SET @Entry=SCOPE_IDENTITY();
    UPDATE dbo.AD_PayrollEntries SET Amount=101 WHERE Id=@Entry;
    ROLLBACK TRANSACTION;
    THROW 51001,'Payroll lock failed: the update was permitted.',1;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    IF ERROR_NUMBER()<>51000 OR ERROR_MESSAGE() NOT LIKE '%Financial history is immutable%' THROW;
    SELECT 'PASS: SQL Server rejected a posted payroll update.' AS Result;
END CATCH;
IF EXISTS(SELECT 1 FROM dbo.AD_PayrollBatches WHERE Reference='__CODEX_FINANCE_LOCK_TEST__')
    THROW 51001,'Rollback-only integration check left a test record.',1;
SELECT 'PASS: No test payroll records remain.' AS Result;

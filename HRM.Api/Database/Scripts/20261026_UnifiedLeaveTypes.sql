-- Consolidates the two runtime catalogues without replacing existing leave identities.
-- Run through EF migration (one transaction); GO separates SQL Server compilation batches.
IF OBJECT_ID(N'dbo.LeaveTypes',N'U') IS NULL THROW 51000, 'Existing LeaveTypes table is required for consolidation.', 1;
IF OBJECT_ID(N'dbo.LT_LeaveTypes',N'U') IS NOT NULL THROW 51000, 'Canonical leave table already exists; verify migration history before proceeding.', 1;
CREATE TABLE dbo.LT_TypeMigrationAudits (
 Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_LT_TypeMigrationAudits PRIMARY KEY,
 SourceEntity nvarchar(80) NOT NULL, SourceId int NOT NULL, LeaveTypeId int NULL,
 SnapshotJson nvarchar(max) NOT NULL, MigratedAtUtc datetime2 NOT NULL
);
CREATE TABLE #DefinitionMap (DefinitionID int NOT NULL PRIMARY KEY, LeaveTypeID int NULL);
IF EXISTS(SELECT m.LeaveDefinitionID FROM dbo.LeaveTypeMappings m WHERE m.IsActive=1 GROUP BY m.LeaveDefinitionID HAVING COUNT(DISTINCT m.LegacyLeaveTypeID)>1)
 THROW 51000, 'A definition maps to multiple existing types; explicit reconciliation is required. No changes committed.', 1;
INSERT #DefinitionMap(DefinitionID,LeaveTypeID)
SELECT d.LeaveDefinitionID, COALESCE((SELECT MIN(m.LegacyLeaveTypeID) FROM dbo.LeaveTypeMappings m WHERE m.LeaveDefinitionID=d.LeaveDefinitionID AND m.IsActive=1),
 (SELECT MIN(t.LeaveTypeID) FROM dbo.LeaveTypes t WHERE t.MigratedLeaveDefinitionID=d.LeaveDefinitionID)) FROM dbo.LeaveDefinitions d;
IF EXISTS(SELECT LeaveTypeID FROM #DefinitionMap WHERE LeaveTypeID IS NOT NULL GROUP BY LeaveTypeID HAVING COUNT(*)>1)
 THROW 51000, 'Multiple definitions map to one existing type; reconcile before consolidation. No changes committed.', 1;
IF EXISTS(SELECT 1 FROM #DefinitionMap m LEFT JOIN dbo.LeaveTypes t ON t.LeaveTypeID=m.LeaveTypeID WHERE m.LeaveTypeID IS NOT NULL AND t.LeaveTypeID IS NULL)
 THROW 51000, 'A mapped leave type is missing. No changes committed.', 1;
-- Keep exact pre-migration balance and leave rows to verify preservation below.
SELECT JobLeaveTypeID,JobID,LeaveTypeID,LeaveDefinitionID,RemainingDays,IsValid,IsLeaveInfoUpdated INTO #BalancesBefore FROM dbo.JobLeaveTypes;
SELECT LeaveID,LeaveTypeID,Duration,LeaveStateID,FromDate,ToDate INTO #LeavesBefore FROM dbo.Leaves;
INSERT dbo.LT_TypeMigrationAudits(SourceEntity,SourceId,LeaveTypeId,SnapshotJson,MigratedAtUtc)
SELECT 'ExistingLeaveType',t.LeaveTypeID,t.LeaveTypeID,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),SYSUTCDATETIME() FROM dbo.LeaveTypes t;
INSERT dbo.LT_TypeMigrationAudits(SourceEntity,SourceId,LeaveTypeId,SnapshotJson,MigratedAtUtc)
SELECT 'LeaveTypeMapping',m.LeaveTypeMappingID,m.LegacyLeaveTypeID,(SELECT m.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),SYSUTCDATETIME() FROM dbo.LeaveTypeMappings m;
INSERT dbo.LT_TypeMigrationAudits(SourceEntity,SourceId,LeaveTypeId,SnapshotJson,MigratedAtUtc)
SELECT 'LeaveFrameworkConfiguration',f.LeaveFrameworkConfigurationID,NULL,(SELECT f.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),SYSUTCDATETIME() FROM dbo.LeaveFrameworkConfigurations f;
INSERT dbo.LT_TypeMigrationAudits(SourceEntity,SourceId,LeaveTypeId,SnapshotJson,MigratedAtUtc)
SELECT 'JobLeaveType',j.JobLeaveTypeID,j.LeaveTypeID,(SELECT j.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),SYSUTCDATETIME() FROM dbo.JobLeaveTypes j WHERE j.LeaveDefinitionID IS NOT NULL;
INSERT dbo.LT_TypeMigrationAudits(SourceEntity,SourceId,LeaveTypeId,SnapshotJson,MigratedAtUtc)
SELECT 'LeavePolicy',p.LeavePolicyID,NULL,(SELECT p.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),SYSUTCDATETIME() FROM dbo.LeavePolicies p;
EXEC sys.sp_rename N'dbo.LeaveTypes', N'LT_LeaveTypes';
ALTER TABLE dbo.LT_LeaveTypes ALTER COLUMN RequestTypeID int NULL;
ALTER TABLE dbo.LT_LeaveTypes ALTER COLUMN Duration int NULL;
ALTER TABLE dbo.LT_LeaveTypes ALTER COLUMN PayPercentage float NULL;
ALTER TABLE dbo.LT_LeaveTypes ADD Code varchar(50) NULL, Description nvarchar(1000) NULL,
 IsActive bit NOT NULL CONSTRAINT DF_LT_LeaveTypes_IsActive DEFAULT(1),
 CreatedDate datetime2 NOT NULL CONSTRAINT DF_LT_LeaveTypes_Created DEFAULT(SYSUTCDATETIME()), UpdatedDate datetime2 NULL;
GO
UPDATE dbo.LT_LeaveTypes SET Code=CONCAT('LT-',LeaveTypeID);
-- Existing names, ownership, historical IDs and operational defaults are authoritative.
UPDATE t SET Code=d.Code,Description=d.Description,CreatedDate=d.CreatedDate,UpdatedDate=d.UpdatedDate,
 IsGlobal=CASE WHEN t.IsGlobal=1 OR d.IsGlobal=1 THEN 1 ELSE 0 END,
 IsSystemType=CASE WHEN t.IsSystemType=1 OR d.IsSystemType=1 THEN 1 ELSE 0 END
FROM dbo.LT_LeaveTypes t JOIN #DefinitionMap m ON m.LeaveTypeID=t.LeaveTypeID JOIN dbo.LeaveDefinitions d ON d.LeaveDefinitionID=m.DefinitionID;
-- Preserve unmapped definitions as independent canonical records, never match/merge balances by name.
DECLARE @audit int;
INSERT dbo.OperationLogs(OperationLogActionID,CreatedByContextID,CreatedDate,UpdatedDate,CreatedByIP,UpdatedByIP,Remarks,UpdatedContextID)
VALUES(18,1,SYSUTCDATETIME(),SYSUTCDATETIME(),'','','Consolidated leave types into LT_LeaveTypes; existing names, IDs, balances and request history preserved. Source snapshots retained in LT_TypeMigrationAudits.',1);
SET @audit=CONVERT(int,SCOPE_IDENTITY());
DECLARE @definition int,@newType int;
DECLARE definition_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT DefinitionID FROM #DefinitionMap WHERE LeaveTypeID IS NULL;
OPEN definition_cursor; FETCH NEXT FROM definition_cursor INTO @definition;
WHILE @@FETCH_STATUS=0
BEGIN
 INSERT dbo.LT_LeaveTypes(Name,NameDhivehi,Duration,IncludeHolidays,IncludePay,IsPublic,IsGlobal,IsLocationRequired,ServiceDurationMonths,OrganisationID,RequestTypeID,IsSystemType,IsRenewed,OperationLogID,IsStaffWideAvailable,PayPercentage,StartInMonth,RepeatedEveryInMonth,Code,Description,IsActive,CreatedDate,UpdatedDate)
 SELECT d.Name,d.NameDhivehi,NULL,0,0,0,d.IsGlobal,0,0,
 COALESCE(d.OwnerOrganisationID,(SELECT MIN(OrganisationID) FROM dbo.LeavePolicies p WHERE p.LeaveDefinitionID=d.LeaveDefinitionID),(SELECT MIN(BusinessEntityID) FROM dbo.Organisations)),
 NULL,d.IsSystemType,0,COALESCE(d.OperationLogID,@audit),0,NULL,0,0,d.Code,d.Description,d.IsActive,d.CreatedDate,d.UpdatedDate
 FROM dbo.LeaveDefinitions d WHERE d.LeaveDefinitionID=@definition;
 SET @newType=CONVERT(int,SCOPE_IDENTITY()); UPDATE #DefinitionMap SET LeaveTypeID=@newType WHERE DefinitionID=@definition;
 FETCH NEXT FROM definition_cursor INTO @definition;
END;
CLOSE definition_cursor; DEALLOCATE definition_cursor;
IF EXISTS(SELECT 1 FROM dbo.LT_LeaveTypes WHERE Code IS NULL OR LTRIM(RTRIM(Code))='') THROW 51000, 'A leave type code is missing. No changes committed.',1;
IF EXISTS(SELECT Code FROM dbo.LT_LeaveTypes GROUP BY Code HAVING COUNT(*)>1) THROW 51000, 'Conflicting leave type codes require reconciliation. No changes committed.',1;
INSERT dbo.LT_TypeMigrationAudits(SourceEntity,SourceId,LeaveTypeId,SnapshotJson,MigratedAtUtc)
SELECT 'LeaveDefinition',d.LeaveDefinitionID,m.LeaveTypeID,(SELECT d.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),SYSUTCDATETIME() FROM dbo.LeaveDefinitions d JOIN #DefinitionMap m ON m.DefinitionID=d.LeaveDefinitionID;
-- Drop only the replaced new-definition FKs; existing type FKs followed the table rename.
DECLARE @fkSql nvarchar(max)=N'';
SELECT @fkSql+=N'ALTER TABLE '+QUOTENAME(OBJECT_SCHEMA_NAME(f.parent_object_id))+N'.'+QUOTENAME(OBJECT_NAME(f.parent_object_id))+N' DROP CONSTRAINT '+QUOTENAME(f.name)+N';'
FROM sys.foreign_keys f WHERE f.referenced_object_id=OBJECT_ID(N'dbo.LeaveDefinitions');
EXEC sys.sp_executesql @fkSql;
DECLARE @checkSql nvarchar(max)=N'';
SELECT @checkSql+=N'ALTER TABLE dbo.JobLeaveTypes DROP CONSTRAINT '+QUOTENAME(name)+N';' FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.JobLeaveTypes') AND definition LIKE N'%LeaveDefinitionID%';
EXEC sys.sp_executesql @checkSql;
UPDATE j SET LeaveTypeID=m.LeaveTypeID FROM dbo.JobLeaveTypes j JOIN #DefinitionMap m ON m.DefinitionID=j.LeaveDefinitionID WHERE j.LeaveTypeID IS NULL;
IF EXISTS(SELECT 1 FROM dbo.JobLeaveTypes WHERE LeaveTypeID IS NULL) THROW 51000, 'A balance has no canonical type. No changes committed.',1;
UPDATE p SET LeaveDefinitionID=m.LeaveTypeID FROM dbo.LeavePolicies p JOIN #DefinitionMap m ON m.DefinitionID=p.LeaveDefinitionID;
DROP TABLE dbo.LeaveTypeMappings;
DROP TABLE dbo.LeaveFrameworkConfigurations;
DROP TABLE dbo.LeaveDefinitions;
DECLARE @indexSql nvarchar(max)=N'';
SELECT @indexSql+=N'DROP INDEX '+QUOTENAME(i.name)+N' ON dbo.JobLeaveTypes;' FROM sys.indexes i WHERE i.object_id=OBJECT_ID(N'dbo.JobLeaveTypes') AND EXISTS(SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND c.name=N'LeaveDefinitionID');
EXEC sys.sp_executesql @indexSql;
ALTER TABLE dbo.JobLeaveTypes DROP COLUMN LeaveDefinitionID;
ALTER TABLE dbo.LT_LeaveTypes DROP COLUMN MigratedLeaveDefinitionID;
ALTER TABLE dbo.JobLeaveTypes ADD CONSTRAINT CK_JobLeaveTypes_LeaveType CHECK(LeaveTypeID IS NOT NULL);
EXEC sys.sp_rename N'dbo.LeavePolicies.LeaveDefinitionID',N'LeaveTypeID',N'COLUMN';
GO
ALTER TABLE dbo.LT_LeaveTypes ALTER COLUMN Code varchar(50) NOT NULL;
EXEC sys.sp_rename N'dbo.PK_LeaveTypes',N'PK_LT_LeaveTypes',N'OBJECT';
IF EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LT_LeaveTypes') AND name=N'IX_LeaveTypes_RequestTypeID') EXEC sys.sp_rename N'dbo.LT_LeaveTypes.IX_LeaveTypes_RequestTypeID',N'IX_LT_LeaveTypes_RequestTypeID',N'INDEX';
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LT_LeaveTypes') AND name=N'IX_LT_LeaveTypes_RequestTypeID') CREATE INDEX IX_LT_LeaveTypes_RequestTypeID ON dbo.LT_LeaveTypes(RequestTypeID);
IF EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LT_LeaveTypes') AND name=N'IX_LeaveTypes_OrganisationID') EXEC sys.sp_rename N'dbo.LT_LeaveTypes.IX_LeaveTypes_OrganisationID',N'IX_LT_LeaveTypes_OrganisationID',N'INDEX';
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LT_LeaveTypes') AND name=N'IX_LT_LeaveTypes_OrganisationID') CREATE INDEX IX_LT_LeaveTypes_OrganisationID ON dbo.LT_LeaveTypes(OrganisationID);
IF EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LT_LeaveTypes') AND name=N'IX_LeaveTypes_OperationLogID') EXEC sys.sp_rename N'dbo.LT_LeaveTypes.IX_LeaveTypes_OperationLogID',N'IX_LT_LeaveTypes_OperationLogID',N'INDEX';
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LT_LeaveTypes') AND name=N'IX_LT_LeaveTypes_OperationLogID') CREATE INDEX IX_LT_LeaveTypes_OperationLogID ON dbo.LT_LeaveTypes(OperationLogID);
IF EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LeavePolicies') AND name=N'IX_LeavePolicies_LeaveDefinitionID') EXEC sys.sp_rename N'dbo.LeavePolicies.IX_LeavePolicies_LeaveDefinitionID',N'IX_LeavePolicies_LeaveTypeID',N'INDEX';
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LeavePolicies') AND name=N'IX_LeavePolicies_LeaveTypeID') CREATE INDEX IX_LeavePolicies_LeaveTypeID ON dbo.LeavePolicies(LeaveTypeID);
DECLARE @accrualFk nvarchar(300);
SELECT @accrualFk=N'dbo.'+QUOTENAME(name) FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.LA_JobTypeLeaveAccrualRules') AND referenced_object_id=OBJECT_ID(N'dbo.LT_LeaveTypes');
IF @accrualFk IS NOT NULL EXEC sys.sp_rename @accrualFk,N'FK_LA_JobTypeLeaveAccrualRules_LT_LeaveTypes_LeaveTypeId',N'OBJECT';
CREATE UNIQUE INDEX IX_LT_LeaveTypes_Code ON dbo.LT_LeaveTypes(Code);
ALTER TABLE dbo.LeavePolicies ADD CONSTRAINT FK_LeavePolicies_LT_LeaveTypes_LeaveTypeID FOREIGN KEY(LeaveTypeID) REFERENCES dbo.LT_LeaveTypes(LeaveTypeID);
IF EXISTS(SELECT 1 FROM #BalancesBefore b FULL JOIN dbo.JobLeaveTypes j ON j.JobLeaveTypeID=b.JobLeaveTypeID
 WHERE j.JobLeaveTypeID IS NULL OR b.JobLeaveTypeID IS NULL OR j.JobID<>b.JobID OR j.IsValid<>b.IsValid OR j.IsLeaveInfoUpdated<>b.IsLeaveInfoUpdated
 OR (j.RemainingDays<>b.RemainingDays OR (j.RemainingDays IS NULL AND b.RemainingDays IS NOT NULL) OR (j.RemainingDays IS NOT NULL AND b.RemainingDays IS NULL)) OR (b.LeaveTypeID IS NOT NULL AND j.LeaveTypeID<>b.LeaveTypeID))
 THROW 51000, 'Balance preservation verification failed; transaction must roll back.',1;
IF EXISTS(SELECT LeaveID,LeaveTypeID,Duration,LeaveStateID,FromDate,ToDate FROM #LeavesBefore EXCEPT SELECT LeaveID,LeaveTypeID,Duration,LeaveStateID,FromDate,ToDate FROM dbo.Leaves)
 OR (SELECT COUNT(*) FROM #LeavesBefore)<>(SELECT COUNT(*) FROM dbo.Leaves)
 THROW 51000, 'Historical leave preservation verification failed; transaction must roll back.',1;
IF EXISTS(SELECT 1 FROM dbo.JobLeaveTypes j LEFT JOIN dbo.LT_LeaveTypes t ON t.LeaveTypeID=j.LeaveTypeID WHERE t.LeaveTypeID IS NULL)
 THROW 51000, 'An entitlement references a missing canonical type; transaction must roll back.',1;
INSERT dbo.LeaveTypeReasonTypes(LeaveTypeID,ReasonTypeName) SELECT t.LeaveTypeID,N'General' FROM dbo.LT_LeaveTypes t WHERE NOT EXISTS(SELECT 1 FROM dbo.LeaveTypeReasonTypes r WHERE r.LeaveTypeID=t.LeaveTypeID);
DROP TABLE #BalancesBefore; DROP TABLE #LeavesBefore; DROP TABLE #DefinitionMap;
